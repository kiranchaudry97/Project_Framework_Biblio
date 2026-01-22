Write-Host "Force clean rebuild and deploy MAUI Android" -ForegroundColor Cyan

# 1. Uninstall from emulator
Write-Host "`n1. Uninstalling old app from emulator..." -ForegroundColor Yellow
adb uninstall com.companyname.biblio_app 2>&1 | Out-Null

# 2. Clean build artifacts
Write-Host "2. Cleaning build artifacts..." -ForegroundColor Yellow
dotnet clean Biblio_App/Biblio_App.csproj --configuration Debug
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue Biblio_App/bin, Biblio_App/obj
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue Biblio_Models/bin, Biblio_Models/obj

# 3. Rebuild
Write-Host "3. Rebuilding Biblio_Models..." -ForegroundColor Yellow
dotnet build Biblio_Models/Biblio_Models.csproj --configuration Debug

Write-Host "4. Rebuilding Biblio_App (Android)..." -ForegroundColor Yellow
dotnet build Biblio_App/Biblio_App.csproj -f net9.0-android --configuration Debug

Write-Host "`n✅ Clean rebuild complete!" -ForegroundColor Green
Write-Host "Now press F5 in Visual Studio to deploy." -ForegroundColor Cyan
