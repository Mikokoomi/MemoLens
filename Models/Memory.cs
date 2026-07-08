using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models;

public class Memory
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Story { get; set; }

    [Required]
    [MaxLength(50)]
    public string Feeling { get; set; } = string.Empty;

    public DateTime MemoryDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<MemoryImage> Images { get; set; } = new List<MemoryImage>();

    public ICollection<MemoryTag> MemoryTags { get; set; } = new List<MemoryTag>();

    public ICollection<AlbumMemory> AlbumMemories { get; set; } = new List<AlbumMemory>();
}
