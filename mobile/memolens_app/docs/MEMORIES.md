# Flutter Memory Timeline va CRUD

## Pham vi Phase 19C

Flutter dung JWT Bearer hien co de goi cac API private: `GET /api/v1/memories`, `GET /api/v1/memories/{id}`, `POST /api/v1/memories`, `PUT /api/v1/memories/{id}`, `DELETE /api/v1/memories/{id}` va `POST /api/v1/memories/{id}/restore`.

Moi request di qua `authenticatedDioProvider`; backend ap ownership va soft delete. Khong truyen user id tren URL, query hoac body.

## Trai nghiem hien co

- `/home`: Timeline rieng tu, pull-to-refresh, load them trang, tim kiem, loc Feeling/Tag va sap xep moi/cu.
- `/memories/create`: tao title, story, feeling, ngay, location va tags.
- `/memories/:id`: xem chi tiet va chuyen vao thung rac co xac nhan.
- `/memories/:id/edit`: chinh sua metadata ky niem.
- Timeline la Riverpod state duy nhat cho danh sach. Tao/sua/xoa thanh cong cap nhat state tai cho; logout hoac doi tai khoan xoa state trong bo nho.

`memoryDate` luon gui theo `yyyy-MM-dd` de ngay ky niem khong bi doi do timezone.

## Anh va gioi han co y

Response metadata anh duoc parse de biet so luong va ten file, nhung app **khong** tai byte, khong render URL anh, khong upload/pick anh o Phase 19C. UI chi co placeholder trung tinh. Private image upload/display la Phase 19D.

Chua co Album, Trash list, Settings, public sharing, feed, like, comment, follower hoac AI trong mobile client.

## Chay app

```powershell
cd mobile/memolens_app
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5296
```

Tren Android emulator, backend Development chay o may host duoc truy cap qua `10.0.2.2`. Khong dung URL anh public va khong ghi token/password/story rieng tu vao log.
## Phase 19C.1 - Android Memory E2E

- Da kiem thu tren Android emulator voi tai khoan tam da xac nhan: tao Memory, xem Details, sua metadata va soft delete. Sau delete, Timeline cap nhat ve empty state.
- Phat hien client parser yeu cau `data` map cho ca DELETE response, trong khi backend dung response `success/message` hop le khong co `data`. Repository da co request void rieng de chap nhan response thanh cong nay; khong doi API contract.
- Da xac nhan tai khoan User B khong thay Memory cua User A sau logout va dang nhap lai. Timeline state duoc xoa khi logout/doi tai khoan.
- Khong co anh hoac Album nao duoc tao trong Phase nay. Trash UI, restore qua UI, va image flow thuoc cac phase sau.
- Toan bo tai khoan, Memory, tag va refresh-token tam dung cho smoke da duoc xoa sau khi kiem thu.
## Phase 19D - Private images

- Memory forms can select multiple local images, preview them, remove selections and validate extension, 5 MB size and remaining image slots before upload.
- Details load private image bytes through the JWT Dio client and show a responsive two-column gallery. Timeline uses the private cover image when `coverImageId` exists.
- Upload happens only after create/update text operations. Individual deletion requires confirmation and accepts the backend success-only DELETE envelope.
- See `MEMORY_IMAGES.md` for privacy, lifecycle and known limitations.
