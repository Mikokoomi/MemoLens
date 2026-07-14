# MemoLens - Private Memory Image API

## 1. Mục đích

Phase 18C bổ sung API ảnh riêng tư cho ứng dụng Flutter/mobile. API dùng JWT Bearer, lưu file ngoài `wwwroot` và chỉ cho người sở hữu kỷ niệm tải lên, xem hoặc xóa ảnh.

Endpoint MVC `/Images/MemoryImage/{id}` vẫn được giữ nguyên cho ứng dụng web dùng cookie. API mobile dùng URL riêng và không thay đổi hành vi MVC hiện có.

## 2. Xác thực và quyền sở hữu

Mọi endpoint trong tài liệu này yêu cầu:

```http
Authorization: Bearer {accessToken}
```

- Thiếu hoặc sai token: `401 Unauthorized`.
- Kỷ niệm/ảnh không tồn tại, đã bị xóa mềm hoặc thuộc user khác: `404 Not Found`.
- Role `Admin` không được bypass ownership của dữ liệu riêng tư trong MVP.
- Response không trả `UserId`, `ImagePath`, đường dẫn vật lý hoặc tên file được sinh trên server.

## 3. Upload ảnh

```http
POST /api/v1/memories/{memoryId}/images
Content-Type: multipart/form-data
```

Tên field multipart là `files` và có thể lặp lại để gửi nhiều ảnh.

Quy tắc:

- Phải có ít nhất một file.
- Chấp nhận `.jpg`, `.jpeg`, `.png`, `.webp`.
- Tối đa 5 MB cho mỗi file.
- Tổng số ảnh của một kỷ niệm tối đa 10.
- Toàn bộ batch được validate trước khi ghi file.
- Nếu một file không hợp lệ, không file/row mới nào trong batch được lưu và ảnh hiện có không bị thay đổi.
- Khi lỗi lưu file hoặc database, transaction được rollback và các file đã ghi trong batch được dọn.
- File vật lý dùng tên GUID an toàn dưới `App_Data/uploads/memories/{userId}/{memoryId}`.

Response thành công trả `201 Created`:

```json
{
  "success": true,
  "message": "Tải ảnh lên thành công.",
  "data": {
    "images": [
      {
        "id": 31,
        "originalFileName": "hoang-hon.jpg",
        "uploadedAt": "2026-07-14T08:00:00Z",
        "contentUrl": "/api/v1/images/31/content"
      }
    ],
    "totalImageCount": 3,
    "remainingSlots": 7
  }
}
```

Validation trả `400 Bad Request` theo `ApiValidationErrorResponse`, với lỗi nằm trong key `files`.

## 4. Xem nội dung ảnh

```http
GET /api/v1/images/{imageId}/content
```

Endpoint stream bytes với MIME tương ứng (`image/jpeg`, `image/png` hoặc `image/webp`) và gửi cache policy:

```http
Cache-Control: private, no-store
Pragma: no-cache
```

Endpoint không dùng download filename và không để lộ đường dẫn storage. File database thiếu, file vật lý thiếu, memory đã xóa hoặc cross-owner đều trả `404` an toàn.

Khi memory được restore, ảnh còn tồn tại sẽ truy cập lại được qua cùng URL.

## 5. Xóa một ảnh

```http
DELETE /api/v1/memories/{memoryId}/images/{imageId}
```

- Chỉ xóa đúng row và file của ảnh được yêu cầu.
- Không thay đổi các ảnh khác trong kỷ niệm.
- File vật lý đã thiếu không cản việc xóa row.
- Gọi lại sau khi xóa trả `404`.
- Cross-owner và Admin truy cập ảnh của user khác nhận `404`.

## 6. URL ảnh trong Memory API

`GET /api/v1/memories/{id}` trả URL JWT API:

```json
{
  "id": 31,
  "originalFileName": "hoang-hon.jpg",
  "uploadedAt": "2026-07-14T08:00:00Z",
  "contentUrl": "/api/v1/images/31/content"
}
```

Mobile client phải gửi Bearer token khi gọi `contentUrl`. Không dùng URL MVC cookie cho mobile.

## 7. Swagger và giới hạn hiện tại

Trong Development, Swagger tại `/swagger` hiển thị multipart upload, JWT Bearer và response types. Request upload có giới hạn endpoint 55 MB để đủ cho tối đa mười file 5 MB cùng multipart overhead; giới hạn business vẫn là 5 MB mỗi file và 10 ảnh mỗi memory.

Chưa có thumbnail, compression, image decoding, cloud storage, resumable upload hoặc cleanup job tự động. Không có public image URL hay public sharing.

## 8. Kiểm thử

`MemoryImageApiIntegrationTests` dùng SQLite in-memory và upload root tạm riêng, không dùng LocalDB hay `App_Data/uploads` Development.

Test bao phủ JWT, owner/User B/Admin isolation, bốn extension hợp lệ, upload nhiều file, giới hạn size/count, mixed batch atomicity, content bytes/MIME/cache, missing/deleted/restored state, xóa row/file, missing physical file và URL JWT trong Memory API.

```bash
dotnet test
```

Không có migration hoặc thay đổi schema trong Phase 18C.

Phase đề xuất tiếp theo: **Phase 18D - Private Album CRUD API**.
