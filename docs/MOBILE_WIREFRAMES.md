# Low-fidelity Mobile Wireframes

Các sơ đồ dưới đây mô tả cấu trúc, không phải thiết kế Paper Note cuối cùng. Screen **Planned** chưa được phê duyệt để triển khai.

| Screen | Status | Khung cấu trúc, entry / exit |
| --- | --- | --- |
| Splash | Implemented | `[Logo] [Đang khôi phục phiên...]` -> Login hoặc Timeline |
| Login | Implemented | `[Email] [Password] [Đăng nhập] [Tạo tài khoản] [Quên mật khẩu: Planned]` |
| Register | Implemented | `[Tên] [Email] [Password] [Đăng ký]` -> Email confirmation |
| Email confirmation | Implemented | `[Kiểm tra email] [Gửi lại] [Về Login]` |
| Timeline | Implemented | `[Title] [Search][Filter] [Memory cards] [+ Create]` -> Details/Create |
| Timeline empty/loading/error | Implemented | `[Loading]` hoặc `[Chưa có Memory] [+ Create]` hoặc `[Error][Retry]` |
| Create Memory | Implemented | `[Title][Story][Date][Feeling][Location][Tags][Selected images][Save]` -> Details |
| Create + preview | Implemented | như Create + `[thumbnail x N][remove]` |
| Partial-success | Implemented | `[Text đã lưu] [Retry image upload] [Continue without images]` |
| Edit Memory | Implemented | như Create + existing private images + delete confirm |
| Memory details | Implemented | `[Back][Title][metadata][story][private gallery][Edit][Delete]` |
| Private gallery | Implemented trong Details | `[authenticated image bytes][loading/error]` |
| Albums list | Planned | `[Albums][empty/list][Create]` -> Album details |
| Album details | Planned | `[Title][Memory cards][add/remove]` -> Back |
| Create/edit Album | Planned | `[Title][Description][Save]` |
| Trash | Planned | `[Deleted Memories][Deleted Albums][Restore]` |
| Settings/Profile | Planned | `[Account][Security][Logout]` |

## Quy tắc wireframe

- Mọi private content screen phải có loading, error và empty state tại vùng nội dung.
- Navigation cuối/chrome chính là **Open decision**; không coi các wireframe này là phê duyệt Option A/B/C.
- Không có likes, comments, follower, public profile, explore hoặc share public.
- Áp dụng hướng Paper Note đã freeze khi port UI sau này, nhưng không dùng wireframe như bản thiết kế hi-fi.
