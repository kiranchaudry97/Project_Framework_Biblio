# Biblio_Web — Webfrontend (ASP.NET Core, .NET 9)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
Biblio_Web is de webfrontend (ASP.NET Core MVC / Razor) van het Biblio-project. De webapp biedt gebruikersauthenticatie (Identity), een UI voor beheer van boeken, leden, categorieën en uitleningen, plus REST‑API endpoints voor externe clients (o.a. een MAUI client).

Installed NuGet packages (selectie)
-----------------------------------
Deze projectreferenties zijn gedefinieerd in `Biblio_Web.csproj`:

- `Azure.Core` 1.50.0
- `Microsoft.AspNetCore.Identity.UI` 9.0.0
- `Microsoft.EntityFrameworkCore` 9.0.10
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.10
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.10
- `Microsoft.Extensions.Localization` 9.0.0
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` 9.0.0
- `Swashbuckle.AspNetCore` 6.5.0
- `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.10
- `AutoMapper.Extensions.Microsoft.DependencyInjection` 12.0.1
- `Microsoft.Extensions.Configuration.Json` 9.0.0
- `Microsoft.Extensions.Configuration.UserSecrets` 9.0.0
- `Microsoft.Extensions.Logging.Debug` 9.0.8
- `Microsoft.AspNetCore.OpenApi` 9.0.0

Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Technische samenvatting & vereisten](#technische-samenvatting--vereisten)
- [Datamodel (kort)](#datamodel-kort)
- [Project- en mappenstructuur](#project--en-mappenstructuur)
- [Installatie & Lokaal uitvoeren](#installatie--lokaal-uitvoeren)
- [Identity, seeding & security](#identity-seeding--security)
- [Foutafhandling & logging](#foutafhandling--logging)
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
+-- SECURE_DB_SETUP.md            # security instructions for DB secrets
+-- set-user-secrets.ps1         # interactive helper to set dotnet user-secrets
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

Aangepaste / toegevoegde bestanden
- `SECURE_DB_SETUP.md` — instructies en aanbevelingen voor veilige DB-configuratie.
- `set-user-secrets.ps1` — interactief PowerShell-script dat user-secrets zet (no hardcoded passwords).
- `appsettings.json` / `appsettings.Development.json` — placeholders bijgewerkt om geen plaintext wachtwoorden te bevatten; gebruik user-secrets voor gevoelige waarden.

## Installatie & Lokaal uitvoeren
Volg deze stappen om de webapp lokaal te starten (veilig, development):

1) Clone repository en open een terminal in de solution root:

```bash
git clone <repo-url>
cd Project_Framework_Biblio
```

2) Configureer de database connection veilig (aanbevolen: user-secrets)

- Gebruik het meegeleverde PowerShell-script (lokale machine):

```powershell
# vanuit solution root
powershell -ExecutionPolicy Bypass -File .\Biblio_Web\set-user-secrets.ps1 -DbUser 'Admin@biblio.local@biblio-sql-server' -DbPassword '<your-db-password>'
```

- Opmerking: het script is interactief wanneer je geen parameters meegeeft; het vraagt om DB-server, DB-naam, DB-user en zal het wachtwoord veilig (verborgen) vragen. Het schrijft secrets met `dotnet user-secrets` in de `Biblio_Web` projectcontext.

- Of stel handmatig user-secrets in (wanneer je in `Biblio_Web` map staat):

```bash
dotnet user-secrets set "PublicConnection_Azure" "Server=tcp:<your-server>.database.windows.net,1433;Initial Catalog=<your-db>;User ID=<user>;Password=<pwd>;Encrypt=True;TrustServerCertificate=False;"
dotnet user-secrets set "Seed:AdminPassword" "<admin-password>"
```

3) Zorg dat Azure SQL firewall je IP toestaat (of gebruik App Service settings in productie).

4) Voer migraties uit (indien je de database wilt bijwerken):

```bash
cd Biblio_Models
dotnet ef database update --startup-project ../Biblio_Web
cd ..
```

5) Run de webapp in Development:

```powershell
# optioneel: forceer Development environment
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# start de app
dotnet run --project .\Biblio_Web
```

6) Open browser: https://localhost:{port} (console toont welke poort gebruikt wordt)

7) Verifieer: kijk in logs of `Using PublicConnection_Azure` verschijnt en controleer of seed-data aanwezig is (admin account `admin@biblio.local`).
## Identity, seeding & security
- Seed logic staat in `Biblio_Models.Seed.SeedData` en maakt rollen (`Admin`, `Medewerker`), admin-account en voorbeelddata in development.
- Gebruik User Secrets om seed‑wachtwoorden en andere gevoelige dev‑waarden op te slaan (zie `UserSecretsId` in csproj).
- JWT instellingen vind je in `appsettings.json` (sectie `Jwt`). Vervang development keys voor productie.

## Foutafhandling & logging
- Gebruik `ILogger<T>` en log exceptions bij DB/Identity operaties.
- UI toont ProblemDetails / ValidationProblemDetails voor API fouten.
- Voor productie: overweeg Serilog met file/seq sink.

## Screenshots (placeholder)
Screenshots / afbeeldingen
--------------------------
Voorbeelden van UI‑beelden :

- Login
  ![Login](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Login_Pagina.png)


- Profiel
  ![Profiel](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Profiel.png)


- Taalkeuze
  ![taalkeuze](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Taalkeuze.png)

  - Thema Toggle
  ![thema](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Toggle_Mode.png)


- Dashboard
  ![Dashboard](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Dashboard.png)

- Boeken
  ![Boeken](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Boeken_Pagina.png)


  
- Categorie
  ![Categorie](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Categorie_Pagina.png)


  
- Leden
  ![Leden](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Leden_Pagina.png)


  
- Uitleningen
  ![Uitleningen](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Uitleningen_Pagina.png)

 
 - Delete
  ![delete](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Delete.png)

- Edit
  ![edit](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Edit.png)

 - Details
 ![details](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7dd05783a3db26053aa67e39b1c1efbd0e6b3e28/Biblio_Web/Biblio_Web_Sceenshots/Details.png)


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


