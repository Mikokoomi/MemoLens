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
- Phase 14B.5: Dependency Security Patch Review.
- Phase 14C: Core API Auth Endpoints.
- Phase 15A: Email Infrastructure and Confirmation Testing.
- Phase 15B: MVC Forgot and Reset Password.
- Phase 15C: API Email Confirmation and Resend Confirmation.
- Phase 15D: API Forgot and Reset Password.
- Phase 16A: Database and Data Integrity Review.
- Phase 16B.1: Database Index Implementation.
- Phase 16B.2: Refresh Token Cleanup.
- Phase 16B.3: Orphan Image and Unused Tag Cleanup Design.
- Phase 16C: Automated Tests Foundation.
- Phase 16D: Auth API Integration Tests.
- Phase 16E: Privacy and Ownership Integration Tests.
- Phase 17A: UI/UX Redesign Direction.
- Phase 17B: Design System Tokens and Base CSS.

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
- Core API auth endpoints cho register, login, refresh, logout và account/me.
- API confirm/resend email confirmation và API forgot/reset password.

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
- MVC và API đều đã có forgot/reset password.
- Chưa có export data.
- Chưa có thumbnails hoặc image compression.
- Chưa có cloud storage.
- Chưa có AI.
- Chưa có public sharing.
- Chưa có trang quản lý tag riêng.
- Chưa có admin dashboard.
- Đã có core JWT bearer auth API, API confirm/resend email và API forgot/reset password.
- Chưa có memory CRUD API.
- Chưa có album CRUD API.
- Chưa có image upload API.
- Chưa có Flutter app.

## 9. Phase tiếp theo được đề xuất

- Phase 16B.4: Backup and Restore Plan.
- Phase 16B.5: Unused Tag Cleanup Implementation.
- Phase 16B.6: Orphan Image Cleanup Dry-Run Service.
- Phase 16B.7: Permanent Delete Design/Implementation.
- Phase 16F: Image Upload and Storage Tests.
- Phase 16G: Database Cleanup Tests.
- Phase 17C: Home/Auth UI redesign.
- Phase 17D: Timeline and Memory Card redesign.
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

## 18. Cap nhat Phase 14B.5: Dependency Security Patch Review

Phase 14B.5 da hoan thanh va chi cap nhat dependency/documentation.

Ket qua audit truoc ban va:

- `Azure.Identity 1.10.3` co advisory muc Moderate.
- `Microsoft.Identity.Client 4.56.0` co advisory muc Low/Moderate.
- `System.Formats.Asn1 5.0.0` co advisory muc High.
- Cac package nay deu la dependency transitive cua `Microsoft.EntityFrameworkCore.SqlServer 8.0.10` thong qua `Microsoft.Data.SqlClient 5.1.5`.

Ban va da thuc hien:

- Nang `Microsoft.AspNetCore.Authentication.JwtBearer` tu `8.0.10` len `8.0.28`.
- Nang `Microsoft.AspNetCore.Identity.EntityFrameworkCore` tu `8.0.10` len `8.0.28`.
- Nang `Microsoft.EntityFrameworkCore.SqlServer` va `Microsoft.EntityFrameworkCore.Tools` tu `8.0.10` len `8.0.28`.
- Nang local tool `dotnet-ef` tu `8.0.10` len `8.0.28`.
- Giu target framework `net8.0`; khong nang major len .NET 10.
- Giu `Swashbuckle.AspNetCore 6.6.2` vi khong nam trong chuoi advisory va task khong can major upgrade.

Trang thai sau ban va:

- `dotnet list package --vulnerable --include-transitive` khong con bao package de ton thuong theo cac source hien tai.
- EF Core khong co pending model changes.
- Khong tao migration moi.
- Khong thay doi MVC, auth/token logic, database schema hoac API behavior.

## 19. Cap nhat Phase 14C: Core API Auth Endpoints

Phase 14C da hoan thanh.

