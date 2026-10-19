@echo off
setlocal
cd /d "%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:CROWNsAlbion.exe /out:DialogHarness.exe tests\DialogHarness.cs
if errorlevel 1 exit /b 1
DialogHarness.exe
exit /b %errorlevel%
