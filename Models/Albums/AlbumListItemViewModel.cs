namespace MemoLens.Models.Albums;

public class AlbumListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CoverImagePath { get; set; }

    public int MemoryCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
