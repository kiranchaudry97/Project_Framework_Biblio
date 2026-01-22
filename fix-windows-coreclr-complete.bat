@echo off
REM ============================================
REM FIX WINDOWS CORECLR ERROR - Batch Wrapper
REM ============================================

echo.
echo ========================================
echo   FIX WINDOWS CORECLR ERROR
echo ========================================
echo.

PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0fix-windows-coreclr-complete.ps1"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo CoreCLR fix completed successfully!
    echo.
    echo NEXT: Close and reopen Visual Studio, then F5 to run
    pause
) else (
    echo.
    echo Fix failed. Check output above for details.
    pause
    exit /b 1
)
