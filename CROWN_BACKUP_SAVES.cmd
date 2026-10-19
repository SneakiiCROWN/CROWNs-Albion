@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Legacy_v1_0\tools\Backup-Fable3-Saves.ps1"
echo.
pause
