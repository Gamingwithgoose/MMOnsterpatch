@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "SOURCE_DIR=%~dp0"
set "PACKAGE_ROOT=%~dp0.."
set "PATCHER_NAME=MMOnsterpatchOfficialServerPatcher.dll"
set "PATCHER_PDB=MMOnsterpatchOfficialServerPatcher.pdb"

if "%MONSTERPATCH_LIB_DIR%"=="" (
  echo MONSTERPATCH_LIB_DIR is not set. The project will try ..\lib, ..\..\lib, and C:\Monsterpatch_Mods\lib.
) else (
  echo Using MONSTERPATCH_LIB_DIR=%MONSTERPATCH_LIB_DIR%
)

echo.
echo Building client patcher...
dotnet build MMOnsterpatchAIOPatcher.csproj -c Release
if errorlevel 1 (
  echo.
  echo Build failed. Make sure MONSTERPATCH_LIB_DIR points to a folder containing Assembly-CSharp.dll, BepInEx.dll, 0Harmony.dll, Mono.Cecil.dll, UnityEngine*.dll, and Unity.TextMeshPro.dll.
  exit /b 1
)

set "BUILT_DLL="
for /f "delims=" %%F in ('dir /b /s "%SOURCE_DIR%bin\Release\%PATCHER_NAME%" 2^>nul') do (
  set "BUILT_DLL=%%F"
)

if not defined BUILT_DLL (
  echo.
  echo ERROR: dotnet build reported success, but %PATCHER_NAME% was not found under:
  echo   %SOURCE_DIR%bin\Release
  echo.
  echo DLLs found under Source\bin\Release:
  dir /b /s "%SOURCE_DIR%bin\Release\*.dll" 2>nul
  exit /b 1
)

set "BUILT_PDB=!BUILT_DLL:.dll=.pdb!"
echo Built patcher found:
echo   !BUILT_DLL!

cd /d "%PACKAGE_ROOT%"
if not exist "patchers" mkdir "patchers"
copy /Y "!BUILT_DLL!" "patchers\%PATCHER_NAME%" >nul
if errorlevel 1 (
  echo ERROR: Failed to copy patcher to package patchers folder.
  exit /b 1
)
if exist "!BUILT_PDB!" copy /Y "!BUILT_PDB!" "patchers\%PATCHER_PDB%" >nul 2>nul

if not exist "patchers\%PATCHER_NAME%" (
  echo ERROR: Package patchers copy is missing after copy.
  exit /b 1
)

echo Copied patcher to:
echo   %PACKAGE_ROOT%\patchers\%PATCHER_NAME%

if exist "Launcher" (
  if not exist "Launcher\Payload\BepInEx\patchers" mkdir "Launcher\Payload\BepInEx\patchers"
  copy /Y "!BUILT_DLL!" "Launcher\Payload\BepInEx\patchers\%PATCHER_NAME%" >nul
  if errorlevel 1 (
    echo ERROR: Failed to copy patcher to Launcher payload patchers folder.
    exit /b 1
  )
  if exist "!BUILT_PDB!" copy /Y "!BUILT_PDB!" "Launcher\Payload\BepInEx\patchers\%PATCHER_PDB%" >nul 2>nul

  if not exist "Launcher\Payload\BepInEx\patchers\%PATCHER_NAME%" (
    echo ERROR: Launcher payload patcher is missing after copy.
    exit /b 1
  )

  echo Copied patcher to:
  echo   %PACKAGE_ROOT%\Launcher\Payload\BepInEx\patchers\%PATCHER_NAME%
)

echo.
echo Build complete.
endlocal
