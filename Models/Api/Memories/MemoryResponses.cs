namespace MemoLens.Models.Api.Memories;

public class MemoryListItemResponse
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

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public class MemoryDetailsResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Story { get; init; }

    public string Feeling { get; init; } = string.Empty;

    public DateTime MemoryDate { get; init; }

    public string? Location { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public IReadOnlyList<MemoryImageResponse> Images { get; init; } = [];

    public int? ManualCoverImageId { get; init; }

    public int? EffectiveCoverImageId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public class MemoryImageResponse
{
    public int Id { get; init; }

    public string OriginalFileName { get; init; } = string.Empty;

    public DateTime UploadedAt { get; init; }

    public string ContentUrl { get; init; } = string.Empty;
}
