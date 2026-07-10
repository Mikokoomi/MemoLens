using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được dài quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;
}
