@echo off
REM One-time setup. Right-click this file and choose
REM "Run as administrator". After it succeeds, you can run
REM CheckersServer.exe normally as a regular user.

echo ===========================================================
echo   Checkers server - URL ACL setup (run as administrator)
echo ===========================================================
echo.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script is not running as administrator.
    echo Right-click the file and choose "Run as administrator".
    echo.
    pause
    exit /b 1
)

set URL=http://+:8733/Design_Time_Addresses/CheckersService/
set ME=%USERDOMAIN%\%USERNAME%

echo Granting %ME%  permission to listen on:
echo     %URL%
echo.

REM Remove an existing reservation for the same URL (no-op if absent)
REM so re-runs don't fail with "URL reservation already exists".
netsh http delete urlacl url=%URL% >nul 2>&1

netsh http add urlacl url=%URL% user="%ME%"
if %errorlevel% neq 0 (
    echo.
    echo Failed to register the URL reservation.
    pause
    exit /b 1
)

echo.
echo Verifying the reservation now exists:
netsh http show urlacl url=%URL%
echo.
echo Done. CheckersServer.exe will now run as a regular user.
echo.
pause
