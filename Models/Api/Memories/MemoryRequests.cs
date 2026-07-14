using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Memories;

public class CreateMemoryRequest
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Tiêu đề không được dài quá 120 ký tự.")]
    public string? Title { get; init; }

    [StringLength(4000, ErrorMessage = "Câu chuyện không được dài quá 4000 ký tự.")]
    public string? Story { get; init; }

    [Required(ErrorMessage = "Cảm xúc là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Cảm xúc không được dài quá 50 ký tự.")]
    public string? Feeling { get; init; }

    [Required(ErrorMessage = "Ngày kỷ niệm là bắt buộc.")]
    public DateTime? MemoryDate { get; init; }

    [StringLength(200, ErrorMessage = "Địa điểm không được dài quá 200 ký tự.")]
    public string? Location { get; init; }

    public IReadOnlyList<string?>? Tags { get; init; }
}

public sealed class UpdateMemoryRequest : CreateMemoryRequest
{
}
