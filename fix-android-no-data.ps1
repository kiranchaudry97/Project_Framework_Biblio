param(
    [string]$AvdName
)

function Exec([string]$cmd, [int]$timeoutSec=0) {
    Write-Host "> $cmd"
    $proc = Start-Process -FilePath "powershell" -ArgumentList "-NoProfile -Command $cmd" -NoNewWindow -RedirectStandardOutput -RedirectStandardError -PassThru -Wait
    return $proc
}

# Check tools
if (-not (Get-Command adb -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: adb not found in PATH. Install Android SDK platform-tools and add to PATH." -ForegroundColor Red
    exit 1
}
if (-not (Get-Command emulator -ErrorAction SilentlyContinue)) {
    Write-Host "WARNING: emulator command not found. You can still use a running device." -ForegroundColor Yellow
}

# Pick AVD
if (-not $AvdName) {
    try {
        $avds = & emulator -list-avds 2>$null
    } catch {
        $avds = @()
    }
    if ($avds -and $avds.Count -gt 0) {
        $AvdName = $avds[0]
        Write-Host "Using AVD: $AvdName"
    } else {
        Write-Host "No AVD found. If you use a physical device or already running emulator, set -AvdName or ignore." -ForegroundColor Yellow
    }
}

# Start emulator if not running
$devices = & adb devices | Select-String "emulator-" -Quiet
if (-not $devices -and $AvdName) {
    Write-Host "Starting emulator: $AvdName..."
    Start-Process -FilePath emulator -ArgumentList "-avd $AvdName -no-snapshot -wipe-data" -WindowStyle Hidden
    Write-Host "Waiting for emulator to appear..."
    for ($i=0;$i -lt 120;$i++) {
        Start-Sleep -Seconds 2
        $devs = & adb devices
        if ($devs -match "emulator-") { break }
    }
}

# Wait for boot
Write-Host "Waiting for device ready (boot)..."
for ($i=0;$i -lt 120;$i++) {
    Start-Sleep -Seconds 2
    try {
        $boot = & adb shell getprop sys.boot_completed 2>$null
        if ($boot -match "1") { break }
    } catch { }
}

# Ensure device present
$devs = & adb devices
if (-not ($devs -match "device")) {
    Write-Host "No device connected. Run emulator or attach a device and retry." -ForegroundColor Red
    exit 2
}

$package = "com.companyname.biblio_app"
Write-Host "Uninstalling app (if installed): $package"
& adb uninstall $package | Out-Null

# Remove DB from app data (requires debug build or root). Try run-as first
$dbPath = "/data/data/$package/files/BiblioApp.db"
Write-Host "Attempting to remove existing DB: $dbPath"
try {
    & adb shell run-as $package rm -f $dbPath 2>$null
    Write-Host "run-as rm attempted"
} catch {
    Write-Host "run-as failed, trying direct rm (may require root)."
    & adb shell rm -f $dbPath 2>$null
}

Write-Host "DB removal done (if file existed)."

# Start app if installed later by VS, or instruct user to deploy
Write-Host "You can now deploy the app from Visual Studio (Debug -> Start) or via 'dotnet build' + VS deploy."
Write-Host "To launch app on device after install run: adb shell monkey -p $package -c android.intent.category.LAUNCHER 1"
Write-Host "If you prefer, run the included 'check-maui-localdb.ps1' or 'diagnose-maui-data.ps1' scripts to verify DB contents after app start."

Write-Host "Done." -ForegroundColor Green
