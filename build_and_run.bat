@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Δεν βρέθηκε το .NET SDK. Εγκατέστησε το .NET 8 SDK ή το workload .NET desktop development από το Visual Studio Installer.
  pause
  exit /b 1
)
dotnet restore ExamBuilderGR.sln
if errorlevel 1 goto :failed
dotnet build ExamBuilderGR.sln -c Debug
if errorlevel 1 goto :failed
dotnet run --project ExamBuilderGR\ExamBuilderGR.csproj
exit /b 0
:failed
echo.
echo Η μεταγλώττιση απέτυχε. Δες τα μηνύματα παραπάνω.
pause
exit /b 1
