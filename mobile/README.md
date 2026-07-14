# MemoLens Flutter Mobile

Ứng dụng Flutter cho MemoLens, một nhật ký ảnh riêng tư. Đây không phải mạng xã hội: không có feed công khai, likes, comments, followers, public profiles hay chia sẻ công khai.

## Trạng thái Phase 19B

Flutter dùng các JWT API đã được QA và khóa contract tại backend commit `5e28d36`.

- Flutter: `3.38.7` stable
- Dart: `3.10.7`
- Nền tảng tạo sẵn: Android và iOS
- Android build/debug cần Android SDK được cấu hình trên máy.
- iOS source được tạo sẵn nhưng chỉ có thể build trên macOS với Xcode.

Đã có:

- Login, Register, confirmation-required/resend và Logout.
- Session restore qua Splash và `GET /api/v1/account/me`.
- Access/refresh token trong `flutter_secure_storage`.
- Bearer interceptor, refresh-token rotation và single-flight concurrent `401`.
- Route guard cho Splash/Login/Register/Confirm Email/Home.
- Retry state khi backend offline mà không tự xóa token có thể còn hợp lệ.

Chưa có Timeline, Memory CRUD, upload/display ảnh, Album, mobile confirm-email deep link hoặc forgot/reset password UI. Chi tiết auth: [memolens_app/docs/AUTHENTICATION.md](memolens_app/docs/AUTHENTICATION.md).

## Cấu trúc

```text
memolens_app/
  lib/app/                 theme, router, composition root
  lib/core/                config, Dio client, secure storage, widgets chung
  lib/features/            Splash, authentication và Home phiên đăng nhập
  test/                    unit, interceptor, controller và widget tests
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

Ứng dụng khai báo quyền Android `INTERNET`. Phase 19B đã smoke-test login, session restore, offline retry và logout trên AVD `MemoLens_API_36` với backend Development.

### Android toolchain đã xác thực

Phase 19A đã xác thực Android toolchain trên Windows bằng Android Studio và Flutter doctor:

- Android SDK Platform `android-36`, Build Tools `36.0.0`, Platform Tools và Android Emulator.
- NDK side-by-side `28.2.13676358` theo yêu cầu của dependency native hiện tại.
- JBR đi kèm Android Studio được Flutter dùng làm JDK.
- Tất cả Android SDK licenses đã được chấp nhận.

Sau khi mở terminal hoặc IDE mới, kiểm tra lại bằng:

```bash
flutter doctor -v
flutter doctor --android-licenses
flutter build apk --debug
```

## Kiểm tra

```bash
dart format --set-exit-if-changed .
flutter analyze
flutter test
flutter build apk --debug
flutter pub outdated
```

Automated tests không cần backend thật. Khi chạy app, Splash xác thực session với backend; lỗi mạng hiển thị trạng thái thử lại và không xóa token chỉ vì server tạm thời offline.

## Phases tiếp theo

- Phase 19C: Timeline và Memory CRUD.
- Phase 19D: Private image upload/display.
- Phase 19E: Albums.

Settings và Trash sẽ được làm sau khi backend có API riêng cho các flow đó.
