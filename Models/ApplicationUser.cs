using Microsoft.AspNetCore.Identity;

namespace MemoLens.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Memory> Memories { get; set; } = new List<Memory>();

    public ICollection<Album> Albums { get; set; } = new List<Album>();
}
