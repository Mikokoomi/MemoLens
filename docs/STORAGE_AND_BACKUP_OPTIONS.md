# Storage và Backup Options

## Thiết kế Development đã xác nhận

- Database: SQL Server LocalDB, `MemoLensDb` qua `DefaultConnection`.
- Image file: `<ContentRoot>/App_Data/uploads/memories/...`, ngoài `wwwroot`.
- `LocalImageStorageService` kiểm tra JPG/JPEG/PNG/WEBP, tối đa 5 MB mỗi file và tối đa 10 ảnh/Memory.
- Database chỉ giữ metadata/path; database backup đơn lẻ **không** là backup đầy đủ khi ảnh ở filesystem.

## PRODUCTION DATABASE - REQUIRES USER DECISION

Production hosting chưa được chọn. Các phương án hợp lý cần tương thích EF Core SQL Server hiện có gồm SQL Server managed hoặc SQL Server tự vận hành. Chọn provider/hosting sẽ tác động connection string, quyền truy cập, backup, restore drill và chi phí vận hành; không đổi provider trong phase này.

## PRODUCTION IMAGE STORAGE - REQUIRES USER DECISION

| Option | Bảo mật và vận hành | Backup/restore | Scale và tác động |
| --- | --- | --- | --- |
| A. Persistent server volume | API vẫn authorise rồi đọc volume private. Đơn giản nhất gần với code hiện tại, nhưng phụ thuộc disk/node/deployment | Phải snapshot volume đồng bộ DB; restore sai thứ tự có thể orphan/missing file | Ít code, hạn chế scale ngang và deployment rolling |
| B. Private object storage | Bucket/container private; DB lưu object key; API hoặc signed access có kiểm soát | Có versioning/lifecycle nếu provider hỗ trợ; cần restore manifest/DB cùng file objects | Scale tốt, nhưng cần storage service mới, credential/secrets, retry/observability và migration path |

Không chọn option nào. Khuyến nghị có điều kiện: private object storage phù hợp khi có production/public beta nhiều node; persistent volume hợp lý cho bản triển khai một server nhỏ nếu backup/restore được diễn tập. Cả hai đều cần user approval.

## Backup và restore phối hợp

1. Chụp hoặc backup database và image storage theo cùng điểm thời gian/mốc logical.
2. Backup secrets/configuration bằng secret manager, không commit credential hay JWT production secret.
3. Khi restore: restore DB và image store cùng backup set; sau đó kiểm tra một mẫu `MemoryImage.ImagePath` có file tương ứng và endpoint owner-only hoạt động.
4. Kiểm kê orphan file (file không có row) và missing file (row không có file), nhưng chưa tự động cleanup.
5. Test rollback và diễn tập restore định kỳ trên môi trường tách biệt.

**Backup frequency, retention, restore RPO/RTO, xoá trong backup và cleanup schedule đều là Open decision.**

## Chính sách ảnh chưa chốt

Đã xác nhận: extension/size/count ở trên; retrieval private authenticated; không public image URL; Flutter không persistent disk cache.

| Quyết định | Lý do / option | Impact | Giai đoạn |
| --- | --- | --- | --- |
| Quota user / số Memory | Không quota, quota dung lượng, quota count | API/model/reporting có thể cần đổi | Trước release |
| Resolution, resize, thumbnail, compression | Giữ original hoặc pipeline derivatives | Storage service, keys, DB/metadata và QA | Post-MVP hoặc trước scale |
| EXIF/GPS | Giữ, strip khi upload, hoặc user choice | Privacy policy và processing | Trước production |
| Content signature | Extension-only hiện tại hoặc verify magic bytes | Upload security implementation | Nên trước public beta |
| Duplicate, ordering, cover | Không xử lý hiện tại / hash-order-cover explicit | DTO/model/API/UI có thể đổi | Post-MVP |
| Failed upload/orphan retention | Manual only hoặc scheduled cleanup | Job, audit, backup behavior | Trước scale |

Mọi hàng trên là **OPEN — REQUIRES USER APPROVAL**; không có lựa chọn nào được triển khai trong phase này.