Endpoint da implement:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/account/me`

Hanh vi bao mat:

- Register khong cap token va van bat buoc email confirmation.
- Login dung Identity password/lockout checks, chi cap JWT va refresh token sau khi email confirmed.
- Login API khong tao hoac phu thuoc MVC cookie.
- Refresh token chi luu dang SHA-256 hash, khong luu plain token.
- Refresh token rotation revoke token cu va chan reuse bang atomic conditional update.
- Logout revoke refresh token duoc gui len va khong logout MVC cookie session.
- `account/me` chi chap nhan JWT bearer va chi tra profile cua current user.
- Validation va API auth errors dung standard `ApiResponse`/`ApiValidationErrorResponse` shape.

Gioi han van con:

- Chua co API confirm email hoac resend confirmation email.
- Chua co API forgot/reset password.
- Chua co memory, album, image, trash hoac settings API.
- Chua co Flutter app.

Khong thay doi:

- MVC register/login/logout va cookie authentication.
- Database schema; khong tao migration moi.
- Memory, album, image, trash va settings behavior.

## 20. Cap nhat Phase 14C.6: Mobile UI Regression Hotfix

Phase 14C.6 da hoan thanh.

Pham vi:

- Sua loi Home page bi horizontal overflow nhe o man hinh dien thoai 360px.
- Sua footer de nam trong normal document flow, khong dung fixed/absolute overlay va khong chan noi dung.
- Lam footer gon hon tren mobile va giu link Quyen rieng tu co vung cham ro rang.
- Sua khu vuc link duoi form Login/Register de link `Dang ky tai khoan` va `Dang nhap` co target cham rieng, khong bi the cha che diem click tren mobile.
- Giam do chat cua hero heading tren mobile de tranh cam giac chu de len nhau.

Khong thay doi:

- Khong doi database schema.
- Khong tao migration.
- Khong doi MVC auth logic.
- Khong doi API auth behavior.
- Khong doi memory, album, image, trash hoac settings behavior.
- Khong them API CRUD, Flutter code, AI, social features hoac public sharing.

## 21. Cập nhật Phase 14C.5: API Auth Security Review and Regression Checklist

Phase 14C.5 đã hoàn thành sau khi review source code và chạy smoke test thực tế cho API auth cùng MVC cookie auth.

Kết quả:

- Đã tạo `docs/API_AUTH_QA_CHECKLIST.md` bằng tiếng Việt.
- Đã review API register, login, refresh, logout, account/me, JWT config, token service, refresh-token model và database mapping.
- Không phát hiện lỗi bảo mật cụ thể cần sửa trong source code hiện tại.
- Register không cấp token; login vẫn bắt buộc email đã xác nhận.
- API login không tạo MVC cookie.
- `account/me` bắt buộc JWT Bearer và chỉ trả `id`, `email`, `displayName`, `roles` của current user.
- Refresh token chỉ lưu dạng SHA-256 hash; rotation revoke token cũ; reuse, revoked và expired token đều bị từ chối bằng `401`.
- Access token qua query string không được chấp nhận.
- MVC Home, Login, Register, private-page redirect, cookie login và logout vẫn hoạt động.
- `GET /api/v1/health` và Swagger trong Development vẫn hoạt động.
- NuGet vulnerability audit sạch theo các package source hiện tại.
- EF không có pending model changes.

Lưu ý:

- Development email sender cố ý ghi confirmation link ra console/log để test local; không được dùng cơ chế này ở production.
- Chưa có production email provider, rate limiting, confirm/resend email API, forgot/reset password API hoặc automated integration tests.

Không thay đổi:

- Không thêm endpoint mới.
- Không sửa auth/token/MVC behavior.
- Không thay đổi database schema.
- Không tạo migration.
- Không thêm API CRUD, Flutter code, AI, social features hoặc public sharing.

## 22. Cập nhật Phase 15A: Email Infrastructure and Confirmation Testing

Phase 15A đã hoàn thành.

Phạm vi đã có:

- Thêm `EmailOptions` cho mode, sender name/email và SMTP configuration keys.
- Development email log được chuẩn hóa với prefix `[MemoLens Development Email]`, recipient, subject và confirmation link dễ tìm.
- Development log chỉ ghi thông tin cần thiết để test link xác nhận, không ghi password, access token hoặc refresh token.
- Thêm nền tảng `SmtpEmailSender` dùng cấu hình SMTP và `UnconfiguredEmailSender` để production không log confirmation token khi chưa cấu hình provider.
- MVC Register Confirmation có nhắc Development bằng tiếng Việt có dấu: kiểm tra terminal/console để lấy link xác nhận email.
- API register vẫn không trả token hoặc confirmation link.
- Đã tạo `docs/EMAIL_SETUP.md` và cập nhật README.

Không thay đổi:

- `RequireConfirmedEmail` vẫn được bật.
- Không auto-confirm user.
- Không thêm forgot password, reset password, API confirm email hoặc API resend confirmation email.
- Không thay đổi JWT/refresh token logic hoặc MVC cookie behavior.
- Không thay đổi database schema và không tạo migration.
- Không thêm API CRUD, Flutter code, AI, social features hoặc public sharing.

Giới hạn còn lại:

- Chưa có SMTP credential/provider thật cho production.
- Chưa có rate limiting cho register/login/resend.
- Chưa có retry queue, delivery tracking hoặc email template provider.

## 23. Cập nhật Phase 15B: MVC Forgot and Reset Password

Phase 15B đã hoàn thành cho MVC web app.

Phạm vi đã có:

- Login page có link `Quên mật khẩu?`.
- Thêm GET/POST `/Account/ForgotPassword` và trang xác nhận chung.
- Forgot password luôn trả cùng thông báo, không tiết lộ email có tồn tại, chưa xác thực hay không.
- Chỉ user có email đã xác thực mới được tạo Identity password reset token và gửi reset link.
- Thêm GET/POST `/Account/ResetPassword` dùng `UserManager.ResetPasswordAsync`.
- Reset token sai, thiếu hoặc hết hạn được xử lý bằng thông báo an toàn.
- Password validation dùng quy tắc Identity hiện có và message tiếng Việt có dấu.
- Reset thành công không auto-login và chuyển tới trang xác nhận riêng.
- Sau reset thành công, toàn bộ API refresh token chưa revoke của user được revoke; access token ngắn hạn tự hết hạn như cũ.
- Development email log hiển thị rõ `Reset password link` nhưng không log password.
- README và `docs/EMAIL_SETUP.md` đã được cập nhật.

Không thay đổi:

- Không thêm API forgot/reset password endpoint.
- Không tắt email confirmation và không auto-confirm user.
- Không thay đổi MVC cookie login/logout behavior.
- Không thay đổi database schema và không tạo migration.
- Không thêm API CRUD, Flutter code, AI, social features hoặc public sharing.

Giới hạn còn lại:

- Chưa có rate limiting cho forgot password.
- Chưa có production SMTP credential/provider thật.

## 24. Cập nhật Phase 15C: API Email Confirmation and Resend Confirmation

Phase 15C đã hoàn thành.

Endpoint đã implement:

- `POST /api/v1/auth/confirm-email`
- `POST /api/v1/auth/resend-confirmation-email`

Hành vi bảo mật:

- Confirm email nhận `userId` và token Base64Url, dùng `UserManager.ConfirmEmailAsync`.
- User/token thiếu dùng standard API validation response tiếng Việt có dấu.
- User không tồn tại, token sai/hết hạn/đã dùng hoặc email đã confirmed đều nhận cùng lỗi an toàn.
- Confirm email thành công không auto-login, không tạo MVC cookie và không cấp JWT/refresh token.
- Resend confirmation luôn trả cùng response `200`, không tiết lộ email có tồn tại hoặc đã confirmed hay không.
- Chỉ user tồn tại và chưa confirmed mới được tạo token và gửi confirmation link.
- Development tiếp tục log link rõ ràng qua `[MemoLens Development Email]`.
- Confirmation link gửi lại vẫn dùng MVC `Account/ConfirmEmail`, giữ nguyên web flow hiện tại.

Không thay đổi:

- Không thêm API forgot/reset password endpoint.
- Không thay đổi MVC register/confirm email hoặc MVC forgot/reset password.
- Không tắt email confirmation và không auto-confirm user.
- Không thay đổi database schema và không tạo migration.
- Không thêm API CRUD, Flutter code, AI, social features hoặc public sharing.

Giới hạn còn lại:

- Chưa có rate limiting cho resend confirmation.
- Chưa có production SMTP credential/provider thật.

## 25. Cập nhật Phase 15D: API Forgot and Reset Password

Phase 15D đã hoàn thành.

Endpoint đã implement:

- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`

