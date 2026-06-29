import base64
import hashlib
import html
import json
import math
import os
import re
import secrets
import socketserver
import sqlite3
import threading
import time
import urllib.parse
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from datetime import datetime, timedelta

import mmonsterpatch_moderation_core as moderation

ROOT = os.path.dirname(os.path.abspath(__file__))
DATA_DIR = os.path.join(ROOT, "data")
LOG_DIR = os.path.join(ROOT, "logs")
DB_PATH = os.path.join(DATA_DIR, "bank.db")
HOST = os.environ.get("PBO_HOST", "0.0.0.0")
PORT = int(os.environ.get("PBO_PORT", "61526"))
SLOT_COUNT = 240
GTS_PAGE_SIZE = 30
RA_PAGE_SIZE = 30
RA_MAX_PRICE = 9_999_999

# -----------------------------------------------------------------------------
# Steam OpenID test settings
# -----------------------------------------------------------------------------
# For local testing on the same PC as the server, the default localhost URL works.
# For real players, this must be a public HTTPS URL that points to this server,
# for example: https://gts.yourdomain.com
OPENID_HTTP_HOST = os.environ.get("PBO_OPENID_HTTP_HOST", "0.0.0.0")
OPENID_HTTP_PORT = int(os.environ.get("PBO_OPENID_HTTP_PORT", "25891"))
OPENID_PUBLIC_BASE_URL = os.environ.get("PBO_OPENID_PUBLIC_BASE_URL", f"http://127.0.0.1:{OPENID_HTTP_PORT}").rstrip("/")
OPENID_REALM = os.environ.get("PBO_OPENID_REALM", OPENID_PUBLIC_BASE_URL).rstrip("/")
OPENID_STATE_TTL_SECONDS = int(os.environ.get("PBO_OPENID_STATE_TTL_SECONDS", "300"))
STEAM_OPENID_PROVIDER = "https://steamcommunity.com/openid/login"

# Optional: Steam Web API key for public display names.
# Get one from https://steamcommunity.com/dev/apikey and set it on the server only.
# The key is never sent to the Monsterpatch client.
def first_env_value(*names: str) -> str:
    for name in names:
        value = os.environ.get(name, "")
        if value and value.strip():
            return value.strip()
    return ""

# Optional: Steam Web API key for public display names.
# Accept a few aliases because older launchers/configs used different setting names.
STEAM_WEB_API_KEY = first_env_value("STEAM_WEB_API_KEY", "PBO_STEAM_WEB_API_KEY", "PBO_STEAM_API_KEY", "STEAM_API_KEY")
STEAM_DISPLAY_NAME_LOOKUP = os.environ.get("PBO_STEAM_DISPLAY_NAME_LOOKUP", "1").strip().lower() not in ("0", "false", "no", "off")
STEAM_DISPLAY_NAME_CACHE = {}

# Pending browser auth states live only in memory. If the server restarts, the
# user just starts the Steam login again. Nothing sensitive is stored here.
PENDING_OPENID = {}
PENDING_OPENID_LOCK = threading.Lock()
ACTIVE_BANK_HANDLERS_LOCK = threading.RLock()
ACTIVE_BANK_HANDLERS = set()

os.makedirs(DATA_DIR, exist_ok=True)
os.makedirs(LOG_DIR, exist_ok=True)


def log(msg: str):
    stamp = datetime.now().strftime("[%Y-%m-%d %H:%M:%S]")
    line = f"{stamp} {msg}"
    print(line)
    with open(os.path.join(LOG_DIR, "server.log"), "a", encoding="utf-8") as f:
        f.write(line + "\n")


def utcnow_iso() -> str:
    return datetime.now().isoformat()


