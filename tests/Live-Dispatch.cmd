@echo off
rem Sends ONE real command through the engine while Fable III is running.
rem Default command is the harmless test message. Usage: Live-Dispatch.cmd [command-id]
setlocal
cd /d "%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "EXE=Exe, Menu\CROWNsAlbion.exe"
if not exist "%EXE%" powershell -ExecutionPolicy Bypass -File tools\build.ps1 || exit /b 1
"%CSC%" /nologo /target:exe /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:"%EXE%" /out:"Exe, Menu\LiveDispatch.exe" tests\LiveDispatch.cs
if errorlevel 1 exit /b 1
"Exe, Menu\LiveDispatch.exe" %*
exit /b %errorlevel%
