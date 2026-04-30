@echo off
setlocal EnableExtensions

REM IMPORTANT:
REM - Save this file as ANSI (GBK) if you use Chinese file names.
REM - Keep commands/comments ASCII to avoid cmd parser issues.
chcp 65001 >nul
call :log UTF-8 console ready (65001).

REM ===== configurable =====
set "PROC_NAME="
set "EXE_PATH="
set "INTERVAL_SEC=5"
set "DEBUG=1"
set "RESTART_COUNT=0"
REM ========================

REM If EXE_PATH is empty, auto-detect the first .exe in current folder.
if "%EXE_PATH%"=="" (
    for %%I in ("%~dp0*.exe") do (
        if /I not "%%~nxI"=="%~nx0" (
            set "EXE_PATH=%%~fI"
            goto :exe_ready
        )
    )
)

:exe_ready
if "%EXE_PATH%"=="" (
    call :log ERROR: EXE_PATH is empty and no exe found.
    pause
    exit /b 1
)

if not exist "%EXE_PATH%" (
    call :log ERROR: EXE_PATH not found.
    call :log EXE_PATH=%EXE_PATH%
    pause
    exit /b 1
)

for %%I in ("%EXE_PATH%") do set "EXE_DIR=%%~dpI"

REM If PROC_NAME is empty, use file name from EXE_PATH.
if "%PROC_NAME%"=="" (
    for %%I in ("%EXE_PATH%") do set "PROC_NAME=%%~nxI"
)

call :log Watchdog start.
call :log PROC_NAME=%PROC_NAME%
call :log EXE_PATH=%EXE_PATH%
call :log EXE_DIR=%EXE_DIR%
call :log INTERVAL_SEC=%INTERVAL_SEC%
call :log DEBUG=%DEBUG%
call :log RESTART_COUNT=%RESTART_COUNT%

call :check_process
if errorlevel 1 (
    call :log Process not found at startup, launching now.
    call :start_process
) else (
    call :log Process already running, enter loop.
)

:watch_loop
timeout /t %INTERVAL_SEC% /nobreak >nul
call :check_process
if errorlevel 1 (
    set /a RESTART_COUNT+=1
    call :log Process missing, restarting.
    call :log RestartCount=%RESTART_COUNT%
    call :start_process
) else (
    call :log Process alive.
    call :log RestartCount=%RESTART_COUNT%
)
goto watch_loop

:check_process
REM Check presence only. No "Not Responding" check.
set "TMP_TASK=%TEMP%\watchdog_task_%RANDOM%.txt"
set "TASK_MATCH=0"
set "PS_COUNT=0"

tasklist /FI "IMAGENAME eq %PROC_NAME%" /NH > "%TMP_TASK%" 2>nul
findstr /I /C:"%PROC_NAME%" "%TMP_TASK%" >nul
if not errorlevel 1 set "TASK_MATCH=1"

if "%DEBUG%"=="1" (
    call :log [DEBUG] check by tasklist+findstr
    for /f "usebackq delims=" %%L in ("%TMP_TASK%") do call :log [DEBUG] TASKLIST: %%L
    call :log [DEBUG] TASK_MATCH=%TASK_MATCH%
)

if "%TASK_MATCH%"=="1" (
    del /q "%TMP_TASK%" >nul 2>nul
    exit /b 0
)

REM Fallback: detect by executable full path via PowerShell.
REM This avoids false negatives when tasklist/findstr fails on non-ASCII process names.
for /f %%P in ('powershell -NoProfile -Command "$p=$env:EXE_PATH; $c=(Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path -ieq $p }).Count; Write-Output $c" 2^>nul') do set "PS_COUNT=%%P"

if "%DEBUG%"=="1" (
    call :log [DEBUG] check by powershell path
    call :log [DEBUG] PS_COUNT=%PS_COUNT%
)

del /q "%TMP_TASK%" >nul 2>nul
if "%PS_COUNT%"=="0" (
    exit /b 1
)
if "%PS_COUNT%"=="" (
    exit /b 1
)
if "%DEBUG%"=="1" call :log [DEBUG] final decision: process exists
if not "%PS_COUNT%"=="0" (
    exit /b 0
)
if "%DEBUG%"=="1" call :log [DEBUG] final decision: process missing
exit /b 1

:start_process
call :log Launching process...
start "" /D "%EXE_DIR%" "%EXE_PATH%"
if errorlevel 1 (
    call :log WARN: start failed, verify path/permission.
) else (
    call :log Launch command sent.
)
timeout /t 2 /nobreak >nul
call :check_process
if errorlevel 1 (
    call :log WARN: process still missing 2s after launch.
) else (
    call :log Launch confirmed by process list.
)
exit /b 0

:log
echo [%date% %time:~0,8%] %*
exit /b 0
