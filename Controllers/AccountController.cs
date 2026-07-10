using System.Text;
using System.Text.Encodings.Web;
using MemoLens.Data;
using MemoLens.Models;
using MemoLens.Models.Account;
using MemoLens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MemoLens.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _dbContext;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _dbContext = dbContext;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, IdentitySeedData.UserRole);
        await SendConfirmationEmailAsync(user);

        return RedirectToAction(nameof(RegisterConfirmation), new { email = user.Email });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterConfirmation(string? email)
    {
        ViewData["Email"] = email;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return View(false);
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return View(false);
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return View(false);
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        return View(result.Succeeded);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng xác thực email trước khi đăng nhập.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Timeline", "Home");
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            await SendPasswordResetEmailAsync(user);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        var model = new ResetPasswordViewModel
        {
            Email = email?.Trim() ?? string.Empty,
            Token = token ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token))
        {
            ModelState.AddModelError(string.Empty, "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        var decodedToken = DecodeToken(model.Token);

        if (user is null || decodedToken is null)
        {
            AddInvalidResetLinkError();
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

        if (!result.Succeeded)
        {
            AddResetPasswordErrors(result);
            return View(model);
        }

        var now = DateTime.UtcNow;
        await _dbContext.UserRefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.RevokedAt, now));

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme);

        var safeLink = HtmlEncoder.Default.Encode(confirmationLink ?? string.Empty);
        var message = $"Vui lòng xác thực tài khoản MemoLens bằng cách mở link này: <a href=\"{safeLink}\">xác thực email</a><br />{safeLink}";

        await _emailSender.SendEmailAsync(user.Email ?? string.Empty, "Xác thực email MemoLens", message);
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetLink = Url.Action(
            nameof(ResetPassword),
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

    private static string? DecodeToken(string encodedToken)
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

    private void AddResetPasswordErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(ResetPasswordViewModel.Password), GetPasswordErrorMessage(error.Code));
                continue;
            }

            AddInvalidResetLinkError();
            return;
        }
    }

    private void AddInvalidResetLinkError()
    {
        ModelState.AddModelError(string.Empty, "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
    }

    private static string GetPasswordErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            "PasswordTooShort" => "Mật khẩu chưa đủ độ dài yêu cầu.",
            "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
            "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ thường.",
            "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ hoa.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt.",
            _ => "Mật khẩu mới chưa đáp ứng yêu cầu bảo mật."
        };
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
