# Project Framework Biblio — Repository overview

This repository contains the Biblio system split into multiple cooperating projects. This root README provides quick links and the purpose of each project so contributors can find start points fast.

Contents (quick links)
- [Biblio_Web](#biblio_web) — Web frontend and API (Razor Pages / ASP.NET Core)
- [Biblio_App](#biblio_app) — .NET MAUI client (mobile / desktop)
- [Biblio_Models](#biblio_models) — Shared domain models, DbContext, migrations, seeding
- [Tools / SeedBiblio](#tools--seedbiblio) — Console tool to generate/seed test data
- [Biblio_App.Tests](#biblio_apptests) — Unit tests

---

## Biblio_Web
- Path: `Biblio_Web/`
- Purpose: Web frontend and API layer that manages books, members, categories and loans. Provides Identity authentication, admin UI (Razor Pages) and JSON API endpoints consumed by clients (including the MAUI app).
- Quick start:
  - Configure secrets (see `Biblio_Web/set-user-secrets.ps1`)
  - Run: `dotnet run --project Biblio_Web`
  - DB migrations: run EF commands from `Biblio_Models` with `Biblio_Web` as startup project.

## Biblio_App
- Path: `Biblio_App/`
- Purpose: Cross-platform client implemented with .NET MAUI (.NET 9). Mobile/desktop UI for browsing, creating and syncing library data with `Biblio_Web`.
- Quick start:
  - Install .NET 9 & MAUI workloads: `dotnet workload install maui`
  - Open solution in Visual Studio, select a target (Android/iOS/Windows) and debug, or build: `dotnet build`

## Biblio_Models
- Path: `Biblio_Models/`
- Purpose: Single source for domain entities (`Boek`, `Lid`, `Lenen`, `Categorie`, `AppUser`, etc.), EF Core `DbContext`, migrations and seeding logic. Shared by `Biblio_Web` and `Biblio_App`.
- Migrations / apply:
  - Add migration: `dotnet ef migrations add <Name> --project Biblio_Models --startup-project ../Biblio_Web`
  - Update DB: `dotnet ef database update --project Biblio_Models --startup-project ../Biblio_Web`

## Tools / SeedBiblio
- Path: `Tools/SeedBiblio/`
- Purpose: Helper console tool for generating and seeding test data (useful for CI or local development).

## Biblio_App.Tests
- Path: `Biblio_App.Tests/`
- Purpose: Unit tests for core logic and services. Run tests: `dotnet test`

---

Tips
- Use `dotnet user-secrets` (startup project) or environment variables for sensitive values — do not commit plaintext credentials.
- For MAUI development prefer Visual Studio for emulator/device debugging.
- If you need the data seed or migrations to be applied to a MAUI local DB, use the provided scripts (`create-and-seed-maui-db.ps1`, `create-android-emulator.ps1`) in the repository.

---

See the project-level README files for detailed instructions:
- `Biblio_Web/README.md`
- `Biblio_App/README.md`
- `Biblio_Models/README.md`
