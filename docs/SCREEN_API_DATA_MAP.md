# Screen - API - Data Map

## Album screens (Phase 19E Checkpoint 2C)

| Screen | Status | API | Private-data rule |
| --- | --- | --- | --- |
| Create Album | Implemented | `POST /api/v1/albums` with optional `memoryIds` | Active owner-only Memories are selected locally; one atomic request only |
| Album Details | Implemented | `GET /api/v1/albums/{id}` | Cover and related Memory covers use authenticated bytes |
| Edit Album | Implemented | `PUT /api/v1/albums/{id}` | Name/description only; no relationship or cover management |

| Screen | Status | Flutter route | Web route | API endpoint | Entity/Table | Auth | Privacy notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Splash/session | Implemented | `/` | N/A | `GET /api/v1/account/me`, refresh | ApplicationUser, UserRefreshToken | JWT | Không hiển thị token |
| Login/Register/confirm | Implemented | `/login`, `/register`, `/confirm-email` | `/Account/*` | auth register/login/confirm/resend | Identity user, tokens | Guest/JWT theo endpoint | Email confirmation required |
| Timeline | Implemented | `/home` | `/Memories` | `GET /api/v1/memories` | Memory, Tag, MemoryImage summary | JWT | Only current user, nondeleted |
| Create/Edit/Details Memory | Implemented | `/memories/create`, `/:id/edit`, `/:id` | `/Memories/Create/Edit/Details` | POST/GET/PUT `/api/v1/memories` | Memory, MemoryTag, Tag | JWT | owner-only; soft delete hidden |
| Private image gallery | Implemented in details | N/A | `/Images/MemoryImage/{id}` | `GET /api/v1/images/{id}/content` | MemoryImage/file | JWT/MVC cookie | No public URL; owner + active Memory |
| Upload/delete image | Implemented | form state | Memory edit | POST/DELETE image routes | MemoryImage/file | JWT/MVC cookie | owner-only; individual delete removes file |
| Album list | Implemented | tab trong `/home` | `/Albums/*` | `GET /api/v1/albums` | Album, AlbumMemory | JWT | Owner-only ListView; effective cover dùng authorized private bytes, không lộ path/URL public |
| Album details/forms | Planned Flutter | Không có route | `/Albums/*` | `/api/v1/albums` family | Album, AlbumMemory | JWT | Checkpoint 2C; không mở route workflow chưa hoàn chỉnh |
| Trash/restore | Planned Flutter | No confirmed route | `/Trash` | Memory/Album restore endpoints; **No confirmed mobile Trash list endpoint** | Memory, Album | JWT/MVC cookie | Deleted resources remain owner-scoped |
| Settings placeholder | Implemented placeholder | tab trong `/home` | `/Settings` | Không gọi settings API | N/A | JWT | Chỉ thông báo Phase 19G; không có profile/Trash/security action giả |

## Cover fields (Phase 19E Checkpoint 1)

Memory/Album API response giữ field cũ và cộng thêm `manualCoverImageId` (override nullable) và `effectiveCoverImageId` (ảnh hiển thị nullable). Cover content tiếp tục dùng `GET /api/v1/images/{imageId}/content` với JWT; không trả private path.
