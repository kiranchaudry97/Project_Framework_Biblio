$root = (Get-Location).Path
$out = Join-Path $root 'PROJECT_CHECKLIST.md'
$lines = @()

function Add-Line($text) { $script:lines += $text }

function Find-Files($patterns) {
    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $name = $_.FullName
            foreach ($p in $patterns) {
                if ($name -like "*$p*") { return $true }
            }
            return $false
        } | Select-Object -ExpandProperty FullName
}

function Get-ProjectNameForPath($path) {
    if ($path -like "*\\Biblio_App\\*") { return 'Biblio_App (.NET MAUI)' }
    if ($path -like "*\\Biblio_Web\\*") { return 'Biblio_Web (Razor Pages + API)' }
    if ($path -like "*\\Biblio_Models\\*") { return 'Biblio_Models' }
    if ($path -like "*\\Biblio_WPF\\*") { return 'Biblio_WPF' }
    return 'Onbekend project'
}

function Format-Files-With-Project($files) {
    $files | ForEach-Object {
        $proj = Get-ProjectNameForPath $_
        "- $_ (Project: $proj)"
    }
}

Add-Line "# Project Checklist"
Add-Line ""
Add-Line "Deze checklist toont per criterium waar het in de code gebruikt is, welke bestanden en in welk project (MAUI + Razor Pages)."
Add-Line ""
Add-Line "## Presentatiehandleiding (schriftelijke uitleg)"
Add-Line "- Doel: Helpt je stap-voor-stap uit te leggen wat je hebt gedaan per criterium en hoe je het aantoont."
Add-Line "- Tip: Gebruik de genoemde bestanden en projecten hieronder tijdens je demo/codewalkthrough."
Add-Line ""

