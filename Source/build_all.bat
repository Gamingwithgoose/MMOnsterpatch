@echo off
setlocal
cd /d "%~dp0"
if "%MONSTERPATCH_LIB_DIR%"=="" (
  echo MONSTERPATCH_LIB_DIR is not set. The project will try ..\lib, ..\..\lib, and C:\Monsterpatch_Mods\lib.
) else (
  echo Using MONSTERPATCH_LIB_DIR=%MONSTERPATCH_LIB_DIR%
)
dotnet build MMOnsterpatchAIOPatcher.csproj -c Release
if errorlevel 1 (
  echo.
  echo Build failed. Make sure MONSTERPATCH_LIB_DIR points to a folder containing Assembly-CSharp.dll, BepInEx.dll, 0Harmony.dll, Mono.Cecil.dll, UnityEngine*.dll, and Unity.TextMeshPro.dll.
  exit /b 1
)
cd /d "%~dp0.."
if not exist patchers mkdir patchers
copy /Y "Source\bin\Release\net472\MMOnsterpatchOfficialServerPatcher.dll" "patchers\MMOnsterpatchOfficialServerPatcher.dll" >nul
copy /Y "Source\bin\Release\net472\MMOnsterpatchOfficialServerPatcher.pdb" "patchers\MMOnsterpatchOfficialServerPatcher.pdb" >nul 2>nul
echo.
echo Built patchers\MMOnsterpatchOfficialServerPatcher.dll
endlocal
