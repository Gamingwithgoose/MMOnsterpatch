@echo off
setlocal
cd /d "%~dp0"
if exist "%~dp0Start-ServerUI-Hidden.vbs" (
  wscript.exe "%~dp0Start-ServerUI-Hidden.vbs"
) else (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0MMOnsterpatchServerUI.ps1"
)
endlocal
