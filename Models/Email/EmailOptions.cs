namespace MemoLens.Models.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Mode { get; set; } = "DevelopmentLog";

    public string FromName { get; set; } = "MemoLens";

    public string FromEmail { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;
}
