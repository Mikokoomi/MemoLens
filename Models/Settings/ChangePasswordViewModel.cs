using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Settings;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui long nhap mat khau hien tai.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mat khau hien tai")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau moi.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mat khau moi can it nhat 8 ky tu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mat khau moi")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long xac nhan mat khau moi.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mat khau xac nhan khong khop.")]
    [Display(Name = "Xac nhan mat khau moi")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
