using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Api.Albums;

public sealed class AlbumListQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Search { get; init; }

    public string? Sort { get; init; }
}

public sealed class AlbumDetailsQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public class CreateAlbumRequest
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tiêu đề không được dài quá 100 ký tự.")]
    public string? Title { get; init; }

    [StringLength(500, ErrorMessage = "Mô tả không được dài quá 500 ký tự.")]
    public string? Description { get; init; }

    // Optional initial membership is persisted atomically with the new Album.
    public IReadOnlyList<int>? MemoryIds { get; init; }
}

public sealed class UpdateAlbumRequest : CreateAlbumRequest
{
}

public sealed class AddAlbumMemoriesRequest
{
    [Required(ErrorMessage = "Danh sách kỷ niệm là bắt buộc.")]
    public IReadOnlyList<int>? MemoryIds { get; init; }
}

public sealed class AddMemoryAlbumsRequest
{
    [Required(ErrorMessage = "Danh sách bộ sưu tập là bắt buộc.")]
    public IReadOnlyList<int>? AlbumIds { get; init; }
}
