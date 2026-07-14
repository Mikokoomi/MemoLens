# MemoLens - Private Album API

## 1. Mục đích

Phase 18D bổ sung API JSON cho ứng dụng Flutter/mobile quản lý các bộ sưu tập riêng tư. API dùng JWT Bearer, giữ nguyên ứng dụng MVC hiện có và không thêm tính năng chia sẻ công khai hoặc mạng xã hội.

## 2. Xác thực và quyền sở hữu

Mọi endpoint yêu cầu header:

```http
Authorization: Bearer {accessToken}
```

- Thiếu hoặc sai token trả `401 Unauthorized`.
- Album không tồn tại, đã bị xóa mềm hoặc thuộc user khác trả `404 Not Found`.
- Role `Admin` không được bypass quyền sở hữu nội dung riêng tư trong MVP.
- Response không trả `UserId`, `CoverImagePath`, `ImagePath` hoặc đường dẫn file vật lý.
- Ảnh bìa chỉ dùng URL có kiểm tra JWT: `/api/v1/images/{imageId}/content`.

## 3. Endpoints

| Method | Endpoint | Mô tả |
| --- | --- | --- |
| GET | `/api/v1/albums` | Danh sách album của current user. |
| GET | `/api/v1/albums/{id}` | Chi tiết album và danh sách memory có phân trang. |
| POST | `/api/v1/albums` | Tạo album chưa có membership. |
| PUT | `/api/v1/albums/{id}` | Cập nhật tiêu đề và mô tả. |
| DELETE | `/api/v1/albums/{id}` | Xóa mềm album. |
| POST | `/api/v1/albums/{id}/restore` | Khôi phục album đã xóa mềm. |
| POST | `/api/v1/albums/{id}/memories` | Thêm batch memory vào album. |
| DELETE | `/api/v1/albums/{id}/memories/{memoryId}` | Gỡ một memory khỏi album. |

API chưa có endpoint `available-memories`; mobile client có thể lấy danh sách từ Memory API và đối chiếu membership hiện tại.

## 4. Danh sách album

```http
GET /api/v1/albums?page=1&pageSize=20&search=du%20lịch&sort=newest
```

Query hỗ trợ:

- `page`: mặc định `1`; giá trị nhỏ hơn `1` được chuẩn hóa thành `1`.
- `pageSize`: mặc định `20`, tối đa `100`.
- `search`: tìm trong tiêu đề và mô tả.
- `sort`: `newest` mặc định, `oldest` hoặc `name`.

Phân trang dùng `Count`, `Skip` và `Take` trong database. Mỗi item trả `memoryCount`, `coverImageId` và `coverImageUrl` nếu tìm thấy file ảnh bìa hợp lệ. Memory đã xóa mềm không được tính và không được dùng làm ảnh bìa.

```json
{
  "success": true,
  "message": "Lấy danh sách bộ sưu tập thành công.",
  "data": {
    "items": [
      {
        "id": 7,
        "title": "Những chuyến đi",
        "description": "Các hành trình đáng nhớ.",
        "memoryCount": 2,
        "coverImageId": 31,
        "coverImageUrl": "/api/v1/images/31/content",
        "createdAt": "2026-07-14T08:00:00Z",
        "updatedAt": "2026-07-14T08:30:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

## 5. Chi tiết album

```http
GET /api/v1/albums/{id}?page=1&pageSize=20
```

`memories` là một `PagedResponse` riêng. Memory được sắp xếp ổn định theo `MemoryDate` mới nhất, sau đó `CreatedAt` và `Id`. Mỗi summary gồm tiêu đề, preview câu chuyện, feeling, ngày, địa điểm, tags, số ảnh, ảnh bìa và thời điểm được thêm vào album.

Memory đã xóa mềm vẫn giữ membership trong database nhưng bị ẩn khỏi `memoryCount`, ảnh bìa và danh sách chi tiết.

## 6. Tạo và cập nhật album

Tạo album:

```http
POST /api/v1/albums
Content-Type: application/json

{
  "title": "Những chuyến đi",
  "description": "Các hành trình đáng nhớ."
}
```

- `title` bắt buộc, được trim và tối đa 100 ký tự.
- `description` tùy chọn, được trim và tối đa 500 ký tự.
- Owner lấy từ JWT, không nhận từ request.
- Create không nhận `memoryIds`; membership được quản lý bằng endpoint riêng.
- Thành công trả `201 Created` và header `Location`.

Update dùng cùng hai field qua `PUT /api/v1/albums/{id}`. Update không thay đổi membership.

## 7. Thêm memory theo batch

```http
POST /api/v1/albums/{id}/memories
Content-Type: application/json

{
  "memoryIds": [12, 13, 13]
}
```

Quy tắc:

- Danh sách không được rỗng.
- ID trùng được gộp trước khi xử lý.
- Tất cả memory phải thuộc current user và chưa bị xóa mềm.
- Toàn bộ batch được kiểm tra trước khi tạo membership.
- Chỉ một ID thiếu, đã xóa hoặc thuộc user khác cũng làm toàn batch trả `404`; không có membership nào được thêm một phần.
- Memory đã có trong album là no-op an toàn.
- Gửi lại cùng batch không tạo row trùng vì `AlbumMemory` có composite key.

## 8. Gỡ memory

```http
DELETE /api/v1/albums/{id}/memories/{memoryId}
```

Endpoint chỉ xóa row `AlbumMemory`. Memory, tags, ảnh và file vật lý không bị xóa. Gọi lại sau khi membership đã bị gỡ trả `404`.

## 9. Xóa mềm và khôi phục

`DELETE /api/v1/albums/{id}` chỉ cập nhật `IsDeleted`, `DeletedAt` và `UpdatedAt`:

- Không xóa memory.
- Không xóa ảnh hoặc file ảnh.
- Không xóa membership.
- Album đã xóa bị ẩn khỏi list/details và lần delete tiếp theo trả `404`.

`POST /api/v1/albums/{id}/restore` khôi phục album và giữ nguyên membership. Memory còn hoạt động xuất hiện lại; memory vẫn đang trong thùng rác tiếp tục bị ẩn.

Permanent delete chưa được triển khai.

## 10. Lỗi và giới hạn hiện tại

- Validation trả `400 Bad Request` theo `ApiValidationErrorResponse`.
- Không tìm thấy hoặc không có quyền trả `404` chung để tránh dò dữ liệu riêng tư.
- Chưa có permanent delete Album API.
- Chưa có endpoint trash list riêng cho mobile.
- Chưa có cover ảnh do người dùng chọn thủ công; ảnh bìa vẫn được suy ra tự động từ membership còn hiển thị.
- Chưa có Flutter client và chưa có UI mobile native.

## 11. Kiểm thử

`AlbumApiIntegrationTests` dùng SQLite in-memory và private upload root tạm, không dùng LocalDB hoặc ảnh Development. Test bao phủ JWT, owner/Admin isolation, create/update, validation, search/sort/pagination, safe cover URL, detail pagination, batch dedupe/idempotency/rollback, remove membership, soft delete và restore.
