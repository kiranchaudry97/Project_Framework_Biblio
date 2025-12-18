# Project Framework — Biblio (overzicht)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
Deze repository bevat een set samenwerkende projecten voor bibliotheekbeheer: gedeelde modellen (`Biblio_Models`), een ASP.NET Core webfrontend/API (`Biblio_Web`), een WPF desktopclient (`Biblio_WPF`) en een cross‑platform .NET MAUI client (`Biblio_App`).

Snelkoppelingen
---------------
- [Biblio_Models — gedeelde domeinmodellen, DbContext en seeding](Biblio_Models/README.md)
- [Biblio_Web — Webfrontend + API (ASP.NET Core)](Biblio_Web/README.md)
- [Biblio_WPF — Desktop client (WPF)](Biblio_WPF/README.md)
- [Biblio_App — Mobile / desktop client (.NET MAUI)](Biblio_App/README.md)

Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Vereisten (kort)](#vereisten-kort)
- Projectspecifieke korte omschrijvingen, snelkoppelingen en mappenstructuren
- [Technische implementaties](#technische-implementaties)
- [AI-hulpmiddelen & ontwikkelworkflow](#ai-hulpmiddelen--ontwikkelworkflow)
- [Problemen die ik voorkwam](#problemen-die-ik-voorkwam)
- [Installatie / snelstart (development)](#installatie--snelstart-development)

---

## Doel & motivatie

Doel: een consistente, herbruikbare set applicaties en libraries leveren waarmee bibliotheekdata kan worden beheerd en geraadpleegd via web, desktop en mobiele clients.

Motivatie: scheiden van concerns — domeinmodellen en database‑logica in één project, platform‑specifieke UI logica in aparte clients.

## Vereisten (kort)

- .NET 9 SDK
- Voor MAUI: MAUI workloads geïnstalleerd (`dotnet workload install maui`)
- Database: SQL Server / LocalDB (of andere EF Core provider indien geconfigureerd)
- Visual Studio met MAUI/WPF workloads of gebruik dotnet CLI en geschikte emulators/devices

---

Projectoverzicht (kort)
-----------------------

Biblio_Models
- Korte omschrijving: gedeelde entiteiten, `DbContext`, migraties en seeding.
- Snelkoppeling: `Biblio_Models/README.md`
- Belangrijkste onderdelen: `Entiteiten/`, `Data/BiblioDbContext.cs`, `Seed/`.
- Mappenstructuur (kort):
```
Biblio_Models/
+-- Entiteiten/
+-- Data/
+-- Seed/
+-- Migrations/
```

Biblio_Web
- Korte omschrijving: ASP.NET Core MVC / API voor beheer en mobiele clients.
- Snelkoppeling: `Biblio_Web/README.md`
- Belangrijkste onderdelen: `Controllers/`, `Areas/Identity/`, `Views/`, `Resources/`.
- Mappenstructuur (kort):
```
Biblio_Web/
+-- Controllers/
+-- Areas/Identity/
+-- Views/
+-- Resources/
+-- wwwroot/
```

Biblio_WPF
- Korte omschrijving: Windows desktop client (WPF, MVVM).
- Snelkoppeling: `Biblio_WPF/README.md`
- Belangrijkste onderdelen: `Window/`, `ViewModels/`, `Styles/`, `Controls/`.
- Mappenstructuur (kort):
```
Biblio_WPF/
+-- Window/
+-- ViewModels/
+-- Controls/
+-- Styles/
```

Biblio_App
- Korte omschrijving: .NET MAUI cross‑platform client (Android/iOS/macCatalyst/Windows).
- Snelkoppeling: `Biblio_App/README.md`
- Belangrijkste onderdelen: `Pages/`, `ViewModels/`, `Services/`, `Resources/`.
- Mappenstructuur (kort):
```
Biblio_App/
+-- Pages/
+-- ViewModels/
+-- Services/
+-- Resources/
```

---

## Technische implementaties

Hier een kort overzicht van concrete technische keuzes en implementaties per component:

Biblio_Models
- EF Core `DbContext` met global query filters voor soft‑delete (`IsDeleted`).
- Separatie van entiteiten (`Entiteiten/`) en seed logic (`Seed/SeedData.cs`).
- Migraties beheerd via EF Core CLI en toegepast met het juiste startup‑project.
- Resourcebestand `SharedModelResource.resx` voor gedeelde vertalingen.

Biblio_Web
- ASP.NET Core MVC + API controllers (REST) en Identity voor gebruikersbeheer.
- JWT / Bearer setup voor API authenticatie en Swagger in development.
- Localization via `Resources/` en resource managers; views en API gebruiken dezelfde modellen uit `Biblio_Models`.
- DI in `Program.cs`, logging met `ILogger<T>` en optionele e‑mail dev sender service.

Biblio_WPF
- MVVM patroon: views in `Window/`, viewmodels in `ViewModels/`.
- Gebruikt `Biblio_Models` als projectreferentie voor entiteiten en context (indien lokaal DB gebruikt).
- Resource dictionaries en thema's (`Styles/Theme.*.xaml`) voor styling en darkmode.
- Local run via Visual Studio; migraties uit `Biblio_Models` toepassen met `--startup-project` op `Biblio_WPF` wanneer nodig.

Biblio_App
- .NET MAUI Shell‑navigatie (`AppShell.xaml`) en MVVM met `ViewModels/`.
- Services: `AuthService`, `TokenHandler`, `LanguageService` worden via DI geregistreerd in `MauiProgram.cs`.
- Lokalisatie: runtime taalwijziging ondersteund via `LanguageService` en ResourceManager fallback logic (NL/EN/FR).
- Image/resources in `Resources/Images/` en platform‑specifieke assets via MAUI resource system.
- MainThread invocations voor UI updates (MainThread.BeginInvokeOnMainThread).

Cross‑cutting
- Dependency Injection (Microsoft.Extensions.DependencyInjection) gebruikt in Web en MAUI projecten.
- Logging via `ILogger<T>` door de hele stack.
- Authenticatie en autorisatie: Identity (web) + JWT voor API; clients houden access/refresh tokens en gebruiken `TokenHandler`/`AuthService`.
- CI/CD: er zijn GitHub workflow(s) in `.github/workflows/` voor provisioning/automation.

---

## AI-hulpmiddelen & ontwikkelworkflow

Deze repository gebruikt AI‑geassisteerde workflows (bijv. GitHub Copilot of vergelijkbare tools) ter ondersteuning van ontwikkeltaken. Typische AI‑taken die in dit project zijn toegepast of kunnen helpen:

- Debuggen en verbeteren van foutmeldingen (error list): analyseren van compile/runtime errors en suggesties voor fixes.
- Aanmaken van EF Core migraties: genereren of voorbereiden van migratiebestanden en SQL‑scripts.
- Automatisch aanmaken of verbeteren van documentatie, zoals README‑bestanden.
- Refactorings en kleine codewijzigingen (XAML/C#) om UX of toegankelijkheid te verbeteren.

## Problemen die ik voorkwam

Kort overzicht van concrete problemen die tijdens ontwikkeling zijn opgespoord en verholpen:

- Bestanden die wel op schijf stonden maar niet in Visual Studio Solution Explorer verschenen (ontbrekende project‑includes).
- Dubbele of gedupliceerde bestanden in het project / op schijf die verwarring en build‑issues veroorzaakten.
- Tijdelijke of IDE‑specifieke bestanden die per ongeluk in de repository konden verschijnen (bv. `.vs/`, temp READMEs).
- Problemen met NuGet‑installatie (package restore, cache‑ of netwerkfouten) waardoor builds faalden.
- TargetFramework / package‑versie mismatch die tot compile‑ of runtime‑fouten leidde.
- Onzichtbare UI‑elementen door thema‑ of resource‑issues (bv. foreground/background brushes niet geladen).
- Migratieproblemen of onvolledige EF Core migratiebestanden die database‑updates blokkeerden.
- Foutmeldingen in de error list die voortkwamen uit niet‑gesynchroniseerde bronnen of ontbrekende referenties.

---

## Installatie / snelstart (development)

Volg deze stappen om de repository lokaal op te zetten en te runnen voor ontwikkeling. De instructies zijn gericht op development; bewaar geen secrets in Git.

1) Vereisten

- .NET 9 SDK geïnstalleerd (dotnet --version toont 9.x).
- Voor MAUI: installeer MAUI workloads:

```
dotnet workload install maui
```

- Visual Studio met MAUI/WPF workloads aanbevolen voor debug op emulators/devices.

2) Clone repository

```
git clone https://github.com/kiranchaudry97/Project_Framework_Biblio.git
cd Project_Framework_Biblio
```

3) Configureer secrets (development)

> Belangrijk: voor veilige opslag van database connection strings en andere secrets, zie `Biblio_Web/SECURE_DB_SETUP.md`. Gebruik `dotnet user-secrets` tijdens development of environment variables / Key Vault in productie. Voor een snelle check kun je vanuit `Biblio_Web` ook `dotnet user-secrets list` draaien.

- Ga naar het webproject en initialiseert user-secrets (indien nog niet gedaan):

```
cd Biblio_Web
dotnet user-secrets init
```

- Zet je daadwerkelijke connectionstring (voor development) als user-secret. Voorbeeld (NIET in Git):

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<host>;Database=<db>;User Id=<user>;Password=<pwd>;TrustServerCertificate=True;"
```

- Stel eventuele seed-wachtwoorden in:

```
dotnet user-secrets set "Seed:AdminPassword" "<your-admin-pw>"
```

4) Migrations en database

- Voeg/migraties toepassen vanuit solution root met `Biblio_Web` als startup project (gebruik EF tools):

```
# vanuit solution root
cd Biblio_Models
dotnet ef database update --startup-project ../Biblio_Web
```

Als je LocalDB of een andere DB gebruikt, pas de connectionstring aan via user-secrets of environment variables.

5) Run de web API / frontend

```
# vanuit solution root
dotnet run --project Biblio_Web
```

Open vervolgens de browser op https://localhost:{port} (console toont poort).

6) Run de MAUI client (Biblio_App)

- Gebruik Visual Studio (aanbevolen) en kies target (Android emulator / Windows) en start Debug.
- Of bouw via CLI (beperkt):

```
dotnet build Biblio_App
```

Opmerking: MAUI apps worden meestal gestart vanuit Visual Studio voor emulator/device ondersteuning.

7) Run de WPF client

- Open `Biblio_WPF` in Visual Studio en start de app (Debug/Release) op Windows.

8) Security & productie

- Zet production connectionstrings nooit in Git. Gebruik environment variables of een secret store (Azure Key Vault) voor productie.
- In Azure App Service kun je connection strings configureren in Configuration → Connection strings.

9) Veelvoorkomende commando's

- User-secrets lijst: `dotnet user-secrets list` (in projectmap)
- EF add migration (voorbeeld):
  `dotnet ef migrations add NameHere --project Biblio_Models --startup-project ../Biblio_Web`
- EF update database: zie stap 4


---

Voor meer details per project, zie de README in elke projectmap (`Biblio_Web/README.md`, `Biblio_App/README.md`, `Biblio_Models/README.md`, `Biblio_WPF/README.md`).






