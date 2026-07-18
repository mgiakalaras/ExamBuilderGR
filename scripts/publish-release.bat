@echo off
setlocal
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\publish-release.ps1"
if errorlevel 1 (
  echo.
  echo Η δημιουργία του release απέτυχε.
  pause
  exit /b 1
)
echo.
echo Η δημιουργία του release ολοκληρώθηκε.
pause
