# Xác thực Flutter MemoLens

## Mục đích

Phase 19B kết nối ứng dụng Flutter với JWT API đã được khóa ở backend commit `5e28d36`. Flutter chỉ quản lý phiên của tài khoản hiện tại; không thay đổi MVC cookie authentication, API contract, database hoặc quyền sở hữu dữ liệu riêng tư.

## API contract đang dùng

| Mục đích | Endpoint | Request chính | Kết quả |
| --- | --- | --- | --- |
| Đăng ký | `POST /api/v1/auth/register` | `displayName`, `email`, `password`, `confirmPassword` | Thông báo thành công, không cấp token |
| Đăng nhập | `POST /api/v1/auth/login` | `email`, `password`, `deviceName` tùy chọn | Access token, refresh token, thời hạn và user summary |
| Làm mới phiên | `POST /api/v1/auth/refresh` | `refreshToken` | Cặp access/refresh token mới và user summary |
| Đăng xuất | `POST /api/v1/auth/logout` | `refreshToken` | Thu hồi refresh token hiện tại |
| User hiện tại | `GET /api/v1/account/me` | Bearer access token | `id`, `email`, `displayName`, `roles` |
| Gửi lại xác nhận | `POST /api/v1/auth/resend-confirmation-email` | `email` | Thông báo trung tính |
| Xác nhận email | `POST /api/v1/auth/confirm-email` | `userId`, `token` | Xác nhận email nếu token hợp lệ |
| Quên mật khẩu | `POST /api/v1/auth/forgot-password` | `email` | Thông báo trung tính |
| Đặt lại mật khẩu | `POST /api/v1/auth/reset-password` | `email`, `token`, `password`, `confirmPassword` | Đặt lại mật khẩu, không cấp token |

API dùng envelope chuẩn `success`, `message`, `data` và `errors`. Access token có thời hạn 15 phút; refresh token có thời hạn 30 ngày và được lưu dạng hash ở backend.

## Bootstrap phiên

1. Splash đọc access token và refresh token từ secure storage.
2. Không có token thì chuyển sang Login.
3. Có đủ token thì gọi `GET /api/v1/account/me` để xác thực phiên và lấy user hiện tại.
4. Access token hết hạn thì thực hiện đúng một lần refresh.
5. Refresh thành công sẽ lưu cả cặp token mới rồi vào Home.
6. Refresh token thiếu, hết hạn, bị thu hồi hoặc không hợp lệ sẽ xóa phiên local và về Login.
7. Backend tạm thời không kết nối được sẽ giữ token, hiển thị trạng thái có thể thử lại và không tự xóa phiên có thể vẫn hợp lệ.

## Lưu token an toàn

- Access token và refresh token chỉ được lưu qua `flutter_secure_storage`.
- Không dùng SharedPreferences, file rõ hoặc route query parameter cho token.
- Khi login/refresh thành công, hai token được thay thế như một cặp; nếu ghi lỗi, storage cố gắng khôi phục cặp trước đó.
- Xóa phiên chỉ xóa hai khóa token của MemoLens và xóa user đang giữ trong bộ nhớ.
- Presentation không được đọc hoặc hiển thị token.

## Bearer và refresh rotation

`AuthInterceptor` là điểm trung tâm gắn `Authorization: Bearer <accessToken>` cho protected request. Health và toàn bộ endpoint `/api/v1/auth/...` được loại trừ để tránh gắn token sai chỗ hoặc tạo refresh đệ quy.

Khi protected request nhận `401`:

1. Repository dùng một shared in-flight future để chỉ chạy một refresh.
2. Các request nhận `401` đồng thời chờ cùng kết quả.
3. Cặp token đã rotation được lưu trước khi retry.
4. Request gốc được retry tối đa một lần, giữ method, query và body mà Dio đang quản lý.
5. Request refresh, login, register hoặc request đã retry không được refresh lại.
6. Refresh không hợp lệ xóa session; lỗi mạng giữ token để người dùng có thể thử lại.

## Riverpod và routing

`AuthController` là nguồn trạng thái duy nhất với các trạng thái `initializing`, `unauthenticated`, `authenticating`, `authenticated`, `registrationPendingConfirmation`, `temporarilyUnavailable` và `failure`.

