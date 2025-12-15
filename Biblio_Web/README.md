# Biblio_Web — Webfrontend (ASP.NET Core, .NET 9)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
Biblio_Web is de webfrontend (ASP.NET Core MVC / Razor) van het Biblio-project. De webapp biedt gebruikersauthenticatie (Identity), een UI voor beheer van boeken, leden, categorieën en uitleningen, plus REST‑API endpoints voor externe clients (o.a. een MAUI client).

Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Technische samenvatting & vereisten](#technische-samenvatting--vereisten)
- [Datamodel (kort)](#datamodel-kort)
- [Project- en mappenstructuur](#project--en-mappenstructuur)
- [Lokaal uitvoeren](#lokaal-uitvoeren)
- [Identity, seeding & security](#identity-seeding--security)
- [Foutafhandeling & logging](#foutafhandeling--logging)
- [Screenshots (placeholder)](#screenshots-placeholder)
- [Licenties](#licenties)
- [AI‑hulpmiddelen & ontwikkelworkflow](#ai-hulpmiddelen--ontwikkelworkflow)

---

## Doel & motivatie
- Doel: een webinterface en API‑laag leveren om bibliotheekdata te beheren en te raadplegen.
- Motivatie: combineer een gebruiksvriendelijke beheerders‑UI (Razor views) met beveiligde API's (JWT) die door mobiele/desktop clients gebruikt kunnen worden.

## Technische samenvatting & vereisten
- Target: .NET 9 (net9.0)
- Webframework: ASP.NET Core MVC / Razor Pages
- Database: EF Core (SQL Server / LocalDB)
- Authenticatie: ASP.NET Core Identity (Areas/Identity)
- API authenticatie: JWT (Bearer)
- Localization: resources in `Resources/Vertalingen` (SharedResource.*.resx)
- Swagger voor API‑documentatie (development)

Vereisten: .NET 9 SDK, SQL Server (of LocalDB). Optionele env var voor connection string.

Belangrijke NuGet‑pakketten
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Swashbuckle.AspNetCore (Swagger)
- Microsoft.AspNetCore.OpenApi

## Datamodel (kort)
De domeinmodellen staan in `Biblio_Models` (projectreferentie). Belangrijke entiteiten:
- `Boek` — Titel, Auteur, Isbn, CategorieID, IsDeleted
- `Lid` — Voornaam, AchterNaam, Email, Telefoon, Adres, IsDeleted
- `Lenen` — BoekId, LidId, StartDate, DueDate, ReturnedAt, IsClosed, IsDeleted
- `Categorie` — Naam

Opmerking: soft‑delete via `IsDeleted` zodat records niet fysiek verwijderd worden.

## Project- en mappenstructuur
Voor dit webproject (belangrijkste mappen/bestanden):

```
Biblio_Web/
+-- Biblio_Web.csproj
+-- Program.cs
+-- appsettings.json
+-- appsettings.Development.json
+-- README.md
+-- Controllers/
¦   +-- AccountController.cs
¦   +-- AdminController.cs
¦   +-- BoekenController.cs
¦   +-- CategorieenController.cs
¦   +-- CultureController.cs
¦   +-- HomeController.cs
¦   +-- LedenController.cs
¦   +-- ProfileController.cs
¦   +-- TalenController.cs
¦   +-- UitleningController.cs
¦   +-- Api/                    # namespaced API controllers (return JSON)
¦       +-- AuthController.cs
¦       +-- BoekenApiController.cs
¦       +-- CategorieenApiController.cs
¦       +-- DiagnosticsController.cs
¦       +-- LedenApiController.cs
¦       +-- MobileDataController.cs
¦       +-- UitleningenApiController.cs
+-- Areas/
¦   +-- Identity/               # ASP.NET Core Identity UI + pages
¦       +-- Pages/Account/
¦           +-- Login.cshtml.cs
¦           +-- Register.cshtml.cs
¦           +-- ForgotPassword.cshtml.cs
¦       +-- Data/
+-- Views/                      # Razor views and shared layout/components
¦   +-- Shared/                 # _Layout.cshtml, Error, ViewComponents, Partials
¦   +-- Admin/
¦   +-- Account/
¦   +-- Boeken/
¦   +-- Categorieen/
¦   +-- Leden/
¦   +-- Uitlening/
¦   +-- Profile/
¦   +-- Talen/
+-- Models/                     # ViewModels / DTOs used by controllers and views
¦   +-- AdminCreateUserViewModel.cs
¦   +-- AdminDeleteUserViewModel.cs
¦   +-- AdminEditRolesViewModel.cs
¦   +-- ChangePasswordViewModel.cs
¦   +-- ErrorViewModel.cs
¦   +-- LoginViewModel.cs
¦   +-- PagedResult.cs
¦   +-- ProfileViewModel.cs
¦   +-- RegisterViewModel.cs
¦   +-- UserRolesViewModel.cs
¦   +-- UserViewModel.cs
+-- Mapping/
¦   +-- MappingProfile.cs
+-- Middleware/
¦   +-- CookiePolicyOptionsProvider.cs
+-- ViewComponents/
¦   +-- LoginStatusViewComponent.cs
+-- Resources/
¦   +-- SharedResource.cs
¦   +-- SharedResource.nl.resx
¦   +-- SharedResource.en.resx
¦   +-- Vertalingen/
¦       +-- SharedResource.nl.resx
¦       +-- SharedResource.en.resx
¦       +-- SharedResource.fr.resx
+-- wwwroot/                    # static files (css, js, lib, images)
¦   +-- css/
¦   +-- js/
¦   +-- lib/
¦   +-- images/
+-- docs/
    +-- postman-collection.json
    +-- postman-quickstart.md
```

Naast dit project in de solution:
- `Biblio_Models/` (entiteiten, DbContext, seed)
- `Biblio_App/` (MAUI client)
- `Biblio_WPF/` (WPF client)

## Lokaal uitvoeren
1. Pas connection string aan in `Biblio_Web/appsettings.json` of gebruik environment variable.
2. (Optioneel) Voer migraties uit en update database:

```bash
cd Biblio_Models
dotnet ef database update --startup-project ../Biblio_Web
```

3. Run de webapp (vanuit solution root):

```bash
dotnet run --project Biblio_Web
```

4. Open browser: `https://localhost:{PORT}` (controleer `launchSettings.json` of console output).

## Identity, seeding & security
- Seed logic staat in `Biblio_Models.Seed.SeedData` en maakt rollen (`Admin`, `Medewerker`), admin‑account en voorbeelddata in development.
- Gebruik User Secrets om seed‑wachtwoorden en andere gevoelige dev‑waarden op te slaan (zie `UserSecretsId` in csproj).
- JWT instellingen vind je in `appsettings.json` (sectie `Jwt`). Vervang development keys voor productie.

Voorbeeld seed (admin):

```csharp
var admin = new AppUser { Email = adminEmail, UserName = adminEmail, EmailConfirmed = true };
await userMgr.CreateAsync(admin, desiredPwd);
await userMgr.AddToRoleAsync(admin, "Admin");
```

## Foutafhandling & logging
- Gebruik `ILogger<T>` en log exceptions bij DB/Identity operaties.
- UI toont ProblemDetails / ValidationProblemDetails voor API fouten.
- Voor productie: overweeg Serilog met file/seq sink.

## Screenshots (placeholder)


## Licenties
- Dit project gebruikt Microsoft‑componenten (MIT/opensource licenties voor EF/ASP.NET Core). Controleer individuele package licenties.

## NuGet-installatieproblemen & AI-assistentie
- Tijdens ontwikkeling kunnen NuGet/TFM mismatches optreden; controleer targetframework en packageversies wanneer restore faalt.
- AI‑assistentie (bijv. Copilot) is gebruikt om README, kleine fixes en sjablonen te genereren en problemen te analyseren.

## AI‑hulpmiddelen & ontwikkelworkflow
- AI gebruikt voor: documentatiegeneratie, foutanalyse, voorbeeldcode en snelle refactors.

## Problemen die zijn voorkomen (kort)
- Ontbrekende project‑includes of dubbele bestanden
- Cache/NuGet restore problemen
- Digestie van resources/language files
- Onzichtbare UI‑icons door ontbrekende static files — controleer `wwwroot/images` en `UseStaticFiles() `

---


