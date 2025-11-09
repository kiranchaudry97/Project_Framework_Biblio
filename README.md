# "Biblio" Bibliotheekbeheer in WPF (.NET 9)

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
- [Licenties](#licenties)

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

## Datamodel tabellen & relaties
- Boeken
  - Velden: BoekId (PK), Titel, Auteur, ISBN, CategorieID (FK), IsDeleted
  - Relatie: 1 Boek ? N Uitleningen

- Leden
  - Velden: LidId (PK), Voornaam, AchterNaam, Telefoon, Email, Adres, IsDeleted
  - Relatie: 1 Lid ? N Uitleningen

- Uitleningen
  - Velden: UitleningId (PK), BoekId (FK), LidId (FK), StartDate, DueDate, ReturnedAt (nullable), IsDeleted, IsClosed

- CategorieÃ«n
  - Velden: CategorieId (PK), Naam, Omschrijving

Opmerking: soft-delete via `IsDeleted`; global query filters in `BiblioDbContext`.

## Project- en mappenstructuur
Hieronder een gedetailleerd voorbeeld van de mappenstructuur in de repository. Pas aan indien nodig.

```
Project_Framework_Biblio/
├── Biblio_Models/
│   ├── Entiteiten/
│   │   ├── Boek.cs
│   │   ├── Lid.cs
│   │   ├── Lenen.cs
│   │   ├── Categorie.cs
│   │   ├── AppUser.cs
│   │   └── BaseEntiteit.cs
│   ├── Data/
│   │   ├── BiblioDbContext.cs
│   │   └── Migrations/           (EF Core migratiebestanden)
│   ├── Seed/
│   │   ├── SeedData.cs
│   │   └── SeedOptions.cs
│   └── Biblio_Models.csproj
│
├── Biblio_WPF/
│   ├── Window/
│   │   ├── BoekWindow.xaml / BoekWindow.xaml.cs
│   │   ├── LidWindow.xaml / LidWindow.xaml.cs
│   │   ├── UitleningWindow.xaml / UitleningWindow.xaml.cs
│   │   ├── AdminUserWindow.xaml / AdminUserWindow.xaml.cs
│   │   ├── LoginWindow.xaml / LoginWindow.xaml.cs
│   │   ├── RegisterWindow.xaml / RegisterWindow.xaml.cs
│   │   ├── ProfileWindow.xaml / ProfileWindow.xaml.cs
│   │   ├── CategoriesWindow.xaml / CategoriesWindow.xaml.cs
│   │   └── Overige dialogen/Windows/
│   │       ├── SimpleBoekWindow.xaml / SimpleBoekWindow.xaml.cs
│   │       ├── SimpleLidWindow.xaml / SimpleLidWindow.xaml.cs
│   │       └── ResetWindow.xaml / ResetWindow.xaml.cs
│   ├── Controls/
│   │   └── LabeledTextBox.xaml / LabeledTextBox.xaml.cs
│   ├── ViewModels/
│   │   └── SecurityViewModel.cs
│   ├── Styles/
│   │   ├── Theme.Light.xaml
│   │   ├── Theme.Dark.xaml
│   │   └── Themes.xaml (+ code-behind)
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   └── Biblio_WPF.csproj
│
├── SEED_USER_SECRETS.md
├── docs/
│   └── screenshots/
├── README.md
└── .gitignore


```

Kort overzicht van belangrijke onderdelen:
- `Biblio_Models/Entiteiten` bevat de domeinmodellen.
- `Biblio_Models/Data` bevat de DbContext en EF Core-migraties.
- `Biblio_WPF/Window` bevat de UI-pagina's/vensters (code-behind).
- `Biblio_WPF/ViewModels` bevat viewmodels en security logic.

## Lokaal uitvoeren (kopieer/plak)
1) Clone & open

