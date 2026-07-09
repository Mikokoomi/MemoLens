namespace MemoLens.Models.Settings;

public class SettingsIndexViewModel
{
    public string? DisplayName { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }
}
