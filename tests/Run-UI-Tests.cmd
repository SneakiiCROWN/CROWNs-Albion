@echo off
rem CROWN's ALBION — full UI regression suite (no Fable III required).
setlocal
cd /d "%~dp0.."
echo === UI regression ===
call :build_exe
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:"Exe, Menu\CROWNsAlbion.exe" /out:"Exe, Menu\UiTests.exe" tests\UiTests.cs
if errorlevel 1 exit /b 1
"Exe, Menu\UiTests.exe"
exit /b 0
:build_exe
powershell -ExecutionPolicy Bypass -File tools\build.ps1 || exit /b 1
exit /b 0