namespace MemoLens.Models.Memories;

public class MemoryImageViewModel
{
    public int Id { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    public string? Caption { get; set; }
}
