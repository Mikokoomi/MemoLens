namespace MemoLens.Models.Trash;

public class DeletedMemoryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Story { get; set; }

    public string Feeling { get; set; } = string.Empty;

    public DateTime MemoryDate { get; set; }

    public string? Location { get; set; }

    public DateTime? DeletedAt { get; set; }
}
