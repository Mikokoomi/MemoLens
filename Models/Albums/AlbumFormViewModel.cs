using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Albums;

public class AlbumFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên bộ sưu tập.")]
    [MaxLength(100, ErrorMessage = "Tên bộ sưu tập tối đa 100 ký tự.")]
    [Display(Name = "Tên bộ sưu tập")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
