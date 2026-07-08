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

## Database Setup Notes

The SQL Server connection string is stored in `appsettings.json` under:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MemoLensDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

The default connection uses SQL Server LocalDB so the project stays beginner-friendly for local development.

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

## Email Confirmation in Development

No real SMTP provider is configured in this phase.

After registration, MemoLens logs the email confirmation link through the development email sender. Check the app console or debug output, then open the confirmation link in the browser. After confirmation, the user can log in.

This keeps the project ready for a real SMTP sender later without hardcoding email credentials.

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

## Memory CRUD Status

Phase 4 memory CRUD is completed:

- Logged-in users can create, view, edit, and delete their own text-based memories.
- Timeline uses `MemoriesController.Index`.
- Create Memory uses `MemoriesController.Create`.
- Memory queries are scoped to the current logged-in user.
- Admin users do not browse other users' private memories in this phase.
- Delete is a soft delete using `IsDeleted` and `DeletedAt`.
- Deleted memories disappear from the normal timeline.
- Image upload is not implemented yet and is planned for Phase 5.

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
- Admin/User role seeding added.
- User-scoped memory CRUD added.
- No image upload yet.
- No albums CRUD yet.
- No AI or social features.
- No admin dashboard yet.

## Product Direction

MemoLens should stay private-first, beginner-friendly, and focused on emotional memory storytelling.