Hành vi bảo mật:

- Forgot password luôn trả response chung, không tiết lộ email có tồn tại hoặc đã xác thực hay không.
- Lỗi gửi reset email không làm thay đổi response chung và log lỗi không kèm email/token.
- Chỉ user tồn tại và đã xác thực email mới được tạo Identity password reset token và gửi reset link.
- Development email sender ghi reset link rõ ràng trong terminal/console; API response không trả token hoặc link.
- Reset password dùng `UserManager.ResetPasswordAsync` và các password validator hiện có của Identity.
- Token sai, hết hạn hoặc không phù hợp nhận cùng thông báo lỗi an toàn.
- Reset thành công không auto-login, không tạo MVC cookie và không cấp access token hoặc refresh token.
- Toàn bộ API refresh token còn hoạt động của user được revoke trong cùng database transaction với việc reset password.
- User phải đăng nhập lại bằng mật khẩu mới; access token đã cấp trước đó tự hết hạn theo lifetime hiện có.

Không thay đổi:

- Không thay đổi MVC register/login/logout, email confirmation hoặc forgot/reset password.
- Không thay đổi database schema và không tạo migration.
- Không thêm memory, album, image, trash hoặc settings API CRUD.
- Không thêm Flutter code, AI, social features hoặc public sharing.

