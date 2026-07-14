# MemoLens - Memory CRUD API

## Mục đích

Phase 18B thêm API JSON cho Flutter/mobile làm việc với các kỷ niệm riêng tư. API dùng JWT Bearer, giữ nguyên MVC hiện có và không có chức năng mạng xã hội.

Mọi endpoint chỉ làm việc với dữ liệu của người dùng đang đăng nhập. User khác, kể cả role `Admin`, nhận `404 Not Found` khi thử truy cập một kỷ niệm không thuộc về mình.

## Xác thực

```http
Authorization: Bearer {accessToken}
```

Bearer token thiếu hoặc không hợp lệ trả `401 Unauthorized`. Swagger trong Development có thể dùng Bearer token để thử API.

## Endpoints

| Method | Endpoint | Mô tả |
| --- | --- | --- |
| GET | `/api/v1/memories` | Danh sách kỷ niệm có phân trang và filter. |
| GET | `/api/v1/memories/{id}` | Chi tiết kỷ niệm thuộc current user. |
| POST | `/api/v1/memories` | Tạo kỷ niệm text/tags, không nhận ảnh. |
| PUT | `/api/v1/memories/{id}` | Cập nhật thông tin và thay thế tags. |
| DELETE | `/api/v1/memories/{id}` | Xóa mềm. |
| POST | `/api/v1/memories/{id}/restore` | Khôi phục item đã xóa mềm. |

## Danh sách và filter

`GET /api/v1/memories` hỗ trợ `page`, `pageSize`, `search`, `feeling`, `tag`, `from`, `to`, `year`, `month`, `sort`.

- `page` mặc định `1`; số nhỏ hơn `1` được chuẩn hoá thành `1`.
- `pageSize` mặc định `20`, tối đa `100`; giá trị không hợp lệ dùng mặc định.
- `search` tìm trong tiêu đề, câu chuyện, địa điểm và tag.
- `feeling` phải là Feeling tiếng Việt hợp lệ.
- `tag` là tên tag chính xác.
- `from/to` có ưu tiên hơn `year/month`.
- `month` không kèm `year` được bỏ qua, giống MVC.
- `sort` là `newest` (mặc định) hoặc `oldest`.

Phân trang dùng `Skip/Take` trong database, không tải toàn bộ timeline vào bộ nhớ trước.

```json
{
  "success": true,
  "message": "Lấy danh sách kỷ niệm thành công.",
  "data": {
    "items": [
      {
        "id": 12,
        "title": "Buổi chiều yên bình",
        "shortStoryPreview": "Một câu chuyện riêng tư.",
        "feeling": "Bình yên",
        "memoryDate": "2026-07-01T00:00:00",
        "location": "Hà Nội",
        "tags": ["du lịch"],
        "imageCount": 2,
        "coverImageId": 31,
        "createdAt": "2026-07-01T08:00:00Z",
        "updatedAt": "2026-07-01T08:00:00Z"
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

## Tạo và cập nhật

`POST /api/v1/memories` và `PUT /api/v1/memories/{id}` dùng body:

```json
{
  "title": "Buổi chiều yên bình",
  "story": "Một câu chuyện riêng tư.",
  "feeling": "Bình yên",
  "memoryDate": "2026-07-01",
  "location": "Hà Nội",
  "tags": ["du lịch", "gia đình"]
}
```

`title`, `feeling` và `memoryDate` là bắt buộc. Giới hạn: title 120, story 4000, feeling 50, location 200 và mỗi tag 50 ký tự. Tags được trim, bỏ rỗng, dedupe không phân biệt hoa/thường và tái sử dụng `Tag` có sẵn khi phù hợp.

Create trả `201 Created` và Location header. Update không thay owner hoặc ảnh hiện có.

## Chi tiết, ảnh và riêng tư

Chi tiết trả tags và metadata ảnh an toàn:

```json
{
  "id": 31,
  "originalFileName": "hoang-hon.jpg",
  "uploadedAt": "2026-07-01T08:00:00Z",
  "contentUrl": "/Images/MemoryImage/31"
}
```

Không có `UserId`, `ImagePath`, đường dẫn vật lý, password hash hay token trong response. `contentUrl` là endpoint ảnh riêng tư hiện có. API upload/quản lý ảnh cho mobile được để lại cho Phase 18C.

## Xóa mềm và khôi phục

Delete chỉ cập nhật `IsDeleted`, `DeletedAt` và `UpdatedAt`; không xóa ảnh vật lý. Item bị xóa không có trong list/detail thường. Restore đưa item trở lại timeline và khôi phục quyền xem ảnh qua endpoint riêng tư. Delete lặp lại, update item đã xóa hoặc restore item chưa xóa đều trả `404`.

## Lỗi chuẩn

Validation trả `400` theo format `ApiValidationErrorResponse`:

```json
{
  "success": false,
  "message": "Dữ liệu gửi lên chưa hợp lệ.",
  "errors": {
    "feeling": ["Cảm xúc không hợp lệ."]
  }
}
```

Không có migration hay thay đổi schema trong Phase 18B.

## Kiểm thử

`MemoryApiIntegrationTests` chạy với SQLite in-memory, kiểm tra JWT bắt buộc, validation, owner/admin isolation, filter/pagination, response privacy, update, soft delete, restore và private image access sau restore.

```bash
dotnet test
```

Phase đề xuất tiếp theo: **Phase 18C - Memory Image API cho mobile**.
