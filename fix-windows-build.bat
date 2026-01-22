@echo off
REM ============================================
REM FIX WINDOWS BUILD - NuGet Package Restore
REM ============================================

echo.
echo ========================================
echo   FIX WINDOWS BUILD - NUGET RESTORE
echo ========================================
echo.

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0fix-windows-build.ps1"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build fix completed successfully!
    pause
) else (
    echo.
    echo Build fix failed. Check output above for details.
    pause
    exit /b 1
)
