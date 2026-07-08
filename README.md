# MemoLens

MemoLens is a private photo journal and memory storytelling web app. It helps users save personal memories with photos, notes, moods, dates, places, tags, and albums.

MemoLens is not a social network. It has no public feed, likes, comments, followers, public profiles, or explore page.

## Tech Stack

- ASP.NET Core MVC
- SQL Server, planned for later phases
- Entity Framework Core, planned for later phases
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

## Current Status

Phase 1 setup is completed:

- ASP.NET Core MVC project created.
- Bootstrap layout added.
- Home and Privacy pages added.
- Placeholder pages added for Timeline, Albums, and Create Memory.
- No login/register yet.
- No database yet.
- No image upload yet.
- No AI or social features.

## Product Direction

MemoLens should stay private-first, beginner-friendly, and focused on emotional memory storytelling.
