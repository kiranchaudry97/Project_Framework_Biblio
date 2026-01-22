# ============================================
# FIX WINDOWS BUILD - NuGet Package Restore
# ============================================
# Dit script fix de Windows build errors door:
# 1. NuGet cache te clearen
# 2. Packages te restoren
# 3. Build te testen

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX WINDOWS BUILD - NUGET RESTORE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Stap 1: Clear NuGet Cache
Write-Host "[1/3] Clearing NuGet cache..." -ForegroundColor Yellow
Write-Host "      Dit kan 30-60 seconden duren..." -ForegroundColor Gray
dotnet nuget locals all --clear

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ NuGet cache cleared successfully!" -ForegroundColor Green
} else {
    Write-Host "      ✗ Failed to clear NuGet cache" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Stap 2: Restore Biblio_App Packages
Write-Host "[2/3] Restoring Biblio_App packages..." -ForegroundColor Yellow
Write-Host "      Dit kan 2-5 minuten duren..." -ForegroundColor Gray
dotnet restore Biblio_App\Biblio_App.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ Biblio_App packages restored!" -ForegroundColor Green
} else {
    Write-Host "      ✗ Failed to restore Biblio_App packages" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Stap 3: Verify Critical Windows Packages
Write-Host "[3/3] Verifying Windows-specific packages..." -ForegroundColor Yellow

$windowsPackages = @(
    "Microsoft.WindowsAppSDK",
    "Microsoft.Maui.Controls",
    "Microsoft.Maui.Controls.Core",
    "Microsoft.Maui.Controls.Compatibility"
)

$allFound = $true
foreach ($package in $windowsPackages) {
    $packagePath = "$env:USERPROFILE\.nuget\packages\$($package.ToLower())"
    if (Test-Path $packagePath) {
        Write-Host "      ✓ $package found" -ForegroundColor Green
    } else {
        Write-Host "      ✗ $package missing" -ForegroundColor Red
        $allFound = $false
    }
}

Write-Host ""

if ($allFound) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✓ ALL PACKAGES RESTORED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Close Visual Studio (if open)" -ForegroundColor White
    Write-Host "  2. Reopen solution" -ForegroundColor White
    Write-Host "  3. Clean solution: Build - Clean Solution" -ForegroundColor White
    Write-Host "  4. Rebuild solution: Build - Rebuild Solution" -ForegroundColor White
    Write-Host ""
    Write-Host "Or run the test build script:" -ForegroundColor Cyan
    Write-Host "  .\test-windows-build.ps1" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ✗ SOME PACKAGES MISSING!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try manual restore in Visual Studio:" -ForegroundColor Yellow
    Write-Host "  Tools - NuGet Package Manager - Package Manager Console" -ForegroundColor White
    Write-Host "  Then run: Update-Package -Reinstall" -ForegroundColor White
    Write-Host ""
    exit 1
}
