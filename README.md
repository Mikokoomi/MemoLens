# MemoLens

MemoLens is a private photo journal and memory storytelling web app. It helps users save personal memories with photos, notes, moods, dates, places, tags, and albums.

MemoLens is not a social network. It has no public feed, likes, comments, followers, public profiles, or explore page.

## Tech Stack

- ASP.NET Core MVC
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity-ready user model
- Bootstrap
- GitHub

## How to Run Locally

1. Install the .NET 8 SDK.
2. Open a terminal in the repository root.
3. Restore and run the project:

```bash
dotnet restore
dotnet run
```

4. Open the local URL shown in the terminal, usually `https://localhost:xxxx` or `http://localhost:xxxx`.

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

To create or update the local database after migrations exist, run:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update
```

To add a new migration later, run:

```bash
dotnet tool run dotnet-ef migrations add MigrationName
```

## Current Status

Phase 2 database/models setup is completed:

- Entity Framework Core SQL Server packages added.
- ASP.NET Core Identity EntityFrameworkCore package added.
- `ApplicationDbContext` added.
- `ApplicationUser` inherits from IdentityUser for future Identity support.
- Domain models added for memories, images, tags, albums, and join tables.
- Initial migration added.
- No login/register UI yet.
- No CRUD controllers or views yet.
- No image upload yet.
- No AI or social features.

## Product Direction

MemoLens should stay private-first, beginner-friendly, and focused on emotional memory storytelling.
