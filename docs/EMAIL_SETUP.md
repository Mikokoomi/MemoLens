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

API forgot/reset password được triển khai ở Phase 15D và dùng cùng Identity token flow.

## 5. API xác nhận và gửi lại email xác nhận

Phase 15C bổ sung hai endpoint cho mobile client tương lai:

- `POST /api/v1/auth/confirm-email`
- `POST /api/v1/auth/resend-confirmation-email`

`confirm-email` nhận `userId` và token Base64Url giống token trong confirmation link hiện tại. Endpoint gọi `UserManager.ConfirmEmailAsync`, không auto-login, không tạo MVC cookie và không cấp access/refresh token. Sau khi xác nhận thành công, user vẫn phải gọi login.

`resend-confirmation-email` luôn trả cùng response chung:

```text
Nếu email hợp lệ và chưa được xác nhận, MemoLens sẽ gửi lại hướng dẫn xác nhận.
```

Chỉ tài khoản tồn tại nhưng chưa xác nhận mới thực sự tạo token và gửi email. Email không tồn tại hoặc đã xác nhận nhận cùng status/message để hạn chế user enumeration.

Trong Development, email gửi lại tiếp tục xuất hiện trong block `[MemoLens Development Email]` với `Confirmation link:`. Link vẫn mở MVC `/Account/ConfirmEmail`, nên luồng web hiện tại không bị thay đổi. Mobile client có thể lấy `userId` và `token` từ deep link/web link theo thiết kế tích hợp sau này.

## 6. API forgot và reset password trong Development

Phase 15D bổ sung hai endpoint:

- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`

Forgot password luôn trả thông báo chung sau, bất kể email không tồn tại, chưa xác thực hay đã xác thực:

```text
Nếu email tồn tại trong hệ thống, MemoLens sẽ gửi hướng dẫn đặt lại mật khẩu.
```

Chỉ tài khoản tồn tại và đã xác thực email mới thực sự tạo reset token. Trong Development, reset link xuất hiện trong block `[MemoLens Development Email]` với subject `Đặt lại mật khẩu MemoLens` và dòng `Reset password link:`. Token hoặc link không xuất hiện trong API response.

Reset link hiện trỏ tới trang MVC `/Account/ResetPassword` để giữ một luồng email tương thích cho web. Mobile client tương lai có thể lấy `email` và `token` từ deep link rồi gửi chúng tới API reset password; Flutter deep link chưa thuộc Phase 15D.

Reset API không auto-login, không tạo MVC cookie và không cấp access/refresh token. Khi reset thành công, toàn bộ refresh token còn hoạt động của user được revoke trong cùng database transaction; user phải đăng nhập lại bằng mật khẩu mới. Access token đã cấp trước đó tự hết hạn theo lifetime hiện có.

## 7. Cấu hình Email

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

## 8. Chuẩn bị Production SMTP

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

## 9. Quy tắc bảo mật

- Không tắt `RequireConfirmedEmail`.
- Không tự động đặt `EmailConfirmed = true` cho user mới.
- Không trả confirmation token hoặc link trong MVC UI hay API register response.
- Không log password, access token hoặc refresh token.
- Confirmation link chỉ được log ở Development để kiểm thử local.
- Không dùng `DevelopmentEmailSender` cho production.
- Luôn lưu SMTP secret ở User Secrets, environment variables hoặc secret store của server.
- Forgot password luôn dùng response chung để tránh tiết lộ email có tồn tại hay không.
- Reset password dùng token chuẩn của ASP.NET Core Identity và không auto-login.
- Resend confirmation luôn dùng response chung, không tiết lộ email có tồn tại hoặc đã xác nhận hay không.
- API confirm email không auto-login và không cấp JWT/refresh token.
- API forgot password không trả reset token hoặc reset link và luôn dùng response chung.
- Lỗi gửi email trong API forgot password được ghi log không kèm email/token và không làm thay đổi response chung.
- API reset password không cấp token/cookie; reset thành công revoke toàn bộ refresh token còn hoạt động.

## 10. Chưa được triển khai

- Rate limiting cho register/login/resend.
- Rate limiting cho forgot/reset password.
- Production email provider đã được cấu hình bằng secret thật.
- Retry queue, delivery tracking hoặc email template provider.
- Flutter deep link cho confirmation và reset password.
