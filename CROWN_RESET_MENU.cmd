@echo off
cd /d "%~dp0"
if exist "settings.json" (
  copy /y settings.json settings.previous.json >nul
  del settings.json
)
echo Only menu preferences reset; game files and saves unchanged.
pause
