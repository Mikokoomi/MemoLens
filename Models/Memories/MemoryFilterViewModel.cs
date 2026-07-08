namespace MemoLens.Models.Memories;

public class MemoryFilterViewModel
{
    public string? Search { get; set; }

    public string? Feeling { get; set; }

    public int? TagId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int? Month { get; set; }

    public int? Year { get; set; }

    public string SortOrder { get; set; } = "newest";

    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Feeling) ||
        TagId.HasValue ||
        FromDate.HasValue ||
        ToDate.HasValue ||
        Month.HasValue ||
        Year.HasValue ||
        !string.Equals(SortOrder, "newest", StringComparison.OrdinalIgnoreCase);
}
