namespace MemoLens.Models.Memories;

public class MemoryDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Story { get; set; }

    public string Feeling { get; set; } = string.Empty;

    public DateTime MemoryDate { get; set; }

    public string? Location { get; set; }

    public IReadOnlyList<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
