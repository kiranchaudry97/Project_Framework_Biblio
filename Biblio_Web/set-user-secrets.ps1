Param(
    [Parameter(Mandatory=$false)] [string]$DbServer,
    [Parameter(Mandatory=$false)] [string]$DbName,
    [Parameter(Mandatory=$false)] [string]$DbUser,
    [Parameter(Mandatory=$false)] [string]$DbPassword
)

function Read-SecurePlainText([string]$prompt) {
    $secure = Read-Host -AsSecureString -Prompt $prompt
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

Write-Host "This script will set user-secrets for the Biblio_Web project."

if (-not $DbServer) {
    $DbServer = Read-Host -Prompt 'Enter DB server (e.g. biblio-sql-server.database.windows.net)'
}
if (-not $DbName) {
    $DbName = Read-Host -Prompt 'Enter DB name (e.g. Biblio-sql-db-6055956)'
}
if (-not $DbUser) {
    $DbUser = Read-Host -Prompt 'Enter DB user (e.g. Admin@biblio.local@biblio-sql-server)'
}
if (-not $DbPassword) {
    $DbPassword = Read-SecurePlainText 'Enter DB password (input hidden)'
}

if ([string]::IsNullOrWhiteSpace($DbServer) -or [string]::IsNullOrWhiteSpace($DbName) -or [string]::IsNullOrWhiteSpace($DbUser) -or [string]::IsNullOrWhiteSpace($DbPassword)) {
    Write-Error "All values (server, db, user, password) are required. Aborting."
    exit 1
}

$cs = "Server=tcp:$DbServer,1433;Initial Catalog=$DbName;Persist Security Info=False;User ID=$DbUser;Password=$DbPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Move into web project folder to ensure correct UserSecretsId context
Push-Location (Join-Path $PSScriptRoot '..\Biblio_Web')
try {
    Write-Host "Setting user-secrets for Biblio_Web (PublicConnection_Azure and Seed:AdminPassword)..."
    dotnet user-secrets set "PublicConnection_Azure" "$cs"
    dotnet user-secrets set "Seed:AdminPassword" "$DbPassword"
    Write-Host "User-secrets set successfully."
}
finally {
    Pop-Location
}

Write-Host "Reminder: do NOT commit secrets to source control. Use Azure App Settings or Key Vault for production."
