@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo MMOnsterpatch full build

echo.
echo [1/2] Building client patcher and updating Launcher payload...
call "%~dp0Source\build_all.bat"
if errorlevel 1 (
  echo.
  echo Client patcher build failed.
  pause
  exit /b 1
)

echo.
echo [2/2] Building/publishing launcher with payload...
call "%~dp0Launcher\build_launcher.bat"
if errorlevel 1 (
  echo.
  echo Launcher build failed.
  pause
  exit /b 1
)

echo.
echo Full build complete.
echo Run the launcher from:
echo   %~dp0Launcher\bin\Release\net8.0-windows\win-x64\publish\MMOnsterpatchLauncher.exe
pause
endlocal
