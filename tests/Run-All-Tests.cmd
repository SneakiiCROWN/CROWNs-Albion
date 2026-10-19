@echo off
rem CROWN's ALBION — full offline regression suite (no Fable III required).
setlocal
cd /d "%~dp0.."
echo === data integrity ===
powershell -ExecutionPolicy Bypass -File tests\check-data.ps1 || exit /b 1
echo.
echo === engine safety (static) ===
powershell -ExecutionPolicy Bypass -File tests\check-source.ps1 || exit /b 1
echo.
echo === engine behaviour (runtime) ===
call tests\Run-Engine-Tests.cmd || exit /b 1
echo.
echo ALL OFFLINE TESTS PASSED
exit /b 0
