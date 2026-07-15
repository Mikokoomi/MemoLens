# Screen - API - Data Map

| Screen | Status | Flutter route | Web route | API endpoint | Entity/Table | Auth | Privacy notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Splash/session | Implemented | `/` | N/A | `GET /api/v1/account/me`, refresh | ApplicationUser, UserRefreshToken | JWT | Không hiển thị token |
| Login/Register/confirm | Implemented | `/login`, `/register`, `/confirm-email` | `/Account/*` | auth register/login/confirm/resend | Identity user, tokens | Guest/JWT theo endpoint | Email confirmation required |
| Timeline | Implemented | `/home` | `/Memories` | `GET /api/v1/memories` | Memory, Tag, MemoryImage summary | JWT | Only current user, nondeleted |
| Create/Edit/Details Memory | Implemented | `/memories/create`, `/:id/edit`, `/:id` | `/Memories/Create/Edit/Details` | POST/GET/PUT `/api/v1/memories` | Memory, MemoryTag, Tag | JWT | owner-only; soft delete hidden |
| Private image gallery | Implemented in details | N/A | `/Images/MemoryImage/{id}` | `GET /api/v1/images/{id}/content` | MemoryImage/file | JWT/MVC cookie | No public URL; owner + active Memory |
| Upload/delete image | Implemented | form state | Memory edit | POST/DELETE image routes | MemoryImage/file | JWT/MVC cookie | owner-only; individual delete removes file |
| Albums list/details/forms | Planned Flutter | No confirmed route | `/Albums/*` | `/api/v1/albums` family | Album, AlbumMemory | JWT | Existing API owner-only; Flutter not implemented |
| Trash/restore | Planned Flutter | No confirmed route | `/Trash` | Memory/Album restore endpoints; **No confirmed mobile Trash list endpoint** | Memory, Album | JWT/MVC cookie | Deleted resources remain owner-scoped |
| Settings/Profile | Planned Flutter except logout | No confirmed route | `/Settings` | account/me, logout; no confirmed full settings API | ApplicationUser, tokens | JWT | No account settings Flutter screen |
