namespace MemoLens.Models.Memories;

public class MemoryIndexViewModel
{
    public MemoryFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<MemoryListItemViewModel> Memories { get; set; } = [];

    public IReadOnlyList<MemoryTagFilterOption> AvailableTags { get; set; } = [];

    public IReadOnlyList<string> FeelingOptions { get; set; } = MemoryFeelingOptions.All;

    public IReadOnlyList<int> AvailableYears { get; set; } = [];

    public IReadOnlyList<string> ValidationMessages { get; set; } = [];

    public int TotalMemoryCount { get; set; }

    public bool HasMemories => TotalMemoryCount > 0;

    public bool HasResults => Memories.Count > 0;
}
