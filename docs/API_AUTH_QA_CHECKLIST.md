# MemoLens - Checklist QA và bảo mật API Auth

## 1. Mục đích

Tài liệu này ghi lại kết quả review bảo mật và kiểm thử hồi quy cho API xác thực được triển khai trong Phase 14C. Mục tiêu là xác nhận JWT/refresh token hoạt động đúng, không làm thay đổi luồng đăng nhập bằng cookie của MVC, và không làm lộ dữ liệu riêng tư của người dùng.

Checklist được thực hiện ngày 10/07/2026 trên môi trường `Development`, .NET 8, SQL Server LocalDB và database local đã áp dụng migration `AddUserRefreshTokens`.

Kết luận của Phase 14C.5:

- Không phát hiện lỗi bảo mật cụ thể cần sửa trong source code hiện tại.
- Không thêm endpoint hoặc tính năng mới.
- Không thay đổi MVC cookie authentication.
- Không thay đổi database schema và không tạo migration.
- Build, NuGet vulnerability audit, kiểm tra EF model và smoke test đều đạt.

## 2. Các endpoint API auth hiện có

| Method | Endpoint | Mục đích | Xác thực |
| --- | --- | --- | --- |
| `POST` | `/api/v1/auth/register` | Tạo Identity user, gán role `User` và gửi email xác nhận | Anonymous |
| `POST` | `/api/v1/auth/login` | Kiểm tra email/mật khẩu và cấp access/refresh token | Anonymous |
| `POST` | `/api/v1/auth/refresh` | Rotate refresh token và cấp cặp token mới | Refresh token trong JSON body |
| `POST` | `/api/v1/auth/logout` | Revoke refresh token hiện tại | Refresh token trong JSON body |
| `GET` | `/api/v1/account/me` | Trả thông tin tối thiểu của user hiện tại | JWT Bearer bắt buộc |

Register không cấp token. Login chỉ cấp token sau khi email đã được xác nhận.

## 3. Kết quả review bảo mật

| # | Nội dung kiểm tra | Kết quả | Ghi chú |
| --- | --- | --- | --- |
| 1 | MVC cookie auth vẫn giữ nguyên | Đạt | Identity cookie vẫn là scheme mặc định cho MVC. |
| 2 | JWT Bearer không làm hỏng MVC login/register/logout | Đạt | Smoke test MVC thành công trước và sau API auth. |
| 3 | API login không tạo MVC cookie | Đạt | Response login không có header `Set-Cookie`. |
| 4 | `account/me` yêu cầu JWT Bearer rõ ràng | Đạt | Controller dùng `Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)`. |
| 5 | Không commit production JWT secret thật | Đạt | Chỉ có placeholder Development; production phải dùng environment variable hoặc user secrets. |
| 6 | Access token có lifetime 15 phút | Đạt | Response trả `900` giây; JWT thực tế hết hạn sau khoảng 15 phút. |
| 7 | JWT chỉ chứa claim cần thiết | Đạt | Có user id (`sub` và `NameIdentifier`), email, role, `jti` và metadata chuẩn `iss/aud/exp`; không có dữ liệu riêng tư. |
| 8 | Không có claim nhạy cảm | Đạt | Không có password hash, security stamp, refresh token, file path hoặc nội dung ký ức. |
| 9 | Refresh token chỉ lưu dạng hash | Đạt | Database lưu SHA-256 Base64 dài 44 ký tự; không tìm thấy plain token. |
| 10 | Refresh token hash không được trả ra API | Đạt | Client chỉ nhận plain refresh token mới tại login/refresh; `TokenHash` và `ReplacedByTokenHash` không xuất hiện trong response. |
| 11 | Rotation revoke token cũ | Đạt | Token cũ có `RevokedAt` và liên kết tới hash token thay thế. |
| 12 | Reuse token cũ thất bại | Đạt | Trả `401 Unauthorized`. |
| 13 | Logout revoke refresh token | Đạt | Token sau logout không thể refresh. |
| 14 | Token hết hạn hoặc đã revoke bị từ chối | Đạt | Cả hai trường hợp trả `401 Unauthorized`. |
| 15 | Register không cấp token | Đạt | Response register không có access token hoặc refresh token. |
| 16 | Login bắt buộc email đã xác nhận | Đạt | Tài khoản chưa xác nhận nhận `401` với thông báo phù hợp. |
| 17 | Invalid login dùng lỗi an toàn | Đạt | Email không tồn tại và mật khẩu sai cùng trả thông báo chung. |
| 18 | Password/access/refresh token không bị log ngoài response dự kiến | Đạt có lưu ý | Development email sender cố ý log confirmation link để test local; không được dùng cơ chế này ở production. |
| 19 | Validation message là tiếng Việt có dấu | Đạt | Data annotations và API validation response đều dùng tiếng Việt có dấu. |
| 20 | `account/me` chỉ trả current user summary | Đạt | Chỉ có `id`, `email`, `displayName`, `roles`. |
| 21 | Không expose Identity internals hoặc dữ liệu riêng tư | Đạt | Không trả password hash, security stamp, refresh hash, paths, memories, albums hoặc images. |
| 22 | Không có secret thật trong Git | Đạt | Kiểm tra config/source chỉ thấy Development placeholder và tên biến cấu hình. |
| 23 | Không nhận token qua query string | Đạt | Chỉ Bearer header được chấp nhận; `access_token` query string nhận `401`. |
| 24 | NuGet vulnerability audit sạch | Đạt | Không phát hiện package dễ bị tổn thương từ các source hiện tại. |
| 25 | Không có pending migration/model changes | Đạt | EF báo model không thay đổi kể từ migration gần nhất. |

