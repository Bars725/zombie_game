@echo off
rem ------------------------------------------------------------------
rem Sets UNITY_EXE and UNITY_VERSION for the other tool scripts.
rem 1. Reads the editor version from Zombie_game\ProjectSettings\ProjectVersion.txt
rem 2. Looks for <root>\<version>\Editor\Unity.exe in:
rem      - Unity Hub's custom install folder (%APPDATA%\UnityHub\secondaryInstallPath.json)
rem      - D:\Unity
rem      - %ProgramFiles%\Unity\Hub\Editor
rem Override: set UNITY_EXE=X:\path\to\Unity.exe before calling.
rem Usage (from another script): call "%~dp0find_unity.bat" || exit /b 1
rem ------------------------------------------------------------------

if defined UNITY_EXE if exist "%UNITY_EXE%" (
    echo [find_unity] Using UNITY_EXE override
    exit /b 0
)

set "PV_FILE=%~dp0..\Zombie_game\ProjectSettings\ProjectVersion.txt"
if not exist "%PV_FILE%" (
    echo [find_unity] ERROR: ProjectVersion.txt not found. Create the Unity project in Zombie_game\ first.
    exit /b 1
)

set "UNITY_VERSION="
for /f "tokens=2" %%V in ('findstr /b /c:"m_EditorVersion:" "%PV_FILE%"') do set "UNITY_VERSION=%%V"
if not defined UNITY_VERSION (
    echo [find_unity] ERROR: could not read m_EditorVersion from ProjectVersion.txt
    exit /b 1
)

rem Unity Hub stores a custom install folder as a JSON string, e.g. "D:\\Unity"
set "HUB_ROOT="
if exist "%APPDATA%\UnityHub\secondaryInstallPath.json" (
    for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "try { Get-Content -Raw (Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json') | ConvertFrom-Json } catch { }"`) do set "HUB_ROOT=%%P"
)

set "UNITY_EXE="
if defined HUB_ROOT if exist "%HUB_ROOT%\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%HUB_ROOT%\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE if exist "D:\Unity\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=D:\Unity\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE if exist "%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"

if not defined UNITY_EXE (
    echo [find_unity] ERROR: Unity %UNITY_VERSION% not found. Install it via Unity Hub,
    echo [find_unity] or set UNITY_EXE to the full path of Unity.exe
    exit /b 1
)

echo [find_unity] Unity %UNITY_VERSION%: %UNITY_EXE%
exit /b 0
