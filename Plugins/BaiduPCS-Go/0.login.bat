@echo off
setlocal enabledelayedexpansion

if not exist "cookies.txt" (
    echo Error: cookies.txt file not found
    exit /b 1
)

for /f "delims=" %%i in (cookies.txt) do set COOKIES=%%i

if "!COOKIES!"=="" (
    echo Error: cookies.txt file is empty
    exit /b 1
)

BaiduPCS-Go.exe login -cookies="!COOKIES!"

endlocal
