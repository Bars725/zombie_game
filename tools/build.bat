@echo off
rem ------------------------------------------------------------------
rem Builds the Windows player in batchmode.
rem Usage:  tools\build.bat [dev]        (dev = Development build)
rem Output: _out\Builds\Windows\ZombieGame.exe, log in _out\Logs\build.log
rem NOTE:   close the Unity Editor first.
rem ------------------------------------------------------------------
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\Zombie_game"
set "OUT=%ROOT%\_out"

call "%~dp0find_unity.bat" || exit /b 1
if not exist "%OUT%\Logs" mkdir "%OUT%\Logs"

set "DEV="
if /i "%~1"=="dev" set "DEV=-developmentBuild"

set "LOG=%OUT%\Logs\build.log"
set "EXE=%OUT%\Builds\Windows\ZombieGame.exe"
echo [build] Building... (log: %LOG%)
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Game.EditorTools.BuildScript.BuildWindows -buildOutput "%EXE%" %DEV% -logFile "%LOG%"
set "CODE=%ERRORLEVEL%"

if "%CODE%"=="0" (
    echo [build] SUCCEEDED: %EXE%
) else (
    echo [build] FAILED, exit code %CODE%. See log: %LOG%
)
endlocal & exit /b %CODE%
