# Album Flutter

## Phase 19E - Checkpoint 2C

Checkpoint 2C implements a real three-step Create Album flow: information, optional active-memory selection, and confirmation. It sends exactly one atomic `POST /api/v1/albums` with optional `memoryIds`; an empty Album is supported and no follow-up membership request is made. Name and description are trimmed, backend length limits are respected, and a duplicate-name warning does not block saving.

Album Details now displays authenticated private covers, details and active related Memories. Edit Album changes only name and description. Draft, selection, picker and details state clear when the session changes. Membership management after create, delete/Undo, cover management, Trash and advanced presentation remain deferred to Checkpoint 2D or Checkpoint 3. Android Album smoke remains unclaimed because of the known native splash/black-screen emulator blocker.

## Phase 19E - Checkpoint 2B và 2B.1

Checkpoint 2B được khóa tại commit `b516dcc`. Shell đã đăng nhập có ba destination: Timeline, Album và Cài đặt. Nút `+` trung tâm luôn mở Create Memory. Cài đặt hiện chỉ là placeholder Phase 19G.

Album dùng ListView tạm thời, lấy danh sách owner-only qua Dio đã xác thực và tải ảnh bìa hiệu lực bằng private image byte loader. List có loading, empty, safe error/retry và success state. Khi logout hoặc đổi tài khoản, state Album và private-cover state của user trước được xóa.

Checkpoint 2B.1 bổ sung regression coverage cho shell navigation, state controller, Album list widget và private-cover isolation. Không có route Album chưa hoàn chỉnh nào được mở cho người dùng.

Commit backend liên quan: `8134a24` (gán một kỷ niệm vào nhiều Album theo cách nguyên tử). Checkpoint 2A atomic Album create được khóa tại `19e0b31`.

Tạo Album, xem chi tiết, chỉnh sửa, quan hệ kỷ niệm, xóa/khôi phục được để lại cho Checkpoint 2C trở đi. Không có search, sort, grid/list toggle, Trash UI hoặc manual cover UI trong Checkpoint 2B.

Android smoke riêng cho Album navigation chưa được ghi nhận trong 2B.1; Android emulator vẫn có native splash/black-screen blocker đã biết ở các QA trước. Checkpoint 2 vẫn chưa hoàn tất, Checkpoint 2C và Checkpoint 3 chưa bắt đầu.
