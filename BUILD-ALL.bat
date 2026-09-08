@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "PSHOST="
where pwsh.exe >nul 2>nul
if not errorlevel 1 set "PSHOST=pwsh.exe"
if not defined PSHOST (
  where powershell.exe >nul 2>nul
  if not errorlevel 1 set "PSHOST=powershell.exe"
)
if not defined PSHOST (
  echo [FAIL] PowerShell 5.1 or pwsh was not found.
  exit /b 1
)
"%PSHOST%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\scripts\build-all-samples.ps1" -Output Package
exit /b %ERRORLEVEL%
