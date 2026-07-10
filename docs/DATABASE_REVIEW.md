# MemoLens - Rà soát Database và Tính toàn vẹn dữ liệu

## 1. Mục đích

Tài liệu này ghi nhận kết quả Phase 16A: rà soát mô hình dữ liệu, migrations, quan hệ, soft delete, ownership, ảnh riêng tư và refresh token của MemoLens.

Mục tiêu là xác định rủi ro trước khi mở rộng API/mobile, không thay đổi schema trong phase này. MemoLens vẫn là nhật ký ảnh riêng tư, không phải mạng xã hội.

## 2. Kết quả tóm tắt

- Không phát hiện lỗi critical buộc phải thay đổi source hoặc schema ngay trong Phase 16A.
- EF Core xác nhận không có pending model changes.
- Bốn migration hiện có đều đang được áp dụng cho LocalDB.
- Kiểm tra read-only trên dữ liệu LocalDB hiện có không phát hiện orphan record, quan hệ album-memory chéo user hoặc trạng thái soft delete không nhất quán.
- Các rủi ro còn lại chủ yếu là vận hành và mở rộng: cleanup, backup, composite index, transaction giữa file và database, cùng test ownership tự động.

## 3. Tổng quan database hiện tại

MemoLens dùng SQL Server/LocalDB với Entity Framework Core và ASP.NET Core Identity. Ảnh không được lưu dạng binary trong SQL Server. Database chỉ lưu metadata và đường dẫn tương đối; file ảnh mới nằm ngoài `wwwroot` tại `App_Data/uploads`.

Các migration hiện có:

1. `20260708215839_InitialCreate`
2. `20260708224933_RenameMoodToFeelingAndAddSoftDelete`
3. `20260709024341_AddAlbumSoftDelete`
4. `20260710054848_AddUserRefreshTokens`

## 4. Bảng và entity hiện tại

| Nhóm | Entity/bảng | Vai trò |
| --- | --- | --- |
| Identity | `AspNetUsers`, roles và bảng Identity liên quan | Tài khoản, role, password hash, email confirmation. |
| Ký ức | `Memories` | Nội dung memory, owner, Feeling, ngày, vị trí, soft delete. |
| Ảnh | `MemoryImages` | Metadata ảnh và `ImagePath` tương đối của memory. |
| Tag | `Tags`, `MemoryTags` | Tag dùng lại toàn hệ thống và quan hệ nhiều-nhiều với memory. |
| Album | `Albums`, `AlbumMemories` | Album riêng tư và quan hệ nhiều-nhiều với memory. |
| API auth | `UserRefreshTokens` | Hash refresh token, thời hạn, trạng thái revoke và metadata thiết bị tối thiểu. |

`CoverImagePath` vẫn tồn tại trong entity `Album`, nhưng UI hiện tạo ảnh bìa từ ảnh đầu tiên của memory hiển thị được trong album. Đây là cột chưa được sử dụng trong luồng hiện tại; không gây lỗi nhưng cần được quyết định rõ ở Phase 16B hoặc trước khi thêm API album.

## 5. Rà soát quan hệ

| Quan hệ | Cấu hình hiện tại | Đánh giá |
| --- | --- | --- |
| User -> Memories | FK `Memories.UserId`, `Restrict` | Giữ an toàn khi sau này xóa account; cần quy trình xóa account rõ ràng. |
| User -> Albums | FK `Albums.UserId`, `Restrict` | Tương tự memories. |
| Memory -> MemoryImages | Cascade khi memory bị xóa cứng | Phù hợp cho permanent delete tương lai, nhưng database không thể tự xóa file vật lý. |
| Memory <-> Tags | PK `(MemoryId, TagId)`, cascade ở bảng nối | Ngăn tag trùng trong cùng memory; tag không dùng có thể còn lại. |
| Album <-> Memories | PK `(AlbumId, MemoryId)`, cascade ở bảng nối | Ngăn relation trùng; code kiểm tra cùng owner trước khi thêm/xóa relation. |
| User -> UserRefreshTokens | Cascade | Phù hợp khi xóa account, miễn có quy trình dọn dữ liệu/ảnh đi kèm. |

LocalDB hiện không có `AlbumMemory` chéo owner và không có orphan record ở `MemoryImages`, `MemoryTags` hoặc `AlbumMemories`.

Database không thể tự biểu diễn quy tắc “album và memory phải cùng `UserId`” bằng FK hiện tại. Controller đã kiểm tra điều kiện này khi thêm/xóa relation và view có defensive filter. Phase 16D cần integration test để giữ quy tắc này khi code thay đổi.

## 6. Ownership và quyền riêng tư

Kết quả review:

- `MemoriesController`, `AlbumsController` và `TrashController` luôn lấy current user và filter theo `UserId`.
- `ImagesController.MemoryImage` chỉ trả file khi ảnh thuộc memory của current user và memory chưa soft delete. Trường hợp thiếu quyền, user khác hoặc file thiếu đều trả `NotFound`.
- Admin không có luồng bypass ownership trong MVP.
- Views dùng endpoint `Images/MemoryImage/{id}`, không render `ImagePath` làm URL public.
- API auth hiện chỉ trả user summary và không có API memory/album/image CRUD, nên không trả private storage path.

Lưu ý: `ImagePath` vẫn là metadata server-side trong entity/view model để xử lý file. Bất kỳ API ảnh/memory tương lai nào phải trả image id hoặc authorized URL, không trả physical path hay đường dẫn riêng tư này.

## 7. Soft delete

`Memories` và `Albums` có `IsDeleted` và `DeletedAt`.

- Normal timeline/album queries filter `!IsDeleted`.
- Trash chỉ lấy item `IsDeleted` của current user.
- Restore cũng filter theo current user và item đã xóa.
- Memory bị xóa mềm vẫn giữ ảnh và relationship trong database; endpoint ảnh từ chối phục vụ cho đến khi memory được restore.
- Album bị xóa mềm không xóa memories hoặc quan hệ album-memory; restore album giữ lại các quan hệ đó.

Dữ liệu LocalDB hiện không có record nào có `IsDeleted` và `DeletedAt` mâu thuẫn.

Rủi ro cần quản lý: chưa có global query filter EF Core. Tính đúng đắn hiện phụ thuộc vào việc mỗi query mới phải nhớ thêm điều kiện soft delete. Phase 16B nên đánh giá global query filter hoặc một convention/repository tối giản kèm test hồi quy, tránh làm đổi hành vi hiện có một cách vội vàng.

## 8. Refresh token và dữ liệu xác thực

- Refresh token được sinh ngẫu nhiên 64 byte, chỉ `SHA-256` hash được lưu trong `UserRefreshTokens`.
- `TokenHash` unique; refresh rotation revoke token cũ bằng update có điều kiện.
- Token revoke/hết hạn không được chấp nhận.
- Reset mật khẩu revoke tất cả refresh token còn hoạt động của user trong cùng transaction với reset password.
- Không có refresh token hiện hữu trong LocalDB tại thời điểm review; các hash đã từng có đều theo định dạng Base64 SHA-256 dài 44 ký tự.

Khoảng trống vận hành: chưa có job dọn refresh token hết hạn hoặc đã revoke. Phase 16B nên thêm cleanup job có lịch chạy, retention rõ ràng và index hỗ trợ cleanup.

## 9. Ảnh, file storage và backup

Ảnh mới được lưu theo cấu trúc gần đúng:

```text
App_Data/uploads/memories/{userId}/{memoryId}/{guid}.{extension}
```

`LocalImageStorageService` kiểm tra extension, giới hạn kích thước và dùng GUID cho tên file. Resolve path kiểm tra file vẫn nằm dưới private root để giảm path traversal. Ảnh được phục vụ qua controller có ownership check, không qua static URL.

Rủi ro và việc cần làm:

- Lưu file và ghi database không nằm trong một transaction phân tán. Nếu SaveChanges hoặc xóa file thất bại sau bước còn lại, có thể phát sinh orphan file hoặc orphan metadata.
- Chưa có scheduled orphan-image cleanup.
- Validation hiện dựa vào extension/size; trước production nên bổ sung kiểm tra content signature, resize/compression và scan/quarantine phù hợp.
- Khi permanent delete hoặc xóa account được thêm, phải xóa file private có kiểm soát trước/sau database transaction và có khả năng retry.

Backup bắt buộc phải gồm cả hai phần đồng bộ theo cùng mốc thời gian:

1. SQL Server database, bao gồm Identity, memories, albums, tags và refresh token hashes.
2. Thư mục `App_Data/uploads`.

Khôi phục database mà thiếu file ảnh sẽ tạo metadata không mở được; khôi phục file mà thiếu database sẽ tạo orphan file. Production nên cân nhắc private object storage, thumbnail/compression, quota theo user và quy trình backup/restore đã được kiểm thử.

## 10. Index và hiệu năng

Index hiện có:

| Entity | Index hiện có |
| --- | --- |
| `Memories` | `UserId`, `MemoryDate`, `Feeling`, `(UserId, IsDeleted, MemoryDate)`, `(UserId, IsDeleted, CreatedAt)`. |
| `Albums` | `UserId`, `UpdatedAt`, `(UserId, IsDeleted, CreatedAt)`. |
| `MemoryImages` | `MemoryId`. |
| `MemoryTags` | PK `(MemoryId, TagId)` và index `TagId`. |
| `AlbumMemories` | PK `(AlbumId, MemoryId)` và index `MemoryId`. |
| `Tags` | `Name` unique. |
| `UserRefreshTokens` | `UserId`, `TokenHash` unique, `ExpiresAt`, `RevokedAt`. |

