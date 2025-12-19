# Biblio_App — MAUI client ( .NET MAUI, .NET 9 )

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
`Biblio_App` is de .NET MAUI client‑applicatie voor het Biblio‑project. De app biedt een mobiele/desktop UI voor browsen, zoeken, aanmaken en bewerken van boeken, leden, categorieën en uitleningen. De client communiceert met de backend API (`Biblio_Web`) en deelt domeinmodellen met `Biblio_Models`.

Installed NuGet packages (selectie)
-----------------------------------
Deze projectreferenties zijn gedefinieerd in `Biblio_App.csproj`:

- `CommunityToolkit.Mvvm` 8.4.0
- `CommunityToolkit.Maui` 8.4.0
- `Microsoft.Maui.Controls` (versie: correspondend met `$(MauiVersion)`)
- `Microsoft.Extensions.Logging.Debug` 9.0.8
- `Microsoft.Extensions.Http` 9.0.0
- `Microsoft.EntityFrameworkCore.Sqlite` 9.0.10
- `Microsoft.Extensions.Configuration.Json` 9.0.0
- `Microsoft.Extensions.Configuration.UserSecrets` 9.0.0
- `Microsoft.EntityFrameworkCore.Design` 9.0.10 (PrivateAssets)
- `Microsoft.EntityFrameworkCore` 9.0.10
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.10 (alleen voor Windows TFM)


Inhoud (snelkoppelingen)
------------------------
- [Doel & motivatie](#doel--motivatie)
- [Technische samenvatting & vereisten](#technische-samenvatting--vereisten)
- [Project- en mappenstructuur](#project--en-mappenstructuur)
- [Lokaal uitvoeren (ontwikkeling)](#lokaal-uitvoeren-ontwikkeling)
- [Lokalisatie & taalservice](#lokalisatie--taalservice)
- [Veelvoorkomende problemen](#veelvoorkomende-problemen)


---

## Doel & motivatie

- Doel: een cross‑platform (Android, iOS, macCatalyst, Windows) client leveren die gebruikers in staat stelt bibliotheekdata te raadplegen en beheren.
- Motivatie: lichte, native UI met offline‑mogelijkheden en eenvoudige synchronisatie naar de web API.

## Technische samenvatting & vereisten

- Target: .NET 9 (net9.0)
- Framework: .NET MAUI
- Platformen: Android, iOS, macCatalyst, Windows
- Dependencies: verwijst naar `Biblio_Models` voor domeinmodellen en eventueel `Biblio_Web` voor de API

Vereisten
- .NET 9 SDK
- MAUI workloads: `dotnet workload install maui`
- Visual Studio 2022/2023 met MAUI workload (aanbevolen) of CLI tooling + emulators/devices

Belangrijke mappen / bestanden
- `MauiProgram.cs` — configureert services, DI en MAUI app
- `AppShell.xaml` / `AppShell.xaml.cs` — Shell‑navigatie en routes
- `App.xaml` / `App.xaml.cs` — globale resources en startup
- `Pages/` — UI Pages (bijv. `Pages/Boek/BoekenPagina.xaml`)
- `ViewModels/` — ViewModels (MVVM)
- `Services/` — platform‑onafhankelijke services (auth, language service, token handling)
- `Resources/` — afbeeldingen, styles en fonts (`Resources/Images`, `Resources/Styles`)

## Project- en mappenstructuur

Voor dit MAUI‑project (belangrijkste mappen/bestanden):

```
Biblio_App/
+-- Biblio_App.csproj
+-- MauiProgram.cs
+-- App.xaml
+-- App.xaml.cs
+-- AppShell.xaml
+-- AppShell.xaml.cs
+-- README.md
+-- Pages/
¦   +-- Boek/
¦       +-- BoekenPagina.xaml
¦       +-- BoekenPagina.xaml.cs
¦       +-- BoekCreatePage.xaml
¦   +-- Leden/
¦       +-- LedenPagina.xaml
¦       +-- LedenPagina.xaml.cs
¦   +-- Uitlening/
¦       +-- UitleningenPagina.xaml
¦       +-- UitleningenPagina.xaml.cs
+-- ViewModels/
¦   +-- BoekenViewModel.cs
¦   +-- LedenViewModel.cs
¦   +-- CategorieenViewModel.cs
¦   +-- UitleningenViewModel.cs
+-- Services/
¦   +-- AuthService.cs
¦   +-- IAuthService.cs
¦   +-- TokenHandler.cs
¦   +-- LanguageService.cs
+-- Resources/
¦   +-- Styles/
¦       +-- Styles.xaml
¦   +-- Images/
¦       +-- details_illustration.svg
¦       +-- delete_illustration.svg
¦       +-- categorie_illustration.svg
+-- Models/  # kan verwijzen naar Biblio_Models project
+-- Platforms/ (gegenereerd door MAUI tijdens build)
```

Opmerking: dit project gebruikt `Biblio_Models` (gescheiden project) voor entiteiten en DB‑logica.

Screenshots / afbeeldingen
--------------------------
Voorbeelden van UI‑beelden :

- Login
  ![Login]()


- Menu
  ![menu](Biblio_App/Biblio_App_Screenshots/Menu.png)

- Taal
  ![taal](Biblio_App/Biblio_App_Screenshots/Taalwijziging.png)

- Thema
  ![thema](Biblio_App/Biblio_App_Screenshots/Thema.png)

- Boeken Pagina 
  ![book](Biblio_App/Biblio_App_Screenshots/Boeken_Pagina.png)

- Leden
  ![lid](Biblio_App/Biblio_App_Screenshots/Leden_Pagina.png)


- Uitleningen
  ![uitleningen]()


- Categorieën 
  ![category](Biblio_App/Biblio_App_Screenshots/Categorie_Pagina.png)

- Instellingen
  ![Instellingen](Biblio_App/Biblio_App_Screenshots/Instelllingen.png)


## Lokaal uitvoeren (ontwikkeling)

1. Zorg dat .NET 9 SDK en MAUI workloads zijn geïnstalleerd:

```bash
dotnet --version
dotnet workload install maui
```

2. (Optioneel) Start de backend API (`Biblio_Web`) voor volledige functionaliteit:

```bash
dotnet run --project ../Biblio_Web
```

3. Build en run de MAUI app vanuit Visual Studio (aanbevolen) of CLI:

- Visual Studio: open oplossing en kies target (Android/iOS/Windows) en start debug
- CLI (voor build):

```bash
dotnet build
```

MAUI applicaties worden doorgaans via Visual Studio gestart voor emulators / devices. Voor sommige targets is extra platform‑tooling vereist.

## Lokalisatie & taalservice

- De app bevat lokalisatie‑ondersteuning (NL/EN/FR) en haalt vertalingen uit gedeelde resourcebestanden indien beschikbaar.
- De taalservice is geïmplementeerd in `Services/LanguageService.cs` en wordt door pages gebruikt (bijv. `BoekenPagina`) om strings te vertalen en UI te vernieuwen bij taalwijziging.

## Veelvoorkomende problemen

- Buildfouten: controleer dat .NET 9 en MAUI workloads correct zijn geïnstalleerd.
- Emulator/device issues: zorg dat Android/iOS emulators zijn geconfigureerd en dat Windows target compatibel is.
- API‑connectiviteit: start eerst `Biblio_Web` backend en controleer base‑URL / configuratie in services.
- Resource‑vertalingen: als shared resource assemblies niet geladen kunnen worden, valt de app terug op ingebouwde sleutelwaarden (NL/EN/FR).


---

Screenshot(s) / voorbeelden

- Voeg hier screenshots of korte demo instructies toe (placeholder)

---

