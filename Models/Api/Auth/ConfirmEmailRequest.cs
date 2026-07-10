using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Auth;

public class ConfirmEmailRequest
{
    [Required(ErrorMessage = "Mã người dùng là bắt buộc.")]
    [StringLength(450, ErrorMessage = "Mã người dùng không hợp lệ.")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token xác nhận email là bắt buộc.")]
    [StringLength(2048, ErrorMessage = "Token xác nhận email không hợp lệ.")]
    public string Token { get; set; } = string.Empty;
}
