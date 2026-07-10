# MemoLens - MVP QA Checklist

Tai lieu nay dung de kiem tra MemoLens truoc khi demo MVP. MemoLens la private photo journal va memory storytelling app, khong phai mang xa hoi.

## 1. Auth checklist

- [ ] Register hien validation khi email hoac password khong hop le.
- [ ] Register tao user moi voi role `User`.
- [ ] Email confirmation bat buoc truoc khi login.
- [ ] Confirmation link hop le xac thuc duoc email.
- [ ] Confirmation token sai hoac hong khong lam app crash.
- [ ] Login bang email chua xac thuc bi chan.
- [ ] Login bang email/password sai bi tu choi.
- [ ] Login thanh cong redirect ve Timeline.
- [ ] Logout dung POST va anti-forgery token.
- [ ] Guest van xem duoc Home va Privacy.

## 2. User isolation checklist

- [ ] Guest khong vao duoc `/Memories`.
- [ ] Guest khong vao duoc `/Albums`.
- [ ] Guest khong vao duoc `/Trash`.
- [ ] Guest khong vao duoc `/Settings`.
- [ ] Guest khong xem duoc private image endpoint.
- [ ] User A khong xem duoc memory cua User B.
- [ ] User A khong sua/xoa duoc memory cua User B.
- [ ] User A khong xem duoc album cua User B.
- [ ] User A khong add/remove memory trong album cua User B.
- [ ] User A khong restore duoc trash item cua User B.
- [ ] User A khong xem duoc image cua User B.
- [ ] Admin role khong bypass ownership trong MVP hien tai.

## 3. Memory CRUD checklist

- [ ] Create Memory yeu cau title, feeling va memory date.
- [ ] Create Memory trim title va optional fields.
- [ ] Details chi hien memory cua current user va chua bi xoa.
- [ ] Edit Memory chi sua memory cua current user va chua bi xoa.
- [ ] Delete Memory dung soft delete.
- [ ] Delete Memory dung POST va anti-forgery token.
- [ ] Memory da soft delete khong hien trong Timeline.
- [ ] Memory da soft delete khong truy cap duoc Details/Edit/Delete.
- [ ] Empty/null story hoac location khong lam crash UI.

## 4. Image upload/private serving checklist

- [ ] Upload toi da 10 anh cho mot memory.
- [ ] Anh tren 5MB bi chan.
- [ ] Chi chap nhan `.jpg`, `.jpeg`, `.png`, `.webp`.
- [ ] Chan `.svg`, `.gif`, `.heic`, `.exe` va file khong ro extension.
- [ ] File moi luu ngoai `wwwroot`, trong `App_Data/uploads`.
- [ ] Database chi luu image path, khong luu binary image.
- [ ] UI dung `/Images/MemoryImage/{id}`, khong dung direct file URL.
- [ ] Missing image id tra `NotFound`.
- [ ] Unauthorized image tra `NotFound`.
- [ ] Image cua soft-deleted memory tra `NotFound`.
- [ ] Missing physical file tra `NotFound`.
- [ ] Delete image dung POST va anti-forgery token.
- [ ] Delete image xoa database row va file private neu file ton tai.
- [ ] UI khong leak private physical file path.

## 5. Albums checklist

- [ ] Album list chi hien album cua current user va chua bi xoa.
- [ ] Create/Edit album validation title va description.
- [ ] Details chi hien album cua current user va chua bi xoa.
- [ ] Album details khong hien deleted memories.
- [ ] Album details chi hien memories cung owner voi album.
- [ ] Add Memories chi hien non-deleted memories cua current user.
- [ ] Add Memories khong them duplicate relationship.
- [ ] Remove Memory chi xoa quan he album-memory, khong xoa memory goc.
- [ ] Delete Album dung soft delete.
- [ ] Delete Album dung POST va anti-forgery token.

## 6. Trash/Restore checklist

- [ ] Trash chi hien deleted memories/albums cua current user.
- [ ] Non-deleted memories/albums khong hien trong Trash.
- [ ] Restore Memory dung POST va anti-forgery token.
- [ ] Restore Album dung POST va anti-forgery token.
- [ ] User A khong restore duoc item cua User B.
- [ ] Restore Memory dua memory ve Timeline.
- [ ] Restore Memory khoi phuc image access qua authorized endpoint.
- [ ] Restore Album dua album ve Album list.
- [ ] Restore Album giu relationship album-memory hien co.
- [ ] Restore Album khong tu dong restore deleted memories ben trong.
- [ ] Permanent delete chua duoc implement.

## 7. Settings checklist

- [ ] Guest khong vao duoc `/Settings`.
- [ ] Logged-in confirmed user vao duoc Settings.
- [ ] Settings hien display name, email va email confirmation status.
- [ ] Settings khong co UI doi email.
- [ ] Edit display name chi update current user.
- [ ] Empty display name khong lam vo navbar/settings UI.
- [ ] Change password dung `UserManager.ChangePasswordAsync`.
- [ ] Change password yeu cau current password.
- [ ] Wrong current password bi tu choi.
- [ ] Confirm password mismatch hien validation.
- [ ] Sau khi doi password, login bang password moi thanh cong.
- [ ] Sau khi doi password, login bang password cu bi chan.
- [ ] Khong co forgot password, export data, delete account, admin settings panel.

## 8. Search/filter checklist

- [ ] Search theo title/story/location/tag chi trong current user.
- [ ] Search khong hien deleted memories.
- [ ] Filter feeling chi trong current user.
- [ ] Filter tag chi dung tag cua non-deleted memories cua current user.
- [ ] Date range hop le tra ket qua dung.
- [ ] FromDate lon hon ToDate hien validation va khong crash.
- [ ] Month khong co Year hien message nhe va bo qua month filter.
- [ ] Sort newest/oldest hoat dong on dinh.

## 9. Mobile UI checklist

- [ ] Navbar mobile bung/thu duoc va khong bi chen chuc.
- [ ] Timeline cards stack tu nhien tren dien thoai.
- [ ] Search/filter panel khong qua dai hoac ngop tren mobile.
- [ ] Create/Edit Memory form co input va button de bam.
- [ ] Upload control de dung tren mobile.
- [ ] Existing image delete buttons de bam.
- [ ] Memory details va gallery responsive.
- [ ] Albums pages dung cards, khong dung table day dac.
- [ ] Trash cards stack mot cot tren mobile.
- [ ] Settings cards/forms stack mot cot tren mobile.
- [ ] Khong co horizontal overflow o viewport dien thoai pho bien.

## 10. Demo preparation checklist

- [ ] Chay `dotnet restore`.
- [ ] Chay `dotnet tool restore`.
- [ ] Chay `dotnet tool run dotnet-ef database update`.
- [ ] Chay `dotnet build`.
- [ ] Chay `dotnet run`.
- [ ] Tao user demo va lay confirmation link tu console/log.
- [ ] Confirm email truoc khi login.
- [ ] Tao memory co anh.
- [ ] Thu search/filter Timeline.
- [ ] Tao album va them memory vao album.
- [ ] Xoa mem memory/album va restore tu Trash.
- [ ] Vao Settings, sua display name va doi password.
- [ ] Kiem tra demo tren mobile viewport.
- [ ] Dam bao khong co feed, like, comment, follower, public profile, public sharing hoac AI trong demo.
