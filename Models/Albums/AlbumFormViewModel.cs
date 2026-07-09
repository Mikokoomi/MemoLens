using System.ComponentModel.DataAnnotations;

namespace MemoLens.Models.Albums;

public class AlbumFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Ten bo suu tap")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Mo ta")]
    public string? Description { get; set; }
}
