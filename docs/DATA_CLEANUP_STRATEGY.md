# MemoLens - Chiến lược Dọn dẹp Dữ liệu

## 1. Mục đích

MemoLens lưu ký ức và ảnh riêng tư lâu dài, vì vậy cleanup phải ưu tiên an toàn hơn tiết kiệm dung lượng. Chiến lược này xác định cách phát hiện và xử lý ảnh mồ côi, tag không dùng, permanent delete và backup/restore trong các phase sau.

Mục tiêu:

- Giảm tăng trưởng storage do file hoặc record không còn cần thiết.
- Phát hiện metadata cũ hoặc file không còn được database tham chiếu.
- Tránh xóa nhầm ký ức người dùng vẫn có thể restore.
- Giữ dữ liệu riêng tư, không đưa path vật lý vào UI/API hoặc log công khai.

Đây là tài liệu thiết kế. Phase 16B.3 không thêm cleanup code, không xóa file/record, không tạo migration và không thay đổi hành vi hiện tại.

## 2. Hành vi lưu ảnh hiện tại

- `MemoryImages` lưu metadata, `ImagePath` tương đối, tên file gốc và thời điểm upload; SQL Server không lưu binary ảnh.
- File ảnh mới được lưu dưới `App_Data/uploads/memories/{userId}/{memoryId}` bên ngoài `wwwroot`.
- `ImagesController.MemoryImage` chỉ phục vụ ảnh sau khi xác nhận current user sở hữu memory và memory chưa soft delete.
- `ImagePath` không được render thành public URL; views dùng authorized image endpoint theo image id.
- Soft delete memory chỉ đặt `IsDeleted/DeletedAt`; file ảnh và `MemoryImage` row được giữ để có thể restore.
- Xóa riêng một ảnh từ memory đang active hiện xóa `MemoryImage` row trước, sau đó yêu cầu storage service xóa file vật lý.

## 3. Định nghĩa ảnh mồ côi

| Loại | Định nghĩa | Ảnh hưởng |
| --- | --- | --- |
| DB orphan record | Có row `MemoryImages` nhưng file vật lý không còn tồn tại. | UI trả `NotFound`; metadata trở nên stale. |
| File orphan | File trong private upload root không còn row `MemoryImages` nào tham chiếu. | Tốn storage, không thể truy cập qua app. |
| User-folder orphan | Thư mục user/memory tồn tại nhưng user hoặc memory tương ứng không còn tồn tại sau một workflow tương lai. | Có thể chứa file không còn owner hợp lệ. |
| Temporary-upload orphan | File được ghi trong request upload nhưng transaction/SaveChanges phía database không hoàn tất. Hiện không có thư mục temporary riêng, nhưng lỗi một phần vẫn có thể tạo dạng orphan này. | Không có metadata tương ứng để dọn tự động. |

## 4. Vì sao cleanup rủi ro

- File system và database không cùng một transaction; upload hoặc delete có thể thành công một nửa.
- Upload/delete đồng thời có thể làm scanner đọc trạng thái cũ.
- Memory soft delete vẫn cần ảnh để user restore đầy đủ.
- Permanent delete, account deletion và migration cũ có thể tạo path/record khác kỳ vọng.
- Một điều kiện sai hoặc path traversal bug trong cleanup có thể xóa vĩnh viễn ký ức riêng tư.

Vì vậy cleanup không được coi là cơ chế sửa lỗi tức thời. Nó là workflow có báo cáo, grace period, backup và khả năng kiểm tra lại.

## 5. Thiết kế cleanup ảnh an toàn

### 5.1 Thứ tự thực hiện

1. **Report-only:** quét và xuất cleanup report, không thay đổi database/file.
2. **Dry-run:** áp dụng toàn bộ điều kiện, batch và thống kê như delete mode nhưng không xóa hoặc di chuyển file.
3. **Quarantine:** với file orphan đủ điều kiện, chuyển file vào private quarantine thay vì xóa ngay; giữ mapping/report an toàn.
4. **Delete có kiểm soát:** chỉ xóa sau quarantine retention, backup thành công và xác nhận vận hành rõ ràng.

### 5.2 Quy tắc không được phá vỡ

- Không xóa file của memory soft-deleted chỉ vì memory đang không hiển thị trên timeline.
- Chỉ cân nhắc file soft-deleted khi permanent delete hoặc trash retention policy đã cho phép xóa vĩnh viễn.
- Chỉ xử lý file orphan cũ hơn grace period; file mới phải được bỏ qua để tránh race với upload/SaveChanges.
- Luôn dùng UTC cho timestamp, xử lý theo batch và kiểm tra path nằm bên trong private upload root.
- Không log path vật lý cho UI/API. Log vận hành chỉ nên có count, thời điểm, loại lỗi và identifier tối thiểu đã được bảo vệ.
- Không bật destructive cleanup mặc định trong Development hoặc Production. Mọi chế độ xóa/quarantine cần config explicit.
- Backup database và `App_Data/uploads` phải tồn tại trước chế độ destructive.

### 5.3 Xử lý từng loại orphan

| Loại | Report/dry-run | Delete/quarantine tương lai |
| --- | --- | --- |
| DB orphan record | Báo `MemoryImage.Id`, owner đã được scope nội bộ và trạng thái file missing. | Chỉ xóa metadata sau review; không suy luận rằng cần xóa memory. |
| File orphan | So sánh canonical relative path với tập `MemoryImages.ImagePath`. | Chỉ move quarantine sau grace period, không xóa trực tiếp. |
| User-folder orphan | Kiểm tra folder name với user/memory hợp lệ và cả trạng thái permanent-delete trong tương lai. | Chỉ xử lý nếu không còn record liên quan và file đủ tuổi. |
| Temporary-upload orphan | Scanner phải coi file mới là hợp lệ tạm thời trong grace period. | Quarantine sau grace period; điều tra lỗi upload nếu lặp lại. |

