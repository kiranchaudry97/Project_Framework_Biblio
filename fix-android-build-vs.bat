@echo off
echo ========================================
echo Android Build Fix voor Visual Studio
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] Cleaning solution...
dotnet clean BiblioLaunch.sln -c Debug

echo.
echo [2/4] Cleaning Android specific...
dotnet clean Biblio_App\Biblio_App.csproj -f net9.0-android -c Debug

echo.
echo [3/4] Rebuild Android...
dotnet build Biblio_App\Biblio_App.csproj -f net9.0-android -c Debug

echo.
echo [4/4] Status check...
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo SUCCESS! Android build klaar
    echo ========================================
    echo.
    echo Nu kan je:
    echo 1. Visual Studio HERLADEN (Ctrl+Shift+F5)
    echo 2. Of project UNLOAD en RELOAD
    echo 3. Dan F5 voor deployment
    echo.
) else (
    echo.
    echo ========================================
    echo BUILD FAILED - Check errors above
    echo ========================================
    echo.
)

pause
