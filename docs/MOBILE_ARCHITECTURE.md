# MemoLens - Mobile Architecture

## 1. Huong kien truc de xuat

MemoLens nen tien hoa theo kien truc:

- Flutter mobile app.
- ASP.NET Core backend.
- SQL Server database.
- Private image storage.
- API layer phuc vu mobile app.
- MVC web app hien tai tiep tuc ton tai cho MVP/demo/internal web surface.

Chua tao Flutter project trong phase nay. Tai lieu nay chi ghi lai huong kien truc.

## 2. Vi sao khong nen bo MVC app hien tai

ASP.NET Core MVC app hien tai khong nen bi bo di vi:

- Da co nhieu business rules quan trong ve privacy va ownership.
- Da co database models va EF Core setup.
- Da co auth bang ASP.NET Core Identity.
- Da co UI demo de trinh bay san pham.
- Da co luong memory, image, album, trash va settings hoat dong.
- Co the lam internal web surface trong khi mobile app phat trien.

Viec viet lai tu dau se tang rui ro va lam mat nhieu logic da duoc harden.

## 3. MVC va API co the cung ton tai nhu the nao

MVC va API co the song song trong cung ASP.NET Core backend:

- MVC routes phuc vu web UI hien tai.
- API routes phuc vu Flutter app.
- Ca hai dung chung database va domain models.
- Ca hai phai tuan thu cung ownership/privacy rules.

Vi du:

- Web: `/Memories`, `/Albums`, `/Trash`, `/Settings`
- API: `/api/v1/memories`, `/api/v1/albums`, `/api/v1/trash`, `/api/v1/settings`

Khi them API, can tranh copy-paste logic privacy qua nhieu noi. Neu logic bat dau lap lai qua nhieu controller, co the tach thanh service nho, ro rang, beginner-friendly.

## 4. Mobile app modules

Flutter app nen duoc chia thanh cac module:

### Authentication

- Register.
- Login.
- Confirm email flow.
- Logout.
- Token/session handling.

### Timeline

- Danh sach memories.
- Sort newest/oldest.
- Empty state.
- Pull-to-refresh sau nay.

### Create/Edit Memory

- Title.
- Story/note.
- Feeling.
- Date.
- Location.
- Tags.
- Add/remove images.

### Private Image Upload/View

- Upload images.
- View authorized images.
- Handle missing/unauthorized images.
- Loading/error placeholders.

### Search/Filter

- Search keyword.
- Filter by feeling.
- Filter by tag.
- Date range.
- Month/year.

### Albums

- Album list.
- Album detail.
- Create/edit/delete album.
- Add/remove memories.

### Trash/Restore

- View deleted memories/albums.
- Restore memory.
- Restore album.
- No permanent delete in current MVP.

### Settings

- View account info.
- Edit display name.
- Change password.
- Privacy notes.

## 5. Mobile-first UX principles

- Quick capture: tao memory nhanh, khong qua nhieu buoc.
- Easy image upload: chon anh tu camera/gallery can de hieu.
- Minimal typing: khuyen khich note ngan, tags don gian.
- Warm private interface: cam giac rieng tu, nhe, khong giong social feed.
- One-hand friendly interactions: nut de bam, layout khong qua day.
- Offline-aware design later: co the can draft local/offline queue sau khi API on dinh, nhung chua implement luc nay.

## 6. Authentication considerations for mobile

### Cookie-based auth

Cookie auth phu hop voi MVC web app hien tai vi browser tu quan ly cookie.

Uu diem:

- Dang dung trong app hien tai.
- Don gian cho web MVC.
- Tich hop tot voi ASP.NET Core Identity.

Han che voi mobile:

- Flutter can quan ly cookie/session can than.
- Khong phai pattern mobile API pho bien nhat.
- Can review CSRF/session behavior rieng.

### Token-based auth

Token-based auth thuong phu hop hon cho mobile API.

Uu diem:

- Mobile app de gui token trong request.
- Phu hop voi `/api/v1/...`.
- Co the tach ro web MVC va mobile API.

Rui ro:

- Can thiet ke token storage an toan tren mobile.
- Can refresh token/expiration/revocation strategy.
- Can review logging de khong leak token.

Khuyen nghi: Khi bat dau API cho Flutter, nen nghien cuu token-based API auth. Chua implement trong phase nay. Day la decision can finalize truoc khi code API.

## 7. Image handling considerations

Trang thai hien tai:

- Local private storage trong `App_Data/uploads` phu hop cho local MVP.
- Images duoc serve qua authorized endpoint.
- Khong co direct public image URLs.

Truoc private beta/production:

- Can can nhac private object storage.
- Can backup uploaded images.
- Can thumbnails/compression truoc khi user upload nhieu anh.
- Can storage quota policy.
- Can performance plan cho gallery va timeline.

## 8. Khong tao Flutter project trong phase nay

Phase nay chi lap ke hoach. Chua tao:

- Flutter app source code.
- API controllers.
- Token auth.
- Mobile CI/CD.

Buoc tiep theo nen la chuan bi backend API foundation sau khi quyet dinh auth mobile.
