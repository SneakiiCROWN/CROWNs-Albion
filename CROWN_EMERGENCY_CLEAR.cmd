@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CROWN_EMERGENCY_CLEAR.ps1"
echo.
pause
