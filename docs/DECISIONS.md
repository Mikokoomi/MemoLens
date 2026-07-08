# MemoLens - Product and Technical Decisions

## 1. MemoLens không phải là mạng xã hội

MemoLens là ứng dụng nhật ký ảnh cá nhân và kể chuyện ký ức riêng tư. Ứng dụng không được định hướng thành mạng xã hội.

## 2. Không có like, comment, follow hoặc feed công khai

Ứng dụng không có:

- Like.
- Comment.
- Follow.
- Public feed.
- Public profile.
- Explore hoặc trending page.

Các chức năng này không phù hợp với mục tiêu riêng tư của MemoLens.

## 3. Photos are private by default

Ảnh và kỷ niệm của người dùng là riêng tư theo mặc định. Người dùng chỉ xem được dữ liệu của chính mình sau khi đăng nhập.

Nếu sau này có tính năng chia sẻ, chia sẻ phải là riêng tư, tùy chọn và có kiểm soát rõ ràng.

## 4. Store image paths in database, not binary image files

File ảnh không được lưu trực tiếp trong database.

Ứng dụng sẽ lưu file ảnh trong thư mục uploads, ví dụ `wwwroot/uploads/memories`, và chỉ lưu đường dẫn ảnh trong bảng MemoryImages.

Cách này giúp database nhẹ hơn và dễ quản lý hơn cho dự án học tập.

## 5. Tech stack

MemoLens sử dụng:

- ASP.NET MVC cho backend và cấu trúc web app.
- SQL Server cho cơ sở dữ liệu.
- Bootstrap cho giao diện.
- GitHub để quản lý source code và lịch sử phát triển.

Tech stack này phù hợp với dự án portfolio và dễ trình bày trong môi trường học tập.

## 6. MVP first, AI later

Phiên bản đầu tiên chỉ tập trung vào tính năng cốt lõi:

- Tài khoản.
- Tạo và quản lý kỷ niệm.
- Upload ảnh.
- Timeline.
- Album.
- Tag.
- Search/filter.
- Thiết lập riêng tư cơ bản.

Các tính năng AI như phân tích ảnh, gợi ý caption, nhận diện cảm xúc hoặc tự động tạo câu chuyện sẽ để sau MVP.

## 7. Ưu tiên privacy, simple UX và emotional memory storytelling

Khi có nhiều lựa chọn thiết kế, ưu tiên theo thứ tự:

1. Bảo vệ dữ liệu riêng tư của người dùng.
2. Trải nghiệm đơn giản, dễ hiểu cho người mới sử dụng.
3. Hỗ trợ người dùng kể lại câu chuyện và cảm xúc phía sau kỷ niệm.
4. Giữ code dễ đọc, dễ học và dễ demo.

## 8. Người dùng sở hữu ký ức của mình

MemoLens cần tôn trọng quyền sở hữu dữ liệu cá nhân của người dùng.

Trong tương lai, ứng dụng nên hỗ trợ:

- Xuất dữ liệu cá nhân.
- Xóa kỷ niệm.
- Xóa ảnh.
- Xóa tài khoản và dữ liệu liên quan.

Các chức năng này không bắt buộc trong MVP nhưng nên được ghi nhớ khi thiết kế database và kiến trúc.

