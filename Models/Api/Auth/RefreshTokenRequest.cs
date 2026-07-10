using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token là bắt buộc.")]
    [StringLength(512, ErrorMessage = "Refresh token không hợp lệ.")]
    public string RefreshToken { get; set; } = string.Empty;
}
