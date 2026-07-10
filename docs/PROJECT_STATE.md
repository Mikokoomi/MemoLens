# MemoLens - Trạng thái dự án hiện tại

## 1. Tên dự án

MemoLens

## 2. Định hướng sản phẩm

MemoLens là một ứng dụng nhật ký ảnh cá nhân và kể chuyện ký ức riêng tư.

MemoLens không phải là mạng xã hội. Ứng dụng tập trung vào ký ức cá nhân, ảnh riêng tư, cảm xúc, ghi chú, câu chuyện, timeline và album cá nhân.

Trong MVP hiện tại, MemoLens không có:

- Public feed.
- Like.
- Comment.
- Follower.
- Public profile.
- Public sharing.
- Explore hoặc trending page.
- AI.

Mọi thay đổi trong tương lai cần giữ MemoLens là một không gian riêng tư để lưu giữ và nhìn lại ký ức, không biến ứng dụng thành nền tảng mạng xã hội.

## 3. Tech stack

- ASP.NET Core MVC.
- SQL Server.
- Entity Framework Core.
- ASP.NET Core Identity.
- Bootstrap.
- Mobile-first UI.
- Taste Skill đã được cài và dùng làm hướng dẫn UI/UX.

## 4. Các phase đã hoàn thành

- Phase 1: Project setup.
- Phase 2: Database/models.
- Phase 3: Authentication with email confirmation and roles.
- Phase 4: User-scoped Memory CRUD.
- Phase 5: Memory image upload and gallery.
- Phase 6: Timeline search/filter.
- Phase 6.5: Mobile-first UI polish.
- Phase 7: Private album management.
- Phase 8: Private image storage and authorized image serving.
- Phase 9: Trash/Restore for memories and albums.
- Phase 10: Settings & Privacy.
- Phase 11: MVP QA / Hardening.
- Phase 12: Productization & Mobile Architecture Plan.
- Phase 12.5: UI Language Consistency, Footer, and Mobile Responsive Fix.
- Phase 13A: API Foundation Code.
- Phase 13B: API Foundation Documentation Update.
- Phase 14A: API Authentication Design.
- Phase 14B: API Auth Infrastructure.

## 5. Tính năng cốt lõi hiện tại

- Register, login, logout.
- Bắt buộc xác thực email trước khi đăng nhập.
- Có role `Admin` và `User`.
- Memories được scope theo từng user.
- Danh sách Feeling tiếng Việt cố định.
- Tags cho memories.
- Upload ảnh riêng tư cho memory.
- Gallery ảnh trong memory detail.
- Authorized image endpoint để phục vụ ảnh.
- Albums riêng tư.
- Timeline có search, filter và sort.
- Trash/Restore cho memories và albums.
- Soft delete cho memories và albums.
- API response models cho Web API.
- Health endpoint `GET /api/v1/health`.
- Swagger/OpenAPI trong môi trường Development.

## 6. Quy tắc riêng tư quan trọng

- User chỉ được truy cập memories, albums, images và trash items của chính mình.
- Admin không được browse nội dung riêng tư của user trong MVP hiện tại.
- Ảnh mới được lưu bên ngoài `wwwroot`, dưới `App_Data/uploads`.
- Ảnh được phục vụ qua authorized endpoint, không phải public static URL.
- Không có direct public image URLs cho ảnh riêng tư.
- Unauthorized users, users khác, missing files và soft-deleted memories phải được xử lý an toàn, thường bằng `NotFound`.

## 7. Hành vi dữ liệu quan trọng

- Soft delete memory không xóa file ảnh.
- Soft delete album không xóa memories gốc.
- Restore memory đưa memory trở lại timeline và khôi phục quyền truy cập ảnh qua authorized endpoint.
- Restore album đưa album trở lại danh sách albums.
- Restore album giữ lại quan hệ album-memory hiện có.
- Restore album không tự động restore các memory đã bị xóa mềm bên trong album.
- Permanent delete chưa được triển khai.

## 8. Giới hạn hiện tại

- Chưa có permanent delete.
- Chưa có password reset.
- Chưa có account settings page.
- Chưa có export data.
- Chưa có thumbnails hoặc image compression.
- Chưa có cloud storage.
- Chưa có AI.
- Chưa có public sharing.
- Chưa có trang quản lý tag riêng.
- Chưa có admin dashboard.
- Chưa có auth API.
- Đã có JWT/token infrastructure nhưng chưa có auth API endpoint sử dụng bearer token.
- Chưa có memory CRUD API.
- Chưa có album CRUD API.
- Chưa có image upload API.
- Chưa có Flutter app.