```bash
# Klonen van de officiële repository
git clone https://github.com/kiranchaudry97/Project_Framework_Biblio.git
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

## Seed: User Secrets (aanbevolen)

Gebruik User Secrets om dev/test-credentials voor `SeedOptions` veilig lokaal te bewaren en niet in de repository op te nemen. Er is een voorbeeldbestand `Biblio_WPF/SEED_USER_SECRETS.md` met JSON en stappen.

Korte stappen (voer uit in `Biblio_WPF` map):

1. Initialiseer user-secrets éénmalig voor het project:

```
cd Biblio_WPF
dotnet user-secrets init
```

2. Stel de seedwaarden in (voorbeeld):

```
dotnet user-secrets set "Seed:CreateTestAccounts" "true"
dotnet user-secrets set "Seed:AdminEmail" "admin@biblio.local"
dotnet user-secrets set "Seed:AdminPassword" "Vb1234?"
# zet ook StaffEmail/StaffPassword en BlockedEmail/BlockedPassword indien gewenst
```

Opmerking: zet `Seed:CreateTestAccounts` op `false` voor productie en bewaar geen echte wachtwoorden in de repository. Zie `Biblio_WPF/SEED_USER_SECRETS.md` voor uitgebreidere instructies.

## Foutafhandeling & logging
- Gebruik `try/catch` rond persistente acties en log fouten met `ILogger`.
- UI: toon gebruikersvriendelijke meldingen via `MessageBox`.
- Overweeg Serilog of file-logging voor productie.

Voorbeeld:
```csharp
_logger.LogError(ex, "Fout bij opslaan gebruiker {Email}", email);
MessageBox.Show($"Fout: {ex.Message}");
```

### Screenshots


### Loginvenster:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/fbc1233417fbe3605ae37647a7b794708aa4c7ae/Screenshot/login.png)
[*Login: Kunnen aanmelden , wachtwoord tonen en indien vergeten*]



### Darkmode:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/b8b038f87a4285dc7b44623e7d0e5afe6f048d0b/Screenshot/darkmode.png)
[*darkmode]

![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/mainwindow.png)


### Openen:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/openen.png)
[*Openen: boeken, leden, uitleningen, categorieën*]





### Beheer:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/beheer.png)
[*Beheer :Gebruikers | rollen kunnen zien aan rechten kunnen geven als admin of medewerker *]


### Gebruiken & Rollen:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/7f28cf8cb386e7a73314e2c78969d4a5051b6a10/Screenshot/gebruiker_rollen.png)
[* Gebruikers rollen kunnen zien aan rechten kunnen geven als admin of medewerker *]



### Bestanden:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/bestand.png)
[*bestand : soort nav voor inloggen, profiel, wachtwoord kunnen wijzigen, afmelden en afsluiten *]




### Profiel:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0089393fa95562fff3b577f0e167e28b00f9e6e6/Screenshot/profile.png)
[*profiel : profiel kunnenn bekijken en opslaan of wijzigen van wachtwoord *]






### wachtwoord wijzigen:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0089393fa95562fff3b577f0e167e28b00f9e6e6/Screenshot/wachtwoord.png)
[*Wachtwoord wijzigen *]





### categorie:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/categorie.png)
[*categorie : categorie kunnen toevoegen en verwijderen *]



### Uitleningen:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/uitleningen.png)
[*uitleningen : Boeken kunnen uitlenen , datum selecteren , vermelden wanneer de boeken teruggebracht zijn *]



### Leden:
![image](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/0bd4fc2df85c362e634ec9dc3cef3a912818f483/Screenshot/leden.png)
[*leden : kunnen toevoegen van hun persoonlijke gegevens , opslaan en verwijderen en opzoeken*]





## Licenties

De belangrijkste components/pakketten:

- .NET / ASP.NET Core / Entity Framework Core (Microsoft) 
- `Microsoft.EntityFrameworkCore` (EF Core) — MIT
- `Microsoft.EntityFrameworkCore.SqlServer` — MIT
- `Microsoft.EntityFrameworkCore.Design` — MIT
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` — MIT







