# Data Architecture và ERD hiện tại

## Lưu ý nền tảng

`ApplicationDbContext` kế thừa `IdentityDbContext<ApplicationUser>`. Bên cạnh các bảng ASP.NET Identity, có **8 entity ứng dụng/infrastructure**: ApplicationUser, Memory, MemoryImage, Tag, MemoryTag, Album, AlbumMemory và UserRefreshToken. Không có binary image column.

```mermaid
erDiagram
  ApplicationUser ||--o{ Memory : owns
  ApplicationUser ||--o{ Album : owns
  ApplicationUser ||--o{ UserRefreshToken : has
  Memory ||--o{ MemoryImage : contains
  Memory ||--o{ MemoryTag : tagged
  Tag ||--o{ MemoryTag : joins
  Album ||--o{ AlbumMemory : contains
  Memory ||--o{ AlbumMemory : belongs_to
  ApplicationUser { string Id PK; string DisplayName; datetime CreatedAt }
  Memory { int Id PK; string UserId FK; string Title; string Feeling; date MemoryDate; bool IsDeleted; datetime DeletedAt }
  MemoryImage { int Id PK; int MemoryId FK; string ImagePath; string OriginalFileName; datetime UploadedAt }
  Tag { int Id PK; string Name UK }
  MemoryTag { int MemoryId PK,FK; int TagId PK,FK }
  Album { int Id PK; string UserId FK; string Title; bool IsDeleted; datetime DeletedAt }
  AlbumMemory { int AlbumId PK,FK; int MemoryId PK,FK; datetime AddedAt }
  UserRefreshToken { int Id PK; string UserId FK; string TokenHash UK; datetime ExpiresAt; datetime RevokedAt }
```

| Entity | Lưu gì / private data | Quan hệ, index và lifecycle |
| --- | --- | --- |
| ApplicationUser | Identity user, `DisplayName?`, `CreatedAt` | Owner của Memories/Albums/tokens. Identity infrastructure quản lý thông tin đăng nhập; `CreatedAt` required. |
| Memory | Tiêu đề, Story?, Feeling, ngày, Location?, timestamps, `UserId`, `IsDeleted`, `DeletedAt?` | User 1-n; Image/MemoryTag/AlbumMemory 1-n. Index UserId, MemoryDate, Feeling và composite `(UserId, IsDeleted, MemoryDate/CreatedAt)`. User delete là Restrict. Soft delete ẩn nhưng không xoá child. |
| MemoryImage | `ImagePath`, OriginalFileName, UploadedAt, Caption? | Private metadata; `ImagePath` là key/path tới filesystem, không phải binary. FK Memory cascade khi Memory bị **physically deleted** bởi EF; app hiện không có permanent delete Memory. |
| Tag | Name | Unique index Name, không owner-specific. Join n-n qua MemoryTag. Cascade khi Tag/Memory physically deleted. |
| MemoryTag | hai FK | Composite PK `(MemoryId, TagId)` ngăn duplicate membership. |
| Album | title, description?, CoverImagePath?, owner, timestamps, soft delete fields | User 1-n, index UserId/UpdatedAt/composite soft-delete. User delete Restrict. Soft delete không xoá Memory. `CoverImagePath` là path/key nullable, hiện không đồng nghĩa với public URL. |
| AlbumMemory | hai FK, AddedAt | Composite PK `(AlbumId, MemoryId)`; n-n Album-Memory. Cascade khi Album hoặc Memory physically deleted. |
| UserRefreshToken | TokenHash, expiry/revocation/replacement/device metadata | Private security data; User 1-n cascade. TokenHash unique; index UserId, ExpiresAt, RevokedAt. Không trả hash ra API. |

## Ràng buộc và delete behavior

- `Tag.Name` và `UserRefreshToken.TokenHash` có unique index.
- Các join table có composite primary key; không có cột surrogate ID.
- Xoá mềm là logic ứng dụng trên Memory/Album. Cascade trong EF chỉ diễn ra khi một parent bị physical delete, một flow hiện chưa được người dùng hỗ trợ.
- Restore Memory/Album chỉ đổi soft-delete fields; không tạo lại row/file.

## Ảnh: database so với file vật lý

`MemoryImages` chứa `Id`, `MemoryId`, `ImagePath` (max 500), `OriginalFileName` (max 255), `UploadedAt`, `Caption?` (max 255). File được ghi bởi `LocalImageStorageService` vào `<ContentRoot>/App_Data/uploads/memories/{safeUserId}/{memoryId}/{guid}.{ext}`; `ImagePath` tương đối lưu `uploads/memories/...`. Vì `App_Data` nằm ngoài `wwwroot`, static file middleware không expose ảnh.

MVC gọi `ImagesController.MemoryImage`; Flutter gọi `GET /api/v1/images/{imageId}/content`. Cả hai kiểm tra owner `Memory.UserId == currentUserId` và `!Memory.IsDeleted`; User B và Admin đều không bypass trong MVP, nhận `404` khi không thuộc owner. Xoá individual image xoá row rồi gọi `DeleteImageFile`; Memory soft delete chỉ chặn access, restore mở lại access.
