@echo off
echo.
echo ========================================
echo   FIX WINDOWS - UNLOCK FILES
echo ========================================
echo.
echo IMPORTANT: Close Visual Studio first!
echo.
pause

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0fix-windows-unlock.ps1"

pause
