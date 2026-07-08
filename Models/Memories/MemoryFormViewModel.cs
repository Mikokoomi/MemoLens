using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Memories;

public class MemoryFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    [Display(Name = "Story / Note")]
    public string? Story { get; set; }

    [Required]
    [MaxLength(50)]
    public string Feeling { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Memory date")]
    public DateTime? MemoryDate { get; set; } = DateTime.Today;

    [MaxLength(200)]
    public string? Location { get; set; }

    [Display(Name = "Tags")]
    public string? TagsText { get; set; }

    public IReadOnlyList<string> FeelingOptions => MemoryFeelingOptions.All;
}
