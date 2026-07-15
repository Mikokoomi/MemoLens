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

Image upload and private access integration tests (Phase 18A):

- `MemoryImageIntegrationTests` chạy qua MVC actions thật cho Create/Edit/DeleteImage và authorized endpoint `/Images/MemoryImage/{id}`.
- Test host vẫn dùng SQLite in-memory, nhưng `LocalImageStorageService` được khởi tạo với một content root tạm riêng cho từng `CustomWebApplicationFactory`. Vì vậy ảnh test chỉ nằm dưới thư mục tạm `MemoLens.Tests/<guid>/App_Data/uploads`, không bao giờ ghi vào `App_Data/uploads` của app Development.
- Thư mục upload tạm được dọn trước và sau từng image test, đồng thời factory xóa toàn bộ content root tạm khi dispose.
- Cover valid upload, nhiều ảnh, bốn extension được hỗ trợ (`.jpg`, `.jpeg`, `.png`, `.webp`), metadata `OriginalFileName`, tên file GUID sinh tự động và file vật lý private.
- Cover extension không hợp lệ, file lớn hơn 5 MB và ảnh thứ 11; các trường hợp này không tạo row/file mới và giữ nguyên ảnh hợp lệ đã có.
- Xác nhận owner xem được bytes qua endpoint có authorize; User B và cả role Admin đều nhận `404`; guest nhận redirect về Login.
- Cover missing file, static URL `/uploads/...` không truy cập được, xóa một ảnh, forged delete cross-user, filename Unicode/repeated dots/path-like và containment trong private upload root.
- Xác nhận soft delete memory chỉ ẩn ảnh, không xóa file vật lý; restore memory làm endpoint ảnh hoạt động lại.

Sau Phase 18A, suite có 32 test tự động passing.

## 5. Chưa được cover

- Permanent delete, backup/restore và UI end-to-end.
- Orphan-image cleanup và các service cleanup/quarantine theo chiến lược dữ liệu.
- Image decoding/compression/thumbnail vì MVP hiện tại chưa có các tính năng đó.

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

## 8. Phase 18B: Memory API integration tests

`MemoryApiIntegrationTests` dùng SQLite in-memory và test upload root tạm, không dùng LocalDB hoặc ảnh Development.

- Kiểm tra JWT bắt buộc, create/validation và tags trim/dedupe.
- Kiểm tra owner/Admin isolation, filters, sort và database pagination.
- Kiểm tra detail không lộ private path, update giữ ảnh, soft delete giữ file và restore khôi phục private image access.

Tổng số test hiện tại: **48**.

## 9. Phase 18C: Private Memory Image API integration tests

`MemoryImageApiIntegrationTests` tiếp tục dùng SQLite in-memory và private upload root tạm theo test factory.

- JWT bắt buộc cho upload, content và delete.
- Owner access; User B và role `Admin` không bypass ownership.
- Upload một/nhiều file `.jpg`, `.jpeg`, `.png`, `.webp`; metadata an toàn, tên file GUID và JWT content URL.
- No-file, extension sai, file trên 5 MB, vượt 10 ảnh và mixed invalid batch không tạo row/file mới.
- Content trả đúng bytes/MIME và `Cache-Control: private, no-store`; missing file, deleted memory và cross-owner trả `404`.
- Restore memory khôi phục image access.
- Delete chỉ xóa ảnh được chọn; missing physical file vẫn xóa row; gọi lại trả `404`.
- Memory API detail không còn trả MVC cookie URL hoặc private storage path.

Tổng số test hiện tại: **64**, tất cả dùng test database/storage tách biệt khỏi Development.

## 10. Phase 18D: Private Album API integration tests

`AlbumApiIntegrationTests` dùng SQLite in-memory và private upload root tạm như các API test hiện có.

- JWT bắt buộc cho toàn bộ 8 Album/membership endpoint.
- Create/update trim dữ liệu, validation tiếng Việt và không nhận owner/membership từ body create.
- List owner-scoped, search, sort, database pagination và authorized cover URL.
- Details phân trang memory, sắp xếp mới nhất, trả tags/image summary và ẩn memory đã xóa mềm.
- User B và role `Admin` không bypass Album ownership.
- Batch add dedupe/idempotent; ID thiếu, cross-owner hoặc deleted làm toàn batch rollback trước khi ghi.
- Remove chỉ xóa `AlbumMemory`, giữ memory, image row và file vật lý.
- Soft delete/restore giữ toàn bộ membership; deleted memory tiếp tục bị ẩn sau restore album.

