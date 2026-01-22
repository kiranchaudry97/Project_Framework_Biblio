@echo off
echo ========================================
echo  FIX NUGET PACKAGES - MAUI BUILD ERROR
echo ========================================
echo.
echo Dit script lost het MSB3030 error op (missing HybridWebView.js en WebView2Loader.dll)
echo.
echo BELANGRIJK: Sluit Visual Studio VOOR je dit script uitvoert!
echo.
pause

echo.
echo Stap 1: Navigeren naar Biblio_App directory...
cd /d "%~dp0Biblio_App"

echo.
echo Stap 2: Verwijderen van bin en obj folders...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo Folders verwijderd.

echo.
echo Stap 3: NuGet cache clearen...
dotnet nuget locals all --clear
echo Cache gecleared.

echo.
echo Stap 4: Packages forceren naar specifieke versies...
dotnet add package Microsoft.Maui.Controls --version 9.0.111
dotnet add package Microsoft.Maui.Controls.Compatibility --version 9.0.111

echo.
echo Stap 5: Project restoren met --force...
dotnet restore --force

echo.
echo Stap 6: Hele solution restoren...
cd ..
dotnet restore Project_Framework_Biblio.sln --force

echo.
echo ========================================
echo  GEREED!
echo ========================================
echo.
echo Je kunt nu Visual Studio opnieuw openen en rebuilden.
echo.
pause
