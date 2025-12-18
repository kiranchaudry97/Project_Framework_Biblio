<#!
Ensure-Default-Connection.ps1
Helper script to ensure ConnectionStrings:DefaultConnection exists in user-secrets.

Usage:
  # From repository root or any location; it runs in the Biblio_Web project folder
  powershell -ExecutionPolicy Bypass -File .\Biblio_Web\ensure-default-connection.ps1

Options:
  -Force : overwrite existing ConnectionStrings:DefaultConnection if present

What it does:
  - Reads current user-secrets for the Biblio_Web project
  - If `ConnectionStrings:DefaultConnection` is missing and one of the legacy keys
    (`PublicConnection_Azure`, `PublicConnection`, `ConnectionStrings:DefaultConnection`) exists,
    it copies that value into `ConnectionStrings:DefaultConnection` using `dotnet user-secrets set`.
#>

[CmdletBinding()]
param(
    [switch]$Force
)

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    Set-Location $scriptDir

    $csproj = Get-ChildItem -Filter *.csproj | Select-Object -First 1
    if (-not $csproj) { Write-Error "No .csproj found in $scriptDir"; exit 1 }

    Write-Host "Using project: $($csproj.Name)" -ForegroundColor Cyan

    # get secrets list
    $listArgs = @('user-secrets','list','--project',$csproj.FullName)
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList $listArgs -NoNewWindow -RedirectStandardOutput -PassThru -Wait
    $output = $proc.StandardOutput.ReadToEnd()

    if (-not $output) { Write-Host "No user-secrets found or dotnet user-secrets returned no output." -ForegroundColor Yellow }

    $secrets = @{}
    foreach ($line in $output -split "`n") {
        $trim = $line.Trim()
        if ($trim -match '^([^=]+)\s*=\s*(.*)$') {
            $k = $matches[1].Trim()
            $v = $matches[2].Trim()
            $secrets[$k] = $v
        }
    }

    if ($secrets.ContainsKey('ConnectionStrings:DefaultConnection') -and -not $Force) {
        Write-Host "ConnectionStrings:DefaultConnection already exists in user-secrets." -ForegroundColor Green
        exit 0
    }

    # prefer already set DefaultConnection or use legacy keys
    $srcKey = $null
    if ($secrets.ContainsKey('ConnectionStrings:DefaultConnection')) { $srcKey = 'ConnectionStrings:DefaultConnection' }
    elseif ($secrets.ContainsKey('DefaultConnection')) { $srcKey = 'DefaultConnection' }
    elseif ($secrets.ContainsKey('PublicConnection_Azure')) { $srcKey = 'PublicConnection_Azure' }
    elseif ($secrets.ContainsKey('PublicConnection')) { $srcKey = 'PublicConnection' }
    elseif ($secrets.ContainsKey('ConnectionStrings:BibliobContextConnection')) { $srcKey = 'ConnectionStrings:BibliobContextConnection' }

    if (-not $srcKey) {
        Write-Host "No suitable source secret found (PublicConnection_Azure / PublicConnection / ConnectionStrings:BibliobContextConnection)." -ForegroundColor Yellow
        Write-Host "You can set ConnectionStrings:DefaultConnection manually with:`n  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<connection-string>\" --project $($csproj.FullName)" -ForegroundColor Cyan
        exit 1
    }

    $value = $secrets[$srcKey]
    Write-Host "Found source key '$srcKey'. Will copy its value to 'ConnectionStrings:DefaultConnection'." -ForegroundColor Cyan

    # set the secret
    $setArgs = @('user-secrets','set','ConnectionStrings:DefaultConnection',$value,'--project',$csproj.FullName)
    $setProc = Start-Process -FilePath 'dotnet' -ArgumentList $setArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput
    $setOut = $setProc.StandardOutput.ReadToEnd()
    Write-Host "dotnet user-secrets set output:`n$setOut" -ForegroundColor DarkGray

    Write-Host "ConnectionStrings:DefaultConnection set from $srcKey." -ForegroundColor Green
}
catch {
    Write-Error "Error: $_"
    exit 1
}
finally {
    # optional: return to original location
}
