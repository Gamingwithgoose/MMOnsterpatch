@echo off
cd /d "%~dp0"
REM Backward-compatible launcher. Preferred launcher is Start-WorldServer.bat.
set "WORLD_CONFIG=%~dp0configs\worldserver.ini"
set "MMONSTERPATCH_WORLD_CONFIG=%WORLD_CONFIG%"
python mmonsterpatch_server.py --config "%WORLD_CONFIG%"
pause
