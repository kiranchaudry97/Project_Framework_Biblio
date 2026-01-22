<#
.SYNOPSIS
    Fix broken Android SDK paths and create a working emulator.

.DESCRIPTION
    This script:
    1. Accepts Android SDK licenses
    2. Downloads missing system images (Android 33/34, Google APIs)
    3. Creates a new working AVD (BiblioEmulator)
    4. Starts the emulator

.EXAMPLE
    .\fix-android-sdk-and-create-emulator.ps1
#>

$ErrorActionPreference = "Continue"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Write-OK($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }

$sdkRoot = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { Join-Path $env:LOCALAPPDATA "Android\Sdk" }
$sdkManager = Join-Path $sdkRoot "cmdline-tools\latest\bin\sdkmanager.bat"
$avdManager = Join-Path $sdkRoot "cmdline-tools\latest\bin\avdmanager.bat"
$emulatorExe = Join-Path $sdkRoot "emulator\emulator.exe"

Write-Info "Android SDK root: $sdkRoot"

# Check if SDK manager exists
if (-not (Test-Path $sdkManager)) {
    Write-Err "SDK Manager not found at: $sdkManager"
    Write-Warn "Install Android SDK command-line tools via Visual Studio Installer:"
    Write-Warn "  Visual Studio Installer → Modify → Individual Components → Android SDK setup (API 33)"
    exit 1
}

Write-Info "Step 1/5: Accepting Android SDK licenses..."
try {
    # Accept all licenses non-interactively
    "y" | & $sdkManager --licenses 2>&1 | Out-Null
    Write-OK "Licenses accepted."
} catch {
    Write-Warn "Could not accept licenses automatically. Continuing anyway."
}

Write-Info "Step 2/5: Checking installed system images..."
$installed = & $sdkManager --list_installed 2>$null | Select-String "system-images"
Write-Host "Currently installed:"
$installed | ForEach-Object { Write-Host "  $_" }

Write-Info "Step 3/5: Installing Android 33 system image (Google APIs, x86_64)..."
Write-Warn "This may take 5-10 minutes depending on your internet connection..."
try {
    & $sdkManager "system-images;android-33;google_apis;x86_64" --channel=0
    Write-OK "System image installed."
} catch {
    Write-Err "Failed to install system image: $_"
    Write-Warn "Try manually: Visual Studio → Tools → Android → Android SDK Manager"
}

Write-Info "Step 4/5: Creating new AVD 'BiblioEmulator' with API 33..."
try {
    # Delete existing if present
    & $avdManager delete avd -n BiblioEmulator 2>$null
    
    # Create new AVD with downloaded system image
    & $avdManager create avd -n BiblioEmulator `
        -k "system-images;android-33;google_apis;x86_64" `
        -d "pixel_7" `
        --force
    
    Write-OK "AVD 'BiblioEmulator' created successfully."
} catch {
    Write-Err "Failed to create AVD: $_"
    exit 1
}

Write-Info "Step 5/5: Starting emulator (this may take 1-2 minutes)..."
Write-Warn "A new window will open. Keep it running and return to Visual Studio to deploy."

try {
    $emulatorProcess = Start-Process -FilePath $emulatorExe `
        -ArgumentList "-avd", "BiblioEmulator", "-no-snapshot-load", "-gpu", "host" `
        -PassThru -WindowStyle Normal
    
    Write-OK "Emulator starting (PID: $($emulatorProcess.Id))..."
    Write-Info "Wait for the Android home screen to appear, then deploy from Visual Studio."
    Write-Info "Visual Studio → Debug → BiblioEmulator → Start"
} catch {
    Write-Err "Failed to start emulator: $_"
    Write-Warn "You can start manually: Visual Studio → Tools → Android → Android Device Manager"
}
