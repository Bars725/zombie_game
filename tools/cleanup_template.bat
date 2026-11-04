@echo off
rem ------------------------------------------------------------------
rem One-time: moves the URP template demo content out of Assets into _to_delete\template\
rem (TutorialInfo folder and Readme.asset). Nothing is deleted.
rem Run with the Unity Editor CLOSED.
rem ------------------------------------------------------------------
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "A=%ROOT%\Zombie_game\Assets"
set "DEST=%ROOT%\_to_delete\template"
if not exist "%DEST%" mkdir "%DEST%"

for %%F in (TutorialInfo TutorialInfo.meta Readme.asset Readme.asset.meta) do (
    if exist "%A%\%%F" (
        move "%A%\%%F" "%DEST%\" > nul && echo Moved %%F
    )
)
echo Done. You can delete %DEST% later.
pause
