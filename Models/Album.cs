using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models;

public class Album
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    // A null value keeps the automatic cover-selection mode.
    public int? CoverImageId { get; set; }

    public MemoryImage? CoverImage { get; set; }

    public ICollection<AlbumMemory> AlbumMemories { get; set; } = new List<AlbumMemory>();
}