### Lưu ý về confirmation link trong Development

`DevelopmentEmailSender` ghi confirmation link ra console/log để phục vụ test local. Link này chứa email confirmation token, vì vậy:

- Chỉ dùng log này trong môi trường Development trên máy local.
- Không chia sẻ log và không commit log vào Git.
- Không triển khai `DevelopmentEmailSender` như email provider production.
- Trước private beta cần email provider thật và cấu hình không log confirmation token.

Access token, refresh token và password không được ghi vào log bởi auth controller hoặc token service.

## 4. Test case thủ công và status code mong đợi

| ID | Test case | Kết quả mong đợi | Kết quả Phase 14C.5 |
| --- | --- | --- | --- |
| AUTH-01 | Register payload hợp lệ | `200 OK`, tạo user, không trả token | Đạt |
| AUTH-02 | Register payload không hợp lệ | `400 Bad Request`, có lỗi theo field | Đạt qua cấu hình validation |
| AUTH-03 | Login khi email chưa xác nhận | `401 Unauthorized` | Đạt |
| AUTH-04 | Login bằng email/mật khẩu hợp lệ đã xác nhận | `200 OK`, trả access/refresh token | Đạt |
| AUTH-05 | Login với email không tồn tại | `401 Unauthorized`, thông báo chung | Đạt |
| AUTH-06 | Login với mật khẩu sai | `401 Unauthorized`, cùng thông báo AUTH-05 | Đạt |
| AUTH-07 | Gọi `account/me` không có token | `401 Unauthorized` | Đạt |
| AUTH-08 | Gọi `account/me` bằng Bearer token hợp lệ | `200 OK`, đúng user hiện tại | Đạt |
| AUTH-09 | Gửi access token qua query string | `401 Unauthorized` | Đạt |
| AUTH-10 | Refresh bằng token hợp lệ | `200 OK`, trả cặp token mới | Đạt |
| AUTH-11 | Dùng lại refresh token cũ sau rotation | `401 Unauthorized` | Đạt |
| AUTH-12 | Refresh bằng token đã logout/revoke | `401 Unauthorized` | Đạt |
| AUTH-13 | Refresh bằng token hết hạn | `401 Unauthorized` | Đạt |
| AUTH-14 | Logout bằng refresh token hợp lệ | `200 OK` | Đạt |
| AUTH-15 | API login kiểm tra header cookie | Không có `Set-Cookie` của MVC | Đạt |
| API-01 | `GET /api/v1/health` | `200 OK`, JSON `success: true` | Đạt |
| API-02 | Mở `/swagger` trong Development | Swagger UI tải được định nghĩa MemoLens API v1 | Đạt |

