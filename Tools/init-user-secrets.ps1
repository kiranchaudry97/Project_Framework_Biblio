<#
PowerShell script to initialize user-secrets for the Biblio_Web project.
Usage:
  .\init-user-secrets.ps1
  .\init-user-secrets.ps1 -JwtKey 'very-secret' -JwtIssuer 'BiblioWebApi' -AdminEmail 'admin@biblio.local' -AdminPassword 'Admin123!'

This will run `dotnet user-secrets init` for the web project and set the provided values.
Do NOT check secrets into source control.
#>
param(
    [string]$ProjectPath = "..\Biblio_Web\Biblio_Web.csproj",
    [string]$JwtKey,
    [string]$JwtIssuer = "BiblioWebApi",
    [string]$AdminEmail = "admin@biblio.local",
    [string]$AdminPassword = "Admin123!"
)

function Read-Secret([string]$prompt) {
    Write-Host -NoNewline "$prompt: " -ForegroundColor Yellow
    $sec = Read-Host -AsSecureString
    return [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found. Install .NET SDK and try again."
    exit 1
}

# If JwtKey not provided, prompt securely
if ([string]::IsNullOrWhiteSpace($JwtKey)) {
    $JwtKey = Read-Secret "Enter JWT Key (will be stored in user-secrets)"
}

$fullProjectPath = Resolve-Path -Path $ProjectPath -ErrorAction SilentlyContinue
if (-not $fullProjectPath) {
    Write-Error "Project file not found at path: $ProjectPath"
    exit 1
}

$proj = $fullProjectPath.Path
Write-Host "Initializing user-secrets for project: $proj" -ForegroundColor Cyan

# Initialize user-secrets (adds UserSecretsId to the project if missing)
dotnet user-secrets init --project "$proj"
if ($LASTEXITCODE -ne 0) { Write-Error "user-secrets init failed"; exit 1 }

Write-Host "Setting secrets..." -ForegroundColor Cyan

dotnet user-secrets set "Jwt:Key" "$JwtKey" --project "$proj"
dotnet user-secrets set "Jwt:Issuer" "$JwtIssuer" --project "$proj"
dotnet user-secrets set "Seed:AdminEmail" "$AdminEmail" --project "$proj"
dotnet user-secrets set "Seed:AdminPassword" "$AdminPassword" --project "$proj"

if ($LASTEXITCODE -ne 0) { Write-Error "Setting one or more secrets failed"; exit 1 }

Write-Host "User-secrets initialized and values set for project: $proj" -ForegroundColor Green
Write-Host "Remember: do not commit user secrets to source control." -ForegroundColor Yellow
