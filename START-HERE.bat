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
if not defined SHARPPROSPERO_ROOT (
  echo [FAIL] Set SHARPPROSPERO_ROOT to the supplied SharpProspero 0.8 folder.
  exit /b 1
)
set "SAMPLE=%~1"
if not defined SAMPLE set "SAMPLE=Hello2D"
if /I "%SAMPLE%"=="Hello2D" set "PROJECT=samples\Hello2D\Hello2D.csproj"
if /I "%SAMPLE%"=="Hello3D" set "PROJECT=samples\Hello3D\Hello3D.csproj"
if /I "%SAMPLE%"=="InputDemo" set "PROJECT=samples\InputDemo\InputDemo.csproj"
if /I "%SAMPLE%"=="ModelViewer" set "PROJECT=samples\ModelViewer\ModelViewer.csproj"
if not defined PROJECT (
  echo [FAIL] Unknown sample "%SAMPLE%". Use Hello2D, Hello3D, InputDemo, or ModelViewer.
  exit /b 2
)
echo MM ENGINE v0.1-alpha - building %SAMPLE%
"%PSHOST%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\scripts\build-ps5.ps1" -ProjectPath "%CD%\%PROJECT%" -Output Package
if errorlevel 1 (
  echo [FAIL] Build failed. Send the complete artifacts\logs transcript.
  exit /b 1
)
echo [OK] Output is beside the sample in its out folder.
exit /b 0
