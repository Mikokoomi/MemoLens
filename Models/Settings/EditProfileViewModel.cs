using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Settings;

public class EditProfileViewModel
{
    [MaxLength(80, ErrorMessage = "Tên hiển thị tối đa 80 ký tự.")]
    [Display(Name = "Tên hiển thị")]
    public string? DisplayName { get; set; }
}
