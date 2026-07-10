using System.Net;
using System.Net.Mail;
using MemoLens.Models.Email;
using Microsoft.Extensions.Options;

namespace MemoLens.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _emailOptions;

    public SmtpEmailSender(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.FromEmail) ||
            string.IsNullOrWhiteSpace(_emailOptions.SmtpHost))
        {
            throw new InvalidOperationException(
                "SMTP chưa được cấu hình đầy đủ. Hãy cung cấp Email:FromEmail và Email:SmtpHost bằng cấu hình bảo mật.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        message.To.Add(email);

        using var client = new SmtpClient(_emailOptions.SmtpHost, _emailOptions.SmtpPort)
        {
            EnableSsl = _emailOptions.SmtpUseSsl
        };

        if (!string.IsNullOrWhiteSpace(_emailOptions.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(
                _emailOptions.SmtpUsername,
                _emailOptions.SmtpPassword);
        }

        await client.SendMailAsync(message);
    }
}
