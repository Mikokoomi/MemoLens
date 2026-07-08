# MemoLens - MVP Features

## 1. Mục tiêu phiên bản MVP

MVP là phiên bản đầu tiên của MemoLens, chỉ tập trung vào các chức năng cốt lõi để người dùng có thể đăng nhập, tạo kỷ niệm với ảnh, xem lại theo dòng thời gian và quản lý dữ liệu cá nhân.

Phiên bản này cần đơn giản, dễ hiểu, dễ demo và phù hợp với một dự án học tập.

## 2. Tính năng có trong MVP

### 2.1. Register/Login

- Người dùng có thể đăng ký tài khoản mới.
- Người dùng có thể đăng nhập và đăng xuất.
- Mỗi người dùng chỉ nhìn thấy kỷ niệm của chính mình.

### 2.2. Create memory with photos

- Người dùng có thể tạo một kỷ niệm mới.
- Một kỷ niệm có thể có một hoặc nhiều ảnh.
- Ảnh được upload vào thư mục lưu trữ của ứng dụng.
- Cơ sở dữ liệu chỉ lưu đường dẫn ảnh, không lưu trực tiếp file ảnh dạng binary.

### 2.3. Add title, note/story, mood, date, location

Khi tạo kỷ niệm, người dùng có thể nhập:

- Tiêu đề.
- Ghi chú hoặc câu chuyện.
- Tâm trạng.
- Ngày của kỷ niệm.
- Địa điểm.
- Thẻ liên quan nếu có.

### 2.4. View memories in timeline

- Người dùng có thể xem các kỷ niệm theo dòng thời gian.
- Kỷ niệm mới hoặc gần nhất có thể hiển thị trước.
- Mỗi mục trên timeline nên hiển thị ảnh đại diện, tiêu đề, ngày và tâm trạng.

### 2.5. View memory detail

- Người dùng có thể mở một kỷ niệm để xem chi tiết.
- Màn hình chi tiết hiển thị đầy đủ ảnh, tiêu đề, câu chuyện, tâm trạng, ngày, địa điểm, thẻ và album liên quan.

### 2.6. Edit/delete memory

- Người dùng có thể chỉnh sửa thông tin của kỷ niệm.
- Người dùng có thể xóa kỷ niệm.
- Khi xóa kỷ niệm, cần xử lý hợp lý các ảnh và liên kết liên quan.

### 2.7. Filter/search by date, mood, tag

- Người dùng có thể tìm kiếm hoặc lọc kỷ niệm theo ngày.
- Người dùng có thể lọc theo tâm trạng.
- Người dùng có thể lọc theo thẻ.
- Tìm kiếm có thể bắt đầu đơn giản bằng tiêu đề hoặc nội dung ghi chú.

### 2.8. Basic privacy lock idea

- Ứng dụng cần có định hướng bảo vệ riêng tư ngay từ đầu.
- MVP có thể bắt đầu bằng yêu cầu đăng nhập trước khi xem dữ liệu.
- Có thể thêm ý tưởng khóa riêng tư cơ bản trong phần Settings, ví dụ: bật/tắt chế độ yêu cầu xác nhận trước khi xem kỷ niệm.
- Chức năng khóa nâng cao có thể để sau MVP.

## 3. Tính năng không đưa vào MVP

Các tính năng sau không thuộc phiên bản MVP và không nên tự ý thêm vào khi phát triển giai đoạn đầu:

- AI tự động phân tích ảnh.
- AI viết lại câu chuyện.
- Nhận diện khuôn mặt.
- Bảng tin xã hội.
- Like.
- Comment.
- Follow.
- Public profile.
- Explore hoặc trending page.
- Chia sẻ công khai.
- Chat hoặc nhắn tin giữa người dùng.

Nếu sau này có tính năng chia sẻ, chia sẻ phải là riêng tư, tùy chọn và có kiểm soát rõ ràng từ người dùng.

## 4. Nguyên tắc MVP

- Ưu tiên hoàn thành chức năng cơ bản trước.
- Giữ giao diện sạch, ấm áp, riêng tư.
- Không biến ứng dụng thành mạng xã hội.
- Không thêm công nghệ phức tạp khi chưa cần thiết.
- Mỗi chức năng nên đủ đơn giản để một người mới học ASP.NET MVC có thể hiểu và bảo trì.

