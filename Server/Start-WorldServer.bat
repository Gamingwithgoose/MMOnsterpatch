@echo off
cd /d "%~dp0"

set "WORLD_CONFIG=%~dp0configs\worldserver.ini"
set "MMONSTERPATCH_WORLD_CONFIG=%WORLD_CONFIG%"

echo Starting MMOnsterpatch Official World Server...
echo Config: %WORLD_CONFIG%
if not exist "%WORLD_CONFIG%" (
  echo.
  echo ERROR: worldserver.ini was not found.
  echo Expected: %WORLD_CONFIG%
  echo.
  pause
  exit /b 2
)

echo.
echo Current configured rate lines:
findstr /I /C:"EXP Rate" /C:"SATS Rate" /C:"Shiny Odds Denominator" /C:"Shiny Rate" /C:"Catch Rate" /C:"Item Drop Chance Rate" /C:"Item Drop Rate" /C:"Random Encounter Rate" /C:"Visible Spawn Rate" /C:"Reward Spawn Rate" "%WORLD_CONFIG%"
echo.

python mmonsterpatch_server.py --config "%WORLD_CONFIG%"
pause
