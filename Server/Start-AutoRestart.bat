@echo off
cd /d "%~dp0"
:restart
echo [%date% %time%] Starting MMOnsterpatch Official World Server...
python mmonsterpatch_server.py --config "%~dp0configs\worldserver.ini"
echo.
echo [%date% %time%] Server stopped or crashed. Restarting in 10 seconds...
timeout /t 10 /nobreak
cls
goto restart