## 9. Phase tiếp theo được đề xuất

- Sau MVP: permanent delete, export data, thumbnails/compression.

## 10. Hướng dẫn cho Codex trong các task sau

Trước mọi task tương lai, Codex phải đọc:

- `docs/PROJECT_STATE.md`
- `README.md`
- `docs/PROJECT_BRIEF.md`
- `docs/DECISIONS.md`
- `docs/CODEX_NOTES.md`

Codex không được chỉ dựa vào context chat trước đó. Tài liệu trong repository là nguồn trạng thái chính của dự án.

Khi task liên quan đến database, Codex cũng cần đọc `docs/DATABASE_DESIGN.md`.

Khi task liên quan đến UI, Codex cũng cần đọc `docs/UI_SCREENS.md` và dùng Taste Skill guidance nếu còn phù hợp.

Mọi thay đổi phải giữ nguyên định hướng private-first, beginner-friendly, không social features, không AI trong MVP hiện tại, và không thay đổi kiến trúc lớn nếu chưa hỏi trước.

## 11. Cap nhat Phase 10: Settings & Privacy

Phase 10 da hoan thanh.

Tinh nang da co:

- User dang dang nhap co the xem trang Settings rieng tu.
- Settings hien thi ten hien thi, email, trang thai xac thuc email va ngay tao tai khoan.
- User co the sua `DisplayName` cua chinh minh.
- User co the doi mat khau bang `UserManager.ChangePasswordAsync`.
- Doi mat khau yeu cau mat khau hien tai va tuan theo password validators cua ASP.NET Core Identity.
- Sau khi sua profile hoac doi mat khau, MemoLens refresh sign-in session.
- Trang Settings co ghi chu quyen rieng tu cho memories, albums, images va trash.

Gioi han van con:

- Chua co doi email.
- Chua co forgot password/password reset email flow.
- Chua co export data.
- Chua co xoa tai khoan.
- Chua co admin settings panel.

## 12. Cap nhat Phase 11: MVP QA / Hardening

Phase 11 da hoan thanh.

Ket qua:

- Da tao `docs/MVP_QA_CHECKLIST.md` cho auth, user isolation, memories, images, albums, trash, settings, search/filter, mobile UI va demo prep.
- Da audit cac controller chinh: Account, Settings, Memories, Images, Albums, Trash va Home.
- Da giu nguyen product direction: khong social features, khong AI, khong public sharing, khong export/delete account, khong permanent delete.
- Da harden ConfirmEmail de token hong khong lam app crash.
- Da harden album display de chi hien album-memory relationship cung owner voi album.
- Khong co database schema change.
- Khong tao migration moi.

Trang thai privacy:

- Private pages can dang nhap.
- Memories, albums, images, trash va settings tiep tuc scope theo current user.
- Admin role khong bypass private content ownership trong MVP hien tai.

## 13. Cap nhat Phase 12: Productization & Mobile Architecture Plan

Phase 12 da hoan thanh va chi thay doi tai lieu.

Tai lieu moi:

- `docs/PRODUCTIZATION_PLAN.md`
- `docs/MOBILE_ARCHITECTURE.md`
- `docs/API_ROADMAP.md`
- `docs/PRIVATE_BETA_PLAN.md`
- `docs/PRODUCTION_RISK_REGISTER.md`

Quyet dinh chien luoc:

- Flutter la huong mobile du kien.
- ASP.NET Core backend duoc giu lai.
- MVC web app hien tai tiep tuc huu ich cho MVP/demo/internal web surface.
- Web API layer se duoc them dan de phuc vu mobile app.
- Private beta la muc tieu launch thuc te tiep theo, truoc public app store.
- MemoLens van phai private-first va khong di theo huong social network.

Trang thai trung thuc:

- MemoLens demo-ready/MVP-ready nhung chua production-ready.
- Chua co Flutter app.
- Chua co API controllers.
- Chua co token-based mobile auth.
- Chua co real SMTP, forgot password, backup, thumbnails/compression, export data, delete account hoac production monitoring.

## 14. Cập nhật Phase 12.5: UI Language Consistency, Footer, and Mobile Responsive Fix

Phase 12.5 đã hoàn thành.

Phạm vi:

