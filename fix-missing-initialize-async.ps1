<#
.SYNOPSIS
    AUTO-FIX: Voeg InitializeAsync toe aan ViewModels en Pages die data laden

.DESCRIPTION
    Dit script patcht automatisch:
    1. BoekenViewModel - add InitializeAsync + LoadDataAsync
    2. CategorieenViewModel - add InitializeAsync + LoadDataAsync  
    3. BoekenPagina.xaml.cs - call ViewModel.InitializeAsync in OnAppearing
    4. CategorieenPagina.xaml.cs - call ViewModel.InitializeAsync in OnAppearing

.EXAMPLE
    .\fix-missing-initialize-async.ps1
#>

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-OK($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "  AUTO-FIX: INITIALIZEASYNC VOOR VIEWMODELS & PAGES" -ForegroundColor Magenta
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""

# Fix 1: BoekenViewModel - Add InitializeAsync
Write-Info "FIX 1/4: BoekenViewModel.cs - add InitializeAsync()..."
$boekenVM = "Biblio_App\ViewModels\BoekenViewModel.cs"
$content = Get-Content $boekenVM -Raw

if ($content -notmatch "public async Task InitializeAsync") {
    # Find constructor end and insert InitializeAsync after it
    $initMethod = @"

        private bool _initialized = false;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await EnsureDataLoadedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(`$"BoekenViewModel.InitializeAsync error: {ex}");
            }
        }
"@
    
    # Insert after the constructor (find first method after constructor)
    $insertPos = $content.IndexOf("public BoekenViewModel(")
    if ($insertPos -ge 0) {
        $closeBrace = $content.IndexOf("}",

 $insertPos)
        $nextMethod = $content.IndexOf("private", $closeBrace)
        if ($nextMethod -lt 0) { $nextMethod = $content.IndexOf("public", $closeBrace) }
        
        if ($nextMethod -gt 0) {
            $content = $content.Insert($nextMethod, $initMethod + "`n`n        ")
            Set-Content -Path $boekenVM -Value $content -NoNewline
            Write-OK "InitializeAsync toegevoegd aan BoekenViewModel"
        }
    }
} else {
    Write-OK "BoekenViewModel heeft al InitializeAsync"
}

# Fix 2: BoekenPagina.xaml.cs - Call InitializeAsync in OnAppearing
Write-Info "FIX 2/4: BoekenPagina.xaml.cs - call InitializeAsync in OnAppearing..."
$boekenPage = "Biblio_App\Pages\Boek\BoekenPagina.xaml.cs"
$content = Get-Content $boekenPage -Raw

if ($content -match "protected override void OnAppearing") {
    # Find OnAppearing and check if it calls InitializeAsync
    if ($content -notmatch "InitializeAsync") {
        # Replace OnAppearing to add InitializeAsync call
        $oldOnAppearing = [regex]::Match($content, "protected override void OnAppearing\(\)[^}]*\{[^}]*\}").Value
        
        if ($oldOnAppearing) {
            $newOnAppearing = @"
protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (BindingContext is BoekenViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(`$"BoekenPagina.OnAppearing error: {ex}");
            }
            
            // Keep existing localization code
            try
            {
                var langService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                if (langService != null)
                {
                    var culture = langService.CurrentCulture;
                    var langCode = culture.TwoLetterISOLanguageName.ToUpperInvariant();
                    PageLanguageLabel.Text = langCode;
                }
            }
            catch { }
        }
"@
            $content = $content.Replace($oldOnAppearing, $newOnAppearing)
            Set-Content -Path $boekenPage -Value $content -NoNewline
            Write-OK "OnAppearing in BoekenPagina aangepast om InitializeAsync aan te roepen"
        }
    } else {
        Write-OK "BoekenPagina roept al InitializeAsync aan"
    }
}

# Fix 3: CategorieenViewModel - Add InitializeAsync
Write-Info "FIX 3/4: CategorieenViewModel.cs - add InitializeAsync()..."
$catVM = "Biblio_App\ViewModels\CategorieenViewModel.cs"
if (Test-Path $catVM) {
    $content = Get-Content $catVM -Raw
    
    if ($content -notmatch "public async Task InitializeAsync") {
        $initMethod = @"

        private bool _initialized = false;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await EnsureDataLoadedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(`$"CategorieenViewModel.InitializeAsync error: {ex}");
            }
        }
"@
        
        $insertPos = $content.IndexOf("public CategorieenViewModel(")
        if ($insertPos -ge 0) {
            $closeBrace = $content.IndexOf("}", $insertPos)
            $nextMethod = $content.IndexOf("private", $closeBrace)
            if ($nextMethod -lt 0) { $nextMethod = $content.IndexOf("public", $closeBrace) }
            
            if ($nextMethod -gt 0) {
                $content = $content.Insert($nextMethod, $initMethod + "`n`n        ")
                Set-Content -Path $catVM -Value $content -NoNewline
                Write-OK "InitializeAsync toegevoegd aan CategorieenViewModel"
            }
        }
    } else {
        Write-OK "CategorieenViewModel heeft al InitializeAsync"
    }
}

# Fix 4: CategorieenPagina.xaml.cs - Call InitializeAsync in OnAppearing
Write-Info "FIX 4/4: CategorieenPagina.xaml.cs - call InitializeAsync in OnAppearing..."
$catPage = "Biblio_App\Pages\Categorieen\CategorieenPagina.xaml.cs"
if (Test-Path $catPage) {
    $content = Get-Content $catPage -Raw
    
    if ($content -match "protected override void OnAppearing") {
        if ($content -notmatch "InitializeAsync") {
            $oldOnAppearing = [regex]::Match($content, "protected override void OnAppearing\(\)[^}]*\{[^}]*\}").Value
            
            if ($oldOnAppearing) {
                $newOnAppearing = @"
protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (BindingContext is CategorieenViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(`$"CategorieenPagina.OnAppearing error: {ex}");
            }
            
            // Keep existing localization code
            try
            {
                var langService = App.Current?.Handler?.MauiContext?.Services?.GetService<ILanguageService>();
                if (langService != null)
                {
                    var culture = langService.CurrentCulture;
                    var langCode = culture.TwoLetterISOLanguageName.ToUpperInvariant();
                    PageLanguageLabel.Text = langCode;
                }
            }
            catch { }
        }
"@
                $content = $content.Replace($oldOnAppearing, $newOnAppearing)
                Set-Content -Path $catPage -Value $content -NoNewline
                Write-OK "OnAppearing in CategorieenPagina aangepast om InitializeAsync aan te roepen"
            }
        } else {
            Write-OK "CategorieenPagina roept al InitializeAsync aan"
        }
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ AUTO-FIX COMPLEET!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "VOLGENDE STAPPEN:" -ForegroundColor Cyan
Write-Host "1. In Visual Studio → Build → Clean Solution" -ForegroundColor White
Write-Host "2. Build → Rebuild Solution" -ForegroundColor White
Write-Host "3. Stop debug (als actief)" -ForegroundColor White
Write-Host "4. Verwijder app van emulator (lange press → uninstall)" -ForegroundColor White
Write-Host "5. Deploy opnieuw → Data moet nu verschijnen!" -ForegroundColor White
Write-Host ""
