@echo off
setlocal
cd /d "%~dp0\.."
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\build-installer.ps1" %*
if errorlevel 1 (
  echo.
  echo Installer build failed.
  pause
  exit /b 1
)
echo.
echo Installer build completed.
pause
