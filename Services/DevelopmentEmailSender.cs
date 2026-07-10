using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

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
        var actionLink = FindFirstLink(htmlMessage);
        var linkLabel = subject.Contains("mật khẩu", StringComparison.OrdinalIgnoreCase)
            ? "Reset password link"
            : "Confirmation link";
        var message = $"""
            [MemoLens Development Email]
            Recipient: {email}
            Subject: {subject}
            {linkLabel}: {actionLink ?? "Không tìm thấy link trong nội dung email."}
            """;

        _logger.LogInformation("{Message}", message);
        Debug.WriteLine(message);

        return Task.CompletedTask;
    }

    private static string? FindFirstLink(string htmlMessage)
    {
        var decodedMessage = WebUtility.HtmlDecode(htmlMessage);
        var match = Regex.Match(decodedMessage, @"https?://[^\s<\""']+");

        return match.Success ? match.Value : null;
    }
}
