namespace MemoLens.Models.Albums;

public class AlbumMemoryItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Story { get; set; }

    public string Feeling { get; set; } = string.Empty;

    public DateTime MemoryDate { get; set; }

    public string? Location { get; set; }

    public string? CoverImagePath { get; set; }

    public DateTime? AddedAt { get; set; }
}
