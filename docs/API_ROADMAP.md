# MemoLens - API Roadmap

## 1. Vi sao can Web API truoc Flutter app

Flutter mobile app can giao tiep voi backend qua HTTP API ro rang. MVC views hien tai phuc vu browser, nhung mobile app can JSON endpoints de:

- Dang ky/dang nhap.
- Lay timeline.
- Tao/sua/xoa memory.
- Upload/view private images.
- Quan ly albums.
- Restore trash.
- Quan ly settings.

API nen duoc thiet ke truoc khi tao Flutter app de tranh mobile phai phu thuoc vao HTML/Razor.

## 2. Versioning de xuat

Dung prefix:

```text
/api/v1/...
```

Ly do:

- De mo rong ve sau.
- Flutter app co the pin vao version cu khi backend thay doi.
- Giam rui ro breaking changes.

## 3. API response style khuyen nghi

Nen dung response JSON nhat quan:

```json
{
  "success": true,
  "data": {}
}
```

Khi loi:

```json
{
  "success": false,
  "message": "Mo ta loi ngan gon",
  "errors": {
    "fieldName": ["Validation message"]
  }
}
```

Khuyen nghi:

- Validation error nen theo field.
- Unauthorized/forbidden/private content nen khong leak data.
- Missing/unauthorized private content co the tra `404 NotFound` de tranh tiet lo item ton tai.
- Khong tra physical file path.
- Khong tra password hash, security stamp, role internals.

## 4. Yeu cau user isolation

Moi API rieng tu phai:

- Lay current user tu auth context.
- Filter bang `UserId`.
- Khong cho Admin bypass private content ownership trong MVP hien tai.
- Khong tra memory/album/image/trash cua user khac.
- Khong co public image URL.

## 5. API groups

### 5.1 Auth API

Purpose:

- Dang ky, dang nhap, dang xuat, confirm email.

