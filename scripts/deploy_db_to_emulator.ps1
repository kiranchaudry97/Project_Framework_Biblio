param(
    [string]$LocalDbPath = "Biblio_Models\bibliodatabase.db",
    [string]$PackageName = "com.companyname.biblio_app"
)

Write-Host "Deploying $LocalDbPath to emulator for package $PackageName" -ForegroundColor Cyan

if (-not (Test-Path $LocalDbPath)) {
    Write-Host "ERROR: Local DB not found at $LocalDbPath" -ForegroundColor Red
    exit 1
}

# Resolve adb path: prefer environment variables, fall back to common SDK locations
$adbCandidates = @()
if ($env:ADB_HOME) { $adbCandidates += Join-Path $env:ADB_HOME 'adb.exe' }
if ($env:ANDROID_HOME) { $adbCandidates += Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe' }
$adbCandidates += 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$adbCandidates += 'C:\Program Files\Android\android-sdk\platform-tools\adb.exe'
$adbCandidates += 'C:\Users\' + $env:USERNAME + '\AppData\Local\Android\Sdk\platform-tools\adb.exe'

$adbPath = $null
foreach ($c in $adbCandidates) {
    if (Test-Path $c) { $adbPath = $c; break }
}

if (-not $adbPath) {
    # fallback to assuming adb is in PATH
    try { Get-Command adb -ErrorAction Stop | Out-Null; $adbPath = 'adb' } catch { }
}

if (-not $adbPath) {
    Write-Host "ERROR: adb not found. Install Android Platform-Tools or set ADB_HOME/ANDROID_HOME." -ForegroundColor Red
    exit 2
}

Write-Host "Using adb: $adbPath" -ForegroundColor Yellow

Write-Host "Checking ADB devices..." -ForegroundColor Yellow
$devices = & "$adbPath" devices | Select-String "device" | Where-Object { $_ -notmatch "List of devices attached" }
if (-not $devices) {
    Write-Host "No emulator/device found. Start an Android emulator and ensure 'adb devices' shows it as 'device'." -ForegroundColor Red
    exit 3
}

Write-Host "Pushing DB to /data/local/tmp..." -ForegroundColor Yellow
& "$adbPath" push $LocalDbPath /data/local/tmp/bibliodatabase.db
if ($LASTEXITCODE -ne 0) { Write-Host "adb push failed" -ForegroundColor Red; exit 4 }

Write-Host "Copying into app private files via run-as..." -ForegroundColor Yellow
$copyCmd = "run-as $PackageName cp /data/local/tmp/bibliodatabase.db /data/data/$PackageName/files/bibliodatabase.db"
$rmTmpCmd = "run-as $PackageName rm /data/local/tmp/bibliodatabase.db"

& "$adbPath" shell "$copyCmd"
if ($LASTEXITCODE -ne 0) {
    Write-Host "run-as copy failed. Ensure the app is a Debug build (debuggable)." -ForegroundColor Red
    Write-Host "Alternative: run 'adb root' on emulator or start emulator as root and re-run this script." -ForegroundColor Yellow
    exit 5
}

& "$adbPath" shell "$rmTmpCmd"

Write-Host "Restarting app..." -ForegroundColor Yellow
& "$adbPath" shell am force-stop $PackageName
Start-Sleep -s 1
& "$adbPath" shell monkey -p $PackageName -c android.intent.category.LAUNCHER 1

Write-Host "Waiting for app init..." -ForegroundColor Yellow
Start-Sleep -s 3

Write-Host "Tailing biblio_seed.log (last 200 lines):" -ForegroundColor Cyan
$log = & "$adbPath" shell "run-as $PackageName cat /data/data/$PackageName/files/biblio_seed.log"
$log -split "`n" | Select-Object -Last 200 | ForEach-Object { Write-Host $_ }

Write-Host "Done. Open the app in the emulator and check the Boeken / Leden / Uitleningen pages." -ForegroundColor Green
