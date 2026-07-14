# MemoLens - Mobile API Final QA

## 1. Mục đích

Phase 18E là cổng QA cuối cho backend API mobile hiện có trước khi bắt đầu Flutter. Phạm vi review gồm API auth, tài khoản hiện tại, Memory, Memory Image và Album. Phase này không thêm endpoint hay tính năng sản phẩm mới.

Mục tiêu chính:

- Xác nhận JWT Bearer và MVC cookie không bị trộn lẫn.
- Xác nhận mọi dữ liệu riêng tư luôn được scope theo current user.
- Kiểm tra response không lộ secret, owner key hoặc đường dẫn file private.
- Kiểm tra status code, validation, pagination, route/method và Swagger ổn định cho mobile client.
- Chạy regression test và một luồng Development E2E bằng hai tài khoản độc lập.

Baseline được review:

- `28dacef` - Memory CRUD API.
- `f065e5e` - Private Memory Image API.
- `49dafd6` - Private Album CRUD API.
- 82 automated tests trước Phase 18E.

## 2. Phạm vi API đã khóa QA

### Auth và tài khoản

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/confirm-email`
- `POST /api/v1/auth/resend-confirmation-email`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/account/me`

### Memory và ảnh

- `GET/POST /api/v1/memories`
- `GET/PUT/DELETE /api/v1/memories/{id}`
- `POST /api/v1/memories/{id}/restore`
- `POST /api/v1/memories/{memoryId}/images`
- `GET /api/v1/images/{imageId}/content`
- `DELETE /api/v1/memories/{memoryId}/images/{imageId}`

### Album

- `GET/POST /api/v1/albums`
- `GET/PUT/DELETE /api/v1/albums/{id}`
- `POST /api/v1/albums/{id}/restore`
- `POST /api/v1/albums/{id}/memories`
- `DELETE /api/v1/albums/{id}/memories/{memoryId}`

## 3. Ma trận xác thực

Kết quả review và test:

| Trường hợp | Kết quả mong đợi | Kết quả |
| --- | --- | --- |
| Không có Bearer token | `401` JSON | Đạt |
| JWT sai định dạng | `401` JSON | Đạt |
| JWT sai chữ ký | `401` JSON | Đạt |
| JWT hết hạn | `401` JSON | Đạt |
| JWT hợp lệ nhưng thiếu user id claim | `401` JSON | Đạt |
| Chỉ có MVC cookie gọi API private | `401` JSON | Đạt |
| MVC cookie truy cập trang MVC private | Hoạt động như cũ | Đạt |
| API login tạo MVC cookie | Không | Đạt |

MVC Identity cookie vẫn là scheme mặc định cho web app. API controller private tiếp tục khai báo rõ `JwtBearerDefaults.AuthenticationScheme`.

## 4. Privacy và ownership

Các automated tests hiện có và test Phase 18E xác nhận:

- User A chỉ xem/sửa/xóa/restore Memory và Album của A.
- User B nhận `404` khi thử truy cập Memory, Image hoặc Album của A.
- Role `Admin` không bypass ownership của nội dung riêng tư trong MVP.
- Album membership chỉ nhận Memory cùng owner và chưa bị xóa mềm.
- Deleted Memory không xuất hiện trong timeline hoặc Album details.
- Ảnh của deleted Memory trả `404`; restore Memory khôi phục authorized image access.
- Deleted Album trả `404`; restore Album giữ nguyên membership.
- Batch upload và batch add Album validate toàn bộ trước khi ghi, không để lại dữ liệu một phần khi request thất bại.

## 5. Kiểm tra dữ liệu nhạy cảm

Response của account, Memory và Album đã được kiểm tra không chứa:

- `UserId` hoặc owner key nội bộ.
- `PasswordHash`, `SecurityStamp`.
- refresh-token hash, replacement hash.
- `ImagePath`, `App_Data`, đường dẫn vật lý hoặc public upload URL.
- stack trace, tên exception hoặc HTML error page trong các lỗi API được kiểm tra.

Ảnh vẫn chỉ được phục vụ qua `/api/v1/images/{imageId}/content` sau khi xác thực owner. Response ảnh dùng `private, no-store`.

## 6. Contract và lỗi HTTP

