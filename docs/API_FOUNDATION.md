# MemoLens - API Foundation

## 1. Mục đích

Phase 13A đã thêm lớp nền tảng Web API đầu tiên cho MemoLens để chuẩn bị cho tích hợp Flutter/mobile trong tương lai.

Mục tiêu của phase này là tạo một cấu trúc API nhỏ, rõ ràng, có version và có định dạng response thống nhất. API foundation hiện tại chưa thay thế MVC web app, chưa thêm mobile auth, chưa thêm CRUD API, và chưa thay đổi luồng đăng nhập bằng cookie của ứng dụng web hiện tại.

MemoLens vẫn là ứng dụng nhật ký ảnh và kể chuyện ký ức riêng tư. API trong tương lai phải tiếp tục phục vụ mục tiêu private-first, không biến sản phẩm thành mạng xã hội.

## 2. Endpoint API hiện tại

Endpoint đã có:

```text
GET /api/v1/health
```

Endpoint này dùng để kiểm tra backend API có hoạt động hay không.

Response trả về JSON gồm:

- `success`: cho biết request thành công hay không.
- `message`: thông điệp ngắn cho client hoặc tester.
- `data.appName`: tên ứng dụng, hiện là `MemoLens`.
- `data.apiVersion`: version API, hiện là `v1`.
- `data.environment`: môi trường đang chạy, ví dụ `Development`.
- `data.serverTimeUtc`: thời gian UTC hiện tại của server.

Ví dụ:

```json
{
  "success": true,
  "message": "API MemoLens đang hoạt động.",
  "data": {
    "appName": "MemoLens",
    "apiVersion": "v1",
    "environment": "Development",
    "serverTimeUtc": "2026-07-10T01:49:51.1397429Z"
  }
}
```

## 3. Định dạng response API

MemoLens bắt đầu dùng các model response chuẩn cho API để sau này Flutter/mobile xử lý dữ liệu dễ hơn.

### ApiResponse

`ApiResponse` dùng cho response cơ bản không cần dữ liệu chi tiết.

Trường chính:

- `success`: `true` hoặc `false`.
- `message`: thông điệp ngắn, có thể dùng để hiển thị lỗi hoặc trạng thái.

### ApiResponse<T>

`ApiResponse<T>` dùng khi response có dữ liệu trả về.

Trường chính:

- `success`: `true` hoặc `false`.
- `message`: thông điệp ngắn.
- `data`: dữ liệu kiểu `T`, ví dụ object health, danh sách memories, album detail trong tương lai.

Ví dụ response thành công:

```json
{
  "success": true,
  "message": "Lấy dữ liệu thành công.",
  "data": {
    "id": 1,
    "title": "Một buổi chiều yên tĩnh"
  }
}
```

### ApiValidationErrorResponse

`ApiValidationErrorResponse` dùng cho lỗi validation.

Trường chính:

- `success`: thường là `false`.
- `message`: mô tả lỗi ngắn.
- `errors`: danh sách lỗi theo từng field.

Ví dụ validation error:

```json
{
  "success": false,
  "message": "Dữ liệu gửi lên chưa hợp lệ.",
  "errors": {
    "title": [
      "Tiêu đề là bắt buộc."
    ],
    "memoryDate": [
      "Ngày kỷ niệm không hợp lệ."
    ]
  }
}
```

## 4. Swagger/OpenAPI

Swagger/OpenAPI chỉ được bật trong môi trường `Development`.

URL khi chạy local ở Development:

```text
/swagger
```

Thông tin Swagger hiện tại:

- Title: `MemoLens API`
- Version: `v1`
- Description: `API nền tảng cho ứng dụng mobile MemoLens.`

Swagger chỉ phục vụ phát triển và kiểm thử API. Không nên xem Swagger là giao diện người dùng chính, và không nên bật công khai trong môi trường production nếu chưa có cấu hình bảo mật phù hợp.

## 5. Giới hạn hiện tại

API foundation hiện tại còn rất nhỏ và có chủ đích.

Chưa có:

- Auth API.
- JWT hoặc token-based auth.
- Memory CRUD API.
- Album CRUD API.
- Image upload API.
- Trash API.
- Settings API.
- Flutter app.

MVC web app hiện tại vẫn là giao diện chính cho MVP/demo. Các tính năng đăng nhập, memories, albums, private images, trash và settings vẫn chạy qua MVC như trước.

## 6. Hướng API tương lai

Các nhóm endpoint dự kiến cho mobile/Flutter:

- `/api/v1/auth`
- `/api/v1/memories`
- `/api/v1/images`
- `/api/v1/albums`
- `/api/v1/trash`
- `/api/v1/settings`

Thứ tự triển khai nên bám theo `docs/API_ROADMAP.md`, đặc biệt cần chốt hướng mobile authentication trước khi thêm auth API thật.

## 7. Quy tắc riêng tư cho API tương lai

Mọi API trong tương lai phải giữ các quy tắc private-first của MemoLens:

- Mỗi API riêng tư phải scope theo current authenticated user.
- User chỉ được truy cập memories, albums, images, trash và settings của chính mình.
- Admin không được bypass private content ownership trong MVP hiện tại.
- Không có public image URLs.
- Không trả physical file path hoặc private storage path ra response.
- Không expose password hash, security stamp, role internals hoặc dữ liệu Identity nhạy cảm.
- Memories và albums đã soft delete phải bị ẩn khỏi các normal APIs.
- Missing, unauthorized hoặc soft-deleted private content nên trả `404 NotFound` khi phù hợp để tránh tiết lộ item có tồn tại hay không.

API của MemoLens phải hỗ trợ ký ức riêng tư, không hỗ trợ public feed, likes, comments, followers, public profiles, public sharing hoặc AI trong MVP hiện tại.
