# MemoLens - Flutter Roadmap

## Mục đích

Flutter là mobile client riêng cho MemoLens. Client này dùng JWT API đã được QA ở backend commit `5e28d36`; không thay thế MVC web app và không thay đổi product direction private-first.

## Phase 19A - Flutter Foundation

- Tạo app tại `mobile/memolens_app` với Android và iOS source.
- Dùng Material 3, Riverpod, go_router, Dio và `flutter_secure_storage`.
- Có Paper Note theme foundation, Splash, Login placeholder, Home placeholder và health diagnostics Development.
- Đọc `API_BASE_URL` bằng `--dart-define`; Android emulator mặc định dùng `http://10.0.2.2:5296`.
- Chưa lưu token, chưa có Bearer interceptor, chưa có gọi private API ngoài health check.

## Phase 19B - JWT Authentication

- Register, confirm email, resend confirmation, login, refresh, logout, forgot/reset password và account/me theo API contract đã khóa.
- Lưu access/refresh token chỉ trong secure storage.
- Thêm Bearer interceptor và refresh-token rotation an toàn.
- Không log hoặc hiển thị token.

## Phase 19C - Timeline và Memory CRUD

- Timeline phân trang, search, filter, detail, create, update, soft delete và restore.
- Mọi request scope theo user hiện tại qua JWT.
- Không thêm feed công khai hay social interactions.

## Phase 19D - Private Image Upload/Display

- Upload multipart field `files` theo giới hạn backend.
- Hiển thị ảnh qua authorized content endpoint với Bearer token.
- Không dùng URL ảnh public hoặc lộ private storage path.

## Phase 19E - Albums

- List/detail/create/update/soft delete/restore Album.
- Batch add và remove Memory membership theo API hiện có.
- Không triển khai public sharing.

## Sau 19E

- Settings và Trash chỉ bắt đầu khi API backend tương ứng được thiết kế, triển khai và QA.
- Sau MVP có thể cân nhắc thumbnails/compression, export data và permanent delete theo quyết định privacy riêng.
