@echo off
REM Windows App SDK Components Installer
echo.
echo Installing Windows App SDK Build Components...
echo.

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0install-windows-app-sdk-components.ps1"

if errorlevel 1 (
    echo.
    echo Installation process failed or cancelled
    pause
    exit /b 1
)

exit /b 0
