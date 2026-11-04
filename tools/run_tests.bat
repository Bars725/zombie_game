@echo off
rem ------------------------------------------------------------------
rem Runs Unity Test Framework tests in batchmode.
rem Usage:  tools\run_tests.bat [EditMode|PlayMode]     (default: EditMode)
rem Output: _out\TestResults\<Platform>.xml  and  _out\Logs\tests_<Platform>.log
rem NOTE:   close the Unity Editor first - batchmode cannot open a project
rem         that is already open in another editor instance.
rem ------------------------------------------------------------------
setlocal

for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\ProjectBreach"
set "OUT=%ROOT%\_out"

set "PLATFORM=%~1"
if "%PLATFORM%"=="" set "PLATFORM=EditMode"
if /i not "%PLATFORM%"=="EditMode" if /i not "%PLATFORM%"=="PlayMode" (
    echo [run_tests] ERROR: platform must be EditMode or PlayMode, got "%PLATFORM%"
    exit /b 1
)

call "%~dp0find_unity.bat" || exit /b 1

if not exist "%OUT%\TestResults" mkdir "%OUT%\TestResults"
if not exist "%OUT%\Logs" mkdir "%OUT%\Logs"

set "RESULTS=%OUT%\TestResults\%PLATFORM%.xml"
set "LOG=%OUT%\Logs\tests_%PLATFORM%.log"

rem EditMode tests do not need a GPU; PlayMode tests may render, so keep graphics on.
set "GFX="
if /i "%PLATFORM%"=="EditMode" set "GFX=-nographics"

rem Do NOT add -quit together with -runTests: Unity exits by itself after the run.
echo [run_tests] Running %PLATFORM% tests... (log: %LOG%)
"%UNITY_EXE%" -batchmode %GFX% -projectPath "%PROJECT%" -runTests -testPlatform %PLATFORM% -testResults "%RESULTS%" -logFile "%LOG%"
set "CODE=%ERRORLEVEL%"

rem Exit codes as commonly observed: 0 = all passed, 2 = some tests failed,
rem anything else = Unity/run error (compile errors, license, project locked). Check the log.
if "%CODE%"=="0" (
    echo [run_tests] PASSED. Results: %RESULTS%
) else if "%CODE%"=="2" (
    echo [run_tests] TESTS FAILED. Results: %RESULTS%
) else (
    echo [run_tests] UNITY ERROR, exit code %CODE%. See log: %LOG%
)

endlocal & exit /b %CODE%
