using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models;

public class Memory
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Story { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Mood { get; set; } = string.Empty;

    public DateTime MemoryDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MemoryImage> Images { get; set; } = new List<MemoryImage>();

    public ICollection<MemoryTag> MemoryTags { get; set; } = new List<MemoryTag>();

    public ICollection<AlbumMemory> AlbumMemories { get; set; } = new List<AlbumMemory>();
}
