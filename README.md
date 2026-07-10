# MemoLens

MemoLens is a private photo journal and memory storytelling web app. It helps users save personal memories with photos, notes, feelings, dates, places, tags, and albums.

MemoLens is not a social network. It has no public feed, likes, comments, followers, public profiles, or explore page.

## Tech Stack

- ASP.NET Core MVC
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- Bootstrap
- GitHub

## How to Run Locally

1. Install the .NET 8 SDK.
2. Open a terminal in the repository root.
3. Restore packages and local tools:

```bash
dotnet restore
dotnet tool restore
```

4. Apply migrations to create or update the LocalDB database:

```bash
dotnet tool run dotnet-ef database update
```

5. Run the project:

```bash
dotnet run
```

6. Open the local URL shown in the terminal, usually `https://localhost:xxxx` or `http://localhost:xxxx`.

If the HTTPS development certificate is not trusted yet, run:

```bash
dotnet dev-certs https --trust
```

## Automated Testing

Run the automated smoke tests with:

```bash
dotnet test
```

The test suite uses an isolated SQLite in-memory database and does not use LocalDB development data. It currently covers smoke checks, core API auth, and MVC privacy/ownership integration flows. See [docs/TESTING.md](docs/TESTING.md) for current coverage and the test roadmap.

## Database Setup Notes

The SQL Server connection string is stored in `appsettings.json` under:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MemoLensDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

The default connection uses SQL Server LocalDB so the project stays beginner-friendly for local development.

The current database, ownership, soft-delete, storage, index, and backup review is documented in [docs/DATABASE_REVIEW.md](docs/DATABASE_REVIEW.md).
The future safe cleanup approach for private files, unused tags, permanent delete, and backup/restore is documented in [docs/DATA_CLEANUP_STRATEGY.md](docs/DATA_CLEANUP_STRATEGY.md).
Migration `AddPerformanceIndexes` adds composite indexes for current user-scoped memory and album queries without changing application behavior.

To add a new migration later, run:

```bash
dotnet tool run dotnet-ef migrations add MigrationName
```

## Authentication Status

- ASP.NET Core Identity is configured.
- Login uses email and password.
- Email confirmation is required before login.
- Register creates a user, assigns the `User` role, and sends a confirmation link through the development email sender.
- Login supports Remember Me.
- Successful login redirects to the private Timeline page.
- Logout redirects to Home.
- Home and Privacy remain public.
- MVC users can request a password reset from the Login page without revealing whether an email exists.
- A valid Identity reset token is required; successful reset does not auto-login the user.

## Email and Development Confirmation

Email confirmation remains required before login for both MVC and API registration.

In Development, MemoLens writes a clearly labeled `[MemoLens Development Email]` block to the terminal/console. It includes the local confirmation link needed for testing. The Register Confirmation page also reminds the user to check the terminal/console, without exposing the raw link in the UI.

SMTP delivery is prepared through configuration but no real provider credentials are committed. Production values must come from User Secrets, environment variables, or server configuration. See `docs/EMAIL_SETUP.md` for the Development workflow, SMTP configuration keys, secret handling, and current limitations.

MVC forgot/reset password is available at `/Account/ForgotPassword`. In Development, the reset link is written to the same clearly labeled email log. After a successful reset, active API refresh tokens for that user are revoked; existing short-lived access tokens expire naturally.

Refresh-token record cleanup is available as a background service. It is disabled by default and, when explicitly enabled through `RefreshTokenCleanup`, removes only revoked or expired records that are older than the configured retention period.

## Role Notes

The app seeds two roles during startup:

- `Admin`
- `User`

New registered users are assigned the `User` role by default.

No permanent admin account or visible admin password is hardcoded. If an initial admin is needed for local development, provide these values using environment variables or development configuration:

```bash
IdentitySeed__AdminEmail
IdentitySeed__AdminPassword
```

If those values are missing, MemoLens only seeds the roles.

## Memory CRUD and Image Upload Status

Phase 4 memory CRUD is completed, and Phase 5 image upload is completed:

- Logged-in users can create, view, edit, and delete their own text-based memories.
- Timeline uses `MemoriesController.Index`.
- Create Memory uses `MemoriesController.Create`.
- Memory queries are scoped to the current logged-in user.
- Admin users do not browse other users' private memories in this phase.
- Delete is a soft delete using `IsDeleted` and `DeletedAt`.
- Deleted memories disappear from the normal timeline.
- Create Memory supports uploading photos.
- Edit Memory supports adding more photos and deleting individual photos.
- Detail pages show a simple private gallery.
- Timeline shows the first uploaded image as the memory cover when available.