### Đã triển khai ở Phase 16B.1

Migration `20260710174606_AddPerformanceIndexes` đã thêm các index an toàn, bám theo query hiện có mà không thay đổi behavior:

| Mục tiêu query | Index đã thêm |
| --- | --- |
| Timeline/trash memory của current user | `Memories(UserId, IsDeleted, MemoryDate)`. |
| Timeline theo thời gian tạo và truy vấn memory chưa xóa | `Memories(UserId, IsDeleted, CreatedAt)`. |
| Album list/trash của current user | `Albums(UserId, IsDeleted, CreatedAt)`. |
| Chuẩn bị cleanup refresh token theo trạng thái revoke | `UserRefreshTokens(RevokedAt)`. |

`MemoryImages(MemoryId)`, `Tags(Name)` unique, `MemoryTags(MemoryId, TagId)`, `AlbumMemories(AlbumId, MemoryId)`, `UserRefreshTokens(UserId/TokenHash/ExpiresAt)` đã tồn tại nên không thêm index trùng lặp. Không thêm unique constraint mới và không thay đổi join table, relationship hoặc soft-delete logic.

Đề xuất còn lại cho các phase sau, sau khi đo query plan trên dữ liệu thực tế:

| Mục tiêu query | Index đề xuất |
| --- | --- |
| Ảnh bìa/ảnh memory có thứ tự | `MemoryImages(MemoryId, UploadedAt)`. |
| Cleanup refresh token có retention thực tế | Cân nhắc composite `UserRefreshTokens(RevokedAt, ExpiresAt)` hoặc filtered index cho token active. |
| Tag ổn định giữa các database collation | Thêm normalized key unique nếu cần phân biệt/chuẩn hóa case độc lập với SQL Server collation. |

Không tạo index trong Phase 16A vì đây là thay đổi schema. Các primary key composite hiện tại đã hỗ trợ lookup theo `MemoryId` trước `TagId` và theo `AlbumId` trước `MemoryId`.

## 11. Cleanup và permanent delete

Hiện chưa có permanent delete. Thiết kế phase sau cần chốt:

- Retention period cho Trash và cách thông báo trước khi xóa vĩnh viễn.
- Permanent delete memory: xóa ảnh vật lý, `MemoryImages`, `MemoryTags`, `AlbumMemories` và memory trong workflow có retry/audit rõ ràng.
- Permanent delete album: xóa album và relation, không xóa memories gốc.
- Xóa account: revoke/xóa refresh token, xóa hoặc ẩn data người dùng theo chính sách, dọn private image files và dữ liệu Identity theo thứ tự an toàn.
- Cleanup tag không còn `MemoryTags` tham chiếu.
- Cleanup refresh token hết hạn/revoked và orphan image files.

Không được dùng cascade database như lý do để bỏ qua dọn file vật lý hoặc audit. Cascade chỉ xử lý row trong SQL Server.

## 12. Rủi ro đã phát hiện

| Mức độ | Rủi ro | Hướng xử lý |
| --- | --- | --- |
| Trung bình | Không có global filter cho soft delete. | Phase 16B/16D thêm convention hoặc global filter sau khi test đầy đủ. |
| Trung bình | File system và DB không atomic. | Thêm cleanup/retry và test lỗi một phần trước permanent delete. |
| Trung bình | Chưa có backup/restore production cho cả DB lẫn `App_Data/uploads`. | Lập và diễn tập backup/storage plan. |
| Thấp | Tag không dùng có thể tích lũy; `Tags.Name` chưa có normalized key rõ ràng. | Cleanup job và quyết định ownership/normalization của tag. |
| Thấp | Không có token cleanup, quota, thumbnail/compression hoặc retention policy. | Làm trong Phase 16B và production plan. |
| Thấp | Không có optimistic concurrency token cho memory/album. | Cân nhắc `rowversion` khi multi-device editing trở nên quan trọng. |
| Thấp | `CoverImagePath` chưa được sử dụng. | Giữ làm legacy hoặc loại bỏ bằng migration có chủ đích sau này. |

## 13. Phase tiếp theo đề xuất

1. **Phase 16B.2: Refresh Token Cleanup**
   - Định nghĩa retention, dọn token hết hạn/revoked và đo query plan cho cleanup index.
2. **Phase 16C: Automated Tests Foundation**
   - Thiết lập test project, test database strategy và regression tests cho auth/soft delete.
3. **Phase 16D: Privacy/Ownership Integration Tests**
   - Kiểm thử user A/B cho memory, album, image, trash và relationship cross-owner.
4. **Sau đó: Permanent delete và account deletion**
   - Chốt data retention, file cleanup, export và user-facing confirmation.
5. **Sau đó: Production backup/storage plan**
   - Object storage riêng tư, backup/restore drill, monitoring, quota, thumbnail/compression.