def get_db():
    conn = sqlite3.connect(DB_PATH, isolation_level=None)
    conn.execute("PRAGMA journal_mode=WAL")
    conn.execute("PRAGMA foreign_keys=ON")
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS accounts (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            password_salt TEXT NOT NULL,
            password_hash TEXT NOT NULL,
            created_at TEXT NOT NULL,
            last_login_at TEXT
        )
        """
    )
    ensure_account_openid_columns(conn)
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS bank_slots (
            account_id INTEGER NOT NULL,
            slot_index INTEGER NOT NULL,
            species TEXT NOT NULL,
            level INTEGER NOT NULL,
            name_b64 TEXT NOT NULL,
            gender INTEGER NOT NULL,
            shiny INTEGER NOT NULL,
            blob_b64 TEXT NOT NULL,
            PRIMARY KEY(account_id, slot_index),
            FOREIGN KEY(account_id) REFERENCES accounts(id) ON DELETE CASCADE
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS gts_listings (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            owner_account_id INTEGER NOT NULL,
            owner_username TEXT NOT NULL,
            request_species TEXT NOT NULL,
            offered_species TEXT NOT NULL,
            level INTEGER NOT NULL,
            name_b64 TEXT NOT NULL,
            gender INTEGER NOT NULL,
            shiny INTEGER NOT NULL,
            blob_b64 TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'open',
            completed_by_account_id INTEGER,
            completed_by_username TEXT,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            FOREIGN KEY(owner_account_id) REFERENCES accounts(id) ON DELETE CASCADE,
            FOREIGN KEY(completed_by_account_id) REFERENCES accounts(id) ON DELETE SET NULL
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS gts_claims (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            owner_account_id INTEGER NOT NULL,
            source_listing_id INTEGER NOT NULL,
            blob_b64 TEXT NOT NULL,
            claimed INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            claimed_at TEXT,
            FOREIGN KEY(owner_account_id) REFERENCES accounts(id) ON DELETE CASCADE,
            FOREIGN KEY(source_listing_id) REFERENCES gts_listings(id) ON DELETE CASCADE
        )
        """
    )
    conn.execute("CREATE INDEX IF NOT EXISTS idx_gts_listings_status_created ON gts_listings(status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_gts_listings_status_request ON gts_listings(status, request_species, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_gts_listings_owner_status ON gts_listings(owner_account_id, status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_gts_claims_owner_claimed ON gts_claims(owner_account_id, claimed, id)")
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS aio_sessions (
            token TEXT PRIMARY KEY,
            account_id INTEGER NOT NULL,
            steam_id64 TEXT,
            display_name TEXT,
            created_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            revoked INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY(account_id) REFERENCES accounts(id) ON DELETE CASCADE
        )
        """
    )
    conn.execute("CREATE INDEX IF NOT EXISTS idx_aio_sessions_account ON aio_sessions(account_id, revoked, expires_at)")

    # Team Rocket Auctions tables. These are intentionally server-only and are
    # not used by the website code.
    try:
        conn.execute("ALTER TABLE gts_claims ADD COLUMN notified_at TEXT")
    except sqlite3.OperationalError as ex:
        if "duplicate column" not in str(ex).lower():
            raise

    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS rocket_pokemon_listings (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            seller_account_id INTEGER NOT NULL,
            seller_username TEXT NOT NULL,
            price INTEGER NOT NULL,
            species TEXT NOT NULL,
            level INTEGER NOT NULL,
            name_b64 TEXT NOT NULL,
            gender INTEGER NOT NULL,
            shiny INTEGER NOT NULL,
            blob_b64 TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'open',
            buyer_account_id INTEGER,
            buyer_username TEXT,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            FOREIGN KEY(seller_account_id) REFERENCES accounts(id) ON DELETE CASCADE,
            FOREIGN KEY(buyer_account_id) REFERENCES accounts(id) ON DELETE SET NULL
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS rocket_item_listings (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            seller_account_id INTEGER NOT NULL,
            seller_username TEXT NOT NULL,
            item_id TEXT NOT NULL,
            item_name_b64 TEXT NOT NULL,
            quantity INTEGER NOT NULL,
            price INTEGER NOT NULL,
            status TEXT NOT NULL DEFAULT 'open',
            buyer_account_id INTEGER,
            buyer_username TEXT,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            FOREIGN KEY(seller_account_id) REFERENCES accounts(id) ON DELETE CASCADE,
            FOREIGN KEY(buyer_account_id) REFERENCES accounts(id) ON DELETE SET NULL
        )
        """
    )
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS rocket_payouts (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            owner_account_id INTEGER NOT NULL,
            amount INTEGER NOT NULL,
            reason TEXT NOT NULL,
            source_kind TEXT NOT NULL,
            source_listing_id INTEGER NOT NULL,
            claimed INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            claimed_at TEXT,
            FOREIGN KEY(owner_account_id) REFERENCES accounts(id) ON DELETE CASCADE
        )
        """
    )
    conn.execute("CREATE INDEX IF NOT EXISTS idx_ra_pokemon_status_created ON rocket_pokemon_listings(status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_ra_pokemon_seller_status ON rocket_pokemon_listings(seller_account_id, status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_ra_item_status_created ON rocket_item_listings(status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_ra_item_seller_status ON rocket_item_listings(seller_account_id, status, created_at, id)")
    conn.execute("CREATE INDEX IF NOT EXISTS idx_ra_payouts_owner_claimed ON rocket_payouts(owner_account_id, claimed, id)")
    return conn


def hash_password(password: str, salt: str) -> str:
    return hashlib.sha256((salt + password).encode("utf-8")).hexdigest()


def encode_message(msg: str) -> str:
    return base64.b64encode(msg.encode("utf-8")).decode("ascii")


def decode_message(msg: str) -> str:
    return base64.b64decode(msg.encode("ascii")).decode("utf-8")


def ensure_account_openid_columns(conn):
    """Add Steam/OpenID columns to older bank.db files without breaking password accounts."""
    cols = {row[1] for row in conn.execute("PRAGMA table_info(accounts)").fetchall()}
    if "auth_provider" not in cols:
        conn.execute("ALTER TABLE accounts ADD COLUMN auth_provider TEXT NOT NULL DEFAULT 'password'")
    if "steam_id64" not in cols:
        conn.execute("ALTER TABLE accounts ADD COLUMN steam_id64 TEXT")
    if "display_name" not in cols:
        conn.execute("ALTER TABLE accounts ADD COLUMN display_name TEXT")
    # Partial unique index allows many password accounts with NULL steam_id64.
    conn.execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_accounts_steam_id64 ON accounts(steam_id64) WHERE steam_id64 IS NOT NULL")


def cleanup_pending_openid():
    now = time.time()
    with PENDING_OPENID_LOCK:
        stale = [state for state, data in PENDING_OPENID.items() if now - data.get("created", 0) > OPENID_STATE_TTL_SECONDS]
        for state in stale:
            PENDING_OPENID.pop(state, None)


def openid_return_url(state: str) -> str:
    return f"{OPENID_PUBLIC_BASE_URL}/steam/return?state={urllib.parse.quote(state)}"


def build_steam_openid_login_url(state: str) -> str:
    return_to = openid_return_url(state)
    params = {
        "openid.ns": "http://specs.openid.net/auth/2.0",
        "openid.mode": "checkid_setup",
        "openid.return_to": return_to,
        "openid.realm": OPENID_REALM,
        "openid.identity": "http://specs.openid.net/auth/2.0/identifier_select",
        "openid.claimed_id": "http://specs.openid.net/auth/2.0/identifier_select",
    }
    return STEAM_OPENID_PROVIDER + "?" + urllib.parse.urlencode(params)


def validate_steam_openid_response(query_params):
    """Validate Steam OpenID browser callback with Steam's check_authentication step."""
    flat = {}
    for key, values in query_params.items():
        if key.startswith("openid.") and values:
            flat[key] = values[0]

    if flat.get("openid.mode") != "id_res":
        return False, None, "Steam did not return a successful login response."

    claimed_id = flat.get("openid.claimed_id", "")
    identity = flat.get("openid.identity", "")
    if identity and identity != claimed_id:
        return False, None, "Steam OpenID identity mismatch."

    match = re.match(r"^https?://steamcommunity\.com/openid/id/(\d{17})$", claimed_id)
    if not match:
        return False, None, "SteamID was missing from the OpenID response."
    steam_id64 = match.group(1)

    check = dict(flat)
    check["openid.mode"] = "check_authentication"
    data = urllib.parse.urlencode(check).encode("utf-8")
    req = urllib.request.Request(
        STEAM_OPENID_PROVIDER,
        data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded", "User-Agent": "Monsterpatch-GTS-OpenID-Test/0.1"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            body = resp.read().decode("utf-8", errors="replace")
    except Exception as ex:
        return False, None, f"Could not validate Steam login with Steam: {ex}"

    if "is_valid:true" not in body:
        return False, None, "Steam rejected the OpenID response."
    return True, steam_id64, None


def sanitize_display_name(name: str, steam_id64: str) -> str:
    """Keep Steam public persona names safe for the tab-delimited socket protocol."""
    name = (name or "").replace("\t", " ").replace("\r", " ").replace("\n", " ").strip()
    name = " ".join(name.split())
    if not name:
        name = f"SteamUser_{steam_id64[-6:]}"
    return name[:64]


def steam_display_fallback(steam_id64: str) -> str:
    return f"SteamUser_{steam_id64[-6:]}"


def is_steam_display_fallback(name: str, steam_id64: str = "") -> bool:
    name = (name or "").strip()
    if not name:
        return True
    if name.startswith("steam_"):
        return True
    if name.startswith("SteamUser_"):
        return True
    if steam_id64 and name == steam_display_fallback(steam_id64):
        return True
    return False


def fetch_steam_display_name(steam_id64: str) -> str:
    """Fetch the user's public Steam persona name. Fails closed to a non-secret fallback."""
    fallback = steam_display_fallback(steam_id64)
    if not steam_id64:
        return fallback
    cached = STEAM_DISPLAY_NAME_CACHE.get(steam_id64)
    if cached:
        return cached
    if not STEAM_DISPLAY_NAME_LOOKUP:
        log("Steam display name lookup is disabled by PBO_STEAM_DISPLAY_NAME_LOOKUP.")
        return fallback
    if not STEAM_WEB_API_KEY:
        log("Steam display name lookup skipped: no Steam Web API key configured. Set STEAM_WEB_API_KEY or PBO_STEAM_WEB_API_KEY.")
        return fallback
    try:
        params = urllib.parse.urlencode({
            "key": STEAM_WEB_API_KEY,
            "steamids": steam_id64,
        })
        url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?" + params
        req = urllib.request.Request(url, headers={"User-Agent": "Monsterpatch-GTS-Server/0.3"})
        with urllib.request.urlopen(req, timeout=8) as resp:
            raw = resp.read().decode("utf-8", errors="replace")
        data = json.loads(raw)
        players = data.get("response", {}).get("players", [])
        if not players:
            log(f"Steam display name lookup returned no player for {steam_id64}.")
            return fallback
        resolved = sanitize_display_name(players[0].get("personaname", ""), steam_id64)
        if resolved and not is_steam_display_fallback(resolved, steam_id64):
            STEAM_DISPLAY_NAME_CACHE[steam_id64] = resolved
            log(f"Steam display name resolved for {steam_id64}: {resolved}")
        return resolved
    except Exception as ex:
        log(f"Steam display name lookup failed for {steam_id64}: {ex}")
        return fallback


def get_or_create_steam_account(conn, steam_id64: str, display_name: str = None):
    ensure_account_openid_columns(conn)
    resolved_name = sanitize_display_name(display_name or steam_display_fallback(steam_id64), steam_id64)
    row = conn.execute(
        "SELECT id, username, display_name FROM accounts WHERE steam_id64 = ?",
        (steam_id64,),
    ).fetchone()
    if row:
        account_id, username, existing_display_name = row
        # Do not overwrite a real display name with SteamUser_#### when the Web API lookup fails.
        final_display_name = resolved_name
        if is_steam_display_fallback(resolved_name, steam_id64) and existing_display_name and not is_steam_display_fallback(existing_display_name, steam_id64):
            final_display_name = existing_display_name
        conn.execute(
            "UPDATE accounts SET last_login_at = ?, display_name = ? WHERE id = ?",
            (utcnow_iso(), final_display_name, account_id),
        )
        return account_id, username, final_display_name

    # Keep username unique and stable for internal account logic. Display name is separate and can change.
    username = f"steam_{steam_id64}"
    conn.execute(
        """
        INSERT INTO accounts(username, password_salt, password_hash, created_at, last_login_at, auth_provider, steam_id64, display_name)
        VALUES (?, '', 'STEAM_OPENID', ?, ?, 'steam_openid', ?, ?)
        """,
        (username, utcnow_iso(), utcnow_iso(), steam_id64, resolved_name),
    )
    account_id = conn.execute("SELECT id FROM accounts WHERE steam_id64 = ?", (steam_id64,)).fetchone()[0]
    return account_id, username, resolved_name


def resolve_account_display_name(conn, account_id, steam_id64, current_display_name, fallback_username):
    """Return the best display name and lazily backfill SteamUser_#### placeholders."""
    current_display_name = (current_display_name or "").strip()
    if steam_id64 and is_steam_display_fallback(current_display_name, steam_id64):
        resolved = fetch_steam_display_name(steam_id64)
        if resolved and not is_steam_display_fallback(resolved, steam_id64):
            conn.execute("UPDATE accounts SET display_name = ? WHERE id = ?", (resolved, account_id))
            return resolved
    if current_display_name:
        return current_display_name
    return fallback_username or (steam_display_fallback(steam_id64) if steam_id64 else "Unknown")





def revoke_aio_sessions_for_steam(steam_id64: str) -> int:
    steam_id64 = moderation.normalize_steam_id64(steam_id64)
    if not steam_id64:
        return 0
    try:
        conn = get_db()
        try:
            cur = conn.execute(
                """
                UPDATE aio_sessions
                SET revoked = 1
                WHERE steam_id64 = ? OR account_id IN (SELECT id FROM accounts WHERE steam_id64 = ?)
                """,
                (steam_id64, steam_id64),
            )
            return int(cur.rowcount or 0)
        finally:
            conn.close()
    except Exception as ex:
        log(f"Could not revoke AIO sessions for banned SteamID {steam_id64}: {ex}")
        return 0


def disconnect_active_steam_sessions(steam_id64: str, reason: str = "This Steam account is banned from MMOnsterpatch online services.") -> int:
    steam_id64 = moderation.normalize_steam_id64(steam_id64)
    if not steam_id64:
        return 0
    with ACTIVE_BANK_HANDLERS_LOCK:
        targets = [h for h in list(ACTIVE_BANK_HANDLERS) if getattr(h, "steam_id64", "") == steam_id64]
    disconnected = 0
    for h in targets:
        try:
            h.send_error(reason)
        except Exception:
            pass
        try:
            h.request.shutdown(2)
        except Exception:
            pass
        try:
            h.request.close()
        except Exception:
            pass
        disconnected += 1
    return disconnected

def create_aio_session(conn, account_id, steam_id64="", display_name=""):
    if moderation.get_active_ban(steam_id64=steam_id64):
        raise RuntimeError("This Steam account is banned from MMOnsterpatch online services.")
    token = secrets.token_urlsafe(36)
    now = datetime.now()
    created = now.isoformat()
    # Long enough for a long play session; client keeps token only in memory and revokes best-effort on game quit.
    expires = (now + timedelta(hours=24)).isoformat()
    conn.execute(
        """
        INSERT INTO aio_sessions(token, account_id, steam_id64, display_name, created_at, last_seen_at, expires_at, revoked)
        VALUES (?, ?, ?, ?, ?, ?, ?, 0)
        """,
        (token, int(account_id), steam_id64 or "", display_name or "", created, created, expires),
    )
    return token


def resolve_aio_session(conn, token):
    token = (token or "").strip()
    if not token:
        return None
    row = conn.execute(
        """
        SELECT s.account_id, COALESCE(a.username, ''), COALESCE(a.steam_id64, s.steam_id64, ''), COALESCE(a.display_name, s.display_name, a.username, '')
        FROM aio_sessions s
        JOIN accounts a ON a.id = s.account_id
        WHERE s.token = ? AND s.revoked = 0 AND s.expires_at > ?
        """,
        (token, datetime.now().isoformat()),
    ).fetchone()
    if not row:
        return None
    # Sliding expiration while the game process is alive. The client never persists this token to disk,
    # so closing/reopening the game still requires fresh Steam auth.
    now = datetime.now()
    conn.execute(
        "UPDATE aio_sessions SET last_seen_at = ?, expires_at = ? WHERE token = ?",
        (utcnow_iso(), (now + timedelta(hours=24)).isoformat(), token),
    )
    return row


def revoke_aio_session(conn, token):
    token = (token or "").strip()
    if token:
        conn.execute("UPDATE aio_sessions SET revoked = 1, last_seen_at = ? WHERE token = ?", (utcnow_iso(), token))


class SteamOpenIDHttpHandler(BaseHTTPRequestHandler):
    server_version = "MonsterpatchSteamOpenIDTest/0.1"

    def log_message(self, fmt, *args):
        log("OpenID HTTP: " + fmt % args)

    def _html(self, status, title, body):
        page = f"""<!doctype html>
<html><head><meta charset=\"utf-8\"><title>{html.escape(title)}</title>
<style>body{{font-family:system-ui,Segoe UI,Arial,sans-serif;max-width:720px;margin:40px auto;padding:0 18px;line-height:1.45}}code{{background:#eee;padding:2px 4px;border-radius:4px}}</style></head>
<body><h1>{html.escape(title)}</h1>{body}</body></html>"""
        raw = page.encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        qs = urllib.parse.parse_qs(parsed.query)
        if parsed.path == "/":
            self._html(200, "Monsterpatch GTS Steam OpenID Server", f"<p>HTTP callback server is running.</p><p>Socket server: <code>{HOST}:{PORT}</code></p><p>Public base URL: <code>{html.escape(OPENID_PUBLIC_BASE_URL)}</code></p>")
            return
        if parsed.path != "/steam/return":
            self._html(404, "Not found", "<p>Unknown path.</p>")
            return

        state = qs.get("state", [""])[0]
        cleanup_pending_openid()
        with PENDING_OPENID_LOCK:
            pending = PENDING_OPENID.get(state)
        if not state or not pending:
            self._html(400, "Steam authentication failed", "<p>This login request expired or does not exist. Please return to the game and try again.</p>")
            return

        ok, steam_id64, error = validate_steam_openid_response(qs)
        if not ok:
            with PENDING_OPENID_LOCK:
                if state in PENDING_OPENID:
                    PENDING_OPENID[state]["status"] = "error"
                    PENDING_OPENID[state]["error"] = error or "Steam authentication failed."
            self._html(400, "Steam authentication failed", f"<p>{html.escape(error or 'Steam authentication failed.')}</p><p>Please return to the game and try again.</p>")
            return

        active_ban = moderation.get_active_ban(steam_id64=steam_id64)
        if active_ban:
            reason = active_ban.get("reason") or "This Steam account is banned from MMOnsterpatch online services."
            with PENDING_OPENID_LOCK:
                if state in PENDING_OPENID:
                    PENDING_OPENID[state]["status"] = "error"
                    PENDING_OPENID[state]["error"] = "This Steam account is banned from MMOnsterpatch online services. Reason: " + str(reason)
            log(f"Steam OpenID blocked banned SteamID: {steam_id64} reason={reason}")
            self._html(403, "Steam authentication blocked", "<p>This Steam account is banned from MMOnsterpatch online services.</p>")
            return

        try:
            conn = get_db()
            display_name = fetch_steam_display_name(steam_id64)
            account_id, username, display_name = get_or_create_steam_account(conn, steam_id64, display_name)
            conn.close()
        except Exception as ex:
            with PENDING_OPENID_LOCK:
                if state in PENDING_OPENID:
                    PENDING_OPENID[state]["status"] = "error"
                    PENDING_OPENID[state]["error"] = f"Server account error: {ex}"
            self._html(500, "Steam authentication failed", "<p>The server could not create/load your GTS account. Please try again later.</p>")
            return

        with PENDING_OPENID_LOCK:
            if state in PENDING_OPENID:
                PENDING_OPENID[state].update({
                    "status": "ok",
                    "steam_id64": steam_id64,
                    "account_id": account_id,
                    "username": username,
                    "display_name": display_name,
                    "completed": time.time(),
                })
        log(f"Steam OpenID success: steam_id64={steam_id64} username={username} display_name={display_name}")
        self._html(200, "Steam authentication successful", "<p>You can return to Monsterpatch now.</p><p>No Steam password was sent to or stored by this server.</p>")


def start_openid_http_server():
    server = ThreadingHTTPServer((OPENID_HTTP_HOST, OPENID_HTTP_PORT), SteamOpenIDHttpHandler)
    t = threading.Thread(target=server.serve_forever, name="SteamOpenIDHttpServer", daemon=True)
    t.start()
    log(f"Steam OpenID HTTP callback listening on {OPENID_HTTP_HOST}:{OPENID_HTTP_PORT}; public base URL: {OPENID_PUBLIC_BASE_URL}")
    return server


class BankHandler(socketserver.StreamRequestHandler):
    def setup(self):
        super().setup()
        self.conn = get_db()
        self.account_id = None
        self.username = None
        self.steam_id64 = ""
        with ACTIVE_BANK_HANDLERS_LOCK:
            ACTIVE_BANK_HANDLERS.add(self)

    def finish(self):
        with ACTIVE_BANK_HANDLERS_LOCK:
            ACTIVE_BANK_HANDLERS.discard(self)
        try:
            self.conn.close()
        except Exception:
            pass
        super().finish()

    def send_line(self, *parts):
        safe = [str(p).replace("\t", " ").replace("\r", " ").replace("\n", " ") for p in parts]
        self.wfile.write(("\t".join(safe) + "\n").encode("utf-8"))
        self.wfile.flush()

    def send_error(self, message: str):
        self.send_line("ERR", encode_message(message))

    def handle(self):
        log(f"Connection from {self.client_address[0]}:{self.client_address[1]}")
        try:
            while True:
                raw = self.rfile.readline()
                if not raw:
                    break
                line = raw.decode("utf-8", errors="ignore").strip("\r\n")
                if not line:
                    continue
                parts = line.split("\t")
                cmd = parts[0].upper()
                if cmd == "REGISTER":
                    self.cmd_register(parts)
                elif cmd == "LOGIN":
                    self.cmd_login(parts)
                elif cmd == "STEAM_OPENID_BEGIN":
                    self.cmd_steam_openid_begin(parts)
                elif cmd == "STEAM_OPENID_POLL":
                    self.cmd_steam_openid_poll(parts)
                elif cmd == "SESSION_LOGIN":
                    self.cmd_session_login(parts)
                elif cmd == "SESSION_LOGOUT":
                    self.cmd_session_logout(parts)
                elif cmd == "PING":
                    self.send_line("OK", "PING")
                elif cmd == "FETCH":
                    self.cmd_fetch()
                elif cmd == "SAVE":
                    self.cmd_save(parts)
                elif cmd == "GTS_CREATE":
                    self.cmd_gts_create(parts)
                elif cmd == "GTS_SEARCH_PAGE":
                    self.cmd_gts_search_page(parts)
                elif cmd == "GTS_MY_LISTINGS_PAGE":
                    self.cmd_gts_my_listings_page(parts)
                elif cmd == "GTS_OFFER":
                    self.cmd_gts_offer(parts)
                elif cmd == "GTS_CANCEL":
                    self.cmd_gts_cancel(parts)
                elif cmd == "GTS_CLAIM":
                    self.cmd_gts_claim()
                elif cmd == "GTS_NOTIFY_ACCEPTED":
                    self.cmd_gts_notify_accepted()
                elif cmd == "RA_POKEMON_CREATE":
                    self.cmd_ra_pokemon_create(parts)
                elif cmd == "RA_POKEMON_SEARCH_PAGE":
                    self.cmd_ra_pokemon_search_page(parts)
                elif cmd == "RA_POKEMON_MY_PAGE":
                    self.cmd_ra_pokemon_my_page(parts)
                elif cmd == "RA_POKEMON_BUY":
                    self.cmd_ra_pokemon_buy(parts)
                elif cmd == "RA_ITEM_CREATE":
                    self.cmd_ra_item_create(parts)
                elif cmd == "RA_ITEM_SEARCH_PAGE":
                    self.cmd_ra_item_search_page(parts)
                elif cmd == "RA_ITEM_MY_PAGE":
                    self.cmd_ra_item_my_page(parts)
                elif cmd == "RA_ITEM_BUY":
                    self.cmd_ra_item_buy(parts)
                elif cmd == "RA_CLAIM_PAYOUTS":
                    self.cmd_ra_claim_payouts()
                elif cmd == "RA_CANCEL":
                    self.cmd_ra_cancel(parts)
                else:
                    self.send_error("Unknown command.")
        except (ConnectionResetError, BrokenPipeError, OSError):
            pass
        finally:
            if self.username:
                log(f"{self.username} disconnected")
            else:
                log(f"Disconnected: {self.client_address[0]}:{self.client_address[1]}")

    def require_login(self):
        if self.account_id is None:
            self.send_error("You are not logged in.")
            return False
        return True

    def parse_int(self, raw, default=None):
        try:
            return int(raw)
        except (TypeError, ValueError):
            return default


    def cmd_steam_openid_begin(self, parts):
        cleanup_pending_openid()
        state = secrets.token_urlsafe(24)
        with PENDING_OPENID_LOCK:
            PENDING_OPENID[state] = {
                "status": "pending",
                "created": time.time(),
                "client_ip": self.client_address[0],
            }
        login_url = build_steam_openid_login_url(state)
        log(f"Steam OpenID begin: state={state} client={self.client_address[0]}")
        self.send_line("OK", "STEAM_OPENID_BEGIN", state, encode_message(login_url), OPENID_STATE_TTL_SECONDS)

    def cmd_steam_openid_poll(self, parts):
        if len(parts) < 2:
            self.send_error("Invalid Steam authentication poll request.")
            return
        state = parts[1].strip()
        cleanup_pending_openid()
        with PENDING_OPENID_LOCK:
            pending = PENDING_OPENID.get(state)
        if not pending:
            self.send_error("Steam authentication failed. Please make sure Steam is running and you are online.")
            return
        status = pending.get("status")
        if status == "pending":
            self.send_line("PENDING", "STEAM_OPENID_POLL")
            return
        if status == "error":
            self.send_error(pending.get("error") or "Steam authentication failed. Please make sure Steam is running and you are online.")
            return
        if status != "ok":
            self.send_error("Steam authentication failed. Please make sure Steam is running and you are online.")
            return
        self.account_id = int(pending["account_id"])
        self.username = pending["username"]
        self.conn.execute("UPDATE accounts SET last_login_at = ? WHERE id = ?", (utcnow_iso(), self.account_id))
        steam_id64 = pending.get("steam_id64", "")
        display_name = pending.get("display_name", self.username)
        active_ban = moderation.get_active_ban(steam_id64=steam_id64)
        if active_ban:
            reason = active_ban.get("reason") or "This Steam account is banned from MMOnsterpatch online services."
            self.send_error("This Steam account is banned from MMOnsterpatch online services. Reason: " + str(reason))
            revoke_aio_sessions_for_steam(steam_id64)
            log(f"Steam OpenID socket login blocked banned SteamID: username={self.username} steam_id64={steam_id64} reason={reason}")
            return
        self.steam_id64 = steam_id64 or ""
        session_token = create_aio_session(self.conn, self.account_id, steam_id64, display_name)
        log(f"Steam OpenID socket login success: username={self.username} steam_id64={steam_id64} aio_session=yes")
        # Existing clients only require OK LOGIN. Extra fields are for new clients/tests.
        self.send_line("OK", "LOGIN", encode_message(self.username), steam_id64, encode_message(display_name), session_token)

    def cmd_session_login(self, parts):
        if len(parts) < 2:
            self.send_error("Invalid session login request.")
            return
        token = parts[1].strip()
        row = resolve_aio_session(self.conn, token)
        if not row:
            self.send_error("Stored session expired. Please connect with Steam again.")
            return
        account_id, username, steam_id64, display_name = row
        active_ban = moderation.get_active_ban(steam_id64=steam_id64)
        if active_ban:
            reason = active_ban.get("reason") or "This Steam account is banned from MMOnsterpatch online services."
            revoke_aio_session(self.conn, token)
            self.send_error("This Steam account is banned from MMOnsterpatch online services. Reason: " + str(reason))
            log(f"AIO session login blocked banned SteamID: username={username} steam_id64={steam_id64} reason={reason}")
            return
        self.account_id = int(account_id)
        self.username = username or display_name or "Steam user"
        self.steam_id64 = steam_id64 or ""
        self.conn.execute("UPDATE accounts SET last_login_at = ? WHERE id = ?", (utcnow_iso(), self.account_id))
        log(f"AIO session login success: username={self.username} steam_id64={steam_id64}")
        self.send_line("OK", "LOGIN", encode_message(self.username), steam_id64 or "", encode_message(display_name or self.username), token)

    def cmd_session_logout(self, parts):
        if len(parts) >= 2:
            revoke_aio_session(self.conn, parts[1].strip())
        self.account_id = None
        self.username = None
        self.steam_id64 = ""
        self.send_line("OK", "SESSION_LOGOUT")

    def cmd_register(self, parts):
        if len(parts) < 3:
            self.send_error("Invalid registration request.")
            return
        username = decode_message(parts[1]).strip()
        password = decode_message(parts[2])
        if not username or not password:
            self.send_error("Username and password are required.")
            return
        salt = secrets.token_hex(16)
        pw_hash = hash_password(password, salt)
        try:
            self.conn.execute(
                "INSERT INTO accounts(username, password_salt, password_hash, created_at) VALUES (?, ?, ?, ?)",
                (username, salt, pw_hash, utcnow_iso())
            )
            log(f"Registered account: {username}")
            self.send_line("OK", "REGISTER")
        except sqlite3.IntegrityError:
            self.send_error("That username is already in use.")

    def cmd_login(self, parts):
        if len(parts) < 3:
            self.send_error("Invalid login request.")
            return
        username = decode_message(parts[1]).strip()
        password = decode_message(parts[2])
        row = self.conn.execute(
            "SELECT id, password_salt, password_hash FROM accounts WHERE username = ?",
            (username,)
        ).fetchone()
        if not row:
            self.send_error("Invalid username or password.")
            return
        account_id, salt, pw_hash = row
        if hash_password(password, salt) != pw_hash:
            self.send_error("Invalid username or password.")
            return
        self.account_id = account_id
        self.username = username
        self.conn.execute("UPDATE accounts SET last_login_at = ? WHERE id = ?", (utcnow_iso(), account_id))
        log(f"Login success: {username}")
        self.send_line("OK", "LOGIN")

    def cmd_fetch(self):
        if not self.require_login():
            return
        rows = self.conn.execute(
            "SELECT slot_index, species, level, name_b64, gender, shiny, blob_b64 FROM bank_slots WHERE account_id = ? ORDER BY slot_index ASC",
            (self.account_id,)
        ).fetchall()
        self.send_line("OK", "FETCH", SLOT_COUNT)
        for row in rows:
            self.send_line("SLOT", row[0], row[1], row[2], row[3], row[4], row[5], row[6])
        self.send_line("END")

    def cmd_save(self, parts):
        if not self.require_login():
            return
        expected_count = SLOT_COUNT
        if len(parts) >= 2:
            try:
                expected_count = int(parts[1])
            except ValueError:
                expected_count = SLOT_COUNT
        if expected_count != SLOT_COUNT:
            self.send_error("Unexpected slot count.")
            return
        records = []
        while True:
            raw = self.rfile.readline()
            if not raw:
                self.send_error("Connection closed during save.")
                return
            line = raw.decode("utf-8", errors="ignore").strip("\r\n")
            if line == "END":
                break
            subparts = line.split("\t")
            if subparts[0] != "SLOT" or len(subparts) < 8:
                self.send_error("Malformed save data.")
                return
            slot_index = self.parse_int(subparts[1], -1)
            if slot_index < 0 or slot_index >= SLOT_COUNT:
                self.send_error("Invalid slot index.")
                return
            records.append((slot_index, subparts[2], int(subparts[3]), subparts[4], int(subparts[5]), int(subparts[6]), subparts[7]))
        with self.conn:
            self.conn.execute("DELETE FROM bank_slots WHERE account_id = ?", (self.account_id,))
            for rec in records:
                self.conn.execute(
                    "INSERT INTO bank_slots(account_id, slot_index, species, level, name_b64, gender, shiny, blob_b64) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    (self.account_id, rec[0], rec[1], rec[2], rec[3], rec[4], rec[5], rec[6])
                )
        log(f"Saved bank for {self.username} ({len(records)} occupied slots)")
        self.send_line("OK", "SAVE")

    def cmd_gts_create(self, parts):
        if not self.require_login():
            return
        if len(parts) < 8:
            self.send_error("Invalid GTS listing request.")
            return
        request_species = parts[1].strip()
        offered_species = parts[2].strip()
        level = self.parse_int(parts[3], -1)
        name_b64 = parts[4]
        gender = self.parse_int(parts[5], 0)
        shiny = self.parse_int(parts[6], 0)
        blob_b64 = parts[7]
        if not request_species or not offered_species or level < 1 or not blob_b64:
            self.send_error("Invalid GTS listing data.")
            return
        now = utcnow_iso()
        cur = self.conn.execute(
            """
            INSERT INTO gts_listings(
                owner_account_id, owner_username, request_species, offered_species, level,
                name_b64, gender, shiny, blob_b64, status, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 'open', ?, ?)
            """,
            (self.account_id, self.username, request_species, offered_species, level, name_b64, gender, shiny, blob_b64, now, now)
        )
        listing_id = cur.lastrowid
        log(f"GTS create: user={self.username} listing_id={listing_id} request={request_species} offered={offered_species} lvl={level}")
        self.send_line("OK", "GTS_CREATE", listing_id)

    def _send_listing_page(self, response_tag: str, page_index: int, total_count: int, rows):
        page_count = max(1, math.ceil(total_count / GTS_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        self.send_line("OK", response_tag, page_index, page_count, total_count, GTS_PAGE_SIZE)
        for row in rows:
            # Newer SELECTs include created_at, owner_account_id, steam_id64,
            # display_name, owner_username after the client-facing listing fields.
            owner_name = row[1]
            created_at = ""
            if len(row) >= 10:
                created_at = row[9] or ""
            if len(row) >= 14:
                owner_name = resolve_account_display_name(self.conn, row[10], row[11], row[12], row[13])
            self.send_line(
                "LISTING",
                row[0], owner_name, row[2], row[3], row[4], row[5], row[6], row[7], row[8], created_at
            )
        self.send_line("END")

    def cmd_gts_search_page(self, parts):
        if not self.require_login():
            return
        page_index = self.parse_int(parts[1] if len(parts) > 1 else 0, 0)
        species_filter = parts[2].strip() if len(parts) > 2 else "*"
        page_index = max(0, page_index)
        # Public browse intentionally hides your own listings. Use My Listings to cancel/inspect yours.
        params = [self.account_id]
        where = ["l.status = 'open'", "l.owner_account_id != ?"]
        if species_filter and species_filter != "*":
            where.append("l.request_species = ?")
            params.append(species_filter)
        where_sql = " AND ".join(where)
        total_count = self.conn.execute(f"SELECT COUNT(*) FROM gts_listings l WHERE {where_sql}", params).fetchone()[0]
        page_count = max(1, math.ceil(total_count / GTS_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        offset = page_index * GTS_PAGE_SIZE
        rows = self.conn.execute(
            f"""
            SELECT l.id, COALESCE(NULLIF(a.display_name, ''), l.owner_username) AS owner_display_name,
                   l.request_species, l.offered_species, l.level, l.name_b64, l.gender, l.shiny, l.blob_b64,
                   l.created_at, l.owner_account_id, a.steam_id64, a.display_name, l.owner_username
            FROM gts_listings l
            LEFT JOIN accounts a ON a.id = l.owner_account_id
            WHERE {where_sql}
            ORDER BY l.created_at ASC, l.id ASC
            LIMIT ? OFFSET ?
            """,
            (*params, GTS_PAGE_SIZE, offset)
        ).fetchall()
        self._send_listing_page("GTS_SEARCH_PAGE", page_index, total_count, rows)

    def cmd_gts_my_listings_page(self, parts):
        if not self.require_login():
            return
        page_index = self.parse_int(parts[1] if len(parts) > 1 else 0, 0)
        page_index = max(0, page_index)
        total_count = self.conn.execute(
            "SELECT COUNT(*) FROM gts_listings WHERE owner_account_id = ? AND status = 'open'",
            (self.account_id,)
        ).fetchone()[0]
        page_count = max(1, math.ceil(total_count / GTS_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        offset = page_index * GTS_PAGE_SIZE
        rows = self.conn.execute(
            """
            SELECT l.id, COALESCE(NULLIF(a.display_name, ''), l.owner_username) AS owner_display_name,
                   l.request_species, l.offered_species, l.level, l.name_b64, l.gender, l.shiny, l.blob_b64,
                   l.created_at, l.owner_account_id, a.steam_id64, a.display_name, l.owner_username
            FROM gts_listings l
            LEFT JOIN accounts a ON a.id = l.owner_account_id
            WHERE l.owner_account_id = ? AND l.status = 'open'
            ORDER BY l.created_at ASC, l.id ASC
            LIMIT ? OFFSET ?
            """,
            (self.account_id, GTS_PAGE_SIZE, offset)
        ).fetchall()
        self._send_listing_page("GTS_MY_LISTINGS_PAGE", page_index, total_count, rows)

    def cmd_gts_cancel(self, parts):
        if not self.require_login():
            return
        listing_id = self.parse_int(parts[1] if len(parts) > 1 else None, None)
        if listing_id is None:
            self.send_error("Invalid listing id.")
            return
        with self.conn:
            row = self.conn.execute(
                "SELECT owner_account_id, status, blob_b64 FROM gts_listings WHERE id = ?",
                (listing_id,)
            ).fetchone()
            if not row:
                self.send_error("That listing does not exist.")
                return
            owner_account_id, status, blob_b64 = row
            if owner_account_id != self.account_id:
                self.send_error("That listing does not belong to you.")
                return
            if status != "open":
                self.send_error("That listing is no longer open.")
                return
            self.conn.execute(
                "UPDATE gts_listings SET status = 'cancelled', updated_at = ? WHERE id = ?",
                (utcnow_iso(), listing_id)
            )
        log(f"GTS cancel: user={self.username} listing_id={listing_id}")
        self.send_line("OK", "GTS_CANCEL", blob_b64)

    def cmd_gts_offer(self, parts):
        if not self.require_login():
            return
        if len(parts) < 8:
            self.send_error("Invalid GTS offer request.")
            return
        listing_id = self.parse_int(parts[1], None)
        offered_species = parts[2].strip()
        level = self.parse_int(parts[3], -1)
        _name_b64 = parts[4]
        _gender = self.parse_int(parts[5], 0)
        _shiny = self.parse_int(parts[6], 0)
        blob_b64 = parts[7]
        if listing_id is None or not offered_species or level < 1 or not blob_b64:
            self.send_error("Invalid GTS offer data.")
            return
        now = utcnow_iso()
        try:
            self.conn.execute("BEGIN IMMEDIATE")
            row = self.conn.execute(
                """
                SELECT id, owner_account_id, owner_username, request_species, blob_b64, status
                FROM gts_listings WHERE id = ?
                """,
                (listing_id,)
            ).fetchone()
            if not row:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing does not exist.")
                return
            _, owner_account_id, owner_username, request_species, listed_blob_b64, status = row
            if owner_account_id == self.account_id:
                self.conn.execute("ROLLBACK")
                self.send_error("You cannot trade with your own listing.")
                return
            if status != "open":
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            if offered_species != request_species:
                self.conn.execute("ROLLBACK")
                self.send_error("That Pokémon does not match the requested species.")
                return
            updated = self.conn.execute(
                """
                UPDATE gts_listings
                SET status = 'completed', completed_by_account_id = ?, completed_by_username = ?, updated_at = ?
                WHERE id = ? AND status = 'open'
                """,
                (self.account_id, self.username, now, listing_id)
            )
            if updated.rowcount != 1:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            self.conn.execute(
                """
                INSERT INTO gts_claims(owner_account_id, source_listing_id, blob_b64, claimed, created_at)
                VALUES (?, ?, ?, 0, ?)
                """,
                (owner_account_id, listing_id, blob_b64, now)
            )
            self.conn.execute("COMMIT")
        except Exception:
            try:
                self.conn.execute("ROLLBACK")
            except Exception:
                pass
            raise
        log(f"GTS offer: buyer={self.username} owner={owner_username} listing_id={listing_id} species={offered_species} lvl={level}")
        self.send_line("OK", "GTS_OFFER", listed_blob_b64)

    def cmd_gts_notify_accepted(self):
        if not self.require_login():
            return
        now = utcnow_iso()
        rows = self.conn.execute(
            """
            SELECT id, source_listing_id
            FROM gts_claims
            WHERE owner_account_id = ?
              AND claimed = 0
              AND (notified_at IS NULL OR notified_at = '')
            ORDER BY id ASC
            """,
            (self.account_id,)
        ).fetchall()
        count = len(rows)
        if count:
            claim_ids = [row[0] for row in rows]
            placeholders = ",".join("?" for _ in claim_ids)
            self.conn.execute(
                f"UPDATE gts_claims SET notified_at = ? WHERE id IN ({placeholders})",
                (now, *claim_ids)
            )
            log(f"GTS notify accepted: user={self.username} count={count}")
        self.send_line("OK", "GTS_NOTIFY_ACCEPTED", count)

    def cmd_gts_claim(self):
        if not self.require_login():
            return
        rows = self.conn.execute(
            "SELECT id, source_listing_id, blob_b64 FROM gts_claims WHERE owner_account_id = ? AND claimed = 0 ORDER BY id ASC",
            (self.account_id,)
        ).fetchall()
        self.send_line("OK", "GTS_CLAIM", len(rows))
        for _, source_listing_id, blob_b64 in rows:
            self.send_line("CLAIM", source_listing_id, blob_b64)
        self.send_line("END")
        if rows:
            claim_ids = [row[0] for row in rows]
            placeholders = ",".join("?" for _ in claim_ids)
            self.conn.execute(
                f"UPDATE gts_claims SET claimed = 1, claimed_at = ? WHERE id IN ({placeholders})",
                (utcnow_iso(), *claim_ids)
            )
            log(f"GTS claim: user={self.username} count={len(rows)}")


    # -------------------------------------------------------------------------
    # Team Rocket Auctions commands
    # -------------------------------------------------------------------------

    def _ra_send_pokemon_page(self, tag, page_index, total_count, rows):
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        self.send_line("OK", tag, page_index, page_count, total_count, RA_PAGE_SIZE)
        for row in rows:
            self.send_line("RA_POKEMON", row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8])
        self.send_line("END")

    def _ra_send_item_page(self, tag, page_index, total_count, rows):
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        self.send_line("OK", tag, page_index, page_count, total_count, RA_PAGE_SIZE)
        for row in rows:
            self.send_line("RA_ITEM", row[0], row[1], row[2], row[3], row[4], row[5], row[6])
        self.send_line("END")

    def cmd_ra_pokemon_create(self, parts):
        if not self.require_login():
            return
        if len(parts) < 8:
            self.send_error("Invalid auction listing request.")
            return
        price = self.parse_int(parts[1], -1)
        species = parts[2].strip()
        level = self.parse_int(parts[3], -1)
        name_b64 = parts[4]
        gender = self.parse_int(parts[5], 0)
        shiny = self.parse_int(parts[6], 0)
        blob_b64 = parts[7]
        if price < 1 or price > RA_MAX_PRICE or not species or level < 1 or not blob_b64:
            self.send_error("Invalid auction listing data.")
            return
        now = utcnow_iso()
        cur = self.conn.execute(
            """
            INSERT INTO rocket_pokemon_listings(
                seller_account_id, seller_username, price, species, level, name_b64,
                gender, shiny, blob_b64, status, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 'open', ?, ?)
            """,
            (self.account_id, self.username, price, species, level, name_b64, gender, shiny, blob_b64, now, now),
        )
        log(f"RA Pokemon create: seller={self.username} listing_id={cur.lastrowid} species={species} price={price}")
        self.send_line("OK", "RA_POKEMON_CREATE", cur.lastrowid)

    def cmd_ra_pokemon_search_page(self, parts):
        if not self.require_login():
            return
        page_index = max(0, self.parse_int(parts[1] if len(parts) > 1 else 0, 0))
        total_count = self.conn.execute("SELECT COUNT(*) FROM rocket_pokemon_listings WHERE status = 'open' AND seller_account_id != ?", (self.account_id,)).fetchone()[0]
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        rows = self.conn.execute(
            """
            SELECT id, seller_username, price, species, level, name_b64, gender, shiny, blob_b64
            FROM rocket_pokemon_listings
            WHERE status = 'open' AND seller_account_id != ?
            ORDER BY created_at ASC, id ASC
            LIMIT ? OFFSET ?
            """,
            (self.account_id, RA_PAGE_SIZE, page_index * RA_PAGE_SIZE),
        ).fetchall()
        self._ra_send_pokemon_page("RA_POKEMON_SEARCH_PAGE", page_index, total_count, rows)

    def cmd_ra_pokemon_my_page(self, parts):
        if not self.require_login():
            return
        page_index = max(0, self.parse_int(parts[1] if len(parts) > 1 else 0, 0))
        total_count = self.conn.execute(
            "SELECT COUNT(*) FROM rocket_pokemon_listings WHERE seller_account_id = ? AND status = 'open'",
            (self.account_id,),
        ).fetchone()[0]
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        rows = self.conn.execute(
            """
            SELECT id, seller_username, price, species, level, name_b64, gender, shiny, blob_b64
            FROM rocket_pokemon_listings
            WHERE seller_account_id = ? AND status = 'open'
            ORDER BY created_at ASC, id ASC
            LIMIT ? OFFSET ?
            """,
            (self.account_id, RA_PAGE_SIZE, page_index * RA_PAGE_SIZE),
        ).fetchall()
        self._ra_send_pokemon_page("RA_POKEMON_MY_PAGE", page_index, total_count, rows)

    def cmd_ra_pokemon_buy(self, parts):
        if not self.require_login():
            return
        listing_id = self.parse_int(parts[1] if len(parts) > 1 else None, None)
        if listing_id is None:
            self.send_error("Invalid listing id.")
            return
        now = utcnow_iso()
        try:
            self.conn.execute("BEGIN IMMEDIATE")
            row = self.conn.execute(
                """
                SELECT seller_account_id, seller_username, price, blob_b64, status
                FROM rocket_pokemon_listings WHERE id = ?
                """,
                (listing_id,),
            ).fetchone()
            if not row:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing does not exist.")
                return
            seller_account_id, seller_username, price, blob_b64, status = row
            if seller_account_id == self.account_id:
                self.conn.execute("ROLLBACK")
                self.send_error("You cannot buy your own listing.")
                return
            if status != "open":
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            updated = self.conn.execute(
                """
                UPDATE rocket_pokemon_listings
                SET status = 'sold', buyer_account_id = ?, buyer_username = ?, updated_at = ?
                WHERE id = ? AND status = 'open'
                """,
                (self.account_id, self.username, now, listing_id),
            )
            if updated.rowcount != 1:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            self.conn.execute(
                """
                INSERT INTO rocket_payouts(owner_account_id, amount, reason, source_kind, source_listing_id, claimed, created_at)
                VALUES (?, ?, ?, 'pokemon', ?, 0, ?)
                """,
                (seller_account_id, price, f"Pokemon auction sold to {self.username}", listing_id, now),
            )
            self.conn.execute("COMMIT")
        except Exception:
            try:
                self.conn.execute("ROLLBACK")
            except Exception:
                pass
            raise
        log(f"RA Pokemon buy: buyer={self.username} seller={seller_username} listing_id={listing_id} price={price}")
        self.send_line("OK", "RA_POKEMON_BUY", blob_b64)

    def cmd_ra_item_create(self, parts):
        if not self.require_login():
            return
        if len(parts) < 5:
            self.send_error("Invalid item listing request.")
            return
        item_id = parts[1].strip()
        quantity = self.parse_int(parts[2], -1)
        price = self.parse_int(parts[3], -1)
        item_name_b64 = parts[4]
        if not item_id or quantity < 1 or price < 1 or price > RA_MAX_PRICE:
            self.send_error("Invalid item listing data.")
            return
        now = utcnow_iso()
        cur = self.conn.execute(
            """
            INSERT INTO rocket_item_listings(
                seller_account_id, seller_username, item_id, item_name_b64, quantity,
                price, status, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, 'open', ?, ?)
            """,
            (self.account_id, self.username, item_id, item_name_b64, quantity, price, now, now),
        )
        log(f"RA Item create: seller={self.username} listing_id={cur.lastrowid} item={item_id} qty={quantity} price={price}")
        self.send_line("OK", "RA_ITEM_CREATE", cur.lastrowid)

    def cmd_ra_item_search_page(self, parts):
        if not self.require_login():
            return
        page_index = max(0, self.parse_int(parts[1] if len(parts) > 1 else 0, 0))
        total_count = self.conn.execute("SELECT COUNT(*) FROM rocket_item_listings WHERE status = 'open' AND seller_account_id != ?", (self.account_id,)).fetchone()[0]
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        rows = self.conn.execute(
            """
            SELECT id, seller_username, item_id, item_name_b64, quantity, price, status
            FROM rocket_item_listings
            WHERE status = 'open' AND seller_account_id != ?
            ORDER BY created_at ASC, id ASC
            LIMIT ? OFFSET ?
            """,
            (self.account_id, RA_PAGE_SIZE, page_index * RA_PAGE_SIZE),
        ).fetchall()
        self._ra_send_item_page("RA_ITEM_SEARCH_PAGE", page_index, total_count, rows)

    def cmd_ra_item_my_page(self, parts):
        if not self.require_login():
            return
        page_index = max(0, self.parse_int(parts[1] if len(parts) > 1 else 0, 0))
        total_count = self.conn.execute(
            "SELECT COUNT(*) FROM rocket_item_listings WHERE seller_account_id = ? AND status = 'open'",
            (self.account_id,),
        ).fetchone()[0]
        page_count = max(1, math.ceil(total_count / RA_PAGE_SIZE)) if total_count > 0 else 1
        page_index = max(0, min(page_index, page_count - 1))
        rows = self.conn.execute(
            """
            SELECT id, seller_username, item_id, item_name_b64, quantity, price, status
            FROM rocket_item_listings
            WHERE seller_account_id = ? AND status = 'open'
            ORDER BY created_at ASC, id ASC
            LIMIT ? OFFSET ?
            """,
            (self.account_id, RA_PAGE_SIZE, page_index * RA_PAGE_SIZE),
        ).fetchall()
        self._ra_send_item_page("RA_ITEM_MY_PAGE", page_index, total_count, rows)

    def cmd_ra_item_buy(self, parts):
        if not self.require_login():
            return
        listing_id = self.parse_int(parts[1] if len(parts) > 1 else None, None)
        if listing_id is None:
            self.send_error("Invalid listing id.")
            return
        now = utcnow_iso()
        try:
            self.conn.execute("BEGIN IMMEDIATE")
            row = self.conn.execute(
                """
                SELECT seller_account_id, seller_username, item_id, item_name_b64, quantity, price, status
                FROM rocket_item_listings WHERE id = ?
                """,
                (listing_id,),
            ).fetchone()
            if not row:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing does not exist.")
                return
            seller_account_id, seller_username, item_id, item_name_b64, quantity, price, status = row
            if seller_account_id == self.account_id:
                self.conn.execute("ROLLBACK")
                self.send_error("You cannot buy your own listing.")
                return
            if status != "open":
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            updated = self.conn.execute(
                """
                UPDATE rocket_item_listings
                SET status = 'sold', buyer_account_id = ?, buyer_username = ?, updated_at = ?
                WHERE id = ? AND status = 'open'
                """,
                (self.account_id, self.username, now, listing_id),
            )
            if updated.rowcount != 1:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer available.")
                return
            self.conn.execute(
                """
                INSERT INTO rocket_payouts(owner_account_id, amount, reason, source_kind, source_listing_id, claimed, created_at)
                VALUES (?, ?, ?, 'item', ?, 0, ?)
                """,
                (seller_account_id, price, f"Item auction sold to {self.username}", listing_id, now),
            )
            self.conn.execute("COMMIT")
        except Exception:
            try:
                self.conn.execute("ROLLBACK")
            except Exception:
                pass
            raise
        log(f"RA Item buy: buyer={self.username} seller={seller_username} listing_id={listing_id} item={item_id} qty={quantity} price={price}")
        self.send_line("OK", "RA_ITEM_BUY", item_id, item_name_b64, quantity, price)

    def cmd_ra_claim_payouts(self):
        if not self.require_login():
            return
        rows = self.conn.execute(
            "SELECT id, amount FROM rocket_payouts WHERE owner_account_id = ? AND claimed = 0 ORDER BY id ASC",
            (self.account_id,),
        ).fetchall()
        total = sum(row[1] for row in rows)
        if rows:
            ids = [row[0] for row in rows]
            placeholders = ",".join("?" for _ in ids)
            self.conn.execute(
                f"UPDATE rocket_payouts SET claimed = 1, claimed_at = ? WHERE id IN ({placeholders})",
                (utcnow_iso(), *ids),
            )
        log(f"RA Claim payouts: user={self.username} total={total} count={len(rows)}")
        self.send_line("OK", "RA_CLAIM_PAYOUTS", total)

    def cmd_ra_cancel(self, parts):
        if not self.require_login():
            return
        if len(parts) < 3:
            self.send_error("Invalid cancel request.")
            return
        kind = parts[1].strip().lower()
        listing_id = self.parse_int(parts[2], None)
        if kind not in ("pokemon", "item") or listing_id is None:
            self.send_error("Invalid cancel request.")
            return

        table = "rocket_pokemon_listings" if kind == "pokemon" else "rocket_item_listings"
        return_column = "blob_b64" if kind == "pokemon" else "item_id || '\t' || item_name_b64 || '\t' || quantity || '\t' || price"
        try:
            self.conn.execute("BEGIN IMMEDIATE")
            row = self.conn.execute(
                f"SELECT seller_account_id, status, {return_column} FROM {table} WHERE id = ?",
                (listing_id,),
            ).fetchone()
            if not row:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing does not exist.")
                return
            seller_account_id, status, return_data = row
            if seller_account_id != self.account_id:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing does not belong to you.")
                return
            if status != "open":
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer open.")
                return
            updated = self.conn.execute(
                f"UPDATE {table} SET status = 'cancelled', updated_at = ? WHERE id = ? AND status = 'open'",
                (utcnow_iso(), listing_id),
            )
            if updated.rowcount != 1:
                self.conn.execute("ROLLBACK")
                self.send_error("That listing is no longer open.")
                return
            self.conn.execute("COMMIT")
        except Exception:
            try:
                self.conn.execute("ROLLBACK")
            except Exception:
                pass
            raise
        log(f"RA Cancel: user={self.username} kind={kind} listing_id={listing_id}")
        if kind == "pokemon":
            self.send_line("OK", "RA_CANCEL", "pokemon", return_data)
        else:
            item_id, item_name_b64, quantity, price = return_data.split("\t", 3)
            self.send_line("OK", "RA_CANCEL", "item", item_id, item_name_b64, quantity, price)


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    daemon_threads = True


if __name__ == "__main__":
    openid_http_server = start_openid_http_server()
    log(f"Monsterpatch GTS Server listening on {HOST}:{PORT}")
    with ThreadedTCPServer((HOST, PORT), BankHandler) as server:
        server.serve_forever()