## Image Upload Rules

MemoLens stores image files on disk and stores only private relative image paths in the database. It does not store binary image data in SQL Server.

Allowed image formats:

- `.jpg`
- `.jpeg`
- `.png`
- `.webp`

Limits:

- Maximum 10 images per memory.
- Maximum 5MB per image.
- SVG, GIF, HEIC, executable files, and unknown file types are blocked.
- Saved file names use generated GUID values, not the original file names.
- The original file name is kept only as metadata in `MemoryImages.OriginalFileName`.

Upload folder behavior:

```text
App_Data/uploads/memories/{userId}/{memoryId}/{generated-file-name}
```

`App_Data/uploads` is outside `wwwroot`, and MemoLens does not enable static file serving or directory browsing for this private folder.

## Private Image Serving

Phase 8 private image storage and authorized image serving is completed.

- Newly uploaded memory images are saved outside `wwwroot` under `App_Data/uploads`.
- Image files are served through `ImagesController.MemoryImage(int id)`.
- The image endpoint requires authentication.
- The endpoint checks that the `MemoryImage` belongs to the current logged-in user's non-deleted memory.
- Unauthorized users, other users, missing files, and soft-deleted memories receive `NotFound`.
- Admin users do not browse other users' private images in this phase.
- Views use the authorized image endpoint instead of direct `ImagePath` URLs.
- Individual image delete removes both the `MemoryImage` database record and the physical private file if it exists.
- Soft deleting a memory hides it from the normal timeline but does not delete image files.

Why this changed:

Earlier development uploads used `wwwroot/uploads`, which meant a file could be served directly if someone knew the URL. Phase 8 fixes that privacy issue for new uploads by storing files outside static web root and serving them only after ownership checks.

Backward compatibility note:

Existing development records may still point to old `wwwroot/uploads` paths. Phase 8 does not include a complex old-file migration helper. Old dev uploads may need to be re-uploaded. Missing old files are handled safely by returning `NotFound` from the image endpoint and showing a placeholder instead of crashing the page.

Current image limitations:

- No thumbnails yet.
- No image resizing or compression yet.
- Local private storage only.
- No cloud storage yet.

## Trash and Restore

Phase 9 trash and restore is completed.

- Logged-in confirmed users can open a private Trash page for their own deleted memories and albums.
- Trash only shows items where `IsDeleted` is true and `UserId` matches the current logged-in user.
- Users can restore a deleted memory or album from Trash.
- Restoring sets `IsDeleted` back to false, clears `DeletedAt`, and updates `UpdatedAt`.
- No permanent delete is implemented in this phase.
- Soft-deleted memories remain hidden from the normal timeline until restored.
- Soft-deleted albums remain hidden from the normal Albums list until restored.
- Restoring an album keeps its existing album-memory relationships.
- Restoring an album does not automatically restore any deleted memories inside it.
- The private image endpoint behavior is unchanged: images from soft-deleted memories still return `NotFound` until the memory is restored.
- Admin users do not browse or restore other users' deleted memories or albums in this phase.
- Trash uses mobile-first cards instead of tables so it stays usable on phones.

## Settings and Privacy

Phase 10 settings and privacy is completed.

- Logged-in confirmed users can open a private Settings page.
- Settings shows the current user's display name, email, email confirmation status, and account creation date.
- Users can edit only their own display name.
- Display name is optional. If it is empty, the UI falls back to the user's email.
- Users can change their password after entering the current password.
- Password changes use ASP.NET Core Identity through `UserManager.ChangePasswordAsync`.
- After profile or password changes, MemoLens refreshes the current sign-in session.
- Settings includes privacy notes explaining that memories, albums, images, and trash are private to the current account.
- The Settings link is only shown to logged-in users.

Current Settings limitations:

- No email change.
- MVC and API forgot/reset password flows are available.
- No export data.
- No delete account.
- No admin settings panel.

## MVP QA and Hardening

Phase 11 MVP QA and hardening is completed.

