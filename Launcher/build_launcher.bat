@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "LAUNCHER_DIR=%CD%"
set "PACKAGE_ROOT=%~dp0.."
set "PATCHER_NAME=MMOnsterpatchOfficialServerPatcher.dll"
set "PATCHER_PDB=MMOnsterpatchOfficialServerPatcher.pdb"
set "PAYLOAD_PATCHERS=%LAUNCHER_DIR%\Payload\BepInEx\patchers"
set "PUBLISH_DIR=%LAUNCHER_DIR%\bin\Release\net8.0-windows\win-x64\publish"

if not exist "%PAYLOAD_PATCHERS%" mkdir "%PAYLOAD_PATCHERS%"

echo MMOnsterpatch Launcher build

echo.
echo Checking for developer Source build...
if exist "%PACKAGE_ROOT%\Source\build_all.bat" (
  echo Source folder found. Building client patcher first...
  call "%PACKAGE_ROOT%\Source\build_all.bat"
  if errorlevel 1 (
    echo.
    echo Source build failed. Launcher payload was not updated.
    pause
    exit /b 1
  )
) else (
  echo No Source folder found. This is a launcher-only package, so the existing Launcher\Payload is used.
)

echo.
echo Locating patcher DLL for launcher payload...
set "PATCHER_SOURCE="
set "PATCHER_DEST=%PAYLOAD_PATCHERS%\%PATCHER_NAME%"

REM Prefer the freshly built Source output first. The payload copy may already exist,
REM but copying a file onto itself makes Windows COPY return a failure.
if exist "%PACKAGE_ROOT%\Source\bin" (
  for /f "delims=" %%F in ('dir /b /s "%PACKAGE_ROOT%\Source\bin\Release\%PATCHER_NAME%" 2^>nul') do set "PATCHER_SOURCE=%%F"
)
if not defined PATCHER_SOURCE if exist "%PACKAGE_ROOT%\Source\bin" (
  for /f "delims=" %%F in ('dir /b /s "%PACKAGE_ROOT%\Source\bin\Debug\%PATCHER_NAME%" 2^>nul') do set "PATCHER_SOURCE=%%F"
)
if not defined PATCHER_SOURCE if exist "%PACKAGE_ROOT%\patchers\%PATCHER_NAME%" set "PATCHER_SOURCE=%PACKAGE_ROOT%\patchers\%PATCHER_NAME%"
if not defined PATCHER_SOURCE if exist "!PATCHER_DEST!" set "PATCHER_SOURCE=!PATCHER_DEST!"

if not defined PATCHER_SOURCE (
  echo.
  echo ERROR: %PATCHER_NAME% was not found anywhere the launcher build can use.
  echo Searched:
  echo   %PACKAGE_ROOT%\Source\bin\Release\%PATCHER_NAME%
  echo   %PACKAGE_ROOT%\Source\bin\Debug\%PATCHER_NAME%
  echo   %PACKAGE_ROOT%\patchers\%PATCHER_NAME%
  echo   !PATCHER_DEST!
  echo.
  echo Online launch would boot vanilla without this DLL, so the launcher build is stopping.
  pause
  exit /b 1
)

echo Patcher source:
echo   !PATCHER_SOURCE!
echo Patcher payload target:
echo   !PATCHER_DEST!

if /I "!PATCHER_SOURCE!"=="!PATCHER_DEST!" (
  echo Patcher is already in Launcher payload; skipping self-copy.
) else (
  copy /Y "!PATCHER_SOURCE!" "!PATCHER_DEST!" >nul
  if errorlevel 1 (
    echo ERROR: Failed to copy patcher into Launcher\Payload\BepInEx\patchers.
    pause
    exit /b 1
  )
)

set "PATCHER_PDB_SOURCE=!PATCHER_SOURCE:.dll=.pdb!"
if exist "!PATCHER_PDB_SOURCE!" (
  if /I not "!PATCHER_PDB_SOURCE!"=="%PAYLOAD_PATCHERS%\%PATCHER_PDB%" copy /Y "!PATCHER_PDB_SOURCE!" "%PAYLOAD_PATCHERS%\%PATCHER_PDB%" >nul 2>nul
)

if not exist "!PATCHER_DEST!" (
  echo ERROR: Launcher payload is still missing %PATCHER_NAME% after copy.
  pause
  exit /b 1
)
if not exist "%LAUNCHER_DIR%\Payload\BepInEx\core\BepInEx.Preloader.dll" (
  echo ERROR: Launcher payload is missing BepInEx\core\BepInEx.Preloader.dll.
  echo The final release folder cannot launch online without the bundled BepInEx runtime.
  pause
  exit /b 1
)

if not exist "%LAUNCHER_DIR%\Payload\Root\winhttp.dll" (
  echo ERROR: Launcher payload is missing Payload\Root\winhttp.dll.
  pause
  exit /b 1
)

if not exist "%LAUNCHER_DIR%\Payload\Root\doorstop_config.ini" (
  echo ERROR: Launcher payload is missing Payload\Root\doorstop_config.ini.
  pause
  exit /b 1
)

echo.
echo Building launcher...
dotnet publish MMOnsterpatchLauncher.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
if errorlevel 1 (
  echo.
  echo Launcher build failed.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%" (
  echo ERROR: Publish directory was not found:
  echo   %PUBLISH_DIR%
  pause
  exit /b 1
)

echo.
echo Force-copying Launcher\Payload into published release folder...
if exist "%PUBLISH_DIR%\Payload" rmdir /s /q "%PUBLISH_DIR%\Payload"
xcopy /E /I /Y "%LAUNCHER_DIR%\Payload" "%PUBLISH_DIR%\Payload" >nul
if errorlevel 2 (
  echo ERROR: Failed to copy Payload into publish folder.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%\Payload\BepInEx\patchers\%PATCHER_NAME%" (
  echo ERROR: Published release folder is missing Payload\BepInEx\patchers\%PATCHER_NAME%.
  echo Online launch would boot vanilla, so the build is failing.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%\Payload\BepInEx\core\BepInEx.Preloader.dll" (
  echo ERROR: Published release folder is missing Payload\BepInEx\core\BepInEx.Preloader.dll.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%\Payload\Root\winhttp.dll" (
  echo ERROR: Published release folder is missing Payload\Root\winhttp.dll.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%\Payload\Root\doorstop_config.ini" (
  echo ERROR: Published release folder is missing Payload\Root\doorstop_config.ini.
  pause
  exit /b 1
)

echo.
echo Built launcher release folder with required payload:
echo   %PUBLISH_DIR%
echo.
echo Verified:
echo   Payload\BepInEx\patchers\%PATCHER_NAME%
echo   Payload\BepInEx\core\BepInEx.Preloader.dll
echo   Payload\Root\winhttp.dll
echo   Payload\Root\doorstop_config.ini
echo.
echo Run:
echo   %PUBLISH_DIR%\MMOnsterpatchLauncher.exe
pause
endlocal
