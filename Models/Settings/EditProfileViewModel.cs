using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Settings;

public class EditProfileViewModel
{
    [MaxLength(80, ErrorMessage = "Ten hien thi toi da 80 ky tu.")]
    [Display(Name = "Ten hien thi")]
    public string? DisplayName { get; set; }
}
