# MemoLens - Thiet ke xac thuc API cho Mobile

## 1. Muc dich

Phase 14A ghi lai thiet ke xac thuc API truoc khi MemoLens tao Flutter mobile app. Flutter can giao tiep voi backend qua JSON API, vi vay can mot co che xac thuc token-based phu hop voi thiet bi di dong.

MVC web app hien tai van dung cookie authentication cua ASP.NET Core Identity. Cookie phu hop voi browser va khong nen bi thay the trong phase nay. API token cho mobile va MVC cookie cho web co the cung ton tai tren cung backend, dung chung Identity va cung tuan thu quy tac private-first.

Tai lieu nay la thiet ke, khong phai code implementation.

## 2. Trang thai xac thuc hien tai

- ASP.NET Core Identity da duoc cau hinh.
- MVC da co Register, Login va Logout.
- Email phai duoc xac thuc truoc khi dang nhap.
- Role `Admin` va `User` da ton tai; user moi mac dinh nhan role `User`.
- Development email sender hien tai ghi confirmation link ra console/debug output.
- Chua co auth API.
- Chua co JWT, access token hoac refresh token.

## 3. Huong xac thuc de xuat cho mobile

Mobile API nen dung token-based authentication:

- JWT access token de goi cac endpoint `/api/v1/...`.
- Refresh token de lay access token moi khi access token het han.
- Access token co thoi han ngan de giam thiet hai neu bi lo.
- Refresh token co thoi han dai hon, co the revoke va phai duoc luu dang hash trong database, khong luu plain text.
- Flutter luu token bang secure storage cua he dieu hanh, khong luu trong text file, query string, hay shared preferences khong bao ve.
- Production bat buoc dung HTTPS.

JWT khong thay the MVC cookie. MVC tiep tuc dung cookie authentication; Flutter/mobile API dung bearer access token.

## 4. Vong doi token

1. **Dang ky:** Mobile gui email va password den auth API. Backend dung ASP.NET Core Identity tao user, gan role `User`, va gui email confirmation.
2. **Xac thuc email:** User mo confirmation link hoac gui confirmation token theo flow mobile. Tai khoan chi duoc login sau khi `EmailConfirmed` la `true`.
3. **Dang nhap:** Mobile gui email va password. Backend kiem tra thong tin dang nhap, email confirmation va cac quy tac lockout sau nay.
4. **Cap token:** Khi login thanh cong, backend cap access token ngan han va refresh token dai han.
5. **Lam moi access token:** Khi access token het han, mobile gui refresh token den endpoint refresh. Backend kiem tra hash, thoi han va trang thai revoke, sau do cap access token moi va nen rotate refresh token.
6. **Dang xuat:** Mobile gui refresh token hien tai den endpoint logout. Backend revoke token do va mobile xoa token khoi secure storage.
7. **Doi mat khau:** Phase sau phai revoke cac refresh token dang con hieu luc cua user sau khi doi mat khau thanh cong.
8. **Xoa tai khoan:** Khi co tinh nang nay, backend phai revoke toan bo refresh token cua user truoc khi xoa/an danh du lieu theo chinh sach da duoc chap thuan.

## 5. Cac endpoint auth de xuat

Tat ca endpoint du kien dung prefix `/api/v1` va response format trong `docs/API_FOUNDATION.md`.

### POST /api/v1/auth/register

- **Muc dich:** Tao tai khoan mobile moi va gui email confirmation.
- **Input:** Email, password, confirm password, display name tuy chon.
- **Thanh cong:** Tao user role `User`; khong cap token truoc khi email duoc xac thuc; tra response an toan khong lo Identity internals.
- **Validation va bao mat:** Dung Identity password validators; chuan hoa email; tranh thong bao lam lo user da ton tai qua muc; rate limit khi can; khong log password hay confirmation token.

### POST /api/v1/auth/login

