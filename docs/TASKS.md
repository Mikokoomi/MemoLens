# MemoLens - Development Tasks

## Phase 1: Project setup

- [ ] Tạo project ASP.NET MVC mới.
- [ ] Kiểm tra project chạy được ở local.
- [ ] Cấu hình Git repository.
- [ ] Tạo file `.gitignore` phù hợp cho ASP.NET.
- [ ] Cài đặt Bootstrap hoặc xác nhận Bootstrap đã có sẵn.
- [ ] Tạo cấu trúc thư mục cơ bản cho Controllers, Models, Views và wwwroot.
- [ ] Tạo layout chung cho ứng dụng.
- [ ] Thêm navigation cơ bản cho Login, Register, Timeline và Albums.

## Phase 2: Database and models

- [ ] Tạo connection string cho SQL Server.
- [ ] Tạo model User.
- [ ] Tạo model Memory.
- [ ] Tạo model MemoryImage.
- [ ] Tạo model Tag.
- [ ] Tạo model MemoryTag.
- [ ] Tạo model Album.
- [ ] Tạo model AlbumMemory.
- [ ] Cấu hình DbContext.
- [ ] Cấu hình quan hệ một-nhiều giữa User và Memory.
- [ ] Cấu hình quan hệ một-nhiều giữa Memory và MemoryImage.
- [ ] Cấu hình quan hệ nhiều-nhiều giữa Memory và Tag.
- [ ] Cấu hình quan hệ nhiều-nhiều giữa Album và Memory.
- [ ] Tạo migration đầu tiên.
- [ ] Chạy migration để tạo database.
- [ ] Kiểm tra database được tạo đúng bảng.

## Phase 3: Authentication

- [ ] Tạo màn hình Register.
- [ ] Xử lý đăng ký tài khoản mới.
- [ ] Hash mật khẩu trước khi lưu.
- [ ] Tạo màn hình Login.
- [ ] Xử lý đăng nhập.
- [ ] Xử lý đăng xuất.
- [ ] Bảo vệ các trang cần đăng nhập.
- [ ] Đảm bảo user chỉ xem được dữ liệu của chính mình.
- [ ] Hiển thị thông báo lỗi đăng nhập hoặc đăng ký.

## Phase 4: Memory CRUD

- [ ] Tạo controller cho Memory.
- [ ] Tạo action hiển thị danh sách memory của user.
- [ ] Tạo action hiển thị form tạo memory.
- [ ] Xử lý lưu memory mới.
- [ ] Tạo action hiển thị chi tiết memory.
- [ ] Tạo action hiển thị form chỉnh sửa memory.
- [ ] Xử lý cập nhật memory.
- [ ] Tạo action xác nhận xóa memory.
- [ ] Xử lý xóa memory.
- [ ] Kiểm tra user không thể mở memory của user khác.

## Phase 5: Image upload

- [ ] Tạo thư mục upload ảnh trong wwwroot.
- [ ] Thiết kế input upload một hoặc nhiều ảnh.
- [ ] Kiểm tra định dạng file ảnh hợp lệ.
- [ ] Kiểm tra kích thước file ảnh.
- [ ] Lưu file ảnh vào thư mục uploads.
- [ ] Lưu đường dẫn ảnh vào bảng MemoryImages.
- [ ] Hiển thị ảnh trong memory detail.
- [ ] Hiển thị ảnh đại diện trong timeline.
- [ ] Cho phép xóa ảnh khỏi memory khi chỉnh sửa.
- [ ] Xử lý khi memory chưa có ảnh.

## Phase 6: Timeline UI

- [ ] Tạo giao diện timeline cho danh sách memory.
- [ ] Sắp xếp memory theo MemoryDate hoặc CreatedAt.
- [ ] Hiển thị tiêu đề, ngày, mood và ảnh đại diện.
- [ ] Thêm trạng thái empty state khi user chưa có memory.
- [ ] Thêm nút tạo memory rõ ràng.
- [ ] Tối ưu giao diện timeline trên mobile.
- [ ] Làm giao diện ấm áp, riêng tư và không giống mạng xã hội.

## Phase 7: Albums and tags

- [ ] Tạo controller cho Album.
- [ ] Tạo màn hình danh sách album.
- [ ] Tạo form tạo album.
- [ ] Xử lý lưu album mới.
- [ ] Tạo màn hình chi tiết album.
- [ ] Cho phép thêm memory vào album.
- [ ] Cho phép gỡ memory khỏi album.
- [ ] Tạo hoặc chọn tag khi tạo memory.
- [ ] Hiển thị tag trong memory detail.
- [ ] Đảm bảo album và tag thuộc về đúng user.

## Phase 8: Search/filter

- [ ] Tạo form tìm kiếm memory.
- [ ] Tìm kiếm theo tiêu đề.
- [ ] Tìm kiếm theo nội dung story hoặc note.
- [ ] Lọc theo ngày.
- [ ] Lọc theo mood.
- [ ] Lọc theo tag.
- [ ] Kết hợp nhiều bộ lọc đơn giản.
- [ ] Hiển thị kết quả tìm kiếm.
- [ ] Thêm nút xóa bộ lọc.
- [ ] Kiểm tra kết quả chỉ thuộc user đang đăng nhập.

## Phase 9: Privacy/settings

- [ ] Tạo màn hình Settings.
- [ ] Hiển thị thông tin tài khoản cơ bản.
- [ ] Cho phép cập nhật tên hiển thị.
- [ ] Tạo form đổi mật khẩu.
- [ ] Thêm ý tưởng privacy lock cơ bản.
- [ ] Ghi rõ dữ liệu là riêng tư theo mặc định.
- [ ] Chuẩn bị định hướng export data trong tương lai.
- [ ] Chuẩn bị định hướng delete account/data trong tương lai.

## Phase 10: UI polish and demo preparation

- [ ] Kiểm tra toàn bộ flow: register, login, create memory, upload image, view timeline.
- [ ] Kiểm tra edit và delete memory.
- [ ] Kiểm tra album và tag.
- [ ] Kiểm tra search/filter.
- [ ] Làm giao diện nhất quán bằng Bootstrap.
- [ ] Thêm nội dung demo mẫu nếu cần.
- [ ] Viết hướng dẫn chạy project.
- [ ] Chụp màn hình demo cho portfolio.
- [ ] Kiểm tra lỗi validation cơ bản.
- [ ] Chuẩn bị phần thuyết trình về mục tiêu, database và tính năng MVP.

