# MemoLens - Production Risk Register

MemoLens hien la MVP/private demo. Bang nay ghi lai rui ro truoc khi tien den private beta va production.

| Risk | Why it matters | Current status | Severity | Mitigation plan | Suggested phase |
|---|---|---|---|---|---|
| Real email not configured | User that can confirm email va nhan thong bao | Development email sender only | High | Cau hinh SMTP/provider, environment secrets, email templates | Production foundation |
| Forgot password missing | User quen mat khau se bi khoa ngoai tai khoan | Chua co | High | Them forgot password/reset email flow bang Identity tokens | Production foundation |
| Local storage not enough for many users | Nhieu anh se lam day server disk | Local `App_Data/uploads` | High | Chuyen sang private object storage hoac storage volume co backup | Private beta prep |
| No thumbnail/compression | Anh goc lon lam cham timeline va ton storage | Chua co | High | Tao thumbnails, resize/compress upload, lazy loading | Private beta prep |
| Backup strategy missing | Mat database/file se lam mat ky niem user | Chua co documented backup | High | Backup SQL + uploaded images, test restore | Production foundation |
| Delete account missing | User ownership/data rights chua day du | Chua co | Medium | Thiet ke delete account va data cleanup strategy | Later privacy phase |
| Export data missing | User chua lay du lieu cua minh ra duoc | Chua co | Medium | Tao export archive cho memories/images/albums | Later privacy phase |
| No automated tests | Regression privacy co the xay ra | Manual QA la chinh | High | Them integration tests cho auth/ownership/image endpoints | MVP hardening continued |
| No monitoring/logging | Loi production kho phat hien | Logging co ban | Medium | Structured logging, error tracking, health checks | Production foundation |
| File upload abuse | User co the upload qua nhieu/qua lon/loai file la | Co validation co ban | High | Rate limit, quotas, malware/content checks neu can | Private beta prep |
| Cross-user data leak risk | Rui ro nghiem trong nhat voi private app | Da scope theo user va QA | High | Automated tests bat buoc cho moi private endpoint | API foundation |
| Image privacy leak risk | Anh la du lieu nhay cam | Authorized endpoint, no public URL cho upload moi | High | Tiep tuc cam direct static serving, object storage private, signed/authorized access | API/mobile phase |
| Database migration/deployment risk | Migration sai co the mat data | Local migrations only | Medium | Staging DB, migration review, backup before deploy | Production foundation |
| App store privacy policy requirement | Mobile launch can privacy policy/terms | Chua co policy/terms | High | Draft privacy policy va terms truoc beta/mobile distribution | Private beta prep |
| Cost of image storage | Anh co the tao chi phi tang nhanh | Chua co cost model | Medium | Quota, compression, storage pricing estimate | Private beta prep |
| User trust risk | Private memory app can niem tin cao | Brand/product direction tot, chua co legal docs | High | Minh bach privacy, no social direction, export/delete roadmap | Productization |
| Account lockout/rate limiting review needed | Bao ve login/register/upload khoi abuse | Identity lockout/rate limiting chua duoc review ky | Medium | Review Identity lockout, add rate limiting cho auth/upload | Production foundation |
| Error handling in production | Loi 500 co the leak/thieu than thien | Error handling co ban | Medium | Better production error pages, no sensitive details | Production foundation |
| Secrets/environment config | Secrets hardcoded/leak se nguy hiem | Admin seed env vars, SMTP chua co | High | User secrets/env vars, no secrets in repo, deployment secret manager | Production foundation |
| Performance with many images | Timeline/gallery co the cham | Chua test scale | Medium | Pagination, thumbnails, caching, query review | API/mobile phase |

## Ghi chu

Nhung rui ro High nen duoc xu ly truoc private beta neu co anh huong den privacy, auth, storage hoac kha nang mat du lieu.
