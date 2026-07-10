# MemoLens - Automated Testing

## 1. Mục đích

Phase 16C tạo nền tảng automated test nhỏ, độc lập với LocalDB phát triển. Phase 16D bổ sung integration tests cho các API auth hiện có. Mục tiêu là giúp các phase sau kiểm thử auth, quyền sở hữu, database và API mà không làm ảnh hưởng dữ liệu cá nhân trong môi trường Development.

## 2. Test project

Test project: `MemoLens.Tests`

Công nghệ hiện dùng:

- xUnit.
- `Microsoft.AspNetCore.Mvc.Testing` để khởi động app trong test host.
- SQLite in-memory cho `ApplicationDbContext` có relational behavior cơ bản.
- Fake `IEmailSender`; test không gửi SMTP/email thật và không log token hoặc secret.

Mỗi test host dùng SQLite in-memory mở trong lifetime của factory. Nó không dùng connection string LocalDB trong `appsettings.json` và không áp dụng migration vào database Development.

Environment test là `Testing`. Identity seed được bỏ qua riêng trong environment này; schema SQLite được tạo bằng `EnsureCreated` trong test factory.

## 3. Chạy test

Từ repository root:

```bash
dotnet test
```

Để build solution:

```bash
dotnet build
```

## 4. Phạm vi hiện tại

Smoke tests hiện có:

- `Home_ReturnsSuccess`: `GET /` trả thành công.
- `Health_ReturnsSuccessJson`: `GET /api/v1/health` trả JSON success với app name `MemoLens`.
- `Memories_GuestRedirectsToLogin`: guest vào `/Memories` được redirect tới Login.
- `AccountMe_WithoutToken_ReturnsUnauthorized`: API account/me không có Bearer token trả `401`.

Auth API integration tests hiện có:

- Register thành công không trả access token hoặc refresh token; user chưa xác nhận email không thể login.
- Login body không hợp lệ trả validation error; sai mật khẩu trả failure an toàn.
- Xác nhận email qua token Identity, sau đó login trả access token và refresh token.
- `account/me` với Bearer token chỉ trả summary của current user, không lộ password hash, security stamp hoặc refresh token data.
- Refresh token được rotate; token cũ không thể reuse; database chỉ chứa hash của refresh token.
- Logout revoke refresh token.
- Forgot password luôn trả response chung; reset password qua fake email link đổi được mật khẩu và revoke refresh token cũ.

Các test này không dùng LocalDB, không gửi email và không tạo dữ liệu trong database Development.

## 5. Chưa được cover

- Ownership user A/B cho memories, albums, images và trash.
- Image upload, private image endpoint và orphan-image cleanup.
- Permanent delete, backup/restore và UI end-to-end.

## 6. Roadmap test

1. **Phase 16E: Privacy/Ownership Tests**
   - User A/B isolation cho memory, album, trash, settings và API tương lai.
2. **Phase 16F: Image Access Tests**
   - Authorized image serving, soft delete, missing file và private storage boundary.
3. **Phase 16G: Database Cleanup Tests**
   - Refresh token retention, unused tag và orphan image dry-run/quarantine khi các service đó được triển khai.
4. **Sau đó: UI/end-to-end tests**
   - Các flow quan trọng trên browser sau khi API/MVC behavior ổn định.
