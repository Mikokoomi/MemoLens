namespace MemoLens.Models.Albums;

public class AlbumAddMemoriesViewModel
{
    public int AlbumId { get; set; }

    public string AlbumTitle { get; set; } = string.Empty;

    public List<int> SelectedMemoryIds { get; set; } = [];

    public IReadOnlyList<AlbumMemoryItemViewModel> AvailableMemories { get; set; } = [];
}
