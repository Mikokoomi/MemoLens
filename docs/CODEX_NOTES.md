# MemoLens - Notes for Future Codex Tasks

## 1. Tài liệu cần đọc trước khi coding

Trước khi viết hoặc sửa code cho MemoLens, Codex phải đọc:

- `docs/PROJECT_BRIEF.md`
- `docs/DECISIONS.md`

Nếu task liên quan đến database, cần đọc thêm:

- `docs/DATABASE_DESIGN.md`

Nếu task liên quan đến giao diện, cần đọc thêm:

- `docs/UI_SCREENS.md`

Nếu task liên quan đến kế hoạch triển khai, cần đọc thêm:

- `docs/TASKS.md`

## 2. Giữ code beginner-friendly

Code nên rõ ràng, dễ đọc và phù hợp cho người mới học ASP.NET MVC.

Ưu tiên:

- Tên class, method và biến dễ hiểu.
- Cấu trúc đơn giản.
- Không thêm abstraction phức tạp khi chưa cần.
- Comment ngắn gọn ở những chỗ khó hiểu.
- Làm từng bước nhỏ để dễ kiểm tra.

## 3. Không tự ý thêm social features

Codex không được tự ý thêm các chức năng mạng xã hội, bao gồm:

- Public feed.
- Like.
- Comment.
- Follow.
- Public profile.
- Explore hoặc trending page.

Chỉ thêm các chức năng này nếu người dùng yêu cầu rõ ràng. Nếu có yêu cầu chia sẻ, Codex nên hỏi lại để đảm bảo chia sẻ là riêng tư và tùy chọn.

## 4. Sau mỗi task cần tóm tắt changed files

Sau khi hoàn thành một task, Codex cần tóm tắt:

- File nào đã được tạo.
- File nào đã được sửa.
- Mục đích của từng thay đổi.
- Có file nào chưa đụng tới hay không nếu liên quan đến task.

## 5. Sau mỗi code task cần giải thích cách chạy và test

Sau khi có thay đổi code, Codex cần giải thích:

- Cách chạy project.
- Cách test chức năng vừa làm.
- Command đã chạy để kiểm tra nếu có.
- Lỗi hoặc giới hạn còn lại nếu có.

## 6. Hỏi trước khi đổi kiến trúc lớn

Codex phải hỏi người dùng trước khi thực hiện thay đổi lớn như:

- Đổi tech stack.
- Chuyển từ ASP.NET MVC sang framework khác.
- Thay SQL Server bằng database khác.
- Thêm hệ thống authentication phức tạp ngoài phạm vi MVP.
- Thêm AI hoặc social features.
- Thay đổi cấu trúc database lớn so với tài liệu ban đầu.

## 7. Luôn giữ đúng định hướng sản phẩm

MemoLens là ứng dụng lưu giữ ký ức riêng tư.

Mỗi thay đổi nên hỗ trợ một trong các mục tiêu:

- Giúp người dùng lưu kỷ niệm cá nhân.
- Giúp người dùng kể lại câu chuyện phía sau ảnh.
- Giúp người dùng tìm lại kỷ niệm.
- Bảo vệ sự riêng tư.
- Giữ trải nghiệm đơn giản và cảm xúc.

