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

## 9. Phase tiếp theo được đề xuất

- Phase 10: Settings & Privacy.
- Phase 11: MVP QA / Hardening.
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
