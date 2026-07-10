# MemoLens - Productization Plan

## 1. Trang thai hien tai

MemoLens hien la mot MVP web co day du nhieu tinh nang rieng tu quan trong:

- Dang ky, dang nhap, xac thuc email.
- Quan ly ky niem theo tung user.
- Upload anh rieng tu.
- Timeline, search/filter, tag.
- Album rieng tu.
- Trash/Restore.
- Settings & Privacy.
- QA checklist va mot so hardening rieng tu co ban.

Trang thai hien tai co the xem la demo-ready va MVP-ready. Tuy nhien, MemoLens chua production-ready cho nhieu nguoi dung that. Du an van thieu nhieu nen tang van hanh quan trong nhu email that, forgot password, backup, monitoring, storage strategy, thumbnails/compression, export data va delete account.

## 2. Muc tieu san pham dai han

Muc tieu dai han cua MemoLens la tro thanh mot ung dung nhat ky anh mobile-first rieng tu cho nguoi dung that.

MemoLens nen giup nguoi dung:

- Luu anh gan voi cau chuyen va cam xuc.
- Nhin lai ky niem theo timeline.
- Gom ky niem vao album rieng.
- Tim lai ky niem nhanh.
- Tin rang du lieu cua minh la rieng tu.

MemoLens khong nen tro thanh mang xa hoi.

## 3. Nguyen tac san pham

- Private-first: ky niem, anh, album, thung rac va settings la rieng tu theo mac dinh.
- Mobile-first: trai nghiem chinh trong tuong lai nen duoc toi uu cho dien thoai.
- User owns their memories: nguoi dung nen co quyen xuat va xoa du lieu trong cac phase sau.
- No social pressure: khong co like, comment, follower, feed cong khai.
- No public feed: khong xay dung huong kham pha noi dung cong khai.
- Simple emotional journaling: uu tien viec ghi lai cam xuc va cau chuyen hon cac tinh nang phuc tap.

## 4. Roadmap stages

### Stage 1: Current Web MVP

Da hoan thanh tinh nang cot loi tren ASP.NET Core MVC:

- Auth voi email confirmation.
- Memory CRUD.
- Private image upload/serving.
- Search/filter.
- Albums.
- Trash/Restore.
- Settings & Privacy.

Muc tieu cua stage nay la demo MVP va co nen tang de hoc tap/portfolio.

### Stage 2: Production foundation

Can bo sung truoc khi co nguoi dung that:

- SMTP/email provider that.
- Forgot password.
- Cau hinh secrets theo environment.
- Logging tot hon.
- Error pages than thien hon.
- Backup database.
- Backup uploaded images.
- Storage quota policy.
- Privacy policy va terms draft.

### Stage 3: Backend API foundation

Tao API layer rieng cho mobile app:

- `/api/v1/...`
- Auth API.
- Memories API.
- Images API.
- Albums API.
- Trash API.
- Settings API.

MVC app van giu lai lam web MVP/admin/demo/internal surface.

### Stage 4: Flutter mobile prototype

Tao Flutter app sau khi API direction ro rang:

- Login/register.
- Timeline.
- Create/Edit Memory.
- Upload/view private images.
- Albums.
- Trash/Restore.
- Settings.

Prototype nen tap trung vao luong mobile co ban, chua can day du tat ca production polish.

### Stage 5: Private beta

Moi mot nhom nho nguoi dung tin cay dung thu:

- Khong public app store ngay.
- Gioi han storage.
- Thu thap feedback ve quick capture, upload anh, feeling list, search/filter va cam giac rieng tu.

### Stage 6: Production hardening

Sau private beta:

- Fix bug va UX issues.
- Them monitoring.
- Review rate limiting/account lockout.
- Toi uu image storage.
- Them thumbnails/compression.
- Review performance voi nhieu anh.

### Stage 7: Public launch later

Chi nen nghi den public launch khi:

- Privacy policy/terms san sang.
- Backup va recovery da test.
- Auth va storage on dinh.
- Co plan chi phi image storage.
- Co support process co ban.

## 5. Chua nen uu tien luc nay

- AI.
- Public sharing.
- Social features.
- Complex recommendation system.
- Over-designed admin dashboard.
- Explore/trending/public feed.

Nhung thu nay co the lam MemoLens lech khoi gia tri rieng tu va lam tang do phuc tap qua som.

## 6. Nen uu tien

- Privacy va user isolation.
- Secure auth.
- Reliable file storage.
- Backup database va uploaded images.
- Mobile UX.
- Thumbnails/compression.
- Data export/delete account trong phase sau.
- Private beta feedback.
- Error handling va logging.

## 7. Ket luan

MemoLens co nen tang MVP tot, nhung chua nen goi la production-ready. Buoc di dung tiep theo la xay nen tang san xuat, sau do tao API cho Flutter mobile app, roi private beta voi nhom nguoi dung nho.