## 26. Cập nhật Phase 16A: Database and Data Integrity Review

Phase 16A đã hoàn thành dưới dạng review và tài liệu.

Kết quả chính:

- Đã tạo `docs/DATABASE_REVIEW.md` để ghi nhận entity, relationship, ownership, soft delete, refresh token, image storage, backup và index hiện có.
- Không phát hiện lỗi critical cần sửa source trong phase này.
- LocalDB không có orphan record, quan hệ album-memory chéo user hoặc trạng thái soft delete không nhất quán tại thời điểm review.
- EF Core không có pending model changes; không tạo schema change hoặc migration.
- Rủi ro cần xử lý sau gồm: composite index theo ownership/soft delete, cleanup refresh token/tag/orphan file, backup database kèm `App_Data/uploads`, permanent delete và ownership integration tests.

Không thay đổi:

- Không thay đổi application source, auth, database schema, migrations, API endpoint hoặc UI.
- Không thêm Flutter code, AI, social features hoặc public sharing.

## 27. Cập nhật Phase 16B.1: Database Index Implementation

Phase 16B.1 đã hoàn thành.

Migration `20260710174606_AddPerformanceIndexes` chỉ thêm index:

- `Memories(UserId, IsDeleted, MemoryDate)`.
- `Memories(UserId, IsDeleted, CreatedAt)`.
- `Albums(UserId, IsDeleted, CreatedAt)`.
- `UserRefreshTokens(RevokedAt)`.

Không thay đổi:

- Không thay đổi table column, data, relationship, cascade rule hoặc soft-delete behavior.
- Không thêm global query filter, cleanup job, permanent delete, API endpoint, Flutter code, AI, social feature hoặc public sharing.
- Không thay đổi controller, MVC/API auth hoặc UI behavior.

Migration đã được áp dụng cho LocalDB. EF Core không có pending model changes sau khi cập nhật.

## 28. Cập nhật Phase 16B.2: Refresh Token Cleanup

Phase 16B.2 đã hoàn thành.

Phạm vi đã có:

- `RefreshTokenCleanupOptions` cấu hình `Enabled`, interval, retention cho token revoked/expired và batch size.
- `IRefreshTokenCleanupService` và service scoped xóa theo UTC, xử lý theo batch.
- Chỉ token đã revoke quá retention hoặc hết hạn quá retention mới bị xóa; token active không thuộc điều kiện cleanup.
- Hosted background service chỉ thực thi khi `RefreshTokenCleanup:Enabled=true`; cấu hình mặc định là tắt, bao gồm Development.
- Log chỉ chứa số lượng record đã xóa, thời điểm UTC và loại exception an toàn; không log token hash hoặc plain token.

Không thay đổi:

- Không thay đổi schema hoặc tạo migration.
- Không thay đổi refresh token issue, rotation, login, logout, MVC cookie auth, API endpoint hoặc UI behavior.
- Không thêm global query filter, cleanup ảnh/tag, permanent delete, account deletion, Flutter code, AI, social feature hoặc public sharing.

Kiểm thử QA xác nhận hai record cũ (expired/revoked) bị xóa, record active được giữ lại, và toàn bộ dữ liệu QA đã được dọn sau test.

## 29. Cập nhật Phase 16B.3: Orphan Image and Unused Tag Cleanup Design

Phase 16B.3 đã hoàn thành dưới dạng design/documentation only.

- Đã tạo `docs/DATA_CLEANUP_STRATEGY.md` cho orphan image, unused tag, permanent delete và backup/restore.
- Chiến lược yêu cầu report-only/dry-run, grace period, quarantine, batch, UTC, backup và explicit configuration trước destructive cleanup.
- File của memory soft-deleted phải được giữ cho đến khi permanent delete/retention policy cho phép xóa.
- Tag chỉ nên được cleanup khi không còn bất kỳ `MemoryTag` relationship nào; tag của memory soft-deleted phải được giữ để restore.

Không thay đổi:

- Không tạo cleanup service/background job, không xóa file hoặc database record và không tạo migration.
- Không thay đổi image upload, image serving, auth, API, UI, schema, Flutter code, AI, social feature hoặc public sharing.

## 30. Cập nhật Phase 16C: Automated Tests Foundation

Phase 16C đã hoàn thành.

