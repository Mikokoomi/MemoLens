# MemoLens Paper Note Static Prototype

Đây là bộ prototype HTML/CSS tĩnh cho hướng giao diện Paper Note của MemoLens.

Prototype này không ảnh hưởng app ASP.NET MVC thật, không dùng Razor, không gọi backend, không đổi database, auth, API hoặc CSS thật của ứng dụng.

## Cách xem

Mở trực tiếp các file HTML trong trình duyệt:

- `home.html`: Trang giới thiệu.
- `login.html`: Trang đăng nhập.
- `register.html`: Trang tạo tài khoản.
- `timeline.html`: Dòng thời gian kỷ niệm.
- `create-memory.html`: Tạo kỷ niệm.
- `edit-memory.html`: Chỉnh sửa kỷ niệm.
- `memory-details.html`: Chi tiết kỷ niệm.
- `albums.html`: Danh sách bộ sưu tập.
- `album-details.html`: Chi tiết bộ sưu tập.
- `create-album.html`: Tạo bộ sưu tập.
- `trash.html`: Thùng rác.
- `settings.html`: Cài đặt.

## Mục tiêu thiết kế

- Warm paper / scrapbook feeling.
- Mobile-first ở khoảng 390px.
- Desktop có sidebar gọn khoảng 220px.
- Font display dùng chọn lọc cho heading, body font dễ đọc tiếng Việt.
- Timeline card dùng ảnh 4:3, tránh ảnh dọc làm card quá cao.
- Không có feed công khai, like, comment, follower, public profile, public sharing hoặc AI.
