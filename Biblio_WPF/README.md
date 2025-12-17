# Biblio_WPF — Desktop client (WPF, .NET 9)

**Initiatiefnemer:** Chaud-Ry Kiran Jamil

Korte omschrijving
------------------
`Biblio_WPF` is de Windows‑desktopclient van het Biblio‑project. Het is een WPF‑toepassing gebouwd met MVVM en gebruikt `Biblio_Models` voor domeinmodellen.

Snelkoppelingen
---------------
- `Biblio_App` — .NET MAUI client (mobile/desktop) — `Biblio_App/README.md`
- `Biblio_Web` — ASP.NET Core webfrontend en API — `Biblio_Web/README.md`
- `Biblio_Models` — gedeelde domeinmodellen, DbContext en seeding — `Biblio_Models/README.md`

Kort (logisch)
---------------
- Target: .NET 9
- UI: WPF (MVVM)
- Database: EF Core via `Biblio_Models` (SQL Server / LocalDB)
- Starten: open in Visual Studio of `dotnet run` vanuit `Biblio_WPF`

Project- en mappenstructuur
---------------------------
Voor ontwikkelaars en snelle navigatie; pas aan indien nodig:

```
Biblio_WPF/
+-- Biblio_WPF.csproj
+-- App.xaml
+-- App.xaml.cs
+-- MainWindow.xaml
+-- MainWindow.xaml.cs
+-- Window/
¦   +-- BoekWindow.xaml
¦   +-- BoekWindow.xaml.cs
¦   +-- LidWindow.xaml
¦   +-- LidWindow.xaml.cs
¦   +-- UitleningWindow.xaml
¦   +-- UitleningWindow.xaml.cs
+-- ViewModels/
¦   +-- BoekViewModel.cs
¦   +-- LedenViewModel.cs
¦   +-- UitleningenViewModel.cs
+-- Controls/
¦   +-- LabeledTextBox.xaml
+-- Styles/
¦   +-- Theme.Light.xaml
¦   +-- Theme.Dark.xaml
+-- Resources/
+-- README.md
```

Screenshots / afbeeldingen
--------------------------
Voorbeelden van UI‑beelden (in repository):

- Login / aanmelden
  ![Login voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/loginwindow.png)

- Dark mode
  ![Darkmode voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/darkmode.png)

- Hoofdscherm
  ![Hoofdscherm voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/mainwindow.png)

- Openen (navigatie)
  ![Openen voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/openen.png)

- Beheer (gebruikers/rollen)
  ![Beheer voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/beheer.png)

- Gebruiker & rollen
  ![Gebruiker rollen](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/gebruiker_rollen.png)

- Bestandsmenu / acties
  ![Bestanden](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/bestand.png)

- Profiel
  ![Profiel](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/profile.png)

- Wachtwoord wijzigen
  ![Wachtwoord wijzigen](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/wachtwoord.png)

- Categorieën
  ![Categorieën](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/categorie.png)

- Uitleningen
  ![Uitleningen](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/uitleningen.png)

- Leden
  ![Leden](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/leden.png)

- Database overzicht
  ![Database voorbeeld](https://github.com/kiranchaudry97/Project_Framework_Biblio/blob/962442033372dee0e49d742543eea1cc5b68183e/Screenshot/database_biblio.png)

Meer details, ontwikkelinstructies en migratie‑opmerkingen staan in de root README en in `Biblio_Models/README.md`.
