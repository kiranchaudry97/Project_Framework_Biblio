# Biblio — Bibliotheekbeheer in WPF (.NET 9)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
Biblio is een WPF-desktopapplicatie (.NET 9) voor het beheren van boeken, leden en uitleningen. Gebouwd met EF Core (SQL Server / LocalDB) en ASP.NET Core Identity.

Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Technische samenvatting & vereisten](#technische-samenvatting--vereisten)
- [Datamodel (tabellen & relaties)](#datamodel-tabellen--relaties)
- [Project- en mappenstructuur](#project--en-mappenstructuur)
- [Lokaal uitvoeren](#lokaal-uitvoeren)
- [Identity, seeding & security](#identity-seeding--security)
- [Foutafhandeling & logging](#foutafhandeling--logging)
- [Screenshots (placeholder)](#screenshots-placeholder)

---

## Doel & motivatie
- Doel: eenvoudig en efficiënt beheer van bibliotheekinventaris en uitleningen.
- Motivatie: leerrijk project voor MVVM/WPF, EF Core en Identity; uitbreidbaar naar een web-API.

## Technische samenvatting & vereisten
- .NET 9, WPF UI
- EF Core + SQL Server / LocalDB
- ASP.NET Core Identity voor gebruikers en rollen
- DI via Microsoft.Extensions.DependencyInjection
- Logging via Microsoft.Extensions.Logging

Vereisten: .NET 9 SDK en (Lokaal) SQL Server. Optionele env var: `BIBLIO_CONNECTION`.

Belangrijke NuGet-pakketten:
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Design
- Microsoft.AspNetCore.Identity.EntityFrameworkCore

## Datamodel — tabellen & relaties
- Boeken
  - Velden: BoekId (PK), Titel, Auteur, ISBN, CategorieID (FK), IsDeleted
  - Relatie: 1 Boek ? N Uitleningen

- Leden
  - Velden: LidId (PK), Voornaam, AchterNaam, Telefoon, Email, Adres, IsDeleted
  - Relatie: 1 Lid ? N Uitleningen

- Uitleningen
  - Velden: UitleningId (PK), BoekId (FK), LidId (FK), StartDate, DueDate, ReturnedAt (nullable), IsDeleted, IsClosed

- Categorieën
  - Velden: CategorieId (PK), Naam, Omschrijving

Opmerking: soft-delete via `IsDeleted`; global query filters in `BiblioDbContext`.

## Project- en mappenstructuur
Hieronder een gedetailleerd voorbeeld van de mappenstructuur in de repository. Pas aan indien nodig.

```
Project_Framework_Biblio/
?? Biblio_Models/
?  ?? Entiteiten/
?  ?  ?? Boek.cs
?  ?  ?? Lid.cs
?  ?  ?? Lenen.cs
?  ?  ?? Categorie.cs
?  ?  ?? AppUser.cs
?  ?  ?? BaseEntiteit.cs
?  ?? Data/
?  ?  ?? BiblioDbContext.cs
?  ?  ?? Migrations/
?  ?? Seed/
?  ?  ?? SeedData.cs
?  ?  ?? SeedOptions.cs
?  ?? Biblio_Models.csproj
?? Biblio_WPF/
?  ?? Window/
?  ?  ?? BoekWindow.xaml(+.cs)
?  ?  ?? LidWindow.xaml(+.cs)
?  ?  ?? UitleningWindow.xaml(+.cs)
?  ?  ?? AdminUserWindow.xaml(+.cs)
?  ?  ?? ...
?  ?? Controls/
?  ?  ?? LabeledTextBox.xaml(+.cs)
?  ?? ViewModels/
?  ?  ?? SecurityViewModel.cs
?  ?? Styles/
?  ?  ?? Theme.Dark.xaml
?  ?  ?? Theme.Light.xaml
?  ?? App.xaml(+.cs)
?  ?? Biblio_WPF.csproj
?? docs/
?  ?? screenshots/
?? README.md
?? .gitignore
```

Kort overzicht van belangrijke onderdelen:
- `Biblio_Models/Entiteiten` bevat de domeinmodellen.
- `Biblio_Models/Data` bevat de DbContext en EF Core-migraties.
- `Biblio_WPF/Window` bevat de UI-pagina's/vensters (code-behind).
- `Biblio_WPF/ViewModels` bevat viewmodels en security logic.

## Lokaal uitvoeren (kopieer/plak)
1) Clone & open

```bash
git clone <repo-url>
cd Project_Framework_Biblio
```

2) Restore & build

```bash
dotnet restore
dotnet build
```

3) Database migraties

- Controleer connection string of zet env var:
  - `BIBLIO_CONNECTION` = `Server=...;Database=...;Trusted_Connection=True;`

```bash
cd Biblio_Models
# optioneel: verwijder lege of foutieve migratie en maak nieuwe aan
# dotnet ef migrations remove
# dotnet ef migrations add <Naam>
dotnet ef database update
```

4) Start de WPF-app (Windows)

```bash
cd ../Biblio_WPF
dotnet run
```

> Opmerking: WPF vereist Windows met GUI-ondersteuning.

## Identity, seeding & security
- `AppUser` breidt IdentityUser uit (FullName, IsBlocked).
- `SeedData.InitializeAsync` maakt rollen (`Admin`, `Medewerker`), admin-account en voorbeelddata.
- Beveiligingstips: wachtwoordbeleid in config, bewaar secrets niet in repo, valideer `IsBlocked` in login-flow.

Voorbeeld seed (admin):
```csharp
var admin = new AppUser { Email = adminEmail, UserName = adminEmail, EmailConfirmed = true };
await userMgr.CreateAsync(admin, desiredPwd);
await userMgr.AddToRoleAsync(admin, "Admin");
```

## Foutafhandeling & logging
- Gebruik `try/catch` rond persistente acties en log fouten met `ILogger`.
- UI: toon gebruikersvriendelijke meldingen via `MessageBox`.
- Overweeg Serilog of file-logging voor productie.

Voorbeeld:
```csharp
_logger.LogError(ex, "Fout bij opslaan gebruiker {Email}", email);
MessageBox.Show($"Fout: {ex.Message}");
```

## Screenshots (placeholder)
Plaats afbeeldingen in `docs/screenshots/` en update de paden hieronder:
- `docs/screenshots/mainwindow.png`
- `docs/screenshots/boeken.png`
- `docs/screenshots/leden.png`

---



