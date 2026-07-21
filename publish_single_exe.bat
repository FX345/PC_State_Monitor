@echo off
setlocal

cd /d "%~dp0\.."
dotnet publish ".\PcGuardianLite\src\PcGuardianLite.App\PcGuardianLite.App.csproj" /p:PublishProfile=SingleExe

if errorlevel 1 (
    echo.
    echo Publish failed.
    pause
    exit /b 1
)

echo.
echo Single exe created:
echo %CD%\PcGuardianLite\publish-single\PcGuardianLite.exe
pause
