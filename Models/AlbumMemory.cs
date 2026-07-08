namespace MemoLens.Models;

public class AlbumMemory
{
    public int AlbumId { get; set; }

    public Album Album { get; set; } = null!;

    public int MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
