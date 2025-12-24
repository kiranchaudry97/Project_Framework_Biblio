param(
  [string]$MigrationName = "InitialCreate",
  [string]$TargetProject = "Biblio_Models",
  [string]$StartupProject = "Biblio_Web",
  [string]$Context = "BiblioDbContext"
)

Write-Host "1) Clearing NuGet caches..."
dotnet nuget locals all --clear

Write-Host "2) Restoring..."
dotnet restore
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet restore failed"; exit $LASTEXITCODE }

Write-Host "3) Building startup project..."
dotnet build $StartupProject -c Debug
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed"; exit $LASTEXITCODE }

Write-Host "4) Listing packages for startup project (sanity check)..."
dotnet list $StartupProject package

Write-Host "5) Creating migration '$MigrationName' for context '$Context'..."
dotnet ef migrations add $MigrationName -p $TargetProject -s $StartupProject --context $Context --output-dir Migrations/$Context -v
if ($LASTEXITCODE -ne 0) { Write-Error "EF migrations add failed"; exit $LASTEXITCODE }

Write-Host "Migration created successfully."