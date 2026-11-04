@echo off
rem ------------------------------------------------------------------
rem Commits everything and pushes to origin.
rem Message: _out\commit_msg.txt (first line = subject). Claude writes it for each step.
rem Safety: aborts if Unity-generated files (Library, Temp, .csproj...) get staged.
rem Log: _out\Logs\commit.log
rem ------------------------------------------------------------------
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
cd /d "%ROOT%" || goto :fail
if not exist "_out\Logs" mkdir "_out\Logs"
set "LOG=%ROOT%\_out\Logs\commit.log"
set "MSG=%ROOT%\_out\commit_msg.txt"
echo === commit %DATE% %TIME% === > "%LOG%"

if not exist "%MSG%" (
    echo ERROR: %MSG% not found. Nothing to commit with.
    echo ERROR: no commit message file >> "%LOG%"
    goto :fail
)

git add -A >> "%LOG%" 2>&1 || goto :fail

git diff --cached --name-only > "_out\Logs\staged_files.txt"
findstr /i /r /c:"/Library/" /c:"/Temp/" /c:"/UserSettings/" /c:"/Logs/" /c:"\.csproj$" /c:"\.sln$" "_out\Logs\staged_files.txt" > nul
if not errorlevel 1 (
    echo ERROR: generated Unity files got staged. See _out\Logs\staged_files.txt
    echo ERROR: generated files staged >> "%LOG%"
    git reset -q
    goto :fail
)

for /f %%C in ('find /c /v "" ^< "_out\Logs\staged_files.txt"') do set "COUNT=%%C"
if "%COUNT%"=="0" (
    echo Nothing to commit.
    echo NOTHING >> "%LOG%"
    pause
    exit /b 0
)
echo Staged files: %COUNT%
git status --short >> "%LOG%" 2>&1

git commit -F "%MSG%" >> "%LOG%" 2>&1 || goto :fail
move /y "%MSG%" "%ROOT%\_out\commit_msg.last.txt" > nul
git push >> "%LOG%" 2>&1 || goto :pushfail

echo.
echo DONE: committed and pushed. Log: %LOG%
echo DONE >> "%LOG%"
pause
exit /b 0

:pushfail
echo.
echo Commit created, but PUSH FAILED. Run: git push
echo PUSH FAILED >> "%LOG%"
pause
exit /b 2

:fail
echo.
echo FAILED. See %LOG%
echo FAILED >> "%LOG%"
pause
exit /b 1
