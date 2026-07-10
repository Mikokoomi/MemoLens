using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MemoLens.Models.Memories;

public class MemoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [MaxLength(120, ErrorMessage = "Tiêu đề tối đa 120 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000, ErrorMessage = "Câu chuyện tối đa 4000 ký tự.")]
    [Display(Name = "Câu chuyện / ghi chú")]
    public string? Story { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn cảm xúc.")]
    [MaxLength(50, ErrorMessage = "Cảm xúc tối đa 50 ký tự.")]
    [Display(Name = "Cảm xúc")]
    public string Feeling { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày kỷ niệm.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày kỷ niệm")]
    public DateTime? MemoryDate { get; set; } = DateTime.Today;

    [MaxLength(200, ErrorMessage = "Địa điểm tối đa 200 ký tự.")]
    [Display(Name = "Địa điểm")]
    public string? Location { get; set; }

    [Display(Name = "Thẻ")]
    public string? TagsText { get; set; }

    [Display(Name = "Ảnh")]
    public List<IFormFile> NewImages { get; set; } = [];

    public IReadOnlyList<MemoryImageViewModel> ExistingImages { get; set; } = [];

    public IReadOnlyList<string> FeelingOptions => MemoryFeelingOptions.All;
}