- Đã tạo test project `MemoLens.Tests` với xUnit, `Microsoft.AspNetCore.Mvc.Testing` và SQLite in-memory.
- Test factory dùng environment `Testing`, fake email sender và database SQLite tách hoàn toàn khỏi LocalDB Development.
- Thêm smoke tests cho Home, health API, guest redirect tới Login và `account/me` không token trả `401`.
- Không thêm full auth flow, refresh rotation, ownership, image upload hoặc cleanup tests trong phase foundation này.

Không thay đổi:

- Không thay đổi database schema hoặc tạo migration.
- Không thay đổi MVC/API auth behavior, image behavior, UI, product feature, Flutter code, AI, social feature hoặc public sharing.

## 31. Cập nhật Phase 16D: Auth API Integration Tests

Phase 16D đã hoàn thành.

- Đã thêm 6 integration tests cho các API auth hiện có, nâng tổng test suite lên 10 tests.
- Test bao phủ register không cấp token, unconfirmed login bị chặn, validation và wrong-password failure, confirm email, login, `account/me`, refresh rotation/reuse, logout, forgot/reset password và refresh-token revocation sau reset.
- Test host vẫn dùng environment `Testing`, SQLite in-memory, fake email sender trong bộ nhớ và role seed tối thiểu cho Identity; không dùng LocalDB hoặc SMTP thật.
- Không thay đổi endpoint, production auth behavior hoặc database schema.

Không thay đổi:

- Không tạo migration.
- Không thêm API CRUD, Flutter code, UI redesign, AI, social feature hoặc public sharing.

## 32. Cập nhật Phase 16E: Privacy and Ownership Integration Tests

Phase 16E đã hoàn thành.

- Đã thêm 6 integration tests privacy/ownership, nâng tổng test suite lên 16 tests.
- Test phủ memory, album, forged album-memory relation, private image endpoint, soft-deleted image, trash restore, settings, guest redirect và Admin không bypass ownership.
- Mọi test vẫn chạy trong environment `Testing` với SQLite in-memory, fake email sender và MVC cookie login thật; không dùng LocalDB hoặc SMTP thật.
- Không phát hiện privacy/security bug cần sửa production.

Không thay đổi:

- Không thay đổi database schema hoặc tạo migration.
- Không thay đổi controller, auth behavior, UI, API endpoint, image upload behavior, Flutter code, AI, social feature hoặc public sharing.

## 33. Cập nhật Phase 17A: UI/UX Redesign Direction

Phase 17A đã hoàn thành dưới dạng tài liệu định hướng thiết kế.

- Đã tạo `docs/UI_UX_REDESIGN_DIRECTION.md` để chốt hướng redesign trước khi sửa giao diện lớn.
- Tài liệu ghi nhận UI hiện tại đã functional, mobile-first hơn trước, nhưng vẫn còn cảm giác Bootstrap/generic và chưa đủ cảm xúc cho một nhật ký ký ức riêng tư.
- Hướng redesign đề xuất là soft journal aesthetic: warm neutral background, photo-first memory cards, typography hỗ trợ tiếng Việt tốt, form thân thiện hơn và layout ít dashboard-like hơn.
- Roadmap UI được chia nhỏ thành Phase 17B đến 17G để tránh redesign toàn bộ trong một commit lớn.

Không thay đổi:

- Không sửa Razor view, CSS hoặc UI implementation trong phase này.
- Không thay đổi backend, auth, API, database schema hoặc migrations.
- Không thêm API CRUD, Flutter code, AI, social feature hoặc public sharing.

## 34. Cập nhật Phase 17B: Design System Tokens and Base CSS

Phase 17B đã hoàn thành dưới dạng nền styling chung.

- Đã cập nhật `wwwroot/css/site.css` với design tokens rõ hơn cho font stack, background, surface/card, text, muted text, border, accent, danger, radius, shadow và spacing.
- Base typography, body background, link, button, form control, select, card, alert, navbar, footer và các panel/card hiện có được chuẩn hóa theo hướng soft journal aesthetic.
- Font vẫn dùng web-safe/system stack, không thêm font file và không import remote font.
- Touch target, footer normal flow, navbar mobile và no-horizontal-overflow tiếp tục được giữ làm nguyên tắc mobile-first.
- Page-by-page redesign vẫn là future work từ Phase 17C đến 17G.

Không thay đổi:

- Không redesign sâu Home/Auth/Timeline/Albums/Trash/Settings trong phase này.
- Không thay đổi controller, route, form POST behavior, auth, API, database schema hoặc migrations.
- Không thêm API CRUD, Flutter code, AI, social feature hoặc public sharing.
