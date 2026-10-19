@echo off
rem Captures each themed dialog to docs\ui for visual QA.
setlocal
cd /d "%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "EXE=Exe, Menu\CROWNsAlbion.exe"
if not exist "%EXE%" powershell -ExecutionPolicy Bypass -File tools\build.ps1 || exit /b 1
"%CSC%" /nologo /target:exe /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:"%EXE%" /out:"Exe, Menu\UiShots.exe" tests\UiShots.cs
if errorlevel 1 exit /b 1
"Exe, Menu\UiShots.exe"
exit /b %errorlevel%