Các route hiện có:

- `/`: Splash/bootstrap.
- `/login`: đăng nhập.
- `/register`: đăng ký.
- `/confirm-email`: hướng dẫn xác nhận và gửi lại email.
- `/home`: trang phiên đã xác thực; Timeline được hoãn sang Phase 19C.

Guest không vào được Home; user đã đăng nhập không ở lại Login/Register. Router chỉ phản ứng theo `AuthController` để tránh nhiều nguồn session và redirect loop.

## Email confirmation

Register không tự đăng nhập và không tạo token giả. Sau khi đăng ký, app hướng dẫn người dùng kiểm tra email và có thể gửi lại yêu cầu bằng response trung tính.

Flow backend hiện tại tạo confirmation link mở MVC `Account/ConfirmEmail`. Phase 19B không thêm mobile deep link: người dùng xác nhận trên web, quay lại app rồi đăng nhập. API confirm-email đã tồn tại nhưng chưa có deep-link UI tương thích để nhận `userId` và `token` trong app.

## Logout

App gửi refresh token hiện tại tới endpoint logout để backend thu hồi. Dù backend offline, token local và user in-memory vẫn bị xóa, vì đăng xuất local là hành vi bắt buộc. Router sau đó đưa người dùng về Login.

## Chính sách log và lỗi

- Không có interceptor log toàn request/response.
- Không log Authorization, password, access token, refresh token hoặc auth response body.
- Model token không đưa giá trị token vào `toString`.
- UI chỉ hiển thị thông báo tiếng Việt an toàn cho credentials sai, email chưa xác nhận, validation, session hết hạn, timeout, backend unavailable và response sai định dạng.
- Backend chưa có stable error code riêng cho trạng thái email chưa xác nhận; client ưu tiên HTTP status và chỉ nhận diện thông báo confirmation hiện có ở boundary API.

## Giới hạn hiện tại

- Chưa có forgot/reset password UI trong Flutter; endpoint backend đã có.
- Chưa có mobile deep link cho confirm email.
- Chưa có social login, biometric, guest, passwordless hoặc offline login.
- Chưa có Timeline, Memory CRUD, ảnh hoặc Album trong Flutter.
- Manual Android integration cần backend Development và emulator/device; automated tests không gọi backend thật.

## Kết quả Android smoke Phase 19B

AVD `MemoLens_API_36` đã chạy APK debug với backend tại `http://10.0.2.2:5296`. Login sau xác nhận email, session restore qua secure storage, backend-offline retry và logout qua restart đều pass. Android manifest khai báo quyền `INTERNET`; cấu hình cleartext local chỉ áp dụng cho host emulator trong Debug.

Manual smoke không chờ đủ access-token lifetime 15 phút. Refresh rotation và reuse boundary được xác nhận bằng test tự động; tài khoản và refresh token smoke đã được dọn khỏi LocalDB.
## Phase 19C.1 - Khoi dong phien Android

- Da sua Splash co the bi giu lai o route `/` sau khi AuthController da ket thuc bootstrap voi trang thai `unauthenticated`. Router nay chuyen guest sang Login thay vi giu Splash.
- Flutter secure storage tren Android API 36 co the can migration cipher sau du lieu cu. Token storage doc ca cap token bang mot thao tac `readAll`, dung Android encrypted-storage options va tat Android backup cho app de tranh khoi phuc state ma hoa cu khong tuong thich.
- Bootstrap session co gioi han 10 giay. Loi secure storage hoac backend timeout chuyen sang trang thai retry an toan, khong xoa token chi vi backend tam thoi khong ket noi duoc.
- Regression tests bao phu storage rong, storage loi, timeout, retry, valid session, router Splash, logout va doi tai khoan. Khong log access token, refresh token hay mat khau.
- Android smoke da xac nhan cai moi thoat Splash sang Login; phien hop le khoi phuc vao Timeline sau khi mo lai app.

## Phase 19D.1 private image requests

Private image content remains a protected Dio request, not an image URL. Regression tests explicitly cover a Bearer header for `/api/v1/images/{id}/content` and simultaneous image `401` responses sharing the existing single-flight refresh. Android QA also confirmed that logging out before signing in as another account did not reveal the earlier account's private image or Timeline state.
