import argparse
import os
import sys
import threading
import time

import mmonsterpatch_mmo_core as mmo
import mmonsterpatch_tradingpost_core as trading
import mmonsterpatch_social_core as social


def env_int(name, default):
    try:
        return int(os.environ.get(name, str(default)))
    except Exception:
        return default


def env_float(name, default):
    try:
        return float(os.environ.get(name, str(default)))
    except Exception:
        return default


def first_env_value(*names):
    for name in names:
        value = os.environ.get(name, "")
        if value and value.strip():
            return value.strip()
    return ""


def serve_tradingpost(host, gts_port, openid_host, openid_port, openid_public_base_url, openid_realm, steam_web_api_key):
    trading.HOST = host
    trading.PORT = int(gts_port)
    trading.OPENID_HTTP_HOST = openid_host
    trading.OPENID_HTTP_PORT = int(openid_port)
    trading.OPENID_PUBLIC_BASE_URL = (openid_public_base_url or f"http://127.0.0.1:{openid_port}").rstrip("/")
    trading.OPENID_REALM = (openid_realm or trading.OPENID_PUBLIC_BASE_URL).rstrip("/")
    if steam_web_api_key:
        trading.STEAM_WEB_API_KEY = steam_web_api_key.strip()

    trading.log("MMOnsterpatch Trading Post/GTS starting")
    trading.log(f"Trading Post socket listening on {trading.HOST}:{trading.PORT}")
    trading.log(f"Steam OpenID callback listening on {trading.OPENID_HTTP_HOST}:{trading.OPENID_HTTP_PORT}")
    trading.log(f"Steam OpenID public base URL: {trading.OPENID_PUBLIC_BASE_URL}")
    trading.log(f"Steam Web API key configured for display names: {'yes' if trading.STEAM_WEB_API_KEY else 'no'}")

    trading.start_openid_http_server()
    with trading.ThreadedTCPServer((trading.HOST, trading.PORT), trading.BankHandler) as server:
        server.serve_forever()


def serve_social(host, social_port, db_path):
    social.init_db(db_path)
    print(f"[Social] Chat/Guild socket listening on {host}:{social_port}")
    print(f"[Social] Database: {db_path}")
    with social.ThreadedTCPServer((host, int(social_port)), social.SocialHandler) as server:
        server.serve_forever()


def serve_mmo(host, mmo_port, snapshot_hz):
    print(f"[MMO] Binding multiplayer socket on {host}:{mmo_port}...")
    try:
        with mmo.ThreadedTCPServer((host, int(mmo_port)), mmo.Handler) as server:
            print(f"[MMO] Multiplayer socket listening on {host}:{mmo_port}")
            threading.Thread(target=mmo.snapshot_loop, args=(float(snapshot_hz),), name="MMOSnapshotLoop", daemon=True).start()
            server.serve_forever()
    except PermissionError:
        print("")
        print("[MMO] FAILED TO BIND MULTIPLAYER PORT")
        print(f"[MMO] Host/port: {host}:{mmo_port}")
        print("[MMO] Keeping your configured port. This usually means Windows/security software denied Python access")
        print("[MMO] or another old server/python process is still holding/reserving the port.")
        print(f"[MMO] Check listeners: netstat -ano | findstr :{mmo_port}")
        print("[MMO] Check excluded ports: netsh interface ipv4 show excludedportrange protocol=tcp")
        print("[MMO] Also try closing old server windows or: taskkill /IM python.exe /F")
        raise


def main():
    parser = argparse.ArgumentParser(description="MMOnsterpatch combined MMO + Trading Post + Social server")
    parser.add_argument("--host", default=os.environ.get("MMONSTERPATCH_HOST", os.environ.get("PBO_HOST", "0.0.0.0")))
    parser.add_argument("--mmo-port", type=int, default=env_int("MMO_PORT", 61528))
    parser.add_argument("--gts-port", type=int, default=env_int("PBO_PORT", 61526))
    parser.add_argument("--openid-host", default=os.environ.get("PBO_OPENID_HTTP_HOST", "0.0.0.0"))
    parser.add_argument("--openid-port", type=int, default=env_int("PBO_OPENID_HTTP_PORT", 61527))
    parser.add_argument("--openid-public-base-url", default=os.environ.get("PBO_OPENID_PUBLIC_BASE_URL", "https://mon-auth.gamingwithgoose.com"))
    parser.add_argument("--openid-realm", default=os.environ.get("PBO_OPENID_REALM", ""))
    parser.add_argument("--steam-web-api-key", default=first_env_value("STEAM_WEB_API_KEY", "PBO_STEAM_WEB_API_KEY", "PBO_STEAM_API_KEY", "STEAM_API_KEY"))
    parser.add_argument("--social-port", type=int, default=env_int("SOCIAL_PORT", 61529))
    parser.add_argument("--social-db", default=os.environ.get("SOCIAL_DB", os.path.join(os.path.dirname(__file__), "data", "social.db")))
    parser.add_argument("--snapshot-hz", type=float, default=env_float("MMO_SNAPSHOT_HZ", 30.0))
    args = parser.parse_args()

    print("MMOnsterpatch Combined Server v1.1.1-social-merge")
    print("One process serving:")
    print(f"  MMO multiplayer TCP:       {args.host}:{args.mmo_port}")
    print(f"  Social chat/guild TCP:     {args.host}:{args.social_port}")
    print(f"  Trading Post/GTS TCP:      {args.host}:{args.gts_port}")
    print(f"  Steam OpenID HTTP:         {args.openid_host}:{args.openid_port}")
    print(f"  Steam OpenID public URL:   {args.openid_public_base_url}")
    print(f"  Steam display names:       {'enabled' if args.steam_web_api_key else 'fallback only - no Steam Web API key'}")
    print(f"  Social database:           {args.social_db}")
    print("Ctrl+C to stop.")

    t = threading.Thread(
        target=serve_tradingpost,
        args=(args.host, args.gts_port, args.openid_host, args.openid_port, args.openid_public_base_url, args.openid_realm, args.steam_web_api_key),
        name="TradingPostServer",
        daemon=True,
    )
    t.start()

    sthread = threading.Thread(
        target=serve_social,
        args=(args.host, args.social_port, args.social_db),
        name="SocialServer",
        daemon=True,
    )
    sthread.start()

    try:
        serve_mmo(args.host, args.mmo_port, args.snapshot_hz)
    except KeyboardInterrupt:
        print("\nMMOnsterpatch server stopping.")


if __name__ == "__main__":
    main()
