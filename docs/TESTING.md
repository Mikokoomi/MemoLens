# MemoLens - Automated Testing

## 1. Mục đích

Phase 16C tạo nền tảng automated test nhỏ, độc lập với LocalDB phát triển. Phase 16D bổ sung integration tests cho các API auth hiện có, còn Phase 16E bảo vệ các boundary privacy/ownership của MVC. Mục tiêu là giúp các phase sau kiểm thử auth, quyền sở hữu, database và API mà không làm ảnh hưởng dữ liệu cá nhân trong môi trường Development.

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

Privacy/ownership integration tests hiện có:

- User A xem được memory của mình; User B không thể xem, sửa hoặc xóa memory của User A. Guest bị redirect về Login.
- Album detail/edit/delete được scope theo owner; forged request thêm memory của User A vào album User B không tạo quan hệ `AlbumMemory`.
- Authorized image endpoint chỉ trả ảnh cho owner; guest, user khác và memory soft-deleted đều không truy cập được ảnh.
- Trash chỉ hiển thị item đã xóa của current user; forged restore memory/album của user khác trả `404` và không đổi trạng thái soft delete.
- Settings yêu cầu login và chỉ hiển thị dữ liệu của current user.
- User có role `Admin` vẫn không bypass ownership của memory riêng tư trong MVP hiện tại.

Các test này không dùng LocalDB, không gửi email và không tạo dữ liệu trong database Development.

## 5. Chưa được cover

- Image upload, private image endpoint và orphan-image cleanup.
- Permanent delete, backup/restore và UI end-to-end.

## 6. Manual UI regression QA

Phase 17L đã chạy manual/browser regression QA cho Paper Note UI sau khi mobile core UI được khóa ở commit `999fbc8`.

Đã kiểm tra các kích thước:

- 360x800
- 390x844
- 430x932
- 768x1024
- 1280x720
- 1440x900

Phạm vi kiểm tra:

- Guest Home, Login, Register, Forgot Password, Reset Password, Register Confirmation, Access Denied và Privacy.
- Authenticated Home, Timeline, search/filter query states, Create/Edit/Details/Delete Memory, Albums, Album Create/Details/Edit/Delete/Add Memories, Trash, Settings, Edit Profile, Change Password và Privacy.
- Mobile topbar, fixed mobile bottom navigation, desktop sidebar, active navigation state, safe-area spacing, zero-image Details state và final form actions.
- HTTP smoke cho `/`, `/Account/Login`, `/Account/Register`, `/api/v1/health` và `/swagger/index.html` trong Development.

Kết quả Phase 17L:

- Không phát hiện horizontal overflow ở các kích thước đã kiểm tra.
- Không phát hiện bottom navigation che nội dung cuối ở các route được kiểm tra.
- Focus cuối form Create Memory và Change Password vẫn hiển thị phía trên mobile bottom navigation ở 390px.
- Không phát hiện duplicate back action trong Memory Details.
- Không có migration mới, không thay đổi backend/auth/API/database.

## 7. Roadmap test

1. **Phase 16F: Image Upload and Storage Tests**
   - Upload validation, physical private storage, missing file và cleanup failure paths.
2. **Phase 16G: Database Cleanup Tests**
   - Refresh token retention, unused tag và orphan image dry-run/quarantine khi các service đó được triển khai.
3. **Sau đó: UI/end-to-end tests**
   - Các flow quan trọng trên browser sau khi API/MVC behavior ổn định.
4. **Sau đó: Mobile API content tests**
   - Các endpoint memory, album, image và trash chỉ sau khi API CRUD riêng tư được thiết kế và triển khai.