- A reusable MVP QA checklist is available at `docs/MVP_QA_CHECKLIST.md`.
- Authorization and ownership checks were reviewed for memories, albums, trash, settings, and private images.
- Guest users are still redirected away from private pages.
- User-scoped queries remain the rule for private content.
- Private image serving continues to return `NotFound` for missing, unauthorized, soft-deleted, or missing-file cases.
- Email confirmation now handles malformed confirmation tokens without crashing the app.
- Album display logic now filters album-memory relationships by album owner as a defensive privacy check.
- No database schema changes or migrations were added in this phase.
- No major new product features were added.

## Productization / Mobile App Direction

Phase 12 productization and mobile architecture planning is completed.

MemoLens aims to evolve from a web MVP into a private mobile-first app for real users. The planned mobile direction is Flutter, while the existing ASP.NET Core backend will be retained and gradually extended with Web APIs.

Important direction:

- The current ASP.NET Core MVC app remains useful as the MVP/demo/internal web surface.
- A future `/api/v1/...` Web API layer should serve the Flutter mobile app.
- Private beta is the next realistic launch goal, not an immediate public app store launch.
- MemoLens is still not production-ready yet.
- The product must remain private-first and must not become a social network.

Planning documents:

- `docs/PRODUCTIZATION_PLAN.md`
- `docs/MOBILE_ARCHITECTURE.md`
- `docs/API_ROADMAP.md`
- `docs/API_FOUNDATION.md`
- `docs/API_AUTH_DESIGN.md`
- `docs/PRIVATE_BETA_PLAN.md`
- `docs/PRODUCTION_RISK_REGISTER.md`

## API Foundation

Phase 13 API foundation is completed.

Current API foundation:

- `GET /api/v1/health` returns a small JSON health check for the MemoLens API.
- API responses use a standard format with `success`, `message`, and optional `data`.
- Validation error responses can return field-based `errors`.
- Swagger/OpenAPI is enabled only in `Development`.
- Swagger is available at `/swagger` when running the app locally in Development.

Current API limitations:

- No memory CRUD API yet.
- No album CRUD API yet.
- No image upload API yet.
- No Flutter app yet.

See `docs/API_FOUNDATION.md` for the current API shape, response format, Swagger notes, limitations, and future privacy rules.

## API Authentication Design

Phase 14A records the authentication design, Phase 14B adds the supporting infrastructure, and Phase 14C implements the core authentication endpoints for a future Flutter/mobile API.

