namespace MemoLens.Models.Api.Albums;

public sealed class AlbumListItemResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int MemoryCount { get; init; }

    public int? CoverImageId { get; init; }

    public int? ManualCoverImageId { get; init; }

    public int? EffectiveCoverImageId { get; init; }

    public string? CoverImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class AlbumDetailsResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int MemoryCount { get; init; }

    public int? CoverImageId { get; init; }

    public int? ManualCoverImageId { get; init; }

    public int? EffectiveCoverImageId { get; init; }

    public string? CoverImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public PagedResponse<AlbumMemorySummaryResponse> Memories { get; init; } = new();
}

public sealed class AlbumMemorySummaryResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? ShortStoryPreview { get; init; }

    public string Feeling { get; init; } = string.Empty;

    public DateTime MemoryDate { get; init; }

    public string? Location { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public int ImageCount { get; init; }

    public int? CoverImageId { get; init; }

    public int? ManualCoverImageId { get; init; }

    public int? EffectiveCoverImageId { get; init; }

    public string? CoverImageUrl { get; init; }

    public DateTime AddedAt { get; init; }
}
