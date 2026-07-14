# MemoLens - Flutter Roadmap

## Mục đích

Flutter là mobile client riêng cho MemoLens. Client này dùng JWT API đã được QA ở backend commit `5e28d36`; không thay thế MVC web app và không thay đổi product direction private-first.

## Phase 19A - Flutter Foundation

- Tạo app tại `mobile/memolens_app` với Android và iOS source.
- Dùng Material 3, Riverpod, go_router, Dio và `flutter_secure_storage`.
- Có Paper Note theme foundation, Splash, Login placeholder, Home placeholder và health diagnostics Development.
- Đọc `API_BASE_URL` bằng `--dart-define`; Android emulator mặc định dùng `http://10.0.2.2:5296`.
- Chưa lưu token, chưa có Bearer interceptor, chưa có gọi private API ngoài health check.
- Android toolchain Windows đã được xác thực: Android SDK API 36, Build Tools 36.0.0, Platform Tools, Emulator, NDK 28.2.13676358 và JBR của Android Studio. `flutter doctor`, analyze, test và debug APK đều đã chạy thành công.

## Phase 19B - JWT Authentication

- Đã triển khai Register, Login, resend confirmation, account/me, refresh rotation và Logout theo API contract đã khóa.
- Access/refresh token chỉ được lưu trong `flutter_secure_storage`; không lưu token ở SharedPreferences hoặc route.
- Bearer interceptor dùng một refresh operation chung cho các `401` đồng thời, retry request đúng một lần và không refresh auth endpoint.
- Splash khôi phục phiên; backend offline giữ token và hiển thị trạng thái thử lại thay vì xóa phiên.
- Riverpod là nguồn auth state duy nhất; go_router bảo vệ `/home` và tránh user đã đăng nhập quay lại Login/Register.
- Confirmation link hiện vẫn mở MVC web; chưa có mobile deep link. Forgot/reset password UI được hoãn dù backend đã có API.
- Không log hoặc hiển thị token, password hay Authorization header.
- Chi tiết: `mobile/memolens_app/docs/AUTHENTICATION.md`.

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
