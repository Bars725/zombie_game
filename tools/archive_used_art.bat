@echo off
setlocal
set "SCRIPT=%~dp0archive_used_art.ps1"

if not exist "%SCRIPT%" (
    echo ERROR: helper script not found: %SCRIPT%
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "CODE=%ERRORLEVEL%"
echo.
if "%CODE%"=="0" (
    echo Archive scan completed.
) else (
    echo Archive scan failed with exit code %CODE%.
)
pause
exit /b %CODE%
