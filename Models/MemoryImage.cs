using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models;

public class MemoryImage
{
    public int Id { get; set; }

    public int MemoryId { get; set; }

    public Memory Memory { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? Caption { get; set; }
}
