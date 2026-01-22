# ============================================
# SIMPLE WINDOWS FIX
# ============================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX WINDOWS BUILD - SIMPLE VERSION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# STAP 1: Stop processes
Write-Host "[1/4] Stopping processes..." -ForegroundColor Yellow
Stop-Process -Name "Biblio_App" -Force -ErrorAction SilentlyContinue
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 2: Clear cache
Write-Host "[2/4] Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 3: Restore for Windows
Write-Host "[3/4] Restoring packages for Windows..." -ForegroundColor Yellow
dotnet restore Biblio_App\Biblio_App.csproj -f net9.0-windows10.0.19041
Write-Host "      Done" -ForegroundColor Green
Write-Host ""

# STAP 4: Build
Write-Host "[4/4] Building Windows..." -ForegroundColor Yellow
dotnet build Biblio_App\Biblio_App.csproj -c Debug -f net9.0-windows10.0.19041
Write-Host ""

if ($LASTEXITCODE -eq 0) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  SUCCESS!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next: Close Visual Studio, reopen, and press F5" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  BUILD FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
}
