using System.Security.Claims;
using MemoLens.Models;
using MemoLens.Models.Api;
using MemoLens.Models.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MemoLens.Controllers.Api.V1;

[ApiController]
[Route("api/v1/account")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AccountApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountApiController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserSummaryResponse>>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Bearer token không hợp lệ."
            });
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Bearer token không hợp lệ."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new ApiResponse<UserSummaryResponse>
        {
            Success = true,
            Message = "Lấy thông tin tài khoản thành công.",
            Data = new UserSummaryResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Roles = roles.ToArray()
            }
        });
    }
}