## 5. Kiểm thử refresh token rotation

Flow bắt buộc:

1. Login thành công và nhận `refreshTokenA`.
2. Gửi `refreshTokenA` tới `/api/v1/auth/refresh`.
3. Server trả `refreshTokenB`, khác `refreshTokenA`.
4. Record của token A được đặt `RevokedAt` và `ReplacedByTokenHash`.
5. Dùng lại token A phải trả `401`.
6. Logout bằng token B phải trả `200` và revoke token B.
7. Dùng token B để refresh sau logout phải trả `401`.

Kết quả thực tế:

- Hai token được lưu thành hai hash khác nhau.
- Cả token cũ sau rotation và token mới sau logout đều có trạng thái revoked.
- Không có plain refresh token nào trùng với giá trị trong cột `TokenHash`.
- Atomic conditional update tiếp tục chặn việc rotate cùng một token cũ nhiều lần.

## 6. Kiểm thử hồi quy MVC

| Test | Kết quả mong đợi | Kết quả |
| --- | --- | --- |
| Mở Home khi chưa đăng nhập | Trang Home tải bình thường | Đạt |
| Mở Login | Form đăng nhập MVC hiển thị | Đạt |
| Từ Login mở Register | Trang Register hiển thị | Đạt |
| Guest mở `/Memories` | `302` tới `/Account/Login?ReturnUrl=/Memories` | Đạt |
| Login MVC bằng tài khoản đã xác nhận | Tạo cookie MVC và vào timeline `/Memories` | Đạt |
| Logout MVC | Xóa phiên MVC và quay về Home | Đạt |
| API login sau đó | Không tạo cookie MVC | Đạt |

Kết luận: JWT Bearer API và Identity cookie MVC đang cùng tồn tại đúng vai trò, không thay thế hoặc làm thay đổi nhau.

## 7. Ghi chú bảo mật

- JWT signing secret Development là placeholder, không phải production secret.
- Production phải cấp `Jwt__Issuer`, `Jwt__Audience` và `Jwt__SecretKey` bằng environment variables hoặc user secrets; secret tối thiểu 32 byte.
- Access token chỉ được gửi trong header `Authorization: Bearer ...`.
- Refresh token chỉ được gửi trong JSON body của refresh/logout.
- Refresh token có 512 bit entropy ngẫu nhiên trước khi Base64Url encode; SHA-256 hash phù hợp để tra cứu token ngẫu nhiên có entropy cao.
- Login API dùng Identity password/lockout checks và không gọi `PasswordSignInAsync`, do đó không tạo MVC cookie.
- Logout API có tính idempotent: token sai, hết hạn hoặc đã revoke không làm lộ trạng thái token và vẫn trả response logout chung.
- `account/me` không cho Admin đọc account khác; endpoint chỉ dựa trên identity trong Bearer token hiện tại.
- Admin vẫn không được bypass quyền sở hữu memories, albums, images hoặc trash trong MVP.

## 8. Giới hạn đã biết

- Chưa có API confirm email.
- Chưa có API resend confirmation email.
- Chưa có API forgot password/reset password.
- Chưa có rate limiting cho register/login/refresh.
- Chưa có production email provider.
- Development email sender vẫn dùng console/log cho confirmation link local.
- Chưa tự động revoke toàn bộ refresh token khi đổi/reset mật khẩu.
- Chưa có quản lý device sessions.
- Chưa có automated integration tests.
- Chưa có Flutter client.

## 9. Phase tiếp theo được đề xuất

Phase tiếp theo nên là **Phase 14D: Confirm Email và Resend Confirmation API**.

Phase 14D cần giữ các nguyên tắc:

- Không cấp token trước khi email được xác nhận.
- Resend confirmation phải dùng response chung để giảm user enumeration.
- Thêm rate limiting trước private beta.
- Không log confirmation token trong production.
- Tiếp tục giữ MVC cookie auth không đổi.
- Không thêm API CRUD memories/albums/images trong cùng phase.
