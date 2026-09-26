@echo off
cd /d "%~dp0"
g++ updater.cpp -o updater.exe -std=c++11 -static -mwindows -lshell32 ^
    -finput-charset=UTF-8 -fexec-charset=UTF-8 -fwide-exec-charset=UTF-16LE
if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)
echo BUILD OK: updater.exe