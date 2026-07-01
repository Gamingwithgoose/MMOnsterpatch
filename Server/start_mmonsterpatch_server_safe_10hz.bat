@echo off
cd /d "%~dp0"
REM Backward-compatible safe launcher. This override does not edit configs/worldserver.ini.
set "WORLD_CONFIG=%~dp0configs\worldserver.ini"
set "MMONSTERPATCH_WORLD_CONFIG=%WORLD_CONFIG%"
set MMO_SNAPSHOT_HZ=10
python mmonsterpatch_server.py --config "%WORLD_CONFIG%"
pause