- Validation model và malformed JSON trả `400` JSON.
- Unsupported content type trả `415`, không trả HTML.
- Sai HTTP method trả `405`.
- Route id sai định dạng hoặc resource không thuộc owner trả `404`.
- Không có Bearer token hoặc token không hợp lệ trả `401` JSON.
- Search/filter/date/sort/page/pageSize hiện được normalize hoặc reject ổn định theo từng contract đã công bố.
- Swagger chỉ bật trong Development, có Bearer security scheme và mô tả multipart upload.

### Defect được phát hiện và sửa

QA phát hiện phép tính offset của Album API có thể tràn `int`:

- `page=int.MaxValue&pageSize=50` có thể quay về dữ liệu trang đầu trên SQLite.
- Guard ban đầu cũng bị tràn khi `pageSize=1`, làm trang hợp lệ trả sai dữ liệu.

Fix tối thiểu:

- Tính maximum page bằng `long` trước khi truyền offset vào EF.
- Áp dụng cùng phép tính an toàn cho Memory và Album list/details.
- Không đổi response DTO, route, filter, sort hoặc schema.

## 7. Automated test

Phase 18E thêm `MobileApiFinalQaIntegrationTests` với 8 test mới:

- JWT malformed, invalid signature, expired và missing user claim.
- Ranh giới MVC cookie/Bearer.
- Status code và response không lộ implementation details.
- Pagination biên cho Memory và Album.
- Refresh token hết hạn.
- Sensitive response field audit.
- Swagger Development-only, Bearer scheme, endpoint paths và multipart upload.

Kết quả cuối:

```text
Passed: 90
Failed: 0
Skipped: 0
```

Test host tiếp tục dùng environment `Testing`, SQLite in-memory, fake email sender và upload root tạm. Automated tests không dùng LocalDB Development hoặc thư mục ảnh thật.

## 8. Development E2E smoke

Đã chạy app trong Development với hai tài khoản tạm A/B:

1. Register và xác nhận email cho A/B bằng confirmation link trong development log.
2. Login A/B và gọi `account/me`.
3. A tạo Memory, upload PNG, tạo Album và thêm Memory vào Album.
4. A xem timeline, Album details và bytes ảnh thành công.
5. B thử xem Memory, Image và Album của A: tất cả trả `404`.
6. A soft-delete Memory: image content trả `404`; restore Memory: ảnh trả `200`.
7. A soft-delete Album: details trả `404`; restore Album thành công.
8. Refresh rotation thành công; reuse refresh token cũ trả `401`.
9. Logout revoke refresh token mới; refresh lại trả `401`.
10. Tài khoản, database rows, token và file ảnh smoke đã được dọn sau kiểm thử.

## 9. Kết quả verification

- `dotnet build`: thành công, 0 warning, 0 error.
- `dotnet test`: 90/90 test passing.
- `dotnet list package --vulnerable --include-transitive`: không có package dễ tổn thương theo nguồn audit hiện tại.
- `dotnet ef migrations list`: 5 migration hiện có, không tạo migration mới.
- `dotnet ef migrations has-pending-model-changes`: không có model change chờ migration.
- `/api/v1/health`: `200` JSON trong Development.
- `/swagger/v1/swagger.json`: hoạt động trong Development và không mở trong Testing.

## 10. Giới hạn còn lại

- Chưa có rate limiting cho API auth.
- Chưa có automated load test hoặc multi-process concurrency test.
- Chưa có production email provider được cấu hình sẵn.
- Chưa có permanent delete API.
- Chưa có mobile Trash API và Settings API riêng; Flutter phase đầu chỉ nên dùng contract đã được khóa trong tài liệu này.
- Chưa có Flutter client và secure token storage phía mobile.

## 11. Kết luận và bước tiếp theo

API auth, Memory, private image và Album hiện đủ ổn định để **freeze contract cho Flutter foundation**. Mọi thay đổi contract sau mốc này cần cập nhật OpenAPI/tài liệu và bổ sung regression test trước khi merge.

Bước tiếp theo đề xuất: **Phase 19A - Flutter Foundation**, gồm cấu trúc project, environment configuration, HTTP client, model response chung và secure token storage; chưa cần triển khai toàn bộ UI/CRUD trong một phase.
