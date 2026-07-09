namespace MemoLens.Models.Albums;

public class AlbumDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CoverImageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<AlbumMemoryItemViewModel> Memories { get; set; } = [];
}
