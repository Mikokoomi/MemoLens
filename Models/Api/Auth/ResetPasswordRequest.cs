using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được dài quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token đặt lại mật khẩu là bắt buộc.")]
    [StringLength(2048, ErrorMessage = "Token đặt lại mật khẩu không hợp lệ.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có từ 8 đến 100 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
