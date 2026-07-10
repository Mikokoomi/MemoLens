using System.Text;
using System.Text.Encodings.Web;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Api.Auth;
using MemoLens.Models.Auth;
using MemoLens.Services;
using MemoLens.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string InvalidLoginMessage = "Email hoặc mật khẩu không đúng.";
    private const string InvalidRefreshTokenMessage = "Refresh token không hợp lệ hoặc đã hết hạn.";
    private const string InvalidConfirmationMessage = "Liên kết xác nhận email không hợp lệ hoặc đã hết hạn.";
    private const string ResendConfirmationMessage = "Nếu email hợp lệ và chưa được xác nhận, MemoLens sẽ gửi lại hướng dẫn xác nhận.";
    private const string ForgotPasswordMessage = "Nếu email tồn tại trong hệ thống, MemoLens sẽ gửi hướng dẫn đặt lại mật khẩu.";
    private const string InvalidPasswordResetMessage = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailSender emailSender,
        ILogger<AuthController> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = NormalizeOptionalValue(request.DisplayName, 100),
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(CreateIdentityValidationResponse(createResult));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, IdentitySeedData.UserRole);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
            {
                Success = false,
                Message = "Không thể hoàn tất đăng ký lúc này. Vui lòng thử lại sau."
            });
        }

        await SendConfirmationEmailAsync(user);

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Đăng ký thành công. Vui lòng xác nhận email trước khi đăng nhập."
        });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.Trim());
        var decodedToken = DecodeIdentityToken(request.Token);

        if (user is null ||
            decodedToken is null ||
            await _userManager.IsEmailConfirmedAsync(user))
        {
            return InvalidEmailConfirmation();
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            return InvalidEmailConfirmation();
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Xác nhận email thành công. Bạn có thể đăng nhập."
        });
    }

    [HttpPost("resend-confirmation-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ResendConfirmationEmail(
        ResendConfirmationEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            await SendConfirmationEmailAsync(user);
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = ResendConfirmationMessage
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            try
            {
                await SendPasswordResetEmailAsync(user);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Không thể gửi email đặt lại mật khẩu cho yêu cầu API.");
            }
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = ForgotPasswordMessage
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        var decodedToken = DecodeIdentityToken(request.Token);

        if (user is null ||
            decodedToken is null ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            return InvalidPasswordReset();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            HttpContext.RequestAborted);

        try
        {
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.Password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);

                if (result.Errors.Any(error => error.Code == "InvalidToken"))
                {
                    return InvalidPasswordReset();
                }

                return BadRequest(CreatePasswordResetValidationResponse(result));
            }

            var now = DateTime.UtcNow;
            await _dbContext.UserRefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedAt == null)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(token => token.RevokedAt, now),
                    HttpContext.RequestAborted);

            await transaction.CommitAsync(HttpContext.RequestAborted);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null)
        {
            return InvalidLogin();
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                return InvalidLogin();
            }

            var passwordIsValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordIsValid)
            {
                if (_userManager.SupportsUserLockout && _userManager.Options.Lockout.AllowedForNewUsers)
                {
                    await _userManager.AccessFailedAsync(user);
                }

                return InvalidLogin();
            }

            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Vui lòng xác nhận email trước khi đăng nhập."
            });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return InvalidLogin();
        }

        var now = DateTime.UtcNow;
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _dbContext.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays),
            DeviceName = NormalizeOptionalValue(request.DeviceName, 100),
            UserAgent = GetCurrentUserAgent(),
            IpAddress = GetCurrentIpAddress()
        });

        await _dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            Data = await CreateAuthResponseAsync(user, accessToken, refreshToken)
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request)
    {
        var presentedTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var now = DateTime.UtcNow;
        var storedToken = await _dbContext.UserRefreshTokens
            .AsNoTracking()
            .Include(token => token.User)
            .SingleOrDefaultAsync(
                token => token.TokenHash == presentedTokenHash,
                HttpContext.RequestAborted);

        if (storedToken is null ||
            storedToken.RevokedAt.HasValue ||
            storedToken.ExpiresAt <= now ||
            !_tokenService.ValidateRefreshTokenHash(request.RefreshToken, storedToken.TokenHash))
        {
            return InvalidRefreshToken();
        }

        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(storedToken.User);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            HttpContext.RequestAborted);

        try
        {
            var revokedRows = await _dbContext.UserRefreshTokens
                .Where(token =>
                    token.Id == storedToken.Id &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(token => token.RevokedAt, now)
                        .SetProperty(token => token.ReplacedByTokenHash, newRefreshTokenHash),
                    HttpContext.RequestAborted);

            if (revokedRows != 1)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                return InvalidRefreshToken();
            }

            _dbContext.UserRefreshTokens.Add(new UserRefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = newRefreshTokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays),
                DeviceName = storedToken.DeviceName,
                UserAgent = GetCurrentUserAgent(),
                IpAddress = GetCurrentIpAddress()
            });

            await _dbContext.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return Ok(new ApiResponse<AuthResponse>
        {
            Success = true,
            Message = "Làm mới phiên đăng nhập thành công.",
            Data = await CreateAuthResponseAsync(
                storedToken.User,
                newAccessToken,
                newRefreshToken)
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Logout(LogoutRequest request)
    {
        var presentedTokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var now = DateTime.UtcNow;
        var storedToken = await _dbContext.UserRefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.TokenHash == presentedTokenHash,
                HttpContext.RequestAborted);

        if (storedToken is not null &&
            storedToken.RevokedAt is null &&
            storedToken.ExpiresAt > now &&
            _tokenService.ValidateRefreshTokenHash(request.RefreshToken, storedToken.TokenHash))
        {
            await _dbContext.UserRefreshTokens
                .Where(token =>
                    token.Id == storedToken.Id &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > now)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(token => token.RevokedAt, now),
                    HttpContext.RequestAborted);
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = "Đăng xuất thành công."
        });
    }

    private UnauthorizedObjectResult InvalidLogin()
    {
        return Unauthorized(new ApiResponse
        {
            Success = false,
            Message = InvalidLoginMessage
        });
    }

    private UnauthorizedObjectResult InvalidRefreshToken()
    {
        return Unauthorized(new ApiResponse
        {
            Success = false,
            Message = InvalidRefreshTokenMessage
        });
    }

    private BadRequestObjectResult InvalidEmailConfirmation()
    {
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = InvalidConfirmationMessage
        });
    }

    private BadRequestObjectResult InvalidPasswordReset()
    {
        return BadRequest(new ApiResponse
        {
            Success = false,
            Message = InvalidPasswordResetMessage
        });
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user,
        string accessToken,
        string refreshToken)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = _jwtOptions.AccessTokenMinutes * 60,
            User = new UserSummaryResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Roles = roles.ToArray()
            }
        };
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = Url.Action(
            "ConfirmEmail",
            "Account",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(confirmationLink))
        {
            throw new InvalidOperationException("Could not create the email confirmation link.");
        }

        var safeLink = HtmlEncoder.Default.Encode(confirmationLink);
        var message = $"Vui lòng xác thực tài khoản MemoLens bằng cách mở link này: <a href=\"{safeLink}\">xác thực email</a><br />{safeLink}";

        await _emailSender.SendEmailAsync(
            user.Email ?? string.Empty,
            "Xác thực email MemoLens",
            message);
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetLink = Url.Action(
            "ResetPassword",
            "Account",
            new { email = user.Email, token = encodedToken },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(resetLink))
        {
            throw new InvalidOperationException("Không thể tạo link đặt lại mật khẩu.");
        }

        var safeLink = HtmlEncoder.Default.Encode(resetLink);
        var message = $"Đặt lại mật khẩu MemoLens bằng cách mở link này: <a href=\"{safeLink}\">đặt lại mật khẩu</a><br />{safeLink}";

        await _emailSender.SendEmailAsync(
            user.Email ?? string.Empty,
            "Đặt lại mật khẩu MemoLens",
            message);
    }

    private static string? DecodeIdentityToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private ApiValidationErrorResponse CreatePasswordResetValidationResponse(IdentityResult result)
    {
        var passwordErrors = result.Errors
            .Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            .Select(GetIdentityErrorMessage)
            .Distinct()
            .ToArray();

        return new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Mật khẩu mới chưa đáp ứng yêu cầu bảo mật.",
            Errors = new Dictionary<string, string[]>
            {
                ["password"] = passwordErrors.Length > 0
                    ? passwordErrors
                    : ["Mật khẩu mới chưa đáp ứng yêu cầu bảo mật."]
            }
        };
    }

    private ApiValidationErrorResponse CreateIdentityValidationResponse(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(GetIdentityErrorField)
            .ToDictionary(
                group => group.Key,
                group => group.Select(GetIdentityErrorMessage).Distinct().ToArray());

        return new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Không thể đăng ký với thông tin đã cung cấp.",
            Errors = errors
        };
    }

    private static string GetIdentityErrorField(IdentityError error)
    {
        if (error.Code.StartsWith("Password", StringComparison.Ordinal))
        {
            return "password";
        }

        if (error.Code is "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" or "InvalidUserName")
        {
            return "email";
        }

        return "request";
    }

    private static string GetIdentityErrorMessage(IdentityError error)
    {
        return error.Code switch
        {
            "PasswordTooShort" => "Mật khẩu chưa đủ độ dài yêu cầu.",
            "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
            "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ thường.",
            "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ hoa.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
            "DuplicateEmail" or "DuplicateUserName" => "Không thể đăng ký với email này.",
            "InvalidEmail" or "InvalidUserName" => "Email không hợp lệ.",
            _ => "Không thể tạo tài khoản với thông tin đã cung cấp."
        };
    }

    private string? GetCurrentUserAgent()
    {
        return NormalizeOptionalValue(Request.Headers["User-Agent"].ToString(), 500);
    }

    private string? GetCurrentIpAddress()
    {
        return NormalizeOptionalValue(HttpContext.Connection.RemoteIpAddress?.ToString(), 45);
    }

    private static string? NormalizeOptionalValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        return normalizedValue.Length <= maxLength
            ? normalizedValue
            : normalizedValue[..maxLength];
    }
}
