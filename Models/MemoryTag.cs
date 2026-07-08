namespace MemoLens.Models;

public class MemoryTag
{
    public int MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    public int TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}
