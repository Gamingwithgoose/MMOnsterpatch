@echo off
cd /d "%~dp0"

REM MMOnsterpatch combined server with Social/Guild support.
REM Public setup expected:
REM   mon.gamingwithgoose.com DNS-only + router TCP 61526 -> this server Trading Post/GTS
REM   mmo.gamingwithgoose.com DNS-only + router TCP 61528 -> this server MMO multiplayer
REM   mmo.gamingwithgoose.com DNS-only + router TCP 61529 -> this server Social chat/guilds
REM   mon-auth.gamingwithgoose.com Cloudflare Tunnel -> http://this-server:61527

set MMONSTERPATCH_HOST=0.0.0.0
set MMO_PORT=61528
set MMO_SNAPSHOT_HZ=30
set SOCIAL_PORT=61529
set SOCIAL_DB=%~dp0data\social.db

set PBO_PORT=61526
set PBO_OPENID_HTTP_HOST=0.0.0.0
set PBO_OPENID_HTTP_PORT=61527
set PBO_OPENID_PUBLIC_BASE_URL=https://mon-auth.gamingwithgoose.com
set PBO_OPENID_REALM=https://mon-auth.gamingwithgoose.com

REM Optional: set this in Windows environment variables or uncomment this line.
REM Any of these names now work: STEAM_WEB_API_KEY, PBO_STEAM_WEB_API_KEY, PBO_STEAM_API_KEY, STEAM_API_KEY.
REM set STEAM_WEB_API_KEY=PUT_YOUR_STEAM_WEB_API_KEY_HERE

python mmonsterpatch_server.py
pause