- **Muc dich:** Xac thuc user va cap token cho mobile.
- **Input:** Email, password, thong tin thiet bi tuy chon de hien thi session sau nay.
- **Thanh cong:** Chi sau khi email da confirmed, tra access token, refresh token, thoi diem het han va thong tin account an toan toi thieu.
- **Validation va bao mat:** Khong tiet lo email co ton tai hay khong; ap dung lockout/rate limiting truoc private beta; khong log token; tu choi email chua xac thuc bang thong diep phu hop.

### POST /api/v1/auth/refresh

- **Muc dich:** Dung refresh token hop le de cap access token moi.
- **Input:** Refresh token.
- **Thanh cong:** Tra access token moi va refresh token moi neu dung rotation.
- **Validation va bao mat:** Kiem tra token hash, user, expiry va `RevokedAt`; revoke token cu khi rotate; token da revoke, het han hoac khong hop le phai bi tu choi ma khong lo thong tin.

### POST /api/v1/auth/logout

- **Muc dich:** Huy refresh token cua phien mobile hien tai.
- **Input:** Refresh token hoac dinh danh phien an toan theo thiet ke implementation.
- **Thanh cong:** Refresh token hien tai bi revoke; mobile xoa token local.
- **Validation va bao mat:** Logout phai idempotent khi co the; khong duoc de token cu tiep tuc refresh access token.

### POST /api/v1/auth/confirm-email

- **Muc dich:** Xac nhan email cua tai khoan mobile.
- **Input:** User identifier an toan va confirmation token, hoac token da duoc ma hoa trong confirmation link.
- **Thanh cong:** Dat `EmailConfirmed` thanh `true`; user co the dang nhap.
- **Validation va bao mat:** Xu ly token hong/het han an toan; khong crash; khong log token; khong doi trang thai cua user khac.

### POST /api/v1/auth/resend-confirmation-email

- **Muc dich:** Gui lai email confirmation.
- **Input:** Email.
- **Thanh cong:** Gui email neu phu hop, hoac tra response chung de tranh user enumeration.
- **Validation va bao mat:** Rate limit theo email/IP; production phai dung email provider that; khong log confirmation link/token.

### POST /api/v1/auth/forgot-password

- **Muc dich:** Bat dau luong dat lai mat khau.
- **Input:** Email.
- **Thanh cong:** Gui reset link/token neu tai khoan phu hop, nhung response nen chung.
- **Validation va bao mat:** Dung ASP.NET Core Identity token flow; rate limit; khong tiet lo email co ton tai; production dung email provider that.

### POST /api/v1/auth/reset-password

- **Muc dich:** Dat mat khau moi bang reset token hop le.
- **Input:** Email, reset token, password moi, confirm password.
- **Thanh cong:** Mat khau duoc doi theo Identity validators; tat ca refresh token dang hoat dong cua user bi revoke.
- **Validation va bao mat:** Khong log password/token; xu ly token hong/het han an toan; response validation theo field.

### GET /api/v1/account/me

- **Muc dich:** Lay thong tin account cua user dang xac thuc.
- **Input:** Bearer access token trong header `Authorization`.
- **Thanh cong:** Tra thong tin an toan nhu id, email, display name, email confirmation status va role can thiet.
- **Validation va bao mat:** Chi tra user hien tai; khong tra password hash, security stamp, refresh token, hoac Identity internals; khong cho Admin doc account cua user khac qua endpoint nay.

## 6. Email confirmation tren mobile

MVC hien tai dung confirmation link do development email sender ghi ra console/debug output. Flow nay phu hop cho local development, nhung private beta va production can email provider that.

Mobile flow trong tuong lai nen ho tro mot trong hai cach, hoac ket hop ca hai:

- User mo confirmation link tren thiet bi; link dua den web confirmation page hoac deep link tro ve mobile app.
- Mobile gui confirmation token den `POST /api/v1/auth/confirm-email`.

Du dung cach nao, login van phai bi chan cho den khi email duoc xac thuc. Confirmation token khong duoc hien thi trong log production hoac dua vao URL cua cac request API khac.

## 7. Forgot password va reset password

