@echo off
rem ------------------------------------------------------------------
rem One-time script: puts the Unity project under the root Git repo.
rem   1. Moves the empty nested repo Zombie_game\.git and Zombie_game\.gitattributes
rem      (created by Unity Hub) into _to_delete\ - nothing is deleted.
rem   2. git lfs install, git add -A
rem   3. Safety checks: aborts if Library/Temp/.csproj/.sln got staged,
rem      or if Zombie_game is still a nested repo.
rem   4. Commits and pushes to origin/main.
rem Log: _out\Logs\git_first_commit.log
rem Run it with the Unity Editor CLOSED.
rem ------------------------------------------------------------------
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
cd /d "%ROOT%" || goto :fail
if not exist "_out\Logs" mkdir "_out\Logs"
set "LOG=%ROOT%\_out\Logs\git_first_commit.log"
echo === git_first_commit %DATE% %TIME% === > "%LOG%"

rem --- 1. Move Hub's nested repo out of the way ---
if not exist "_to_delete" mkdir "_to_delete"
if exist "Zombie_game\.git" (
    echo Moving nested Zombie_game\.git to _to_delete\ ...
    move "Zombie_game\.git" "_to_delete\Zombie_game.git" >> "%LOG%" 2>&1 || goto :fail
)
if exist "Zombie_game\.gitattributes" (
    move "Zombie_game\.gitattributes" "_to_delete\Zombie_game.gitattributes" >> "%LOG%" 2>&1 || goto :fail
)

rem --- 2. Stage ---
git lfs install >> "%LOG%" 2>&1 || goto :fail
git add -A >> "%LOG%" 2>&1 || goto :fail

rem --- 3. Safety checks ---
git diff --cached --name-only > "_out\Logs\staged_files.txt"
findstr /i /r /c:"/Library/" /c:"/Temp/" /c:"/UserSettings/" /c:"\.csproj$" /c:"\.sln$" "_out\Logs\staged_files.txt" > nul
if not errorlevel 1 (
    echo ERROR: generated Unity files got staged. See _out\Logs\staged_files.txt
    echo ERROR: generated Unity files got staged >> "%LOG%"
    git reset -q
    goto :fail
)
git ls-files -s Zombie_game | findstr /b "160000" > nul
if not errorlevel 1 (
    echo ERROR: Zombie_game is still staged as a nested repository.
    echo ERROR: nested repo gitlink >> "%LOG%"
    git reset -q
    goto :fail
)

for /f %%C in ('find /c /v "" ^< "_out\Logs\staged_files.txt"') do set "COUNT=%%C"
echo Staged files: %COUNT%
git status --short >> "%LOG%" 2>&1

rem --- 4. Commit and push ---
git commit -m "Add Unity 6.3 LTS URP project (Zombie_game)" >> "%LOG%" 2>&1 || goto :fail
git push -u origin main >> "%LOG%" 2>&1 || goto :pushfail

echo.
echo DONE: committed and pushed. Log: %LOG%
echo You can delete the _to_delete folder.
echo DONE >> "%LOG%"
pause
exit /b 0

:pushfail
echo.
echo Commit created, but PUSH FAILED (sign-in to GitHub needed?). Run: git push -u origin main
echo PUSH FAILED >> "%LOG%"
pause
exit /b 2

:fail
echo.
echo FAILED. See %LOG%
echo FAILED >> "%LOG%"
pause
exit /b 1
