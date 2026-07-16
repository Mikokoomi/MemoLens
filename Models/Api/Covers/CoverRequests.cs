using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Covers;

public sealed class SetCoverImageRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Ảnh bìa không hợp lệ.")]
    public int ImageId { get; init; }
}