# Structuur locale databank
Add-Line "## Structuur locale databank"
$localDb = Find-Files @(
    'Biblio_Models\Data\LocalDbContext.cs',
    'Biblio_Models\Data\BiblioDbContext.cs',
    'Biblio_Models\Entiteiten\'
)
if ($localDb.Count -gt 0) { Format-Files-With-Project $localDb | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: DbContext en entiteiten definiëren tabellen/relaties voor lokale opslag en gedeelde modellen."
Add-Line "- Uitleg: Toon `LocalDbContext` en entiteiten. Licht toe: tabellen, primaire sleutels, relaties (HasMany/WithOne), migraties en waarom lokale opslag (offline)."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `LocalDbContext.cs` en wijs op `DbSet<>`s."
Add-Line "  2) Open een entiteit (bijv. `Lid.cs`) en bespreek properties en keys."
Add-Line "  3) Open een migratiebestand en toon schema-creation."
Add-Line "  4) Leg offline-doel uit: lokale opslag voor gebruik zonder netwerk."
Add-Line ""

# Structuur geconsumeerde API
Add-Line "## Structuur geconsumeerde API"
$api = Find-Files @(
    'Biblio_Web\Controllers\Api\BoekenApiController.cs',
    'Biblio_Web\Controllers\Api\LedenApiController.cs',
    'Biblio_Web\Controllers\Api\UitleningenApiController.cs',
    'Biblio_Web\Models\ApiDtos\'
)
if ($api.Count -gt 0) { Format-Files-With-Project $api | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: REST endpoints en DTO’s voor CRUD en data-uitwisseling."
Add-Line "- Uitleg: Beschrijf padstructuur, endpoints (HTTP-methodes, routes), DTO’s en validatie. Leg uit hoe Razor Pages en API naast elkaar bestaan."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `BoekenApiController.cs` en toon routes/methodes (GET/POST/PUT/DELETE)."
Add-Line "  2) Open een DTO (bijv. `UitleningDto.cs`) en leg mapping naar model uit."
Add-Line "  3) Wijs in `Program.cs` de API-configuratie aan (routing, services)."
Add-Line "  4) Benoem dat Razor Pages UI en API endpoints naast elkaar draaien."
Add-Line ""

# Gebruik van de API in de MAUI-app
Add-Line "## Gebruik van de API in de MAUI-app"
$services = Find-Files @(   
    'Biblio_App\Services\BoekService.cs',
    'Biblio_App\Services\LedenService.cs',
    'Biblio_App\Services\DataSyncService.cs',
    'Biblio_App\appsettings.json'
)
if ($services.Count -gt 0) { Format-Files-With-Project $services | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: HTTP-calls naar web-API, configuratie en synchronisatie."
Add-Line "- Uitleg: Laat `BoekService/LedenService` zien met base-URL uit `appsettings.json`. Leg uit hoe `HttpClient`/`HttpRequestMessage` gebruikt wordt, headers (auth), en hoe responses gemapt worden naar modellen."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `appsettings.json` en toon API-baseURL."
Add-Line "  2) Open `BoekService.cs` en wijs op async methods voor API-calls."
Add-Line "  3) Toon mapping van JSON-responses naar C#-modellen."
Add-Line "  4) Leg uit hoe errors/logging worden afgehandeld."
Add-Line ""

# CRUD-bewerkingen voor drie tabellen
Add-Line "## CRUD-bewerkingen (Boeken, Leden, Uitleningen)"
$crud = Find-Files @(
    'Biblio_App\ViewModels\BoekenViewModel.cs',
    'Biblio_App\ViewModels\LedenViewModel.cs',
    'Biblio_App\ViewModels\UitleningenViewModel.cs',
    'Biblio_App\Services\BoekService.cs',
    'Biblio_App\Services\LedenService.cs'
)
if ($crud.Count -gt 0) { Format-Files-With-Project $crud | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: Create/Read/Update/Delete via service- en ViewModel-laag (async) met binding naar XAML-pagina’s."
Add-Line "- Uitleg: Toon per tabel de methoden (Create, Get/List, Update, Delete) in service en de `Command`s in ViewModel. Leg de stroom uit: UI-command -> VM -> service -> API -> resultaat -> UI update."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `BoekenViewModel.cs` en toon `Commands` (Create/Update/Delete)."
Add-Line "  2) Open `BoekService.cs` en toon bijbehorende CRUD-methoden."
Add-Line "  3) Leg callflow uit en demonstreer in de MAUI UI."
Add-Line ""

# Selectieveld op overzicht-view
Add-Line "## Selectieveld op overzicht-view"
$views = Find-Files @(
    'Biblio_App\Pages\Boek\BoekenPagina.xaml',
    'Biblio_App\Pages\Uitlening\UitleningenPagina.xaml'
)
if ($views.Count -gt 0) { Format-Files-With-Project $views | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: Filter/selectie via gebonden Picker/CollectionView naar ViewModel-eigenschappen/commando’s."
Add-Line "- Uitleg: Demonstreer `Picker`/`CollectionView` met `ItemsSource` en `SelectedItem`/`SelectedIndex` binding. Leg uit hoe de selectie de lijst filtert of sorteert via een VM-property/command."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `BoekenPagina.xaml` en wijs `Picker`/`CollectionView` bindings aan."
Add-Line "  2) Open bijbehorende ViewModel property (filter/sort) en logic."
Add-Line "  3) Demonstreer selectie en effect op overzicht."
Add-Line ""

# Aanmeldingsprocedure via Identity API
Add-Line "## Aanmelding via API (Identity Framework)"
$auth = Find-Files @(
    'Biblio_Web\Controllers\Api\AuthController.cs',
    'Biblio_Web\Program.cs',
    'Biblio_Web\Models\LoginViewModel.cs',
    'Biblio_Web\Models\ChangePasswordViewModel.cs'
)
if ($auth.Count -gt 0) { Format-Files-With-Project $auth | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: expliciete login (éénmalig), sessie-hergebruik via token/cookie; configuratie van Identity in Program.cs."
Add-Line "- Uitleg: Beschrijf loginflow: credentials naar `AuthController`, ontvang token/cookie, bewaar veilig (Preferences/secure storage). Toon automatische her-aanmelding per sessie via message handlers of service-initialisatie."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `AuthController.cs` en toon login endpoint."
Add-Line "  2) Open `Program.cs` en wijs op Identity-configuratie."
Add-Line "  3) Leg op de MAUI kant uit waar token/cookie wordt gezet en hergebruikt (Preferences/handler)."
Add-Line "  4) Demonstreer een login en daaropvolgende beveiligde call."
Add-Line ""

# Basis-seeding lokale databank
Add-Line "## Basis-seeding lokale databank"
$seed = Find-Files @(
    'seed_database.ps1',
    'check_and_seed_data.bat',
    'Biblio_Models\Migrations\',
    'Biblio_Models\Migrations\Local\'
)
if ($seed.Count -gt 0) { Format-Files-With-Project $seed | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: initieel data voor offline gebruik (migraties + seed scripts)."
Add-Line "- Uitleg: Toon hoe migraties worden toegepast en initdata wordt ingevoegd (scripts of `OnModelCreating`/seed-services). Leg uit waarom dit offline scenario ondersteunt."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Toon `seed_database.ps1`/`check_and_seed_data.bat` gebruik."
Add-Line "  2) Open migratiebestand en toon seed/insert data."
Add-Line "  3) Demonstreer app in offline modus met lokale data."
Add-Line ""

# Volledig asynchrone communicatie
Add-Line "## Volledig asynchrone communicatie"
$async = Find-Files @(
    'Biblio_App\Services\',
    'Biblio_App\ViewModels\'
)
if ($async.Count -gt 0) { Format-Files-With-Project $async | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: gebruik van async/await in netwerk- en data-operaties voor non-blocking UI."
Add-Line "- Uitleg: Licht toe dat alle API-calls `async/await` gebruiken, geen `.Result`/`.Wait()`. Toon voorbeelden uit services/VM’s en hoe UI responsive blijft."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Zoek in services naar `async Task` methoden en wijs `await`-gebruik aan."
Add-Line "  2) Open een ViewModel en toon async commands."
Add-Line "  3) Demonstreer dat UI niet blokkeert tijdens netwerkcalls."
Add-Line ""

# XAML bindingstechnieken
Add-Line "## Front-end binding (XAML)"
$xaml = Find-Files @(
    'Biblio_App\MainPage.xaml',
    'Biblio_App\Pages\',
    'Biblio_App\Resources\Styles\',
    'Biblio_App\Resources\Vertalingen\'
)
if ($xaml.Count -gt 0) { Format-Files-With-Project $xaml | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: MVVM-bindingen, styles, resources en i18n (zoals LanguageService)."
Add-Line "- Uitleg: Toon bindings (`{Binding}`) naar properties/commands, DataTemplates en Styles. Leg i18n uit via `SharedResource` en `LanguageService` + event om UI te verversen."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open XAML-pagina’s en wijs bindings aan (`Binding Context`, properties/commands)."
Add-Line "  2) Toon Styles/resources en hoe ze gedeeld worden."
Add-Line "  3) Demonstreer taalwissel via `LanguageService` en UI-refresh."
Add-Line ""

# Taalinstellingen (optioneel, i18n)
Add-Line "## Taalinstellingen (i18n)"
$lang = Find-Files @(
    'Biblio_App\Services\LanguageService.cs',
    'Biblio_App\App.xaml',
    'Biblio_App\Resources\Vertalingen\SharedResource.resx',
    'Biblio_Web\Resources\Vertalingen\'
)
if ($lang.Count -gt 0) { Format-Files-With-Project $lang | ForEach-Object { Add-Line $_ } } else { Add-Line "- Geen bestanden gevonden" }
Add-Line "- Doel: cultuur wisselen en UI-herlaad via events en resources."
Add-Line "- Uitleg: Beschrijf hoe cultuur wordt gezet (Preferences + `CultureInfo.DefaultThreadCurrentCulture`), `LanguageChanged` event, en waar UI zich herlaadt (AppShell/subscribers)."
Add-Line "- Presentatie-stappen:"
Add-Line "  1) Open `LanguageService.cs` en wijs op `SetLanguage`/`ResetLanguage`."
Add-Line "  2) Toon waar `LanguageChanged` gesubscribed wordt (AppShell/VM)."
Add-Line "  3) Demonstreer wissel en effect op UI via resources."
Add-Line ""

Set-Content -Path $out -Value ($lines -join "`r`n")
Write-Host "Checklist gegenereerd: $out"
