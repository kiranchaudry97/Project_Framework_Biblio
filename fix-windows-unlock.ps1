# ============================================
# FIX WINDOWS - WITH FILE UNLOCK
# ============================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX WINDOWS - UNLOCK FILES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# STAP 1: Stop ALL processes
Write-Host "[1/6] Stopping ALL processes..." -ForegroundColor Yellow

# Stop Visual Studio
Stop-Process -Name "devenv" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "MSBuild" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "Biblio_App" -Force -ErrorAction SilentlyContinue

Write-Host "      Done - Wait 3 seconds..." -ForegroundColor Green
Start-Sleep -Seconds 3
Write-Host ""

# STAP 2: Delete bin/obj
Write-Host "[2/6] Deleting bin/obj folders..." -ForegroundColor Yellow
Remove-Item "Biblio_App\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "Biblio_App\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 3: Clear cache
Write-Host "[3/6] Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 4: Restore for Windows
Write-Host "[4/6] Restoring packages for Windows..." -ForegroundColor Yellow
dotnet restore Biblio_App\Biblio_App.csproj -f net9.0-windows10.0.19041
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 5: Clean
Write-Host "[5/6] Cleaning..." -ForegroundColor Yellow
dotnet clean Biblio_App\Biblio_App.csproj -c Debug -f net9.0-windows10.0.19041
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 6: Build
Write-Host "[6/6] Building Windows..." -ForegroundColor Yellow
dotnet build Biblio_App\Biblio_App.csproj -c Debug -f net9.0-windows10.0.19041
Write-Host ""

if ($LASTEXITCODE -eq 0) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  SUCCESS!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next: Open Visual Studio and press F5" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try: Restart computer, then run this again" -ForegroundColor Yellow
    Write-Host ""
}
