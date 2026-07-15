# Data Lifecycle và Privacy

## Hành vi hiện tại đã xác nhận

| Sự kiện | Hành vi |
| --- | --- |
| Soft delete Memory | Đặt `IsDeleted`/`DeletedAt`; normal Memory query và private image content không trả resource |
| Restore Memory | Bỏ soft-delete; Memory và ảnh private hiện có truy cập lại được cho owner |
| Xoá individual image | Xoá `MemoryImage` row và cố xoá file local |
| Soft delete Album | Đặt fields xóa mềm, không xoá Memories và giữ membership để restore |
| Refresh token | Hash được revoke khi logout/rotation; cleanup service có cấu hình nhưng `Enabled=false` mặc định |
| Ownership | API/MVC query theo current user; User B và Admin không browse content private User A trong MVP |

## Chính sách chưa được duyệt

| Chủ đề | Trạng thái / lý do |
| --- | --- |
| Trash retention và permanent deletion | **OPEN — REQUIRES USER APPROVAL.** Hiện không có permanent delete UX/policy. |
| Account deletion | **OPEN — REQUIRES USER APPROVAL.** Cần xác định scope Identity, Memory, image, token, backup. |
| Export/download data | **OPEN — REQUIRES USER APPROVAL.** Cần format, authorization và privacy review. |
| Orphan image / unused Tag cleanup | **OPEN — REQUIRES USER APPROVAL.** Không có cleanup job cho file/tag hiện tại. |
| Log, refresh-token, backup retention | **OPEN — REQUIRES USER APPROVAL.** Cần cân bằng security, audit và recovery. |
| Deletion from backups | **OPEN — REQUIRES USER APPROVAL.** Cần policy pháp lý/vận hành trước khi hứa với user. |
| Inactive account policy | **OPEN — REQUIRES USER APPROVAL.** Chưa có tự động khoá/xoá account. |

Không suy diễn rằng dữ liệu bị xoá vĩnh viễn sau một số ngày; ảnh bị soft-deleted chỉ bị ẩn, không bị xoá file bởi flow hiện tại.
