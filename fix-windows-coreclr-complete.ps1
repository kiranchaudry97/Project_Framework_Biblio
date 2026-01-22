# ============================================
# FIX WINDOWS CoreCLR DEPLOYMENT ERROR
# ============================================
# Dit script fix de Windows CoreCLR error 0x8000809a

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX WINDOWS CORECLR ERROR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# STAP 1: Stop running processes
Write-Host "[1/6] Stopping Windows app processes..." -ForegroundColor Yellow
$stopped = $false
try {
    $null = taskkill /F /IM Biblio_App.exe 2>&1
    $stopped = $true
    Write-Host "      ✓ Processes stopped" -ForegroundColor Green
} catch {
    Write-Host "      ℹ No running processes found" -ForegroundColor Gray
}

Write-Host ""

# STAP 2: Clear NuGet Cache
Write-Host "[2/6] Clearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ NuGet cache cleared" -ForegroundColor Green
} else {
    Write-Host "      ✗ Failed to clear cache" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STAP 3: Clean Windows deployment cache
Write-Host "[3/6] Cleaning Windows deployment cache..." -ForegroundColor Yellow
$cleaned = 0
try {
    $paths = @(
        "$env:LOCALAPPDATA\Packages\*Biblio*",
        "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\ComponentModelCache",
        "$env:TEMP\*Biblio*"
    )
    
    foreach ($path in $paths) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
            $cleaned++
        }
    }
    
    Write-Host "      ✓ Cleaned $cleaned cache locations" -ForegroundColor Green
} catch {
    Write-Host "      ⚠ Partial clean completed" -ForegroundColor Yellow
}

Write-Host ""

# STAP 4: Restore packages
Write-Host "[4/6] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore Biblio_App\Biblio_App.csproj
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ Packages restored" -ForegroundColor Green
} else {
    Write-Host "      ✗ Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STAP 5: Clean build
Write-Host "[5/6] Cleaning build..." -ForegroundColor Yellow
dotnet clean Biblio_App\Biblio_App.csproj -c Debug -f net9.0-windows10.0.19041
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✓ Clean successful" -ForegroundColor Green
} else {
    Write-Host "      ⚠ Clean completed with warnings" -ForegroundColor Yellow
}

Write-Host ""

# STAP 6: Rebuild for Windows
Write-Host "[6/6] Rebuilding for Windows..." -ForegroundColor Yellow
Write-Host "      This may take 2-5 minutes..." -ForegroundColor Gray
Write-Host ""

$buildOutput = dotnet build Biblio_App\Biblio_App.csproj `
    -c Debug `
    -f net9.0-windows10.0.19041 `
    2>&1

$buildSuccess = $LASTEXITCODE -eq 0

# Parse errors
$errors = @()
$warnings = @()

foreach ($line in $buildOutput) {
    if ($line -match "error (MSB|CS)") {
        $errors += $line
    }
    if ($line -match "warning (MSB|CS)") {
        $warnings += $line
    }
}

Write-Host ""

# Show results
if ($buildSuccess) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✓ WINDOWS BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Build Statistics:" -ForegroundColor Cyan
    Write-Host "  • Errors:   0" -ForegroundColor Green
    Write-Host "  • Warnings: $($warnings.Count)" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Close Visual Studio" -ForegroundColor White
    Write-Host "  2. Reopen solution" -ForegroundColor White
    Write-Host "  3. Set Biblio_App as startup project" -ForegroundColor White
    Write-Host "  4. Select 'Windows Machine' as target" -ForegroundColor White
    Write-Host "  5. Press F5 to run" -ForegroundColor White
    Write-Host ""
    Write-Host "✓ CoreCLR error should now be fixed!" -ForegroundColor Green
    Write-Host ""
    
} else {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ✗ BUILD FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Errors found:" -ForegroundColor Red
    foreach ($error in $errors | Select-Object -First 10) {
        Write-Host "  • $error" -ForegroundColor Red
    }
    
    if ($errors.Count -gt 10) {
        Write-Host "  ... and $($errors.Count - 10) more errors" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Try these solutions:" -ForegroundColor Yellow
    Write-Host "  1. Install Windows SDK 10.0.19041" -ForegroundColor White
    Write-Host "  2. Restart computer" -ForegroundColor White
    Write-Host "  3. Run as Administrator" -ForegroundColor White
    Write-Host ""
    
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TROUBLESHOOTING CORECLR ERROR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "If CoreCLR error still occurs:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Option 1: Developer Mode (Recommended)" -ForegroundColor Cyan
Write-Host "  1. Open Windows Settings" -ForegroundColor White
Write-Host "  2. Go to: Update & Security - For developers" -ForegroundColor White
Write-Host "  3. Enable 'Developer Mode'" -ForegroundColor White
Write-Host ""
Write-Host "Option 2: Uninstall Previous Version" -ForegroundColor Cyan
Write-Host "  1. Open Settings - Apps" -ForegroundColor White
Write-Host "  2. Search for 'Biblio_App'" -ForegroundColor White
Write-Host "  3. Uninstall any previous versions" -ForegroundColor White
Write-Host ""
Write-Host "Option 3: Certificate Fix" -ForegroundColor Cyan
Write-Host "  Run in Visual Studio:" -ForegroundColor White
Write-Host "  Project - Publish - Create App Packages - Yes" -ForegroundColor White
Write-Host "  This will install the certificate" -ForegroundColor White
Write-Host ""
