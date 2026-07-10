# MemoLens - Thiết lập Email và Xác nhận Email

## 1. Mục đích

MemoLens yêu cầu người dùng xác thực email trước khi đăng nhập. Quy tắc này áp dụng cho cả MVC web app và API mobile trong tương lai, giúp hạn chế việc tạo tài khoản bằng email không thuộc quyền sở hữu của người đăng ký.

Phase 15A làm rõ hai chế độ email:

- `Development`: ghi confirmation link vào log local để dễ kiểm thử.
- `Smtp`: nền tảng gửi email thật qua SMTP khi production đã có cấu hình bảo mật đầy đủ.

Không có mật khẩu, access token hoặc refresh token nào được ghi vào log email.

## 2. Hành vi trong Development

Khi một user đăng ký bằng MVC hoặc `POST /api/v1/auth/register` trong môi trường `Development`:

1. ASP.NET Core Identity tạo user với `EmailConfirmed = false`.
2. MemoLens tạo email confirmation token và confirmation link.
3. `DevelopmentEmailSender` ghi thông tin dễ tìm vào terminal/console hoặc debug output.
4. User mở confirmation link để xác nhận email.
5. Sau khi xác nhận, user mới có thể login bằng MVC hoặc API.

Log có prefix:

```text
[MemoLens Development Email]
Recipient: user@example.com
Subject: Xác thực email MemoLens
Confirmation link: http://localhost:xxxx/Account/ConfirmEmail?userId=...&token=...
```

Link chỉ dùng cho local Development. Không chia sẻ log, không chụp log có token để công khai, và không commit log vào Git.

Trang MVC sau khi đăng ký cũng nhắc rõ: `Môi trường Development: kiểm tra terminal/console để lấy link xác nhận email.` Link thô không được hiển thị trên trang.

## 3. Cách kiểm thử confirmation link local

1. Chạy MemoLens bằng `dotnet run`.
2. Mở `/Account/Register` và tạo một email mới.
3. Tìm block có prefix `[MemoLens Development Email]` trong terminal/console.
4. Mở `Confirmation link` trong trình duyệt.
5. Quay lại `/Account/Login` và đăng nhập bằng email, mật khẩu vừa tạo.

Với API, gửi `POST /api/v1/auth/register`. Response thành công không trả confirmation token, confirmation link, access token hoặc refresh token. Link chỉ xuất hiện trong Development log local.

## 4. Đặt lại mật khẩu MVC trong Development

MVC web app có luồng forgot/reset password tại `/Account/ForgotPassword`:

1. User nhập email trên trang Quên mật khẩu.
2. MemoLens luôn chuyển tới cùng một trang xác nhận chung, dù email không tồn tại, chưa xác thực hay đã xác thực.
3. Chỉ khi user tồn tại và email đã xác thực, Identity mới tạo password reset token và email sender mới nhận reset link.
4. Trong Development, tìm block `[MemoLens Development Email]` có subject `Đặt lại mật khẩu MemoLens` và dòng `Reset password link:`.
5. Mở link, nhập mật khẩu mới và xác nhận mật khẩu.
6. MemoLens dùng `UserManager.ResetPasswordAsync` để kiểm tra token và đổi mật khẩu.

Reset link không được hiển thị trên MVC UI. Password mới không được ghi vào email, log hoặc URL.

Sau khi reset thành công:

- User không được auto-login.
- Mật khẩu cũ không còn dùng được.
- Các API refresh token chưa revoke của user được revoke.
- JWT access token đã cấp trước đó không bị lưu hoặc thu hồi trong database; token này tự hết hạn trong tối đa khoảng 15 phút.

API forgot/reset password chưa được triển khai trong Phase 15B.

## 5. Cấu hình Email

Model cấu hình là `EmailOptions`, nằm tại `Models/Email/EmailOptions.cs`.

| Khóa cấu hình | Mục đích |
| --- | --- |
| `Email:Mode` | `DevelopmentLog` hoặc `Smtp`. |
| `Email:FromName` | Tên người gửi hiển thị, ví dụ `MemoLens`. |
| `Email:FromEmail` | Địa chỉ email người gửi. |
| `Email:SmtpHost` | SMTP host của email provider. |
| `Email:SmtpPort` | SMTP port, thường là `587`. |
| `Email:SmtpUseSsl` | Bật TLS/SSL khi provider yêu cầu. |
| `Email:SmtpUsername` | Tài khoản SMTP. |
| `Email:SmtpPassword` | Mật khẩu SMTP hoặc app password. |

`appsettings.Development.json` chỉ có cấu hình local không nhạy cảm:

```json
"Email": {
  "Mode": "DevelopmentLog",
  "FromName": "MemoLens",
  "FromEmail": "noreply@memolens.local"
}
```

Không đặt SMTP username, SMTP password, API key hoặc secret thật trong `appsettings.json`, `appsettings.Development.json` hay Git.

## 6. Chuẩn bị Production SMTP

Khi sẵn sàng gửi email thật, đặt `Email__Mode=Smtp` và cung cấp các giá trị còn lại qua User Secrets, environment variables hoặc server configuration. Ví dụ tên biến môi trường:

```text
Email__Mode=Smtp
Email__FromName=MemoLens
Email__FromEmail=noreply@example.com
Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__SmtpUseSsl=true
Email__SmtpUsername=...
Email__SmtpPassword=...
```

`SmtpEmailSender` dùng thư viện có sẵn của .NET, gửi nội dung HTML và không ghi recipient, email body, token hoặc SMTP password ra log.

Ngoài Development, MemoLens chỉ dùng SMTP khi `Email:Mode` là `Smtp`. Nếu SMTP chưa được cấu hình, `UnconfiguredEmailSender` dừng việc gửi với lỗi cấu hình thay vì ghi confirmation token vào production log.

Trước private beta, cần kiểm tra SMTP với email provider thật, HTTPS, domain gửi mail, SPF/DKIM/DMARC theo provider, monitoring và chính sách retry phù hợp.

## 7. Quy tắc bảo mật

- Không tắt `RequireConfirmedEmail`.
- Không tự động đặt `EmailConfirmed = true` cho user mới.
- Không trả confirmation token hoặc link trong MVC UI hay API register response.
- Không log password, access token hoặc refresh token.
- Confirmation link chỉ được log ở Development để kiểm thử local.
- Không dùng `DevelopmentEmailSender` cho production.
- Luôn lưu SMTP secret ở User Secrets, environment variables hoặc secret store của server.
- Forgot password luôn dùng response chung để tránh tiết lộ email có tồn tại hay không.
- Reset password dùng token chuẩn của ASP.NET Core Identity và không auto-login.

## 8. Chưa được triển khai

- API confirm email riêng cho mobile.
- API resend confirmation email.
- API forgot password.
- API reset password.
- Rate limiting cho register/login/resend.
- Production email provider đã được cấu hình bằng secret thật.
- Retry queue, delivery tracking hoặc email template provider.
