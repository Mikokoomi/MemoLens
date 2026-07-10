namespace MemoLens.Services;

public class UnconfiguredEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        throw new InvalidOperationException(
            "Chưa cấu hình email provider cho môi trường này. Cấu hình Email:Mode=Smtp và SMTP secrets trước khi gửi email.");
    }
}
