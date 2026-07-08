using System.Diagnostics;

namespace MemoLens.Services;

public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = $"""
            Development email sender
            To: {email}
            Subject: {subject}
            Message: {htmlMessage}
            """;

        _logger.LogInformation("{Message}", message);
        Debug.WriteLine(message);

        return Task.CompletedTask;
    }
}
