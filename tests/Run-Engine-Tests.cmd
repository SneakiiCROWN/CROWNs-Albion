@echo off
rem Builds and runs the v2.0 engine regression harness against the built EXE.
setlocal
cd /d "%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "EXE=Exe, Menu\CROWNsAlbion.exe"
if not exist "%EXE%" (
  echo Building CROWN's ALBION first...
  powershell -ExecutionPolicy Bypass -File tools\build.ps1 || exit /b 1
)
"%CSC%" /nologo /target:exe /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:"%EXE%" /out:"Exe, Menu\EngineHarness.exe" tests\EngineHarness.cs
if errorlevel 1 exit /b 1
"Exe, Menu\EngineHarness.exe"
exit /b %errorlevel%