- JWT options, bearer validation, a token service, and the `UserRefreshTokens` table are now available as infrastructure.
- Refresh tokens are designed to be persisted as SHA-256 hashes only; plain tokens must only be returned to the mobile client when future auth endpoints are implemented.
- Core endpoints are available at `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, and `GET /api/v1/account/me`.
- Email confirmation endpoints are available at `POST /api/v1/auth/confirm-email` and `POST /api/v1/auth/resend-confirmation-email`.
- API login requires a confirmed email, returns a 15-minute JWT access token, and stores only the SHA-256 hash of each 30-day refresh token.
- Refresh rotates the refresh token atomically; logout revokes the supplied refresh token.
- Resend confirmation always returns a generic response to avoid revealing whether an account exists or is already confirmed.
- Password recovery is available at `POST /api/v1/auth/forgot-password` and `POST /api/v1/auth/reset-password`.
- Forgot password always returns a generic response. A successful reset issues no token or cookie, revokes all active API refresh tokens for the user, and requires a new login.
- The existing MVC web authentication remains cookie-based and unchanged.
- Development JWT values are placeholders in `appsettings.Development.json`. Production values must come from environment variables or user secrets, especially `Jwt__SecretKey`.
- Phase 14B.5 aligns the Microsoft ASP.NET Core and EF Core packages, plus the local `dotnet-ef` tool, on `8.0.28`. The NuGet vulnerability audit is clean after this patch.

See `docs/API_AUTH_DESIGN.md` for the proposed token lifecycle, endpoint plan, email confirmation flow, security rules, and phased implementation approach.

The Phase 14C.5 security review, manual auth test cases, expected status codes, refresh-token rotation checks, and MVC regression results are recorded in `docs/API_AUTH_QA_CHECKLIST.md`.

## Timeline Search and Filters

Phase 6 timeline search and filters are completed.

Search checks the current user's non-deleted memories across:

- Title
- Story / Note
- Location
- Tags

Filter options:

- One feeling at a time from the fixed Feeling list.
- One tag from tags used by the current user's non-deleted memories.
- Custom date range with FromDate and ToDate.
- Month + Year.
- Year only.

Date filter behavior:

- FromDate filters memories where `MemoryDate >= FromDate`.
- ToDate filters memories where `MemoryDate <= ToDate`.
- If FromDate/ToDate and Month/Year are both provided, the custom date range is used first and Month/Year is ignored.
- Month requires Year. If Month is selected without Year, MemoLens shows a gentle message and ignores the month filter.
- Year only filters the whole selected year.
- If FromDate is after ToDate, MemoLens shows a validation message and returns no filtered results.

Sort behavior:

- Default sort is newest first.
- Users can choose newest first or oldest first.
- Sorting uses `MemoryDate` first, then `CreatedAt`.

Privacy reminder:

All timeline search, filters, tag dropdowns, and sort results are scoped to the logged-in user. Deleted memories are excluded. Admin users do not browse other users' private memories in this phase.

## Private Albums

Phase 7 private album management is completed.

- Logged-in confirmed users can create, view, edit, and delete their own albums.
- Albums are private by default and every album query is scoped to the current logged-in user.
- Admin users do not browse other users' private albums or memories in this phase.
- Albums use a many-to-many relationship with memories, so one memory can belong to multiple albums.
- Users add memories from the Album Details flow.
- Add Memories only shows the current user's non-deleted memories that are not already in the album.
- Removing a memory from an album only removes the album relationship. It does not delete the memory or its images.
- Deleting an album is a soft delete using `IsDeleted` and `DeletedAt`.
- Deleted albums disappear from the normal Albums list.
- Deleted memories are not shown in album details or Add Memories options.
- Album cover images are automatic: MemoLens uses the first image from the first non-deleted memory in the album. If no image exists, the UI shows a calm placeholder.
- There is no separate Tags management page in this phase.

Phase 7 keeps the Phase 6.5 mobile-first direction. Album pages use responsive cards instead of tables, with one column on phones and wider grids on desktop.

## Mobile-first UI Status

Phase 6.5 mobile-first UI polish is completed.

- MemoLens now prioritizes phone-sized screens first while keeping desktop layouts usable.
- The mobile navbar is less crowded and gives account actions more tap space.
- Timeline search keeps the keyword field visible and moves the longer filter controls into a collapsible filter area.
- Selected filters remain visible after applying filters.
- Timeline cards stack naturally on mobile, with larger image covers.
- Create and Edit Memory forms use larger touch targets and clearer upload spacing.
- Existing image delete buttons are easier to tap on phones.
- Memory details and galleries scale better on narrow screens.
- Footer and content spacing were adjusted so mobile content is not crowded or overlapped.

This phase only changes Razor views and CSS. It does not change database schema, authentication behavior, memory CRUD logic, image upload validation, ownership/privacy checks, or search/filter results.

Phase 12.5 UI language and responsive stabilization is completed.

- User-facing MVC UI text is Vietnamese-first with full accents.
- Navbar and footer wording are more consistent and less crowded on mobile.
- Auth, timeline, memory, album, trash, settings, and privacy pages were polished for phone-sized screens.
- No backend behavior, database schema, migrations, API controllers, or mobile app code were changed in this phase.

## Feeling List

Memory feelings are stored as strings from this fixed list:

- Bình yên
- Vui vẻ
- Buồn
- Nhớ
- Lo lắng
- Mệt mỏi
- Khó chịu
- Lẫn lộn
- Khác

## Tag Notes

Create and Edit forms accept comma-separated tags, for example:

```text
thực tập, gia đình, cà phê
```

MemoLens trims spaces, ignores empty tags, reuses existing tags when possible, and creates new tags when needed. Tags are displayed on the timeline and memory detail page.

## Current Status

- Documentation created.
- ASP.NET Core MVC project created.
- EF Core database models and migrations added.
- Identity authentication configured.
- Custom Register/Login/Confirm Email/Logout flow added.
- MVC forgot/reset password flow added.
- Admin/User role seeding added.
- User-scoped memory CRUD added.
- Memory image upload and gallery added.
- Timeline search and filters added.
- Mobile-first UI polish added.
- Private album CRUD added.
- Private image storage and authorized image serving added.
- Trash and restore for memories/albums added.
- Settings and privacy pages added.
- MVP QA checklist and small privacy hardening added.
- Productization and mobile architecture plan added.
- API foundation added with `/api/v1/health`, standard API responses, and Development-only Swagger.
- Vietnamese UI language and mobile responsive stabilization added.
- No AI or social features.
- No admin dashboard yet.

## Product Direction

MemoLens should stay private-first, beginner-friendly, and focused on emotional memory storytelling.
