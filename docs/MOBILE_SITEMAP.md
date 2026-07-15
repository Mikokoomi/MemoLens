# Mobile Sitemap

Legend: **Implemented** = có Flutter route/screen; **Planned** = backend hoặc roadmap đã có nhưng Flutter chưa làm; **Proposed** = wireframe/định hướng chưa phê duyệt; **Open decision** = cần chủ sở hữu dự án chốt.

## Routes hiện có

| Screen | Status | Flutter route | Entry / exit | Auth | API/data | State và privacy |
| --- | --- | --- | --- | --- | --- | --- |
| Splash/session restore | Implemented | `/` | app launch -> Login hoặc Timeline | Token nếu có | `GET /api/v1/account/me`, refresh | Loading, unavailable/retry; không xoá token khi backend tạm offline |
| Login | Implemented | `/login` | guest -> Timeline/Register | Guest | `POST /api/v1/auth/login` | Validation/error; email chưa xác nhận bị chặn |
| Register | Implemented | `/register` | Login -> confirm email | Guest | `POST /api/v1/auth/register` | Validation/error; không phát token |
| Email confirmation | Implemented | `/confirm-email` | Register -> Login | Guest | resend API; confirm link còn mở MVC web | Pending/error/resend |
| Timeline | Implemented | `/home` | root -> details/create/logout | JWT | `GET /api/v1/memories` | Loading/empty/error; chỉ Memory của user |
| Create Memory | Implemented | `/memories/create` | Timeline -> details/Timeline | JWT | `POST /api/v1/memories`, image upload | Validation, partial-success retry/continue |
| Memory details | Implemented | `/memories/:id` | Timeline -> edit/back | JWT | `GET /api/v1/memories/{id}` | Loading/error/404; owner only |
| Edit Memory | Implemented | `/memories/:id/edit` | Details -> details | JWT | `PUT /api/v1/memories/{id}` | Validation/partial image retry |
| Private image gallery | Implemented trong details/form | Không có route riêng | Details/form | JWT | image content/delete endpoints | Byte memory only; owner only |
| Soft delete / restore Memory | Implemented qua actions | Không có route riêng | Details/Timeline | JWT | `DELETE /api/v1/memories/{id}`, `POST .../restore` | Deleted item không xuất hiện normal API |
| Albums list/details/create/edit | Planned Flutter | No confirmed route | Navigation decision | JWT | API Album đã có | Empty/loading/error cần thiết; owner only |
| Trash / restore | Planned Flutter | No confirmed route | Navigation decision | JWT | No confirmed mobile Trash endpoint | API Memory/Album restore có; danh sách Trash API chưa xác nhận |
| Settings/Profile/logout | Logout implemented; screen Planned | No confirmed route | menu/root nav | JWT | logout API; account/me | Account/security screen chưa làm |
| Forgot/reset password | Planned Flutter | No confirmed route | Login | Guest | Backend auth endpoints đã có | Không có Flutter UI/deep link flow |
| Session expired | Implemented behavior | Redirect to `/login` | protected request/session restore | JWT | refresh/logout | Xoá token và báo state an toàn |

## Navigation - REQUIRES USER DECISION

| Option | Cấu trúc | Ưu điểm | Nhược điểm / tác động | Phù hợp |
| --- | --- | --- | --- | --- |
| A | Timeline / Albums / Trash / Settings | Rõ chức năng, dễ học | Create là FAB; 4 tabs khi Albums/Trash/Settings chưa có | Phù hợp sau 19E |
| B | Timeline / Albums / Create action / Trash / Settings | Create luôn dễ chạm | 5 mục chật trên màn nhỏ, create không phải destination thường | Cần prototype/accessibility QA |
| C | Timeline root; Albums/Trash/Settings qua menu phụ | Tập trung nhật ký, ít chrome | Cần thêm thao tác để vào chức năng phụ | Phù hợp private journal MVP |

Không chọn option nào trong phase này. Bottom navigation và placement Albums/Trash/Settings vẫn là **Open decision**.

## Quy tắc trạng thái chung

Mỗi screen private cần có loading, empty, API error và unavailable state; offline không được ngụ ý có queue/offline sync. User A không bao giờ thấy dữ liệu User B; `404` được dùng để tránh lộ tồn tại của private resource.
