@echo off
setlocal
cd /d "%~dp0"
title CROWN's ALBION v1.6.1 - Fable III
if not exist "%~dp0catalog.json" (
 echo Missing catalog.json. Extract the entire ZIP first.
 pause
 exit /b 1
)
if not exist "%~dp0references.json" (
 echo Missing references.json. Extract the entire ZIP first.
 pause
 exit /b 1
)
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
 echo A .NET Framework 4.x C# compiler was not found on this computer.
 echo Do NOT download random compiler DLLs; send us a screenshot of this output.
 pause
 exit /b 1
)
echo Compiling current CROWN's ALBION v1.6.1 source...
"%CSC%" /nologo /target:winexe /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Web.Extensions.dll /out:"%~dp0CROWNsAlbion.exe" "%~dp0src\CROWNsAlbion.cs"
if errorlevel 1 (
 echo.
 echo Build failed. Copy or screenshot the error. Existing game files are untouched.
 pause
 exit /b 1
)
echo Starting CROWN's ALBION v1.6.1...
start "CROWN's ALBION" "%~dp0CROWNsAlbion.exe"
endlocal
