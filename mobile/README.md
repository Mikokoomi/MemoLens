# MemoLens Flutter Mobile

Ứng dụng Flutter cho MemoLens, một nhật ký ảnh riêng tư. Đây không phải mạng xã hội: không có feed công khai, likes, comments, followers, public profiles hay chia sẻ công khai.

## Phase 19A

Phase này chỉ tạo foundation Flutter để dùng các JWT API đã được QA và khóa contract tại backend commit `5e28d36`.

- Flutter: `3.38.7` stable
- Dart: `3.10.7`
- Nền tảng tạo sẵn: Android và iOS
- Android build/debug cần Android SDK được cấu hình trên máy.
- iOS source được tạo sẵn nhưng chỉ có thể build trên macOS với Xcode.

Chưa có đăng nhập JWT, token thật, Timeline, CRUD kỷ niệm, upload ảnh, album hay dữ liệu mẫu. Đăng nhập thuộc Phase 19B.

## Cấu trúc

```text
memolens_app/
  lib/app/                 theme, router, composition root
  lib/core/                config, Dio client, secure storage, widgets chung
  lib/features/            Splash và các placeholder feature
  test/                    unit và widget test foundation
```

Dependencies runtime tối thiểu:

- `flutter_riverpod`
- `go_router`
- `dio`
- `flutter_secure_storage`
- `intl`

## Chạy local

Từ thư mục `mobile/memolens_app`:

```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5296
```

### API_BASE_URL

`API_BASE_URL` được đọc bằng `String.fromEnvironment`. Không commit URL production, secret, access token hoặc refresh token.

| Môi trường | URL local |
| --- | --- |
| Android emulator | `http://10.0.2.2:5296` |
| iOS simulator | `http://127.0.0.1:5296` |
| Thiết bị thật | `http://<LAN-IP-của-máy-dev>:5296` |

Ví dụ Android thiết bị thật:

```bash
flutter run --dart-define=API_BASE_URL=http://192.168.1.10:5296
```

Backend ASP.NET Core phải chạy trước. Với thiết bị thật, máy phát triển cần lắng nghe trên LAN interface phù hợp và Windows Firewall cần cho phép truy cập cục bộ. Không mở backend ra Internet chỉ để phát triển mobile.

Android Debug có network security config hẹp cho local HTTP `10.0.2.2`; Release không nhận cấu hình này. Cần Android SDK/Android Studio để chạy emulator hoặc build APK.

## Kiểm tra

```bash
dart format --set-exit-if-changed .
flutter analyze
flutter test
flutter build apk --debug
flutter pub outdated
```

Health check Development gọi `GET /api/v1/health` từ Login placeholder. Khi backend offline, app hiển thị thông báo an toàn thay vì crash. Health check không tạo session và không thay thế luồng auth.

## Phases tiếp theo

- Phase 19B: JWT authentication, token interceptor và refresh-token flow.
- Phase 19C: Timeline và Memory CRUD.
- Phase 19D: Private image upload/display.
- Phase 19E: Albums.

Settings và Trash sẽ được làm sau khi backend có API riêng cho các flow đó.
