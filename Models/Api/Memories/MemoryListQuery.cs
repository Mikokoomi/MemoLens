namespace MemoLens.Models.Api.Memories;

public class MemoryListQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Search { get; init; }

    public string? Feeling { get; init; }

    public string? Tag { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int? Year { get; init; }

    public int? Month { get; init; }

    public string? Sort { get; init; }
}