Tổng số test hiện tại: **82**, không dùng LocalDB hoặc upload storage Development.

## 11. Phase 18E: Mobile API final security QA

`MobileApiFinalQaIntegrationTests` bổ sung 8 test tập trung vào các boundary còn thiếu:

- JWT malformed, sai chữ ký, hết hạn và thiếu user id claim đều trả `401` JSON.
- MVC cookie không thể thay Bearer token cho private API; MVC session vẫn hoạt động như cũ.
- Malformed JSON, content type sai, method sai, route id sai và query date sai không trả HTML hoặc stack trace.
- Memory/Album pagination không wrap về trang đầu khi page/pageSize chạm biên số nguyên.
- Refresh token hết hạn bị từ chối và không lộ raw token/hash.
- Account/Memory/Album response không lộ owner key, Identity secret hoặc private storage path.
- Swagger chỉ có trong Development, mô tả Bearer auth, mobile endpoints và multipart upload.

Phase 18E phát hiện một defect pagination thật và thêm regression coverage trước khi sửa. Tổng suite hiện tại: **90 test passing**.

Development E2E A/B cũng đã được chạy cho register/confirm/login, Memory, upload ảnh, Album membership, cross-owner `404`, delete/restore, refresh rotation và logout. Dữ liệu/file test đã được dọn sau smoke.

Chi tiết evidence và kết quả command: [MOBILE_API_FINAL_QA.md](MOBILE_API_FINAL_QA.md).

## 12. Phase 19B: Flutter authentication tests

Flutter auth tests dùng fake API, fake secure token storage, custom Dio adapter và Riverpod provider override; không gọi backend Development hoặc LocalDB.

- Model: parse login response đúng contract, từ chối response thiếu field và không lộ token qua `toString`.
- Repository: lưu cặp token, không lưu khi login lỗi, refresh rotation, invalid refresh, backend offline, logout local và single-flight refresh.
- Interceptor: Bearer chỉ cho protected route, retry một lần, nhiều `401` dùng một refresh, auth endpoint không recursion và refresh fail trả session invalid an toàn.
- Controller: bootstrap không/có token, access hết hạn, refresh invalid, backend unavailable, login và logout.
- Widget/router: Login ở 360/390/430, Register validation, password visibility, loading chống submit lặp, confirmation page, Home identity, route guards và Logout về Login.

Tổng Flutter suite sau Phase 19B: **39 test passing**.

Manual integration đã chạy trên AVD `MemoLens_API_36` với backend Development:

- Register UI hiển thị đúng; API chặn login trước khi email được xác nhận.
- Sau khi mở confirmation link trên web, Flutter login thành công và hiển thị đúng user hiện tại.
- Force-stop/mở lại app vẫn khôi phục Home từ secure storage.
- Khi backend tạm offline, Splash hiện trạng thái `Thử lại` và không đưa user về Login; sau khi backend chạy lại, retry khôi phục phiên cũ.
- Logout đưa app về Login và vẫn giữ trạng thái logout sau restart.
- Tài khoản/refresh token smoke đã được dọn khỏi LocalDB.

Không chờ thủ công đủ 15 phút để access token hết hạn trên emulator. Refresh rotation, concurrent `401`, old-token reuse và refresh failure được bao phủ bởi automated Flutter tests cùng backend integration suite.

## 13. Roadmap test

1. **Phase 16G: Database Cleanup Tests**
   - Refresh token retention, unused tag và orphan image dry-run/quarantine khi các service đó được triển khai.
2. **Sau đó: UI/end-to-end tests**
   - Các flow quan trọng trên browser sau khi API/MVC behavior ổn định.
3. **Sau đó: Trash API integration tests**
   - Ownership, trash list, restore và permanent-delete boundaries cho API mobile tiếp theo.
## Phase 19D Flutter image tests

Flutter tests cover supported extension handling, 5 MB client validation, safe filename display and frozen upload metadata parsing. Existing auth and Memory tests remain part of the full Flutter suite. Android picker behavior requires a device/emulator smoke test because automated tests do not open a real gallery.