Likely endpoints:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/confirm-email`

Privacy rule:

- Login chi thanh cong khi email da xac thuc.
- Token/session khong duoc log ra console.

Maps to current MVC:

- `AccountController.Register`
- `AccountController.Login`
- `AccountController.Logout`
- `AccountController.ConfirmEmail`

Risks:

- Can quyet dinh cookie-based hay token-based auth cho mobile.
- Forgot password chua co, can them truoc private beta.
- Can rate limiting/lockout review.

### 5.2 Account/Settings API

Purpose:

- Doc account info, sua display name, doi mat khau.

Likely endpoints:

- `GET /api/v1/settings`
- `PUT /api/v1/settings/profile`
- `POST /api/v1/settings/change-password`

Privacy rule:

- Chi current user duoc xem/sua settings cua minh.
- Khong cho doi email trong MVP hien tai.
- Khong cho sua role hoac `EmailConfirmed`.

Maps to current MVC:

- `SettingsController.Index`
- `SettingsController.EditProfile`
- `SettingsController.ChangePassword`

Risks:

- Validation password can dung Identity validators.
- Response khong duoc leak Identity internals.

### 5.3 Memories API

Purpose:

- CRUD memories rieng tu.

Likely endpoints:

- `GET /api/v1/memories`
- `POST /api/v1/memories`
- `GET /api/v1/memories/{id}`
- `PUT /api/v1/memories/{id}`
- `DELETE /api/v1/memories/{id}`

Privacy rule:

- Moi query phai filter current user.
- Deleted memories khong hien trong normal endpoints.
- Delete la soft delete trong MVP hien tai.

Maps to current MVC:

- `MemoriesController.Index`
- `MemoriesController.Create`
- `MemoriesController.Details`
- `MemoriesController.Edit`
- `MemoriesController.Delete`

Risks:

- DTO can tach khoi EF entities.
- Date handling/timezone can ro rang.
- Tags parse can nhat quan voi MVC.

### 5.4 Memory Images API

Purpose:

- Upload, xem va xoa anh memory rieng tu.

Likely endpoints:

- `POST /api/v1/memories/{id}/images`
- `GET /api/v1/images/{id}`
- `DELETE /api/v1/images/{id}`

Privacy rule:

- Image chi tra ve khi image thuoc memory cua current user va memory chua bi xoa.
- Soft-deleted memory image phai tra `NotFound`.
- Missing physical file tra `NotFound`.
- Khong public image URL.

Maps to current MVC:

- `MemoriesController.Create/Edit/DeleteImage`
- `ImagesController.MemoryImage`

Risks:

- Upload abuse.
- File size/storage cost.
- Thumbnails/compression can co truoc nhieu user that.
- Private object storage co the can thiet cho beta/production.

### 5.5 Timeline Search/Filter API

Purpose:

- Ho tro mobile timeline voi search/filter/sort.

Likely endpoints:

- `GET /api/v1/timeline`
- `GET /api/v1/memories?search=...&feeling=...&tagId=...&fromDate=...&toDate=...&sortOrder=...`
- `GET /api/v1/tags`

Privacy rule:

- Search/filter chi trong current user's non-deleted memories.
- Tag options chi tu current user's non-deleted memories.

Maps to current MVC:

- `MemoriesController.Index`

Risks:

- Query performance khi co nhieu memories/images.
- Pagination can duoc thiet ke som.
- Invalid date ranges can response validation nhat quan.

### 5.6 Albums API

Purpose:

- Quan ly albums rieng tu va relationship album-memory.

Likely endpoints:

- `GET /api/v1/albums`
- `POST /api/v1/albums`
- `GET /api/v1/albums/{id}`
- `PUT /api/v1/albums/{id}`
- `DELETE /api/v1/albums/{id}`
- `POST /api/v1/albums/{id}/memories`
- `DELETE /api/v1/albums/{albumId}/memories/{memoryId}`

Privacy rule:

- Album phai thuoc current user.
- Memories add vao album phai thuoc current user va chua bi xoa.
- Deleted memories khong hien trong album detail.

Maps to current MVC:

- `AlbumsController.Index/Create/Details/Edit/Delete`
- `AlbumsController.AddMemories`
- `AlbumsController.RemoveMemory`

Risks:

- Cross-user add/remove relationship.
- Duplicate relationship.
- Automatic cover image performance.

### 5.7 Trash API

Purpose:

- Xem va restore deleted memories/albums.

Likely endpoints:

- `GET /api/v1/trash`
- `POST /api/v1/trash/memories/{id}/restore`
- `POST /api/v1/trash/albums/{id}/restore`

Privacy rule:

- Trash chi current user.
- Restore chi item cua current user va dang `IsDeleted`.
- Khong permanent delete trong current MVP.

Maps to current MVC:

- `TrashController.Index`
- `TrashController.RestoreMemory`
- `TrashController.RestoreAlbum`

Risks:

- Restore album khong nen tu dong restore deleted memories ben trong.
- Image access chi khoi phuc sau khi memory restore.

## 6. Thu tu trien khai API de xuat

1. Auth decision va API auth prototype.
2. Settings API nho de test auth.
3. Memories list/details API.
4. Private image API.
5. Create/edit/delete memory API.
6. Search/filter API.
7. Albums API.
8. Trash API.

Moi group can co manual QA va sau do automated tests.

## 7. Cập nhật Phase 18B: Memory CRUD API

Phase 18B đã hoàn thành phần Memory API nền tảng cho mobile:

- `GET /api/v1/memories` có pagination, search, feeling/tag/date filter và newest/oldest sort.
- `GET /api/v1/memories/{id}`, `POST`, `PUT`, `DELETE` và `POST /api/v1/memories/{id}/restore` đã có.
- Mọi endpoint yêu cầu JWT Bearer, chỉ query current user và không cho `Admin` bypass ownership.
- DTO không trả `UserId`, image path hoặc đường dẫn vật lý. Detail chỉ trả authorized image content URL.
- Delete là soft delete; restore khôi phục visibility và quyền xem ảnh riêng tư.
- Image upload API chưa được thêm. Đây là scope của Phase 18C.

## 8. Cập nhật Phase 18C: Private Memory Image API

Phase 18C đã hoàn thành API ảnh riêng tư cho mobile:

- `POST /api/v1/memories/{memoryId}/images` nhận multipart field `files`, tối đa 10 ảnh/memory và 5 MB/file.
- `GET /api/v1/images/{imageId}/content` stream ảnh qua JWT với `private, no-store`.
- `DELETE /api/v1/memories/{memoryId}/images/{imageId}` xóa đúng một row/file.
- Upload validate toàn batch trước khi lưu; lỗi ghi file/database rollback và dọn file đã tạo.
- User B và role `Admin` đều nhận `404` khi thử truy cập dữ liệu không thuộc mình.
- Memory detail đã chuyển `contentUrl` từ MVC cookie route sang JWT API route.
- MVC image endpoint vẫn giữ nguyên cho web app; không có schema change hay migration.

Chi tiết contract: [MEMORY_IMAGE_API.md](MEMORY_IMAGE_API.md).

Phase tiếp theo đề xuất: **Phase 18D - Private Album CRUD API**.

## 9. Cập nhật Phase 18D: Private Album CRUD API

Phase 18D đã hoàn thành backend API cho bộ sưu tập riêng tư:

- JWT CRUD: `GET/POST /api/v1/albums`, `GET/PUT/DELETE /api/v1/albums/{id}` và `POST /api/v1/albums/{id}/restore`.
- Membership: `POST /api/v1/albums/{id}/memories` và `DELETE /api/v1/albums/{id}/memories/{memoryId}`.
- List và details đều có database pagination; list hỗ trợ search và sort.
- Batch add validate toàn bộ trước khi ghi, dedupe ID và không tạo cập nhật một phần.
- Album soft delete/restore giữ memory, ảnh, file và membership; memory đang bị xóa mềm vẫn ẩn.
- User khác và role `Admin` không bypass ownership.
- Không thay đổi MVC, schema hoặc migration.

Contract chi tiết: [ALBUM_API.md](ALBUM_API.md).

Phase tiếp theo đề xuất: **Phase 18E - Private Trash API**.
