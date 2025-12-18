# Biblio_Models — gedeelde domeinmodellen (.NET 9)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
`Biblio_Models` bevat de gedeelde domeinmodellen, DbContext, migraties en seeding‑logica die door de verschillende clients en de web API in dit repository worden gebruikt. Dit project is bedoeld als single source of truth voor entiteiten zoals `Boek`, `Lid`, `Lenen`, `Categorie`, `AppUser`, `RefreshToken` en `Taal`.

Installed NuGet packages (selectie)
-----------------------------------
Deze projectreferenties zijn gedefinieerd in `Biblio_Models.csproj`:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.10
- `Microsoft.EntityFrameworkCore.Design` 9.0.10
- `Microsoft.EntityFrameworkCore.Tools` 9.0.10
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.10
- `Microsoft.EntityFrameworkCore.Sqlite` 9.0.10
- `Microsoft.Extensions.Hosting` 9.0.0

Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Technische samenvatting & vereisten](#technische-samenvatting--vereisten)
- [Datamodel (kort)](#datamodel-kort)
- [Project- en mappenstructuur](#project--en-mappenstructuur)
- [Lokaal uitvoeren / migraties](#lokaal-uitvoeren--migraties)
- [Seeding & User Secrets](#seeding--user-secrets)
- [Localization / Resources](#localization--resources)
- [Veelvoorkomende problemen](#veelvoorkomende-problemen)
- [Licentie & contact](#licentie--contact)

---

## Doel & motivatie

- Doel: centrale, herbruikbare modellen en database‑laag bieden voor `Biblio_Web`, `Biblio_App`, `Biblio_WPF`, etc.
- Motivatie: voorkomen van duplicate modeldefinities en zorgen voor consistente migraties en seeding across projects.

## Technische samenvatting & vereisten

- Target framework: .NET 9 (net9.0)
- ORM: Entity Framework Core (DbContext + Migrations)
- Database: SQL Server (of andere provider indien aangepast)

Belangrijke NuGet‑pakketten
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Design

## Datamodel (kort)

Belangrijke entiteiten (voorbeeld):
- `Boek` — Titel, Auteur, Isbn, CategorieID, IsDeleted
- `Lid` — Voornaam, Achternaam, Email, Telefoon, Adres, IsDeleted
- `Lenen` — BoekId, LidId, StartDate, DueDate, ReturnedAt, IsClosed, IsDeleted
- `Categorie` — Naam
- `AppUser` — Identity user uitbreiding (indien aanwezig)
- `RefreshToken` — voor token refresh flow
- `Taal` — om beschikbare talen te beheren

Opmerking: veel entiteiten gebruiken een `IsDeleted` veld voor soft‑delete gedrag en global query filters in `BiblioDbContext`.

## Project- en mappenstructuur

```
Biblio_Models/
+-- Biblio_Models.csproj
+-- Entiteiten/
¦   +-- Boek.cs
¦   +-- Lid.cs
¦   +-- Lenen.cs
¦   +-- Categorie.cs
¦   +-- AppUser.cs
¦   +-- RefreshToken.cs
¦   +-- Taal.cs
+-- Data/
¦   +-- BiblioDbContext.cs
¦   +-- LocalDbContext.cs    # optioneel voor lokale dev/test
¦   +-- Migrations/          # EF Core migraties
+-- Seed/
¦   +-- SeedData.cs
¦   +-- SeedOptions.cs
+-- Resources/
¦   +-- SharedModelResource.resx
+-- README.md
```

## Lokaal uitvoeren / migraties

Dit project bevat alleen modellen en DbContext; gebruik een startup project (meestal `Biblio_Web`) bij het toepassen van migraties of het uitvoeren van de applicatie.

Voorbeeld: migraties toepassen met `Biblio_Web` als startup project

```bash
# vanuit solution root
cd Biblio_Models
# voeg migratie toe (optioneel)
dotnet ef migrations add InitialCreate --project Biblio_Models --startup-project ../Biblio_Web
# update database
dotnet ef database update --project Biblio_Models --startup-project ../Biblio_Web
```

Als je `LocalDbContext` of een test‑startup gebruikt, pas de `--startup-project` en connection string aan.

## Seeding & User Secrets

- De seed‑logic (bijv. rollen, admin account, voorbeelddata) staat in `Seed/SeedData.cs`.
- Gebruik User Secrets in het startup project (`Biblio_Web`) om dev‑wachtwoorden en seed‑opties op te slaan.

Voorbeeld (in `Biblio_Web`):
```
dotnet user-secrets set "Seed:AdminEmail" "admin@biblio.local"
dotnet user-secrets set "Seed:AdminPassword" "Vb1234?"
```

## Localization / Resources

- Gedeelde strings voor modellen kunnen in `Resources/SharedModelResource.resx` staan.
- Andere projecten (bijv. `Biblio_App`) zoeken mogelijk naar `Biblio_Models.Resources.SharedModelResource` via ResourceManager (zie `BoekenPagina.xaml.cs`).

## Veelvoorkomende problemen

- Migraties uitvoeren zonder correct startup project leidt tot verkeerde `DbContext` configuratie. Gebruik `--startup-project` zoals hierboven.
- TFM mismatch: controleer dat projecten die `Biblio_Models` refereren dezelfde targetframework ondersteunen (.NET 9).
- ResourceManager kan geen resources vinden als assembly‑namen of resx‑paths niet overeenkomen.



## Licentie & contact

Chaud-ry Kiran Jamil

---

Bekijk ook:
- `Biblio_Web/README.md` — webfrontend & API
- `Biblio_App/README.md` — .NET MAUI client
