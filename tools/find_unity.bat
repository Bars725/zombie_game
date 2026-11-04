@echo off
rem ------------------------------------------------------------------
rem Sets UNITY_EXE and UNITY_VERSION for the other tool scripts.
rem Reads the editor version from ProjectBreach\ProjectSettings\ProjectVersion.txt
rem and looks for it in the default Unity Hub install folder.
rem Override: set UNITY_EXE=D:\path\to\Unity.exe before calling.
rem Usage (from another script): call "%~dp0find_unity.bat" || exit /b 1
rem ------------------------------------------------------------------

if defined UNITY_EXE if exist "%UNITY_EXE%" (
    echo [find_unity] Using UNITY_EXE override
    exit /b 0
)

set "PV_FILE=%~dp0..\ProjectBreach\ProjectSettings\ProjectVersion.txt"
if not exist "%PV_FILE%" (
    echo [find_unity] ERROR: ProjectVersion.txt not found. Create the Unity project in ProjectBreach\ first.
    exit /b 1
)

set "UNITY_VERSION="
for /f "tokens=2" %%V in ('findstr /b /c:"m_EditorVersion:" "%PV_FILE%"') do set "UNITY_VERSION=%%V"
if not defined UNITY_VERSION (
    echo [find_unity] ERROR: could not read m_EditorVersion from ProjectVersion.txt
    exit /b 1
)

set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
    echo [find_unity] ERROR: Unity %UNITY_VERSION% not found at "%UNITY_EXE%"
    echo [find_unity] Install it via Unity Hub, or set UNITY_EXE to the full path of Unity.exe
    exit /b 1
)

echo [find_unity] Unity %UNITY_VERSION%
exit /b 0