Forgot password va reset password da duoc implement cho ca MVC va API. Hai flow dung token cua ASP.NET Core Identity thay vi tu tao password reset token.

API forgot password luon tra response chung de tranh user enumeration. Sau reset thanh cong, MemoLens revoke tat ca refresh token dang hoat dong cua user, khong auto-login va khong cap token hoac MVC cookie. Production van can email provider that, email template an toan, HTTPS va rate limiting.

## 8. Roles va authorization

- Role mac dinh cua user binh thuong la `User`.
- Role `Admin` da ton tai, nhung khong duoc bypass quyen so huu noi dung rieng tu trong MVP.
- Moi API cua memories, albums, images, trash va settings phai lay current user tu auth context va filter bang `UserId`.
- Normal API khong duoc cho phep Admin xem memory, album, image hoac trash cua user khac.
- Neu sau nay can Admin API, no phai tach rieng, duoc xac dinh pham vi can than va duoc review privacy/security truoc khi them.

## 9. Quy tac bao mat

- Khong luu plain text password.
- Khong luu plain text refresh token.
- Khong expose private file path hoac direct public image URL.
- Khong expose su ton tai cua noi dung cua user khac.
- Dung HTTPS trong production.
- Review rate limiting va lockout truoc private beta.
- Giu access token ngan han.
- Revoke refresh token khi logout.
- Revoke token khi doi mat khau trong phase sau.
- Khong dua token vao query string.
- Tranh log access token, refresh token, password, confirmation token va reset token.
- Unauthorized, missing hoac soft-deleted private content nen tra `404 NotFound` khi phu hop de giam user/content enumeration.

## 10. Thay doi database co the can sau nay

Phase nay khong thay doi database. Khi bat dau Phase 14B, co the them model/table `ApiRefreshToken` hoac `UserRefreshToken`:

| Field | Muc dich |
| --- | --- |
| `Id` | Khoa chinh cua refresh token record. |
| `UserId` | Khoa ngoai toi `ApplicationUser`. |
| `TokenHash` | Hash cua refresh token, khong luu token goc. |
| `CreatedAt` | Thoi diem token duoc tao. |
| `ExpiresAt` | Thoi diem token het han. |
| `RevokedAt` | Nullable; thoi diem token bi revoke. |
| `ReplacedByTokenHash` | Nullable; danh vet token moi khi refresh token rotation. |
| `DeviceName` | Nullable; mo ta thiet bi de quan ly session sau nay. |
| `UserAgent` | Nullable; thong tin client phuc vu audit han che. |
| `IpAddress` | Nullable; thong tin audit can duoc xem xet theo chinh sach privacy. |
| `IsRevoked` / `IsActive` | Y tuong computed property dua tren `RevokedAt` va `ExpiresAt`. |

Day chi la de xuat thiet ke. Model, migration va database schema chua duoc them.

## 11. Cac phase implementation de xuat

1. **Phase 14B (da hoan thanh):** Them refresh token model, migration, JWT configuration va token service infrastructure.
2. **Phase 14C (da hoan thanh):** Implement API register, login, refresh, logout va account/me.
3. **Phase 15C (da hoan thanh):** Implement API email confirmation va resend confirmation email.
4. **Phase 15D (da hoan thanh):** Implement API forgot password va reset password.
5. **Sau do:** Revoke token khi doi mat khau, UI quan ly device sessions, va hardening lockout/rate limiting.

Moi phase can nho, co manual QA, co test ownership/privacy va khong duoc lam hong MVC cookie auth hien co.

## 12. Rui ro can quan ly

- Token theft.
- Refresh token leakage.
- Access token co thoi han qua dai.
- Logout yeu khien refresh token cu van dung duoc.
- Thieu rate limiting hoac lockout.
- User enumeration qua login, register, resend email hoac forgot password.
- Email delivery failure.
- Mobile luu token khong an toan.
- Admin privilege misuse.
- Cross-user data leak qua API.

## 13. Khuyen nghi cuoi cung