## 6. Đề xuất service trong tương lai

Các class dưới đây chỉ là đề xuất, không được tạo ở Phase 16B.3:

- `IImageCleanupService`: chạy report/dry-run/quarantine/delete và trả kết quả không chứa path công khai.
- `ImageCleanupService`: đọc DB + private storage qua abstraction đã kiểm tra root path.
- `ImageCleanupOptions`: cấu hình mode và retention.
- `ImageCleanupHostedService`: tùy chọn, chỉ kích hoạt khi explicit config và sau khi dry-run đã được duyệt.
- `ImageCleanupReport`: số file quét, DB orphan, file orphan, skipped, quarantined, deleted, lỗi an toàn và thời điểm UTC.

Option đề xuất:

| Option | Ý nghĩa |
| --- | --- |
| `Enabled` | Mặc định `false`; chỉ chạy khi được bật rõ ràng. |
| `DryRun` | Mặc định `true` khi service lần đầu được triển khai. |
| `GracePeriodDays` | Tuổi tối thiểu của file orphan trước khi có thể quarantine. |
| `BatchSize` | Giới hạn file/record xử lý mỗi batch. |
| `QuarantineBeforeDelete` | Bắt buộc `true` trong giai đoạn đầu. |
| `QuarantineRetentionDays` | Thời gian giữ file trong quarantine trước permanent delete. |

## 7. Chiến lược cleanup tag không dùng

`Tags` hiện là danh mục dùng lại toàn hệ thống, còn `MemoryTags` là bảng nối.

- Tag có zero `MemoryTag` relationship là ứng viên cleanup an toàn nhất.
- Tag chỉ gắn với memory soft-deleted nên được giữ, vì restore memory phải khôi phục tag đầy đủ.
- Không xóa tag chỉ vì nó không xuất hiện trong normal timeline của một user.
- Cleanup tag nên chạy sau permanent delete hoặc sau khi cleanup metadata đã được review.
- Nếu sau này tag trở thành user-scoped, cần migration và ownership review riêng; không trộn quyết định đó với cleanup hiện tại.

Khuyến nghị ban đầu: chỉ xóa tag có **zero** `MemoryTag` relationship, chạy report-only trước, xử lý theo batch và không xóa tag cần bởi memory có thể restore.

## 8. Thiết kế permanent delete trong tương lai

Permanent delete phải là workflow user-scoped, có confirmation rõ ràng và chỉ áp dụng sau trash retention policy.

### Memory

Permanent delete memory sẽ cần xử lý theo thứ tự có kiểm soát:

1. Xác nhận current user sở hữu memory và memory đủ điều kiện permanent delete.
2. Lập danh sách `MemoryImages` và private paths đã canonicalize.
3. Xóa/đánh dấu `MemoryTag` links và `AlbumMemory` links.
4. Xóa `MemoryImages` rows, memory row và file vật lý theo workflow có retry/quarantine phù hợp.
5. Tạo audit event an toàn: action, timestamp, count; không ghi story, file path hoặc token.

### Album

Permanent delete album chỉ xóa album và `AlbumMemory` links. Nó không được xóa memories gốc hoặc ảnh của memories.

### Điều kiện UX và retention

- Trash cần retention period được công bố rõ.
- UI cần confirmation cho destructive action và nêu rõ không thể restore.
- Account deletion phải được thiết kế riêng, bao gồm Identity, refresh token, memory/album và private files.

## 9. Backup và restore

Backup chỉ database là không đủ vì ảnh nằm trong filesystem.

- Backup SQL Server phải bao gồm Identity, memories, albums, tags, `MemoryImages` và refresh token hashes.
- Backup filesystem phải bao gồm `App_Data/uploads` và mọi quarantine folder tương lai.
- Hai backup cần cùng recovery point hoặc có manifest/timestamp để xác định tính nhất quán.
- Cần diễn tập restore database và files vào môi trường riêng trước private beta.
- Cleanup destructive chỉ được chạy khi backup gần nhất đã được kiểm tra và restore plan có owner rõ ràng.

## 10. Các phase triển khai đề xuất

1. **Phase 16B.4: Backup and Restore Plan**
   - Chốt backup scope, recovery point, restore drill và trách nhiệm vận hành.
2. **Phase 16B.5: Unused Tag Cleanup Implementation**
   - Report/dry-run trước; chỉ xử lý tag zero relationship.
3. **Phase 16B.6: Orphan Image Cleanup Dry-Run Service**
   - Không xóa file, chỉ scan/report/batch/grace period; review kết quả trước quarantine.
4. **Phase 16B.7: Permanent Delete Design/Implementation**
   - Thêm retention, confirmation và workflow user-scoped sau integration tests.
5. **Sau đó: Production Storage/Object Storage Strategy**
   - Private object storage, quota, thumbnail/compression, backup lifecycle và monitoring.

## 11. Không được làm

- Không tự động xóa toàn bộ file của memory soft-deleted.
- Không chạy destructive cleanup khi chưa có backup/restore đã kiểm tra.
- Không trả physical path qua UI, API, report người dùng hoặc log công khai.
- Không bật cleanup mặc định.
- Không trộn cleanup với feature work không liên quan.
- Không dùng cleanup để né thiết kế permanent delete, account deletion hoặc ownership check.
