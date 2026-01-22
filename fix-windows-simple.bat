@echo off
echo.
echo ========================================
echo   SIMPLE WINDOWS FIX
echo ========================================
echo.

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0fix-windows-simple.ps1"

pause