- Chuẩn hóa phần lớn text hiển thị trong MVC UI sang tiếng Việt có dấu.
- Giữ brand name `MemoLens` không đổi.
- Làm footer gọn và có chủ đích hơn: tên MemoLens, câu nhắc riêng tư ngắn, link quyền riêng tư.
- Tinh chỉnh CSS để navbar, nút, form, filter, gallery và footer dễ dùng hơn trên màn hình điện thoại.
- Cập nhật README để ghi nhận trạng thái UI language/responsive stabilization.

Không thay đổi:

- Không đổi backend behavior.
- Không đổi database schema.
- Không tạo migration.
- Không thêm API controller.
- Không thêm Flutter/mobile app code.
- Không đổi authentication, memory CRUD, image upload/private serving, album, trash hoặc settings logic.
- Không thêm social features hoặc AI.

## 15. Cập nhật Phase 13: API Foundation

Phase 13A và 13B đã hoàn thành.

Phạm vi đã có:

- Thêm response model chuẩn cho API:
  - `ApiResponse`
  - `ApiResponse<T>`
  - `ApiValidationErrorResponse`
- Thêm health endpoint:
  - `GET /api/v1/health`
- Health endpoint trả JSON gồm `success`, `message`, `appName`, `apiVersion`, `environment` và `serverTimeUtc`.
- Swagger/OpenAPI được bật trong môi trường `Development`.
- Swagger title là `MemoLens API`, version là `v1`.
- Tài liệu API foundation được tạo tại `docs/API_FOUNDATION.md`.

Giới hạn hiện tại:

- Chưa có auth API.
- Chưa có JWT hoặc token-based auth.
- Chưa có memory CRUD API.
- Chưa có album CRUD API.
- Chưa có image upload API.
- Chưa có Flutter app.

Không thay đổi:

- Không đổi database schema.
- Không tạo migration.
- Không đổi MVC auth/cookie behavior.
- Không đổi memory, album, trash, image hoặc settings behavior.
- Không thêm social features, AI hoặc public sharing.

Quy tắc riêng tư cho API tương lai:

- Mọi API riêng tư phải scope theo current authenticated user.
- Admin không được bypass private content ownership trong MVP hiện tại.
- Không có public image URLs.
- Không trả private physical file path ra API response.
- Deleted memories/albums phải bị ẩn khỏi normal APIs.

## 16. Cap nhat Phase 14A: API Authentication Design

Phase 14A da hoan thanh va chi thay doi tai lieu.

Da them `docs/API_AUTH_DESIGN.md` de chot huong xac thuc cho Flutter/mobile API trong tuong lai:

- Mobile API du kien dung JWT access token ngan han va refresh token co the revoke.
- Refresh token se duoc luu dang hash trong database khi Phase 14B bat dau, khong luu plain text.
- Email confirmation van la dieu kien bat buoc truoc khi login.
- MVC web app tiep tuc dung cookie authentication.
- Auth API, JWT, refresh token model/table va migration chua duoc implement.
- Moi API tuong lai van phai scope theo current user; Admin khong duoc bypass private content ownership trong MVP.

Khong thay doi:

- Khong thay doi application source code.
- Khong thay doi database schema.
- Khong tao migration.
- Khong thay doi MVC auth/cookie behavior.

## 17. Cap nhat Phase 14B: API Auth Infrastructure

Phase 14B da hoan thanh.

Ha tang da them:

- `JwtOptions` voi access token mac dinh 15 phut va refresh token mac dinh 30 ngay.
- JWT bearer authentication scheme rieng cho API tuong lai.
- `ITokenService` va `TokenService` de sinh access token, refresh token ngau nhien va hash/validate refresh token.
- Model `UserRefreshToken`, `DbSet`, relationship voi `ApplicationUser` va migration `AddUserRefreshTokens`.
- Bang `UserRefreshTokens` co index cho `UserId`, `TokenHash`, `ExpiresAt`; `TokenHash` la unique.
- Refresh token chi duoc luu dang SHA-256 hash trong database; plain token khong duoc log hoac persist.
- Development JWT config dung placeholder; production phai cap issuer, audience va secret bang environment variables hoac user secrets.

Gioi han van con:

- Chua co API register/login/refresh/logout.
- Chua co API confirm email/resend confirmation.
- Chua co API forgot/reset password.
- Chua co `GET /api/v1/account/me`.
- Chua co memory, album hoac image CRUD API.

Khong thay doi:

- MVC Identity cookie van la auth mac dinh cho web app.
- Email confirmation van bat buoc.
- Password rules va role `Admin`/`User` khong doi.
- Khong thay doi logic memories, albums, images, trash hoac settings.
