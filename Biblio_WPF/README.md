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
  ![Login voorbeeld](Screenshot/loginwindow.png)

- Dark mode
  ![Darkmode voorbeeld](Screenshot/darkmode.png)

- Hoofdscherm
  ![Hoofdscherm voorbeeld](Screenshot/mainwindow.png)

- Openen (navigatie)
  ![Openen voorbeeld](Screenshot/openen.png)

- Beheer (gebruikers/rollen)
  ![Beheer voorbeeld](Screenshot/beheer.png)

- Gebruiker & rollen
  ![Gebruiker rollen](Screenshot/gebruiker_rollen.png)

- Bestandsmenu / acties
  ![Bestanden](Screenshot/bestand.png)

- Profiel
  ![Profiel](Screenshot/profile.png)

- Wachtwoord wijzigen
  ![Wachtwoord wijzigen](Screenshot/wachtwoord.png)

- Categorieën
  ![Categorieën](Screenshot/categorie.png)

- Uitleningen
  ![Uitleningen](Screenshot/uitleningen.png)

- Leden
  ![Leden](Screenshot/leden.png)

- Database overzicht
  ![Database voorbeeld](Screenshot/database_biblio.png)

Meer details, ontwikkelinstructies en migratie‑opmerkingen staan in de root README en in `Biblio_Models/README.md`.
