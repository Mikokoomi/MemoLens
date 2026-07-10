using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được dài quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Mật khẩu không được dài quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Tên thiết bị không được dài quá 100 ký tự.")]
    public string? DeviceName { get; set; }
}