Khong nen implement API auth trong mot task lon. Nen thuc hien theo cac phase nho o tren, review bao mat sau tung phase va giu MVC cookie authentication hoat dong binh thuong.

Khi bat dau implementation auth API thuc te, nen dung model **5.6 Sol / High** vi day la phan nhay cam ve bao mat.

## 14. Trang thai implementation Phase 14B

Phase 14B da them phan ha tang sau:

- `JwtOptions` voi issuer, audience, secret key, access-token lifetime va refresh-token lifetime co the cau hinh.
- JWT bearer scheme rieng cho API tuong lai; MVC van dung Identity cookie lam co che xac thuc mac dinh.
- `UserRefreshToken` va migration `AddUserRefreshTokens`.
- Index cho `UserId`, `TokenHash` va `ExpiresAt`; `TokenHash` la unique.
- `ITokenService` va `TokenService` de sinh access token, sinh refresh token ngau nhien, hash refresh token va so sanh hash constant-time.
- Development chi dung placeholder secret. Production phai dung environment variables hoac user secrets.

Phase 14B chua them bat ky auth API endpoint nao. Register, login, refresh, logout, confirm email, forgot/reset password va account/me van la ke hoach cho cac phase sau.

## 15. Trang thai implementation Phase 14C

Phase 14C da implement cac endpoint cot loi:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/account/me`

Quy tac da ap dung:

- Register tao Identity user role `User`, gui confirmation link qua development email sender va khong cap token.
- Login chi cap token khi email da confirmed; login API khong tao MVC cookie.
- Access token co lifetime 15 phut.
- Refresh token co lifetime 30 ngay va chi duoc persist dang SHA-256 hash.
- Refresh token rotation revoke token cu, luu `ReplacedByTokenHash` va tao hash token moi trong cung transaction.
- Atomic conditional update chan hai request cung rotate mot token cu.
- Logout revoke refresh token nhung khong anh huong MVC cookie session.
- `account/me` bat buoc dung JWT bearer scheme va chi tra current user summary.

Van chua implement:

- Device sessions UI, password-change token revocation va rate limiting hardening.

## 16. Trang thai implementation Phase 15C

Phase 15C da implement:

- `POST /api/v1/auth/confirm-email` de xac nhan email bang `userId` va token Base64Url tu confirmation link.
- `POST /api/v1/auth/resend-confirmation-email` voi response chung cho email khong ton tai, da confirmed hoac chua confirmed.
- Confirm email thanh cong khong auto-login, khong tao MVC cookie va khong cap access/refresh token.
- Resend chi gui email khi user ton tai va chua confirmed; Development tiep tuc log confirmation link qua email sender hien co.
- Link gui lai van tro den MVC `Account/ConfirmEmail`, giu tuong thich voi web flow hien tai.

Van chua implement:

- Rate limiting cho register, login, resend confirmation va password recovery.
- Production email provider da duoc cau hinh bang credential that.

## 17. Trang thai implementation Phase 15D

Phase 15D da implement:

- `POST /api/v1/auth/forgot-password` voi response chung cho email khong ton tai, chua confirmed hoac da confirmed.
- Chi user ton tai va da confirmed moi duoc tao Identity password reset token va gui reset link.
- `POST /api/v1/auth/reset-password` dung `UserManager.ResetPasswordAsync` va password validators hien co.
- Token sai, het han hoac khong phu hop tra thong bao an toan; weak password tra validation error tieng Viet theo field.
- Reset thanh cong khong auto-login, khong tao MVC cookie va khong cap access/refresh token.
- Tat ca refresh token dang hoat dong cua user duoc revoke trong cung database transaction voi password reset.
- Development reset link tuong thich voi trang MVC `/Account/ResetPassword`; Flutter deep link la cong viec tuong lai.

Van chua implement:

- Rate limiting cho password recovery.
- Production email provider da duoc cau hinh bang credential that.
- Flutter client va mobile deep link.
- Automated integration tests.
