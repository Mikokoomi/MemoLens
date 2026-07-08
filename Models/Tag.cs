using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MemoryTag> MemoryTags { get; set; } = new List<MemoryTag>();
}
