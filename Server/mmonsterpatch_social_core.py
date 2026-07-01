#!/usr/bin/env python3
"""
MMOnsterpatch Social Server v0.3.7-moderation-reports
Raw TCP line server for Social Patcher v0.3.0.

Identity model:
  - The visible character name is display_name.
  - The server-owned identity is character_id.
  - The client proves ownership with secret_token saved locally in the social config.
  - Steam-authenticated players get a permanent MMOnsterpatch account UUID.
  - Characters are attached under that account by save slot.
  - Slot fingerprint mismatch archives the old character and creates a new active character.
  - Public handles use display_name + #4827 style random 4-digit tags.
  - Tags only need to be unique for the full handle; Goose#4827 and Birb#4827 can both exist.

Protocol highlights:
  ACCOUNT_SLOT_HELLO|<base64 aio_session>|<slot>|<base64 old_character_id>|<base64 old_secret>|<base64 display_name>|<base64 slot_fingerprint>|<base64 recovery_snapshot>|<base64 slot_birth_key>
  REGISTER|<base64 display_name>                 (legacy/debug fallback)
  HELLO_ID|<base64 character_id>|<base64 secret_token>|<base64 display_name>  (legacy/debug fallback)
  GUILD_STATE_REQ
  GUILD_CREATE|<base64 guild name>|<base64 guild tag>
  GUILD_INVITE|<base64 public_handle or display_name>
  GUILD_ACCEPT|<guild id>
  GUILD_DECLINE|<guild id>
  GUILD_LEAVE
  RANKED_PROFILE_REQ
  PROFILE_REQ|<base64 public_handle or display name>
  REPORT_USER|<base64 public_handle>|<base64 reason>|<base64 details>
  CHAT|GLOBAL|<base64 message>
  CHAT|GUILD|<base64 message>

Server replies:
  WELCOME|server-ready
  IDENTITY|<base64 character_id>|<base64 secret_token>|<serial>|<base64 public_handle>|<base64 display_name>
  WELCOME|<base64 public_handle>
  GUILD_STATE|NONE
  GUILD_STATE|IN|<guild id>|<base64 guild name>|Leader|Member|<base64 guild tag>
  GUILD_CREATED|<guild id>|<base64 guild name>|Leader|<base64 guild tag>
  GUILD_ERROR|<base64 message>
  GUILD_INVITE|<guild id>|<base64 guild name>|<base64 inviter public_handle>|<base64 guild tag>
  GUILD_JOINED|<guild id>|<base64 guild name>|Member|<base64 guild tag>
  GUILD_LEFT
  RANKED_PROFILE|season_0|<base64 Season 0>|planned|0|E|0|0|0|E|1000|1790812800|4|50|2|0|<base64 RP rules>
  PROFILE|<base64 public_handle>|<base64 display>|<base64 guild>|<base64 tag>|<base64 guild_rank>|<base64 ranked_rank>|rp|max_rp|wins|losses|<base64 highest_rank>|<base64 season>|<base64 character_id>
  REPORT_SUBMITTED|<report_id>|<base64 message>
  CHAT|GLOBAL|<base64 display_name>|<base64 message>|<unix_time>|<base64 guild tag or empty>|<base64 public_handle>
  CHAT|GUILD|<base64 "Leader DisplayName" or "Member DisplayName">|<base64 message>|<unix_time>|<base64 public_handle>

Guild/friend metadata is persistent in SQLite. Chat messages are written to daily JSONL files under data/chat_logs.
"""
import argparse
import base64
import json
import os
import re
import shutil
import secrets
import socketserver
import sqlite3
import threading
import time
import uuid
from typing import Dict, Optional, Tuple

import mmonsterpatch_moderation_core as moderation

VERSION = "0.11.0-base"
clients_lock = threading.RLock()
clients: Dict[object, str] = {}  # handler -> character_id
online_by_character_id: Dict[str, object] = {}
db_lock = threading.RLock()
DB_PATH = None
CHAT_LOG_DIR = None
USER_REPORT_DIR = None
OFFICIAL_ARCHIVE_DIR = None
RANKED_STARTING_RP = 0
RANKED_RP_WRITES_ENABLED = 0
RP_GAIN_RATE = 1.0
RP_LOSS_RATE = 1.0
SEASON_REWARD_RATE = 1.0

OFFICIAL_EXP_RATE = 1.0
OFFICIAL_SATS_RATE = 1.0
OFFICIAL_SHINY_RATE = 1000.0
OFFICIAL_CATCH_RATE = 1.0
OFFICIAL_ITEM_DROP_RATE = 1.0
OFFICIAL_RANDOM_ENCOUNTER_RATE = 1.0
OFFICIAL_VISIBLE_SPAWN_RATE = 2.0
OFFICIAL_REWARD_SPAWN_RATE = 1.0

RANKED_MAX_RP = 1000
RANKED_SEASON0_ID = "season_0"
RANKED_SEASON0_NAME = "Season 0"
RANKED_SEASON0_STARTS_AT = 1790812800  # 2026-10-01 00:00:00 UTC
RANKED_SEASON0_ENDS_AT = 1798761600    # 2027-01-01 00:00:00 UTC
RANKED_REQUIRED_TEAM_SIZE = 4
RANKED_MIN_MON_LEVEL = 50
RANKED_MAX_RANK_GAP = 2
RANKED_ACTIONS_ENABLED = 0  # 0 = database/UI foundation only; ranked buttons remain disabled.
RANKED_REPEAT_FULL_MATCHES_PER_DAY = 3
RANKED_REPEAT_HALF_MATCHES_PER_DAY = 1

# Season 0 draft RP rules. The server owns these numbers so the client only displays them.
# RP gains/losses are not applied yet in this build.
RANKED_RP_RULES = [
    ("E", 0, 0, 199, 4, 2),
    ("D", 1, 200, 399, 5, 4),
    ("C", 2, 400, 599, 6, 6),
    ("B", 3, 600, 799, 7, 8),
    ("A", 4, 800, 999, 8, 10),
    ("S", 5, 1000, 1000, 0, 12),
]

# winner_remaining_mons, winner_adjust, loser_loss_adjust
RANKED_PERFORMANCE_MODIFIERS = [
    (4, 2, 1),
    (3, 1, 0),
    (2, 0, -1),
    (1, -1, -2),
]


def b64_decode(value: str) -> str:
    try:
        return base64.b64decode(value.encode("ascii"), validate=False).decode("utf-8", "replace")
    except Exception:
        return value


def b64_encode(value: str) -> str:
    return base64.b64encode((value or "").encode("utf-8")).decode("ascii")


def sanitize_name(value: str) -> str:
    value = (value or "Player").replace("|", "").replace("\r", "").replace("\n", "").strip()
    value = re.sub(r"\s+", " ", value)
    if not value:
        value = "Player"
    return value[:24]


def sanitize_guild_name(value: str) -> Tuple[Optional[str], Optional[str]]:
    value = (value or "").strip()
    value = re.sub(r"\s+", " ", value)
    if len(value) < 3:
        return None, "Guild name must be at least 3 characters."
    if len(value) > 18:
        return None, "Guild name must be 18 characters or less. Spaces count as characters."
    if not re.fullmatch(r"[A-Za-z0-9 ]+", value):
        return None, "Guild name can only use letters, numbers, and spaces."
    return value, None


def sanitize_guild_tag(value: str) -> Tuple[Optional[str], Optional[str]]:
    value = (value or "").strip().upper()
    if len(value) < 3 or len(value) > 4:
        return None, "Guild tag must be 3-4 characters."
    if not re.fullmatch(r"[A-Za-z0-9]+", value):
        return None, "Guild tag can only use letters and numbers. Spaces are not allowed."
    return value, None


def make_public_handle(display_name: str, serial: int) -> str:
    return f"{sanitize_name(display_name)}#{int(serial) % 10000:04d}"


def send_line(handler, line: str) -> bool:
    try:
        handler.wfile.write((line + "\n").encode("utf-8"))
        handler.wfile.flush()
        return True
    except Exception:
        return False


def send_identity(handler):
    if not handler.character_id or not handler.secret_token:
        return
    send_line(
        handler,
        "IDENTITY|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}".format(
            b64_encode(handler.character_id),
            b64_encode(handler.secret_token),
            int(handler.public_serial or 0),
            b64_encode(handler.public_handle),
            b64_encode(handler.display_name),
            b64_encode(getattr(handler, "account_uuid", "") or ""),
            int(getattr(handler, "slot_index", -1) or -1),
            b64_encode(getattr(handler, "slot_fingerprint", "") or ""),
            getattr(handler, "identity_status", "active") or "active",
            b64_encode(getattr(handler, "slot_birth_key", "") or ""),
        ),
    )


def broadcast(line: str, skip=None):
    dead = []
    with clients_lock:
        targets = list(clients.keys())
    for h in targets:
        if h is skip:
            continue
        if not send_line(h, line):
            dead.append(h)
    if dead:
        with clients_lock:
            for h in dead:
                cid = clients.pop(h, None)
                if cid:
                    online_by_character_id.pop(cid, None)


def broadcast_guild(guild_id: int, line: str):
    dead = []
    with clients_lock:
        targets = list(clients.items())
    for h, character_id in targets:
        membership = get_membership(character_id)
        if membership and membership[0] == guild_id:
            if not send_line(h, line):
                dead.append(h)
    if dead:
        with clients_lock:
            for h in dead:
                cid = clients.pop(h, None)
                if cid:
                    online_by_character_id.pop(cid, None)


def utc_timestamp(ts: Optional[int] = None) -> str:
    try:
        t = int(ts if ts is not None else time.time())
    except Exception:
        t = int(time.time())
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime(t))


def console_ts(ts: Optional[int] = None) -> str:
    try:
        t = int(ts if ts is not None else time.time())
    except Exception:
        t = int(time.time())
    return time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(t))


def chat_log_path(ts: Optional[int] = None) -> str:
    base = CHAT_LOG_DIR or os.path.join(os.path.dirname(DB_PATH or __file__), "chat_logs")
    os.makedirs(base, exist_ok=True)
    try:
        t = int(ts if ts is not None else time.time())
    except Exception:
        t = int(time.time())
    day = time.strftime("%Y-%m-%d", time.gmtime(t))
    return os.path.join(base, f"chat_{day}.jsonl")


def write_chat_log(channel: str, sender_display_name: str, sender_public_handle: str, character_id: str, message: str,
                   guild_id: Optional[int] = None, guild_name: str = "", guild_rank: str = "", guild_tag: str = "", ts: Optional[int] = None):
    """Append-only JSONL chat audit log. Best-effort; chat should not fail if disk logging fails."""
    try:
        now = int(ts if ts is not None else time.time())
        record = {
            "timestamp_utc": utc_timestamp(now),
            "timestamp_unix": now,
            "channel": channel or "",
            "sender_display_name": sender_display_name or "",
            "sender_public_handle": sender_public_handle or "",
            "sender_character_id": character_id or "",
            "guild_id": guild_id,
            "guild_name": guild_name or "",
            "guild_rank": guild_rank or "",
            "guild_tag": guild_tag or "",
            "message": message or "",
        }
        with open(chat_log_path(now), "a", encoding="utf-8") as f:
            f.write(json.dumps(record, ensure_ascii=False, separators=(",", ":")) + "\n")
    except Exception as ex:
        print(f"[Social][chat-log] Write failed: {ex}", flush=True)


def print_chat_to_console(channel: str, sender: str, message: str, ts: Optional[int] = None, meta: str = ""):
    label = f"[{console_ts(ts)}][{channel}]"
    if meta:
        label += f"[{meta}]"
    print(f"{label} {sender}: {message}", flush=True)


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    daemon_threads = True


def db_conn():
    if not DB_PATH:
        raise RuntimeError("DB_PATH not initialized")
    return sqlite3.connect(DB_PATH, timeout=10)


def backup_db_before_migration(path: str, build_label: str = "v0.11.0-base"):
    """Best-effort safety backup before schema updates. Never blocks server startup."""
    try:
        if not path or not os.path.exists(path) or os.path.getsize(path) <= 0:
            return
        backup_dir = os.path.join(os.path.dirname(path), "social_backups")
        os.makedirs(backup_dir, exist_ok=True)
        stamp = time.strftime("%Y%m%d_%H%M%S")
        base = os.path.splitext(os.path.basename(path))[0] or "social"
        backup_path = os.path.join(backup_dir, f"{base}_{stamp}_before_{build_label}.db")
        shutil.copy2(path, backup_path)
        print(f"[Social] Safety backup created before migration: {backup_path}", flush=True)
    except Exception as ex:
        print(f"[Social] Database backup skipped: {ex}", flush=True)


def note_schema_migration(con, name: str):
    now = int(time.time())
    con.execute("""
        CREATE TABLE IF NOT EXISTS schema_migrations (
            name TEXT PRIMARY KEY,
            applied_at INTEGER NOT NULL,
            build TEXT NOT NULL
        )
    """)
    existing = con.execute("SELECT build, applied_at FROM schema_migrations WHERE name=?", (name,)).fetchone()
    if existing:
        print(f"[Social][migration] {name}: already applied by {existing[0]} at {existing[1]}", flush=True)
        return False
    con.execute(
        "INSERT INTO schema_migrations(name, applied_at, build) VALUES(?,?,?)",
        (name, now, VERSION),
    )
    print(f"[Social][migration] {name}: recorded for build {VERSION}", flush=True)
    return True


def init_db(path: str):
    global DB_PATH, CHAT_LOG_DIR, USER_REPORT_DIR
    DB_PATH = path
    os.makedirs(os.path.dirname(path), exist_ok=True)
    CHAT_LOG_DIR = os.path.join(os.path.dirname(path), "chat_logs")
    USER_REPORT_DIR = os.path.join(os.path.dirname(path), "User Reports")
    os.makedirs(CHAT_LOG_DIR, exist_ok=True)
    os.makedirs(USER_REPORT_DIR, exist_ok=True)
    moderation.set_db_path(path)
    print(f"[Social][migration] Starting non-destructive schema check for {path}", flush=True)
    backup_db_before_migration(path)
    moderation.ensure_moderation_schema(path)
    with db_lock, db_conn() as con:
        con.execute("PRAGMA journal_mode=WAL")
        ensure_account_character_schema(con)
        ensure_official_online_save_schema(con)
        ensure_ranked_schema(con)
        con.execute("""
            CREATE TABLE IF NOT EXISTS guilds (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                tag TEXT NOT NULL DEFAULT '',
                owner_character_id TEXT NOT NULL,
                owner_display_name TEXT NOT NULL,
                owner_public_handle TEXT NOT NULL,
                created_at INTEGER NOT NULL
            )
        """)
        cols = [str(row[1]) for row in con.execute("PRAGMA table_info(guilds)").fetchall()]
        if "tag" not in cols:
            con.execute("ALTER TABLE guilds ADD COLUMN tag TEXT NOT NULL DEFAULT ''")
        con.execute("""
            CREATE TABLE IF NOT EXISTS guild_members (
                guild_id INTEGER NOT NULL,
                character_id TEXT NOT NULL,
                display_name TEXT NOT NULL,
                public_handle TEXT NOT NULL,
                rank TEXT NOT NULL,
                joined_at INTEGER NOT NULL,
                PRIMARY KEY (guild_id, character_id)
            )
        """)
        con.execute("""
            CREATE TABLE IF NOT EXISTS guild_invites (
                guild_id INTEGER NOT NULL,
                invited_character_id TEXT NOT NULL,
                invited_display_name TEXT NOT NULL,
                invited_public_handle TEXT NOT NULL,
                invited_by_character_id TEXT NOT NULL,
                invited_by_display_name TEXT NOT NULL,
                invited_by_public_handle TEXT NOT NULL,
                created_at INTEGER NOT NULL,
                PRIMARY KEY (guild_id, invited_character_id)
            )
        """)
        con.execute("""
            CREATE TABLE IF NOT EXISTS player_reports (
                report_id INTEGER PRIMARY KEY AUTOINCREMENT,
                created_at INTEGER NOT NULL,
                status TEXT NOT NULL DEFAULT 'Open',
                reporter_character_id TEXT NOT NULL DEFAULT '',
                reporter_public_handle TEXT NOT NULL DEFAULT '',
                reporter_account_uuid TEXT NOT NULL DEFAULT '',
                reporter_steam_id64 TEXT NOT NULL DEFAULT '',
                reported_character_id TEXT NOT NULL DEFAULT '',
                reported_public_handle TEXT NOT NULL DEFAULT '',
                reported_account_uuid TEXT NOT NULL DEFAULT '',
                reported_steam_id64 TEXT NOT NULL DEFAULT '',
                reported_display_name TEXT NOT NULL DEFAULT '',
                reason TEXT NOT NULL DEFAULT '',
                details TEXT NOT NULL DEFAULT '',
                chat_log_reference TEXT NOT NULL DEFAULT '',
                report_file TEXT NOT NULL DEFAULT '',
                reviewed_by TEXT NOT NULL DEFAULT '',
                reviewed_at INTEGER NOT NULL DEFAULT 0,
                review_note TEXT NOT NULL DEFAULT ''
            )
        """)
        con.execute("CREATE INDEX IF NOT EXISTS idx_player_reports_status_created ON player_reports(status, created_at)")
        con.execute("CREATE INDEX IF NOT EXISTS idx_player_reports_reported ON player_reports(reported_public_handle, created_at)")
        note_schema_migration(con, "guild_tag_and_ranked_safe_migration_v0_8_3")
        note_schema_migration(con, "account_bans_safety_migration_v0_8_3")
        note_schema_migration(con, "player_reports_v0_8_4")
        note_schema_migration(con, "official_online_saves_v0_0_1")
        con.commit()
    print(f"[Social][migration] Schema check complete. No destructive migrations were run.", flush=True)
    print(f"[Social] Chat logs: {CHAT_LOG_DIR}", flush=True)
    print(f"[Social] User reports: {USER_REPORT_DIR}", flush=True)


def ensure_official_online_save_schema(con):
    con.execute("""
        CREATE TABLE IF NOT EXISTS official_online_saves (
            account_uuid TEXT NOT NULL,
            slot_index INTEGER NOT NULL,
            save_json TEXT NOT NULL DEFAULT '',
            display_name TEXT NOT NULL DEFAULT '',
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL,
            PRIMARY KEY (account_uuid, slot_index)
        )
    """)
    con.execute("CREATE INDEX IF NOT EXISTS idx_official_online_saves_account ON official_online_saves(account_uuid, slot_index)")

    # v0.9.3/v0.9.5/v0.9.6: mirror important vanilla SaveData fields into columns for diagnostics,
    # future admin tools, and safer server-side ownership. The full save_json remains the
    # source of truth for loading a slot; these columns let us verify at a glance that
    # player/best-friend appearance and core state were actually received by the server.
    existing_cols = {str(r[1]) for r in con.execute("PRAGMA table_info(official_online_saves)").fetchall()}
    columns_to_add = [
        ("player_name", "TEXT NOT NULL DEFAULT ''"),
        ("player_design", "INTEGER NOT NULL DEFAULT 0"),
        ("player_color1", "INTEGER NOT NULL DEFAULT 0"),
        ("player_color2", "INTEGER NOT NULL DEFAULT 0"),
        ("best_friend_name", "TEXT NOT NULL DEFAULT ''"),
        ("best_friend_design", "INTEGER NOT NULL DEFAULT 0"),
        ("best_friend_color1", "INTEGER NOT NULL DEFAULT 0"),
        ("best_friend_color2", "INTEGER NOT NULL DEFAULT 0"),
        ("version", "INTEGER NOT NULL DEFAULT 0"),
        ("sats", "INTEGER NOT NULL DEFAULT 0"),
        ("save_counter", "INTEGER NOT NULL DEFAULT 0"),
        ("cur_location", "TEXT NOT NULL DEFAULT ''"),
        ("cur_unique_id_counter", "INTEGER NOT NULL DEFAULT 0"),
        ("world_block", "INTEGER NOT NULL DEFAULT 0"),
        ("day", "INTEGER NOT NULL DEFAULT 0"),
        ("day_count", "INTEGER NOT NULL DEFAULT 0"),
        ("battles_won", "INTEGER NOT NULL DEFAULT 0"),
        ("spell_unlocked_json", "TEXT NOT NULL DEFAULT ''"),
        ("battle_speed", "INTEGER NOT NULL DEFAULT 0"),
    ]
    for col, ddl in columns_to_add:
        if col not in existing_cols:
            con.execute(f"ALTER TABLE official_online_saves ADD COLUMN {col} {ddl}")
    con.execute("CREATE INDEX IF NOT EXISTS idx_official_online_saves_player ON official_online_saves(account_uuid, player_name)")
    try:
        note_schema_migration(con, "official_online_saves_game_update_v0_9_5")
    except Exception:
        pass


def _save_int(payload, key, default=0):
    try:
        return int(payload.get(key, default) or 0) if isinstance(payload, dict) else int(default)
    except Exception:
        return int(default)


def extract_official_save_metadata(save_json: str) -> dict:
    meta = {
        'player_name': 'Player',
        'player_design': 0,
        'player_color1': 0,
        'player_color2': 0,
        'best_friend_name': '',
        'best_friend_design': 0,
        'best_friend_color1': 0,
        'best_friend_color2': 0,
        'version': 0,
        'sats': 0,
        'save_counter': 0,
        'cur_location': '',
        'cur_unique_id_counter': 0,
        'world_block': 0,
        'day': 0,
        'day_count': 0,
        'battles_won': 0,
        'spell_unlocked_json': '',
        'battle_speed': 0,
    }
    try:
        payload = json.loads(save_json or '{}')
        if not isinstance(payload, dict):
            return meta
        meta['player_name'] = sanitize_name(str(payload.get('playerName') or payload.get('display_name') or 'Player'))
        meta['player_design'] = _save_int(payload, 'playerDesign')
        meta['player_color1'] = _save_int(payload, 'playerColor1')
        meta['player_color2'] = _save_int(payload, 'playerColor2')
        meta['best_friend_name'] = sanitize_name(str(payload.get('bestFriendName') or ''))
        meta['best_friend_design'] = _save_int(payload, 'bestFriendDesign')
        meta['best_friend_color1'] = _save_int(payload, 'bestFriendColor1')
        meta['best_friend_color2'] = _save_int(payload, 'bestFriendColor2')
        meta['version'] = _save_int(payload, 'version')
        meta['sats'] = _save_int(payload, 'sats')
        meta['save_counter'] = _save_int(payload, 'saveCounter')
        meta['cur_location'] = str(payload.get('curLocation') or '')[:120]
        meta['cur_unique_id_counter'] = _save_int(payload, 'curUniqueIDCounter')
        meta['world_block'] = _save_int(payload, 'worldBlock')
        meta['day'] = _save_int(payload, 'day')
        meta['day_count'] = _save_int(payload, 'dayCount')
        meta['battles_won'] = _save_int(payload, 'battlesWon')
        meta['battle_speed'] = _save_int(payload, 'battleSpeed')
        try:
            spell_unlocked = payload.get('spellUnlocked')
            meta['spell_unlocked_json'] = json.dumps(spell_unlocked, separators=(',', ':')) if isinstance(spell_unlocked, (list, tuple, dict)) else ''
        except Exception:
            meta['spell_unlocked_json'] = ''
    except Exception:
        pass
    return meta


def resolve_official_account_from_session(session_token: str, source_ip: str = None) -> Tuple[str, str]:
    session = resolve_account_session(session_token, source_ip)
    if not session:
        raise RuntimeError('Steam account session was not accepted by the server. Please reconnect with Steam.')
    steam_account_id, steam_username, steam_id64, steam_display_name = session
    active_ban = moderation.get_active_ban(steam_id64=steam_id64)
    if active_ban:
        reason = active_ban.get('reason') or 'This Steam account is banned from MMOnsterpatch online services.'
        raise RuntimeError('This Steam account is banned from MMOnsterpatch online services. Reason: ' + str(reason))
    with db_lock, db_conn() as con:
        ensure_official_online_save_schema(con)
        account_uuid = get_or_create_account(con, steam_id64, steam_account_id, steam_display_name or steam_username)
        con.commit()
        return account_uuid, str(steam_id64 or '')


def official_save_slots_payload(session_token: str, source_ip: str = None) -> str:
    account_uuid, _steam_id64 = resolve_official_account_from_session(session_token, source_ip)
    slots = []
    with db_lock, db_conn() as con:
        ensure_official_online_save_schema(con)
        rows = {int(r[0]): r for r in con.execute(
            "SELECT slot_index, save_json, display_name, updated_at FROM official_online_saves WHERE account_uuid=?",
            (account_uuid,),
        ).fetchall()}
        for i in range(6):
            r = rows.get(i)
            if r and str(r[1] or '').strip():
                slots.append({
                    'slot': i,
                    'occupied': 1,
                    'save_json': str(r[1] or ''),
                    'display_name': str(r[2] or ''),
                    'updated_at': int(r[3] or 0),
                })
            else:
                slots.append({'slot': i, 'occupied': 0, 'save_json': '', 'display_name': '', 'updated_at': 0})
    return json.dumps({'slots': slots}, separators=(',', ':'))



def official_save_slots_v2_lines(session_token: str, source_ip: str = None) -> Tuple[list, str, int]:
    account_uuid, steam_id64 = resolve_official_account_from_session(session_token, source_ip)
    lines = []
    occupied = 0
    with db_lock, db_conn() as con:
        ensure_official_online_save_schema(con)
        rows = {int(r[0]): r for r in con.execute(
            "SELECT slot_index, save_json, display_name, updated_at FROM official_online_saves WHERE account_uuid=?",
            (account_uuid,),
        ).fetchall()}
        for i in range(6):
            r = rows.get(i)
            if r and str(r[1] or '').strip():
                occupied += 1
                save_json = str(r[1] or '')
                display_name = str(r[2] or '')
                updated_at = int(r[3] or 0)
                lines.append(f"OFFICIAL_SAVE_SLOT|{i}|1|{b64_encode(display_name)}|{updated_at}|{b64_encode(save_json)}")
            else:
                lines.append(f"OFFICIAL_SAVE_SLOT|{i}|0||0|")
    return lines, account_uuid, occupied

def write_official_save_slot(session_token: str, slot_index: int, save_json: str, source_ip: str = None) -> Tuple[int, str]:
    account_uuid, _steam_id64 = resolve_official_account_from_session(session_token, source_ip)
    slot_index = max(0, min(5, int(slot_index)))
    save_json = save_json or ''
    if not save_json.strip():
        raise RuntimeError('Empty save payload was rejected.')
    meta = extract_official_save_metadata(save_json)
    display_name = meta.get('player_name') or 'Player'
    now = int(time.time())
    with db_lock, db_conn() as con:
        ensure_official_online_save_schema(con)
        existing = con.execute(
            "SELECT created_at FROM official_online_saves WHERE account_uuid=? AND slot_index=?",
            (account_uuid, slot_index),
        ).fetchone()
        created_at = int(existing[0]) if existing else now
        con.execute(
            """
            INSERT OR REPLACE INTO official_online_saves(
                account_uuid, slot_index, save_json, display_name, created_at, updated_at,
                player_name, player_design, player_color1, player_color2,
                best_friend_name, best_friend_design, best_friend_color1, best_friend_color2,
                version, sats, save_counter, cur_location, cur_unique_id_counter, world_block, day, day_count,
                battles_won, spell_unlocked_json, battle_speed
            ) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
            """,
            (
                account_uuid, slot_index, save_json, display_name, created_at, now,
                meta['player_name'], meta['player_design'], meta['player_color1'], meta['player_color2'],
                meta['best_friend_name'], meta['best_friend_design'], meta['best_friend_color1'], meta['best_friend_color2'],
                meta['version'], meta['sats'], meta['save_counter'], meta['cur_location'],
                meta['cur_unique_id_counter'], meta['world_block'], meta['day'], meta['day_count'],
                meta['battles_won'], meta['spell_unlocked_json'], meta['battle_speed'],
            ),
        )
        con.commit()
    return slot_index, display_name


def archived_characters_dir() -> str:
    if OFFICIAL_ARCHIVE_DIR:
        path = OFFICIAL_ARCHIVE_DIR
    else:
        base = os.path.dirname(DB_PATH or __file__)
        path = os.path.join(base, "Archived Characters")
    os.makedirs(path, exist_ok=True)
    return path


def archive_official_save_record(account_uuid: str, steam_id64: str, slot_index: int, row, active_user_rows, reason: str) -> str:
    now = int(time.time())
    display_name = str(row[2] or "Player") if row else "Empty"
    safe_name = re.sub(r"[^A-Za-z0-9_.-]+", "_", display_name).strip("_") or "Player"
    filename = f"{now}_slot{int(slot_index)}_{safe_name}.json"
    path = os.path.join(archived_characters_dir(), filename)
    record = {
        "archive_version": 1,
        "archived_at_unix": now,
        "archived_at_utc": utc_timestamp(now),
        "reason": reason or "official_online_save_deleted",
        "account_uuid": account_uuid or "",
        "steam_id64": steam_id64 or "",
        "slot_index": int(slot_index),
        "display_name": display_name,
        "official_save": {
            "save_json": str(row[1] or "") if row else "",
            "created_at": int(row[4] or 0) if row and len(row) >= 5 else 0,
            "updated_at": int(row[3] or 0) if row else 0,
            "metadata": extract_official_save_metadata(str(row[1] or "") if row else ""),
        },
        "linked_characters": [
            {
                "character_id": str(r[0]),
                "display_name": str(r[1] or ""),
                "public_handle": str(r[2] or ""),
                "public_serial": int(r[3] or 0),
                "created_at": int(r[4] or 0),
                "last_seen_at": int(r[5] or 0),
            }
            for r in (active_user_rows or [])
        ],
    }
    with open(path, "w", encoding="utf-8") as f:
        json.dump(record, f, ensure_ascii=False, indent=2)
        f.write("\n")
    return path


def remove_live_character_links_for_archive(con, character_id: str):
    # This is intentionally conservative. It removes live membership/invites so a deleted
    # online save cannot keep guild/social state attached to a future character in the slot.
    membership = con.execute("SELECT guild_id, rank FROM guild_members WHERE character_id=?", (character_id,)).fetchone()
    if membership:
        gid = int(membership[0])
        rank = str(membership[1] or "")
        if rank == "Leader":
            replacement = con.execute(
                "SELECT character_id, display_name, public_handle FROM guild_members WHERE guild_id=? AND character_id<>? ORDER BY joined_at ASC LIMIT 1",
                (gid, character_id),
            ).fetchone()
            if replacement:
                new_owner_id, new_owner_display, new_owner_handle = str(replacement[0]), str(replacement[1]), str(replacement[2])
                con.execute("UPDATE guild_members SET rank='Leader' WHERE guild_id=? AND character_id=?", (gid, new_owner_id))
                con.execute(
                    "UPDATE guilds SET owner_character_id=?, owner_display_name=?, owner_public_handle=? WHERE id=?",
                    (new_owner_id, new_owner_display, new_owner_handle, gid),
                )
                con.execute("DELETE FROM guild_members WHERE guild_id=? AND character_id=?", (gid, character_id))
            else:
                con.execute("DELETE FROM guild_invites WHERE guild_id=?", (gid,))
                con.execute("DELETE FROM guild_members WHERE guild_id=?", (gid,))
                con.execute("DELETE FROM guilds WHERE id=?", (gid,))
        else:
            con.execute("DELETE FROM guild_members WHERE guild_id=? AND character_id=?", (gid, character_id))
    con.execute("DELETE FROM guild_invites WHERE invited_character_id=? OR invited_by_character_id=?", (character_id, character_id))


def delete_official_save_slot(session_token: str, slot_index: int, source_ip: str = None) -> Tuple[int, str, str]:
    account_uuid, steam_id64 = resolve_official_account_from_session(session_token, source_ip)
    slot_index = max(0, min(5, int(slot_index)))
    now = int(time.time())
    with db_lock, db_conn() as con:
        ensure_official_online_save_schema(con)
        row = con.execute(
            "SELECT slot_index, save_json, display_name, updated_at, created_at FROM official_online_saves WHERE account_uuid=? AND slot_index=?",
            (account_uuid, slot_index),
        ).fetchone()
        active_user_rows = con.execute(
            """
            SELECT character_id, display_name, public_handle, public_serial, created_at, last_seen_at
            FROM users
            WHERE account_uuid=? AND slot_index=? AND status='active'
            """,
            (account_uuid, slot_index),
        ).fetchall()
        if not row and not active_user_rows:
            con.commit()
            return slot_index, "Empty", ""
        archive_path = archive_official_save_record(account_uuid, steam_id64, slot_index, row, active_user_rows, "official_online_save_deleted")
        for user_row in active_user_rows:
            cid = str(user_row[0])
            remove_live_character_links_for_archive(con, cid)
            snapshot_b64 = b64_encode(str(row[1] or "") if row else "")
            con.execute(
                """
                UPDATE users
                SET status='archived', archived_at=?, archive_reason=?, recovery_snapshot_b64=?
                WHERE character_id=? AND status='active'
                """,
                (now, "official_online_save_deleted", snapshot_b64, cid),
            )
        con.execute("DELETE FROM official_online_saves WHERE account_uuid=? AND slot_index=?", (account_uuid, slot_index))
        con.commit()
    display_name = str(row[2] or "Empty") if row else "Empty"
    return slot_index, display_name, archive_path

def rank_for_rp(rp: int) -> str:
    try:
        rp = int(rp)
    except Exception:
        rp = 0
    rp = max(0, min(RANKED_MAX_RP, rp))
    if rp >= 1000:
        return "S"
    if rp >= 800:
        return "A"
    if rp >= 600:
        return "B"
    if rp >= 400:
        return "C"
    if rp >= 200:
        return "D"
    return "E"


def ensure_ranked_schema(con):
    note_schema_migration(con, "ranked_rules_foundation_v0_8_2")
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_seasons (
            season_id TEXT PRIMARY KEY,
            season_name TEXT NOT NULL,
            starts_at INTEGER NOT NULL,
            ends_at INTEGER NOT NULL,
            status TEXT NOT NULL,
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL
        )
    """)
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_profiles (
            season_id TEXT NOT NULL,
            character_id TEXT NOT NULL,
            account_uuid TEXT NOT NULL,
            rp INTEGER NOT NULL DEFAULT 0,
            rank TEXT NOT NULL DEFAULT 'E',
            wins INTEGER NOT NULL DEFAULT 0,
            losses INTEGER NOT NULL DEFAULT 0,
            highest_rp INTEGER NOT NULL DEFAULT 0,
            highest_rank TEXT NOT NULL DEFAULT 'E',
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL,
            PRIMARY KEY (season_id, character_id)
        )
    """)
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_profiles_character ON ranked_profiles(character_id)")
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_profiles_account ON ranked_profiles(account_uuid, season_id)")

    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_rules (
            season_id TEXT PRIMARY KEY,
            required_team_size INTEGER NOT NULL,
            min_mon_level INTEGER NOT NULL,
            max_rank_gap INTEGER NOT NULL,
            max_rp INTEGER NOT NULL,
            actions_enabled INTEGER NOT NULL DEFAULT 0,
            repeat_full_matches_per_day INTEGER NOT NULL DEFAULT 3,
            repeat_half_matches_per_day INTEGER NOT NULL DEFAULT 1,
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL
        )
    """)
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_rp_rules (
            season_id TEXT NOT NULL,
            rank_code TEXT NOT NULL,
            tier INTEGER NOT NULL,
            min_rp INTEGER NOT NULL,
            max_rp INTEGER NOT NULL,
            base_win INTEGER NOT NULL,
            base_loss INTEGER NOT NULL,
            PRIMARY KEY (season_id, rank_code)
        )
    """)
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_performance_modifiers (
            season_id TEXT NOT NULL,
            winner_remaining_mons INTEGER NOT NULL,
            winner_rp_adjust INTEGER NOT NULL,
            loser_loss_adjust INTEGER NOT NULL,
            PRIMARY KEY (season_id, winner_remaining_mons)
        )
    """)
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_matches (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            season_id TEXT NOT NULL,
            status TEXT NOT NULL,
            winner_character_id TEXT,
            loser_character_id TEXT,
            winner_rank_before TEXT,
            loser_rank_before TEXT,
            winner_rp_before INTEGER,
            loser_rp_before INTEGER,
            winner_rp_delta INTEGER,
            loser_rp_delta INTEGER,
            winner_rp_after INTEGER,
            loser_rp_after INTEGER,
            winner_starting_mons INTEGER,
            loser_starting_mons INTEGER,
            winner_remaining_mons INTEGER,
            loser_defeated_mons INTEGER,
            formula_version TEXT NOT NULL DEFAULT 'season0-draft-v1',
            created_at INTEGER NOT NULL,
            resolved_at INTEGER
        )
    """)
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_matches_season ON ranked_matches(season_id, created_at)")
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_matches_winner ON ranked_matches(winner_character_id)")
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_matches_loser ON ranked_matches(loser_character_id)")
    con.execute("""
        CREATE TABLE IF NOT EXISTS ranked_audit_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            season_id TEXT NOT NULL,
            character_id TEXT,
            event_type TEXT NOT NULL,
            details TEXT NOT NULL DEFAULT '',
            created_at INTEGER NOT NULL
        )
    """)
    con.execute("CREATE INDEX IF NOT EXISTS idx_ranked_audit_character ON ranked_audit_log(character_id, created_at)")

    now = int(time.time())
    con.execute(
        """
        INSERT INTO ranked_seasons(season_id, season_name, starts_at, ends_at, status, created_at, updated_at)
        VALUES(?,?,?,?,?,?,?)
        ON CONFLICT(season_id) DO UPDATE SET
            season_name=excluded.season_name,
            starts_at=excluded.starts_at,
            ends_at=excluded.ends_at,
            status=excluded.status,
            updated_at=excluded.updated_at
        """,
        (RANKED_SEASON0_ID, RANKED_SEASON0_NAME, RANKED_SEASON0_STARTS_AT, RANKED_SEASON0_ENDS_AT, "planned", now, now),
    )
    con.execute(
        """
        INSERT INTO ranked_rules(
            season_id, required_team_size, min_mon_level, max_rank_gap, max_rp, actions_enabled,
            repeat_full_matches_per_day, repeat_half_matches_per_day, created_at, updated_at
        ) VALUES(?,?,?,?,?,?,?,?,?,?)
        ON CONFLICT(season_id) DO UPDATE SET
            required_team_size=excluded.required_team_size,
            min_mon_level=excluded.min_mon_level,
            max_rank_gap=excluded.max_rank_gap,
            max_rp=excluded.max_rp,
            actions_enabled=excluded.actions_enabled,
            repeat_full_matches_per_day=excluded.repeat_full_matches_per_day,
            repeat_half_matches_per_day=excluded.repeat_half_matches_per_day,
            updated_at=excluded.updated_at
        """,
        (
            RANKED_SEASON0_ID,
            RANKED_REQUIRED_TEAM_SIZE,
            RANKED_MIN_MON_LEVEL,
            RANKED_MAX_RANK_GAP,
            RANKED_MAX_RP,
            RANKED_ACTIONS_ENABLED,
            RANKED_REPEAT_FULL_MATCHES_PER_DAY,
            RANKED_REPEAT_HALF_MATCHES_PER_DAY,
            now,
            now,
        ),
    )
    for rank_code, tier, min_rp, max_rp, base_win, base_loss in RANKED_RP_RULES:
        con.execute(
            """
            INSERT INTO ranked_rp_rules(season_id, rank_code, tier, min_rp, max_rp, base_win, base_loss)
            VALUES(?,?,?,?,?,?,?)
            ON CONFLICT(season_id, rank_code) DO UPDATE SET
                tier=excluded.tier,
                min_rp=excluded.min_rp,
                max_rp=excluded.max_rp,
                base_win=excluded.base_win,
                base_loss=excluded.base_loss
            """,
            (RANKED_SEASON0_ID, rank_code, tier, min_rp, max_rp, base_win, base_loss),
        )
    for winner_remaining_mons, winner_adjust, loser_adjust in RANKED_PERFORMANCE_MODIFIERS:
        con.execute(
            """
            INSERT INTO ranked_performance_modifiers(season_id, winner_remaining_mons, winner_rp_adjust, loser_loss_adjust)
            VALUES(?,?,?,?)
            ON CONFLICT(season_id, winner_remaining_mons) DO UPDATE SET
                winner_rp_adjust=excluded.winner_rp_adjust,
                loser_loss_adjust=excluded.loser_loss_adjust
            """,
            (RANKED_SEASON0_ID, winner_remaining_mons, winner_adjust, loser_adjust),
        )

def get_ranked_season(con, season_id: str = RANKED_SEASON0_ID):
    row = con.execute(
        "SELECT season_id, season_name, starts_at, ends_at, status FROM ranked_seasons WHERE season_id=?",
        (season_id,),
    ).fetchone()
    if row:
        return row
    ensure_ranked_schema(con)
    return con.execute(
        "SELECT season_id, season_name, starts_at, ends_at, status FROM ranked_seasons WHERE season_id=?",
        (season_id,),
    ).fetchone()


def ensure_ranked_profile(con, character_id: str, account_uuid: str, season_id: str = RANKED_SEASON0_ID):
    if not character_id:
        return None
    account_uuid = account_uuid or ''
    season = get_ranked_season(con, season_id)
    if not season:
        return None
    now = int(time.time())
    con.execute(
        """
        INSERT OR IGNORE INTO ranked_profiles(
            season_id, character_id, account_uuid, rp, rank, wins, losses, highest_rp, highest_rank, created_at, updated_at
        ) VALUES(?,?,?,?,?,?,?,?,?,?,?)
        """,
        (season_id, character_id, account_uuid, 0, "E", 0, 0, 0, "E", now, now),
    )
    row = con.execute(
        """
        SELECT rp, rank, wins, losses, highest_rp, highest_rank
        FROM ranked_profiles
        WHERE season_id=? AND character_id=?
        """,
        (season_id, character_id),
    ).fetchone()
    if not row:
        return None
    rp = max(0, min(RANKED_MAX_RP, int(row[0] or 0)))
    expected_rank = rank_for_rp(rp)
    highest_rp = max(0, min(RANKED_MAX_RP, int(row[4] or 0)))
    expected_highest_rank = rank_for_rp(highest_rp)
    if str(row[1] or '') != expected_rank or str(row[5] or '') != expected_highest_rank:
        con.execute(
            "UPDATE ranked_profiles SET rp=?, rank=?, highest_rp=?, highest_rank=?, updated_at=? WHERE season_id=? AND character_id=?",
            (rp, expected_rank, highest_rp, expected_highest_rank, now, season_id, character_id),
        )
        row = (rp, expected_rank, int(row[2] or 0), int(row[3] or 0), highest_rp, expected_highest_rank)
    season_id, season_name, starts_at, _ends_at, status = season
    return {
        "season_id": str(season_id),
        "season_name": str(season_name),
        "status": str(status),
        "starts_at": int(starts_at),
        "rp": int(row[0] or 0),
        "rank": str(row[1] or 'E'),
        "wins": int(row[2] or 0),
        "losses": int(row[3] or 0),
        "highest_rp": int(row[4] or 0),
        "highest_rank": str(row[5] or 'E'),
    }


def get_ranked_rules(con, season_id: str = RANKED_SEASON0_ID):
    ensure_ranked_schema(con)
    row = con.execute(
        """
        SELECT required_team_size, min_mon_level, max_rank_gap, max_rp, actions_enabled,
               repeat_full_matches_per_day, repeat_half_matches_per_day
        FROM ranked_rules
        WHERE season_id=?
        """,
        (season_id,),
    ).fetchone()
    if not row:
        return {
            "required_team_size": RANKED_REQUIRED_TEAM_SIZE,
            "min_mon_level": RANKED_MIN_MON_LEVEL,
            "max_rank_gap": RANKED_MAX_RANK_GAP,
            "max_rp": RANKED_MAX_RP,
            "actions_enabled": RANKED_ACTIONS_ENABLED,
            "repeat_full_matches_per_day": RANKED_REPEAT_FULL_MATCHES_PER_DAY,
            "repeat_half_matches_per_day": RANKED_REPEAT_HALF_MATCHES_PER_DAY,
        }
    return {
        "required_team_size": int(row[0] or RANKED_REQUIRED_TEAM_SIZE),
        "min_mon_level": int(row[1] or RANKED_MIN_MON_LEVEL),
        "max_rank_gap": int(row[2] or RANKED_MAX_RANK_GAP),
        "max_rp": int(row[3] or RANKED_MAX_RP),
        "actions_enabled": int(row[4] or 0),
        "repeat_full_matches_per_day": int(row[5] or RANKED_REPEAT_FULL_MATCHES_PER_DAY),
        "repeat_half_matches_per_day": int(row[6] or RANKED_REPEAT_HALF_MATCHES_PER_DAY),
    }


def get_rank_tier(con, rank_code: str, season_id: str = RANKED_SEASON0_ID) -> int:
    rank_code = (rank_code or "E").upper()
    row = con.execute(
        "SELECT tier FROM ranked_rp_rules WHERE season_id=? AND rank_code=?",
        (season_id, rank_code),
    ).fetchone()
    if row:
        return int(row[0] or 0)
    for code, tier, _min, _max, _win, _loss in RANKED_RP_RULES:
        if code == rank_code:
            return int(tier)
    return 0


def ranked_rules_summary() -> str:
    # Human-readable summary for the client Ranked tab.
    return (
        f"Season 0 draft: {RANKED_REQUIRED_TEAM_SIZE}v{RANKED_REQUIRED_TEAM_SIZE}, "
        f"Lv. {RANKED_MIN_MON_LEVEL}+, max rank gap {RANKED_MAX_RANK_GAP}. "
        "Base RP uses rank difference; clean wins get a small bonus, close wins get a small reduction. "
        "Ranked battles are database-ready but disabled in this build."
    )


def calculate_ranked_delta_preview(winner_rank: str, loser_rank: str, winner_remaining_mons: int, season_id: str = RANKED_SEASON0_ID):
    """Draft RP formula for future ranked resolution. Not applied in this build."""
    winner_rank = (winner_rank or "E").upper()
    loser_rank = (loser_rank or "E").upper()
    rule_by_rank = {code: (tier, base_win, base_loss) for code, tier, _min, _max, base_win, base_loss in RANKED_RP_RULES}
    winner_tier, winner_base_win, _winner_base_loss = rule_by_rank.get(winner_rank, rule_by_rank["E"])
    loser_tier, _loser_base_win, loser_base_loss = rule_by_rank.get(loser_rank, rule_by_rank["E"])
    rank_diff = int(loser_tier) - int(winner_tier)
    winner_gain = int(winner_base_win) + (rank_diff * 2)
    loser_loss = int(loser_base_loss) + ((int(loser_tier) - int(winner_tier)) * 2)
    modifier = {remaining: (win_adjust, loss_adjust) for remaining, win_adjust, loss_adjust in RANKED_PERFORMANCE_MODIFIERS}
    win_adjust, loss_adjust = modifier.get(int(winner_remaining_mons), (0, 0))
    winner_gain = max(1, min(14, winner_gain + int(win_adjust)))
    loser_loss = max(1, min(14, loser_loss + int(loss_adjust)))
    return winner_gain, loser_loss


def send_ranked_profile(handler):
    if not getattr(handler, "character_id", ""):
        return
    try:
        with db_lock, db_conn() as con:
            ensure_ranked_schema(con)
            profile = ensure_ranked_profile(con, handler.character_id, getattr(handler, "account_uuid", "") or "")
            rules = get_ranked_rules(con, RANKED_SEASON0_ID)
            con.commit()
        if not profile:
            send_line(handler, f"GUILD_ERROR|{b64_encode('Ranked profile is not available yet.')}")
            return
        send_line(
            handler,
            "RANKED_PROFILE|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}".format(
                profile["season_id"],
                b64_encode(profile["season_name"]),
                profile["status"],
                int(profile["rp"]),
                profile["rank"],
                int(profile["wins"]),
                int(profile["losses"]),
                int(profile["highest_rp"]),
                profile["highest_rank"],
                int(rules.get("max_rp", RANKED_MAX_RP)),
                int(profile["starts_at"]),
                int(rules.get("required_team_size", RANKED_REQUIRED_TEAM_SIZE)),
                int(rules.get("min_mon_level", RANKED_MIN_MON_LEVEL)),
                int(rules.get("max_rank_gap", RANKED_MAX_RANK_GAP)),
                int(rules.get("actions_enabled", 0)),
                b64_encode(ranked_rules_summary()),
            ),
        )
    except Exception as ex:
        send_line(handler, f"GUILD_ERROR|{b64_encode('Ranked profile error: ' + str(ex))}")


def create_users_table(con):
    con.execute("""
        CREATE TABLE IF NOT EXISTS users (
            character_id TEXT PRIMARY KEY,
            account_uuid TEXT,
            slot_index INTEGER NOT NULL DEFAULT -1,
            secret_token TEXT NOT NULL,
            display_name TEXT NOT NULL,
            public_handle TEXT NOT NULL COLLATE NOCASE,
            public_serial INTEGER NOT NULL,
            slot_fingerprint TEXT NOT NULL DEFAULT '',
            slot_birth_key TEXT NOT NULL DEFAULT '',
            status TEXT NOT NULL DEFAULT 'active',
            recovery_snapshot_b64 TEXT NOT NULL DEFAULT '',
            created_at INTEGER NOT NULL,
            last_seen_at INTEGER NOT NULL,
            archived_at INTEGER NOT NULL DEFAULT 0,
            archive_reason TEXT NOT NULL DEFAULT ''
        )
    """)
    con.execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_public_handle_unique ON users(public_handle COLLATE NOCASE)")
    con.execute("CREATE INDEX IF NOT EXISTS idx_users_display_active ON users(display_name COLLATE NOCASE, status)")
    con.execute("CREATE INDEX IF NOT EXISTS idx_users_account_slot_status ON users(account_uuid, slot_index, status)")
    try:
        con.execute("DROP INDEX IF EXISTS idx_users_active_account_slot_unique")
    except Exception:
        pass
    con.execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_active_account_slot_birth_unique ON users(account_uuid, slot_index, slot_birth_key) WHERE status='active' AND slot_index >= 0")


def ensure_account_character_schema(con):
    con.execute("""
        CREATE TABLE IF NOT EXISTS accounts (
            account_uuid TEXT PRIMARY KEY,
            steam_id64 TEXT NOT NULL UNIQUE,
            steam_account_id INTEGER,
            steam_display_name TEXT,
            created_at INTEGER NOT NULL,
            last_seen_at INTEGER NOT NULL
        )
    """)

    row = con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='users'").fetchone()
    if not row:
        create_users_table(con)
        return

    cols = {r[1] for r in con.execute("PRAGMA table_info(users)").fetchall()}
    if 'account_uuid' in cols and 'slot_fingerprint' in cols and 'status' in cols:
        if 'slot_birth_key' not in cols:
            con.execute("ALTER TABLE users ADD COLUMN slot_birth_key TEXT NOT NULL DEFAULT ''")
        create_users_table(con)
        return

    backup_name = 'users_legacy_' + str(int(time.time()))
    con.execute(f"ALTER TABLE users RENAME TO {backup_name}")
    create_users_table(con)
    legacy_rows = con.execute(
        f"SELECT character_id, secret_token, display_name, public_handle, public_serial, created_at, last_seen_at FROM {backup_name}"
    ).fetchall()
    for r in legacy_rows:
        account_uuid = 'legacy_' + str(r[0])
        con.execute(
            """
            INSERT OR IGNORE INTO users(
                character_id, account_uuid, slot_index, secret_token, display_name, public_handle,
                public_serial, slot_fingerprint, status, recovery_snapshot_b64,
                created_at, last_seen_at, archived_at, archive_reason
            ) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?)
            """,
            (str(r[0]), account_uuid, -1, str(r[1]), str(r[2]), str(r[3]), int(r[4]), '', 'active', '', int(r[5]), int(r[6]), 0, ''),
        )
    con.commit()

def public_handle_exists(con, public_handle: str) -> bool:
    row = con.execute("SELECT 1 FROM users WHERE public_handle = ? COLLATE NOCASE LIMIT 1", (public_handle,)).fetchone()
    return row is not None


def allocate_random_public_handle(con, display_name: str) -> Tuple[int, str]:
    display_name = sanitize_name(display_name)
    for _ in range(100):
        serial = secrets.randbelow(10000)
        handle = make_public_handle(display_name, serial)
        if not public_handle_exists(con, handle):
            return serial, handle
    for serial in range(10000):
        handle = make_public_handle(display_name, serial)
        if not public_handle_exists(con, handle):
            return serial, handle
    raise RuntimeError("No public handle tags are available for that character name.")


def get_or_create_account(con, steam_id64: str, steam_account_id: int = None, steam_display_name: str = '') -> str:
    steam_id64 = (steam_id64 or '').strip()
    if not steam_id64:
        raise RuntimeError('Steam identity is required for account login.')
    now = int(time.time())
    row = con.execute("SELECT account_uuid FROM accounts WHERE steam_id64 = ?", (steam_id64,)).fetchone()
    if row:
        account_uuid = str(row[0])
        con.execute(
            "UPDATE accounts SET steam_account_id=COALESCE(?, steam_account_id), steam_display_name=?, last_seen_at=? WHERE account_uuid=?",
            (steam_account_id, steam_display_name or '', now, account_uuid),
        )
        return account_uuid
    account_uuid = 'acct_' + uuid.uuid4().hex
    con.execute(
        "INSERT INTO accounts(account_uuid, steam_id64, steam_account_id, steam_display_name, created_at, last_seen_at) VALUES(?,?,?,?,?,?)",
        (account_uuid, steam_id64, steam_account_id, steam_display_name or '', now, now),
    )
    return account_uuid


def resolve_account_session(session_token: str, source_ip: str = None) -> Optional[Tuple[int, str, str, str]]:
    session_token = (session_token or '').strip()
    if not session_token:
        return None
    try:
        import mmonsterpatch_tradingpost_core as trading_core
        conn = trading_core.get_db()
        try:
            row = trading_core.resolve_aio_session(conn, session_token, source_ip)
            if not row:
                return None
            account_id, username, steam_id64, display_name = row
            return int(account_id), str(username or ''), str(steam_id64 or ''), str(display_name or username or '')
        finally:
            try:
                conn.close()
            except Exception:
                pass
    except Exception as ex:
        print(f"[account-session-error] {ex}", flush=True)
        return None


def row_to_identity(row) -> Tuple[str, str, str, int, str, str, int, str, str, str]:
    return (
        str(row[0]), str(row[1]), str(row[2]), int(row[3]), str(row[4]),
        str(row[5] or ''), int(row[6] if row[6] is not None else -1), str(row[7] or ''), str(row[8] or 'active'),
        str(row[9] or '') if len(row) >= 10 else ''
    )


def create_character(con, account_uuid: str, slot_index: int, display_name: str, slot_fingerprint: str = '', recovery_snapshot: str = '', slot_birth_key: str = ''):
    display_name = sanitize_name(display_name)
    now = int(time.time())
    for _ in range(20):
        character_id = 'char_' + uuid.uuid4().hex
        secret_token = secrets.token_urlsafe(32)
        serial, public_handle = allocate_random_public_handle(con, display_name)
        try:
            con.execute(
                """
                INSERT INTO users(
                    character_id, account_uuid, slot_index, secret_token, display_name, public_handle,
                    public_serial, slot_fingerprint, slot_birth_key, status, recovery_snapshot_b64,
                    created_at, last_seen_at, archived_at, archive_reason
                ) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
                """,
                (character_id, account_uuid, int(slot_index), secret_token, display_name, public_handle,
                 int(serial), slot_fingerprint or '', slot_birth_key or '', 'active', b64_encode(recovery_snapshot or ''), now, now, 0, ''),
            )
            return character_id, secret_token, display_name, int(serial), public_handle, account_uuid, int(slot_index), slot_fingerprint or '', 'active', slot_birth_key or ''
        except sqlite3.IntegrityError:
            time.sleep(0.01)
    raise RuntimeError('Could not allocate a unique character identity.')


def archive_active_slot_character(con, character_id: str, reason: str, recovery_snapshot: str = ''):
    now = int(time.time())
    row = con.execute("SELECT recovery_snapshot_b64 FROM users WHERE character_id=?", (character_id,)).fetchone()
    existing_snapshot = str(row[0] or '') if row else ''
    snapshot_b64 = existing_snapshot or b64_encode(recovery_snapshot or '')
    con.execute(
        """
        UPDATE users
        SET status='archived', archived_at=?, archive_reason=?, recovery_snapshot_b64=?
        WHERE character_id=? AND status='active'
        """,
        (now, reason or 'archived', snapshot_b64, character_id),
    )


def register_or_resume_account_slot(session_token: str, slot_index: int, old_character_id: str, old_secret_token: str, display_name: str, slot_fingerprint: str, recovery_snapshot: str, slot_birth_key: str = '', source_ip: str = None):
    session = resolve_account_session(session_token, source_ip)
    if not session:
        raise RuntimeError('Steam account session was not accepted by the server. Please reconnect with Steam.')
    steam_account_id, steam_username, steam_id64, steam_display_name = session
    active_ban = moderation.get_active_ban(steam_id64=steam_id64)
    if active_ban:
        reason = active_ban.get("reason") or "This Steam account is banned from MMOnsterpatch online services."
        raise RuntimeError("This Steam account is banned from MMOnsterpatch online services. Reason: " + str(reason))
    display_name = sanitize_name(display_name)
    slot_index = max(0, min(5, int(slot_index)))
    now = int(time.time())
    with db_lock, db_conn() as con:
        account_uuid = get_or_create_account(con, steam_id64, steam_account_id, steam_display_name or steam_username)
        active = con.execute(
            """
            SELECT character_id, secret_token, display_name, public_serial, public_handle,
                   account_uuid, slot_index, slot_fingerprint, status, slot_birth_key
            FROM users
            WHERE account_uuid=? AND slot_index=? AND status='active' AND (slot_birth_key=? OR slot_birth_key='' OR ?='')
            ORDER BY CASE WHEN slot_birth_key=? THEN 0 WHEN slot_birth_key='' THEN 1 ELSE 2 END
            LIMIT 1
            """,
            (account_uuid, slot_index, slot_birth_key or '', slot_birth_key or '', slot_birth_key or ''),
        ).fetchone()
        if active:
            active_id = str(active[0])
            active_fp = str(active[7] or '')
            active_birth_key = str(active[9] or '') if len(active) >= 10 else ''
            old_display = str(active[2])

            if slot_birth_key and not active_birth_key:
                con.execute("UPDATE users SET slot_birth_key=? WHERE character_id=?", (slot_birth_key, active_id))
                active_birth_key = slot_birth_key

            # v0.8.4 guild identity safety fix:
            # The client-side slot fingerprint can legitimately drift because it is built from
            # mutable save data and from fields that may be unavailable during early connect.
            # Do NOT archive/create a new character only because the fingerprint changed.
            # Keep the active account+slot character_id stable so guild ownership/membership,
            # ranked data, reports, and public handle remain attached to the same identity.
            if display_name and display_name != old_display:
                con.execute("UPDATE users SET display_name=?, last_seen_at=? WHERE character_id=?", (display_name, now, active_id))
                con.execute("UPDATE guild_members SET display_name=? WHERE character_id=?", (display_name, active_id))
                con.execute("UPDATE guilds SET owner_display_name=? WHERE owner_character_id=?", (display_name, active_id))
                con.execute("UPDATE guild_invites SET invited_display_name=? WHERE invited_character_id=?", (display_name, active_id))
            else:
                con.execute("UPDATE users SET last_seen_at=? WHERE character_id=?", (now, active_id))

            if slot_fingerprint and active_fp != slot_fingerprint:
                con.execute(
                    "UPDATE users SET slot_fingerprint=?, recovery_snapshot_b64=?, last_seen_at=? WHERE character_id=?",
                    (slot_fingerprint, b64_encode(recovery_snapshot or ''), now, active_id),
                )
                try:
                    print(f"[identity] updated slot fingerprint without archiving character_id={active_id} slot={slot_index}", flush=True)
                except Exception:
                    pass

            con.commit()
            refreshed = con.execute(
                "SELECT character_id, secret_token, display_name, public_serial, public_handle, account_uuid, slot_index, slot_fingerprint, status, slot_birth_key FROM users WHERE character_id=?",
                (active_id,),
            ).fetchone()
            return row_to_identity(refreshed)

        # If the client presented a valid old character that belongs to this account/slot, allow it to resume even if the slot index mapping was missing.
        old_character_id = (old_character_id or '').strip()
        old_secret_token = (old_secret_token or '').strip()
        if old_character_id and old_secret_token:
            row = con.execute(
                """
                SELECT character_id, secret_token, display_name, public_serial, public_handle,
                       account_uuid, slot_index, slot_fingerprint, status, slot_birth_key
                FROM users
                WHERE character_id=? AND account_uuid=? AND status='active'
                """,
                (old_character_id, account_uuid),
            ).fetchone()
            if row and secrets.compare_digest(str(row[1]), old_secret_token):
                row_fp = str(row[7] or '')
                row_birth_key = str(row[9] or '') if len(row) >= 10 else ''
                if ((not slot_birth_key) or (not row_birth_key) or row_birth_key == slot_birth_key) and ((not slot_fingerprint) or row_fp == slot_fingerprint):
                    con.execute("UPDATE users SET slot_index=?, display_name=?, slot_birth_key=COALESCE(NULLIF(?, ''), slot_birth_key), last_seen_at=? WHERE character_id=?", (slot_index, display_name, slot_birth_key or '', now, old_character_id))
                    con.commit()
                    refreshed = con.execute(
                        "SELECT character_id, secret_token, display_name, public_serial, public_handle, account_uuid, slot_index, slot_fingerprint, status, slot_birth_key FROM users WHERE character_id=?",
                        (old_character_id,),
                    ).fetchone()
                    return row_to_identity(refreshed)

        identity = create_character(con, account_uuid, slot_index, display_name, slot_fingerprint or '', recovery_snapshot or '', slot_birth_key or '')
        con.commit()
        return identity


def register_new_identity(display_name: str) -> Tuple[str, str, str, int, str]:
    """Legacy non-Steam registration path. Kept for old test clients only."""
    display_name = sanitize_name(display_name)
    with db_lock, db_conn() as con:
        legacy_account = 'legacy_' + uuid.uuid4().hex
        identity = create_character(con, legacy_account, -1, display_name, '', '')
        con.commit()
        return identity[:5]


def validate_identity(character_id: str, secret_token: str, display_name: str) -> Optional[Tuple[str, str, str, int, str]]:
    character_id = (character_id or '').strip()
    secret_token = (secret_token or '').strip()
    display_name = sanitize_name(display_name)
    if not character_id or not secret_token:
        return None
    now = int(time.time())
    with db_lock, db_conn() as con:
        row = con.execute(
            "SELECT character_id, secret_token, display_name, public_serial, public_handle FROM users WHERE character_id = ? AND status='active'",
            (character_id,),
        ).fetchone()
        if not row:
            return None
        if not secrets.compare_digest(str(row[1]), secret_token):
            return None
        old_display = str(row[2])
        public_serial = int(row[3])
        public_handle = str(row[4])
        # Keep public_handle stable, but update display_name for chat and manual DB readability.
        if display_name and display_name != old_display:
            con.execute("UPDATE users SET display_name=?, last_seen_at=? WHERE character_id=?", (display_name, now, character_id))
            con.execute("UPDATE guild_members SET display_name=? WHERE character_id=?", (display_name, character_id))
            con.execute("UPDATE guilds SET owner_display_name=? WHERE owner_character_id=?", (display_name, character_id))
            con.execute("UPDATE guild_invites SET invited_display_name=? WHERE invited_character_id=?", (display_name, character_id))
            con.commit()
            return character_id, secret_token, display_name, public_serial, public_handle
        else:
            con.execute("UPDATE users SET last_seen_at=? WHERE character_id=?", (now, character_id))
            con.commit()
        return character_id, secret_token, old_display, public_serial, public_handle

def find_user(identifier: str) -> Tuple[Optional[Tuple[str, str, str, int]], Optional[str]]:
    """Find a user by hidden character_id, public handle, or unique display name.

    Chat context-menu actions prefer hidden character_id metadata from the chat packet.
    Public handles and display names are kept as fallback paths for typed commands. Archived
    records are allowed for profile/report lookups so moderation records can still resolve
    a user even if a same-SteamID test or slot replacement archived the old character.
    """
    raw_identifier = (identifier or "").replace("|", "").replace("\r", "").replace("\n", "").strip()
    raw = sanitize_name(identifier)
    with db_lock, db_conn() as con:
        if raw_identifier.lower().startswith("char_"):
            row = con.execute(
                "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE character_id = ? LIMIT 1",
                (raw_identifier,),
            ).fetchone()
            if row:
                return (str(row[0]), str(row[1]), str(row[2]), int(row[3])), None

        row = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE public_handle = ? COLLATE NOCASE AND status='active'",
            (raw,),
        ).fetchone()
        if not row:
            row = con.execute(
                "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE public_handle = ? COLLATE NOCASE ORDER BY status='active' DESC, last_seen_at DESC LIMIT 1",
                (raw,),
            ).fetchone()
        if row:
            return (str(row[0]), str(row[1]), str(row[2]), int(row[3])), None

        rows = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE display_name = ? COLLATE NOCASE AND status='active' ORDER BY public_handle ASC",
            (raw,),
        ).fetchall()
        if not rows:
            rows = con.execute(
                "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE display_name = ? COLLATE NOCASE ORDER BY status='active' DESC, last_seen_at DESC, public_handle ASC",
                (raw,),
            ).fetchall()
        if not rows:
            return None, "No registered player found for: " + raw
        if len(rows) > 1:
            handles = ", ".join(str(r[2]) for r in rows[:8])
            return None, "Multiple players named {} found. Use the full handle: {}".format(raw, handles)
        r = rows[0]
        return (str(r[0]), str(r[1]), str(r[2]), int(r[3])), None



def get_steam_id_for_account_uuid(account_uuid: str) -> str:
    account_uuid = (account_uuid or '').strip()
    if not account_uuid:
        return ""
    try:
        with db_lock, db_conn() as con:
            row = con.execute("SELECT steam_id64 FROM accounts WHERE account_uuid=?", (account_uuid,)).fetchone()
        if row:
            return str(row[0] or "")
    except Exception:
        pass
    return ""


def disconnect_active_banned_steam(steam_id64: str, reason: str = "This Steam account is banned from MMOnsterpatch online services.") -> int:
    steam_id64 = moderation.normalize_steam_id64(steam_id64)
    if not steam_id64:
        return 0
    disconnected = 0
    with clients_lock:
        targets = [h for h in list(clients.keys()) if getattr(h, "steam_id64", "") == steam_id64]
    for h in targets:
        try:
            send_line(h, f"IDENTITY_ERROR|{b64_encode(reason)}")
        except Exception:
            pass
        try:
            send_line(h, f"GUILD_ERROR|{b64_encode(reason)}")
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

def get_user(character_id: str) -> Optional[Tuple[str, str, str, int]]:
    with db_lock, db_conn() as con:
        row = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE character_id = ? AND status='active'",
            (character_id,),
        ).fetchone()
    if not row:
        return None
    return str(row[0]), str(row[1]), str(row[2]), int(row[3])


def get_membership(character_id: str) -> Optional[Tuple[int, str, str, str, str, str]]:
    with db_lock, db_conn() as con:
        row = con.execute(
            """
            SELECT g.id, g.name, g.tag, gm.rank, gm.display_name, gm.public_handle
            FROM guild_members gm
            JOIN guilds g ON g.id = gm.guild_id
            WHERE gm.character_id = ?
            ORDER BY gm.joined_at DESC
            LIMIT 1
            """,
            (character_id,),
        ).fetchone()
    if not row:
        return None
    return int(row[0]), str(row[1]), str(row[2] or ""), str(row[3]), str(row[4]), str(row[5])


def get_public_profile(identifier: str) -> Tuple[Optional[Dict[str, object]], Optional[str]]:
    target, err = find_user(identifier)
    if err or not target:
        return None, err or "Player was not found."
    target_character_id, target_display_name, target_public_handle, _serial = target
    with db_lock, db_conn() as con:
        user_row = con.execute(
            "SELECT account_uuid FROM users WHERE character_id=? AND status='active'",
            (target_character_id,),
        ).fetchone()
        account_uuid = str(user_row[0] or "") if user_row else ""
        guild_row = con.execute(
            """
            SELECT g.name, g.tag, gm.rank
            FROM guild_members gm
            JOIN guilds g ON g.id = gm.guild_id
            WHERE gm.character_id=?
            ORDER BY gm.joined_at DESC
            LIMIT 1
            """,
            (target_character_id,),
        ).fetchone()
        ranked = ensure_ranked_profile(con, target_character_id, account_uuid) if account_uuid else None
        rules = get_ranked_rules(con)
    guild_name = str(guild_row[0]) if guild_row else ""
    guild_tag = str(guild_row[1] or "") if guild_row else ""
    guild_rank = str(guild_row[2] or "") if guild_row else ""
    if not ranked:
        ranked = {"rank": "E", "rp": 0, "wins": 0, "losses": 0, "highest_rank": "E", "highest_rp": 0, "season_name": RANKED_SEASON0_NAME}
    return {
        "character_id": target_character_id,
        "display_name": target_display_name,
        "public_handle": target_public_handle,
        "guild_name": guild_name,
        "guild_tag": guild_tag,
        "guild_rank": guild_rank,
        "rank": ranked.get("rank", "E"),
        "rp": int(ranked.get("rp", 0) or 0),
        "max_rp": int(rules.get("max_rp", RANKED_MAX_RP) or RANKED_MAX_RP),
        "wins": int(ranked.get("wins", 0) or 0),
        "losses": int(ranked.get("losses", 0) or 0),
        "highest_rank": ranked.get("highest_rank", "E"),
        "season_name": ranked.get("season_name", RANKED_SEASON0_NAME),
    }, None


def send_public_profile(handler, identifier: str):
    profile, err = get_public_profile(identifier)
    if err or not profile:
        send_line(handler, f"PROFILE_ERROR|{b64_encode(err or 'Player profile was not found.')}")
        return
    send_line(
        handler,
        "PROFILE|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}|{}".format(
            b64_encode(str(profile.get("public_handle", ""))),
            b64_encode(str(profile.get("display_name", ""))),
            b64_encode(str(profile.get("guild_name", ""))),
            b64_encode(str(profile.get("guild_tag", ""))),
            b64_encode(str(profile.get("guild_rank", ""))),
            b64_encode(str(profile.get("rank", "E"))),
            int(profile.get("rp", 0) or 0),
            int(profile.get("max_rp", RANKED_MAX_RP) or RANKED_MAX_RP),
            int(profile.get("wins", 0) or 0),
            int(profile.get("losses", 0) or 0),
            b64_encode(str(profile.get("highest_rank", "E"))),
            b64_encode(str(profile.get("season_name", RANKED_SEASON0_NAME))),
            b64_encode(str(profile.get("character_id", ""))),
        ),
    )


def _safe_report_filename(report_id: int, reported_public_handle: str, created_at: int) -> str:
    safe_handle = re.sub(r"[^A-Za-z0-9#_-]+", "_", reported_public_handle or "unknown")[:48]
    stamp = time.strftime("%Y%m%d_%H%M%S", time.gmtime(int(created_at or time.time())))
    return f"report_{stamp}_{int(report_id)}_{safe_handle}.txt"


def submit_player_report(handler, target_identifier: str, reason: str, details: str) -> Tuple[bool, str, Optional[int]]:
    if not handler.character_id:
        return False, "Social identity is not registered yet.", None
    target, err = find_user(target_identifier)
    if err or not target:
        return False, err or "Reported player was not found.", None
    reported_character_id, reported_display_name, reported_public_handle, _serial = target
    if reported_character_id == handler.character_id:
        return False, "You cannot report yourself.", None
    reason = (reason or "").replace("\r", " ").replace("\n", " ").strip()[:120]
    details = (details or "").replace("\r\n", "\n").replace("\r", "\n").strip()[:3000]
    if len(reason) < 3:
        return False, "Report reason must be at least 3 characters.", None
    if len(details) < 3:
        return False, "Report details must be at least 3 characters.", None
    now = int(time.time())
    chat_ref = chat_log_path(now)
    with db_lock, db_conn() as con:
        target_row = con.execute("SELECT account_uuid FROM users WHERE character_id=?", (reported_character_id,)).fetchone()
        reported_account_uuid = str(target_row[0] or "") if target_row else ""
        reported_steam_id64 = get_steam_id_for_account_uuid(reported_account_uuid)
        reporter_steam_id64 = get_steam_id_for_account_uuid(handler.account_uuid)
        cur = con.execute(
            """
            INSERT INTO player_reports(
                created_at, status, reporter_character_id, reporter_public_handle, reporter_account_uuid, reporter_steam_id64,
                reported_character_id, reported_public_handle, reported_account_uuid, reported_steam_id64, reported_display_name,
                reason, details, chat_log_reference, report_file
            ) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
            """,
            (now, "Open", handler.character_id, handler.public_handle, handler.account_uuid, reporter_steam_id64,
             reported_character_id, reported_public_handle, reported_account_uuid, reported_steam_id64, reported_display_name,
             reason, details, chat_ref, ""),
        )
        report_id = int(cur.lastrowid)
        os.makedirs(USER_REPORT_DIR or os.path.join(os.path.dirname(DB_PATH or __file__), "User Reports"), exist_ok=True)
        report_path = os.path.join(USER_REPORT_DIR or os.path.join(os.path.dirname(DB_PATH or __file__), "User Reports"), _safe_report_filename(report_id, reported_public_handle, now))
        text = []
        text.append("MMOnsterpatch User Report")
        text.append("==========================")
        text.append(f"Report ID: {report_id}")
        text.append(f"Created UTC: {utc_timestamp(now)}")
        text.append(f"Status: Open")
        text.append("")
        text.append("Reporter")
        text.append(f"  Public Handle: {handler.public_handle}")
        text.append(f"  Character ID: {handler.character_id}")
        text.append(f"  Account UUID: {handler.account_uuid}")
        text.append(f"  SteamID64: {reporter_steam_id64}")
        text.append("")
        text.append("Reported User")
        text.append(f"  Public Handle: {reported_public_handle}")
        text.append(f"  Display Name: {reported_display_name}")
        text.append(f"  Character ID: {reported_character_id}")
        text.append(f"  Account UUID: {reported_account_uuid}")
        text.append(f"  SteamID64: {reported_steam_id64}")
        text.append("")
        text.append("Reason")
        text.append(reason)
        text.append("")
        text.append("Details")
        text.append(details)
        text.append("")
        text.append("Chat Log Reference")
        text.append(chat_ref)
        with open(report_path, "w", encoding="utf-8") as f:
            f.write("\n".join(text) + "\n")
        con.execute("UPDATE player_reports SET report_file=? WHERE report_id=?", (report_path, report_id))
        con.commit()
    print(f"[report] #{report_id} {handler.public_handle} reported {reported_public_handle} reason={reason}", flush=True)
    return True, f"Report submitted. Report ID: {report_id}", report_id


def send_guild_state(handler):
    membership = get_membership(handler.character_id)
    if not membership:
        send_line(handler, "GUILD_STATE|NONE")
        return
    gid, gname, gtag, rank, _display, _handle = membership
    send_line(handler, f"GUILD_STATE|IN|{gid}|{b64_encode(gname)}|{rank}|{b64_encode(gtag)}")


def send_pending_invites(handler):
    if not handler.character_id:
        return
    # If the character is already in a guild, do not keep presenting old invites.
    if get_membership(handler.character_id):
        with db_lock, db_conn() as con:
            con.execute("DELETE FROM guild_invites WHERE invited_character_id = ?", (handler.character_id,))
            con.commit()
        return
    with db_lock, db_conn() as con:
        rows = con.execute(
            """
            SELECT gi.guild_id, g.name, g.tag, gi.invited_by_public_handle
            FROM guild_invites gi
            JOIN guilds g ON g.id = gi.guild_id
            WHERE gi.invited_character_id = ?
            ORDER BY gi.created_at DESC
            """,
            (handler.character_id,),
        ).fetchall()
    for row in rows:
        send_line(handler, f"GUILD_INVITE|{int(row[0])}|{b64_encode(str(row[1]))}|{b64_encode(str(row[3]))}|{b64_encode(str(row[2] or ''))}")


def create_guild(handler, requested_name: str, requested_tag: str = "") -> Tuple[bool, str, Optional[Tuple[int, str, str, str]]]:
    guild_name, err = sanitize_guild_name(requested_name)
    if err:
        return False, err, None
    guild_tag, err = sanitize_guild_tag(requested_tag)
    if err:
        return False, err, None

    existing = get_membership(handler.character_id)
    if existing:
        return False, f"You are already in guild: {existing[1]}.", (existing[0], existing[1], existing[3], existing[2])

    now = int(time.time())
    with db_lock, db_conn() as con:
        try:
            cur = con.execute(
                "INSERT INTO guilds(name, tag, owner_character_id, owner_display_name, owner_public_handle, created_at) VALUES(?,?,?,?,?,?)",
                (guild_name, guild_tag, handler.character_id, handler.display_name, handler.public_handle, now),
            )
            gid = int(cur.lastrowid)
            con.execute(
                "INSERT INTO guild_members(guild_id, character_id, display_name, public_handle, rank, joined_at) VALUES(?,?,?,?,?,?)",
                (gid, handler.character_id, handler.display_name, handler.public_handle, "Leader", now),
            )
            con.commit()
            return True, "created", (gid, guild_name, "Leader", guild_tag)
        except sqlite3.IntegrityError:
            return False, "That guild name already exists.", None


def find_online_handler(character_id: str):
    with clients_lock:
        return online_by_character_id.get(character_id)


def create_invite(inviter, target_identifier: str) -> Tuple[bool, str, Optional[Tuple[int, str, str, str]], Optional[Tuple[str, str, str, int]]]:
    membership = get_membership(inviter.character_id)
    if not membership:
        return False, "You are not in a Guild.", None, None
    gid, gname, gtag, rank, _display, _handle = membership
    if rank != "Leader":
        return False, "Only the guild Leader can invite players.", (gid, gname, rank, gtag), None

    target, err = find_user(target_identifier)
    if err:
        return False, err, (gid, gname, rank, gtag), None
    target_character_id, target_display_name, target_public_handle, target_serial = target

    if target_character_id == inviter.character_id:
        return False, "You cannot invite yourself.", (gid, gname, rank, gtag), target
    if get_membership(target_character_id):
        return False, target_public_handle + " is already in a guild.", (gid, gname, rank, gtag), target

    now = int(time.time())
    with db_lock, db_conn() as con:
        con.execute(
            """
            INSERT INTO guild_invites(
                guild_id, invited_character_id, invited_display_name, invited_public_handle,
                invited_by_character_id, invited_by_display_name, invited_by_public_handle, created_at
            ) VALUES(?,?,?,?,?,?,?,?)
            ON CONFLICT(guild_id, invited_character_id) DO UPDATE SET
                invited_display_name=excluded.invited_display_name,
                invited_public_handle=excluded.invited_public_handle,
                invited_by_character_id=excluded.invited_by_character_id,
                invited_by_display_name=excluded.invited_by_display_name,
                invited_by_public_handle=excluded.invited_by_public_handle,
                created_at=excluded.created_at
            """,
            (gid, target_character_id, target_display_name, target_public_handle, inviter.character_id, inviter.display_name, inviter.public_handle, now),
        )
        con.commit()
    return True, target_public_handle, (gid, gname, rank, gtag), target


def accept_invite(handler, guild_id_text: str) -> Tuple[bool, str, Optional[Tuple[int, str, str, str]]]:
    try:
        gid = int(guild_id_text)
    except Exception:
        return False, "Invalid guild invite ID.", None
    if get_membership(handler.character_id):
        return False, "You are already in a guild.", None
    now = int(time.time())
    with db_lock, db_conn() as con:
        row = con.execute(
            """
            SELECT g.id, g.name, g.tag
            FROM guild_invites gi
            JOIN guilds g ON g.id = gi.guild_id
            WHERE gi.guild_id = ? AND gi.invited_character_id = ?
            """,
            (gid, handler.character_id),
        ).fetchone()
        if not row:
            return False, "No invite found for that guild ID.", None
        gname = str(row[1])
        gtag = str(row[2] or "")
        con.execute(
            "INSERT INTO guild_members(guild_id, character_id, display_name, public_handle, rank, joined_at) VALUES(?,?,?,?,?,?)",
            (gid, handler.character_id, handler.display_name, handler.public_handle, "Member", now),
        )
        con.execute("DELETE FROM guild_invites WHERE invited_character_id = ?", (handler.character_id,))
        con.commit()
    return True, "joined", (gid, gname, "Member", gtag)


def decline_invite(handler, guild_id_text: str) -> Tuple[bool, str, Optional[Tuple[int, str, str, str]]]:
    try:
        gid = int(guild_id_text)
    except Exception:
        return False, "Invalid guild invite ID.", None
    with db_lock, db_conn() as con:
        row = con.execute(
            """
            SELECT g.id, g.name, gi.invited_by_character_id, gi.invited_by_public_handle
            FROM guild_invites gi
            JOIN guilds g ON g.id = gi.guild_id
            WHERE gi.guild_id = ? AND gi.invited_character_id = ?
            """,
            (gid, handler.character_id),
        ).fetchone()
        if not row:
            return False, "No invite found for that guild ID.", None
        con.execute("DELETE FROM guild_invites WHERE guild_id = ? AND invited_character_id = ?", (gid, handler.character_id))
        con.commit()
    return True, "declined", (int(row[0]), str(row[1]), str(row[2]), str(row[3]))


def leave_guild(handler) -> Tuple[bool, str, Optional[Tuple[int, str]]]:
    """Silently remove the current character from their guild.

    Returns (ok, message, old_membership). No chat broadcast is emitted here;
    the client updates local guild state from GUILD_LEFT / GUILD_STATE|NONE.
    If a Leader leaves with members remaining, leadership transfers to the
    oldest remaining member. If the Leader is alone, the guild is disbanded.
    """
    membership = get_membership(handler.character_id)
    if not membership:
        return False, "You are not in a Guild.", None

    gid, gname, _gtag, rank, _display, _handle = membership
    with db_lock, db_conn() as con:
        if rank == "Leader":
            replacement = con.execute(
                """
                SELECT character_id, display_name, public_handle
                FROM guild_members
                WHERE guild_id = ? AND character_id <> ?
                ORDER BY joined_at ASC
                LIMIT 1
                """,
                (gid, handler.character_id),
            ).fetchone()
            if replacement:
                new_owner_id, new_owner_display, new_owner_handle = str(replacement[0]), str(replacement[1]), str(replacement[2])
                con.execute("UPDATE guild_members SET rank='Leader' WHERE guild_id=? AND character_id=?", (gid, new_owner_id))
                con.execute(
                    "UPDATE guilds SET owner_character_id=?, owner_display_name=?, owner_public_handle=? WHERE id=?",
                    (new_owner_id, new_owner_display, new_owner_handle, gid),
                )
                con.execute("DELETE FROM guild_members WHERE guild_id=? AND character_id=?", (gid, handler.character_id))
            else:
                con.execute("DELETE FROM guild_invites WHERE guild_id=?", (gid,))
                con.execute("DELETE FROM guild_members WHERE guild_id=?", (gid,))
                con.execute("DELETE FROM guilds WHERE id=?", (gid,))
        else:
            con.execute("DELETE FROM guild_members WHERE guild_id=? AND character_id=?", (gid, handler.character_id))
            con.execute("DELETE FROM guild_invites WHERE invited_character_id=?", (handler.character_id,))
        con.commit()
    return True, "left", (gid, gname)


class SocialHandler(socketserver.StreamRequestHandler):
    def setup(self):
        super().setup()
        self.character_id = ""
        self.secret_token = ""
        self.display_name = f"Player-{self.client_address[1]}"
        self.public_handle = self.display_name
        self.public_serial = 0
        self.account_uuid = ""
        self.steam_id64 = ""
        self.slot_index = -1
        self.slot_fingerprint = ""
        self.slot_birth_key = ""
        self.identity_status = "active"
        self.alive = True

    @property
    def username(self):
        # Backward-compatible label for old print/debug calls.
        return self.public_handle or self.display_name

    def attach_identity(self, identity_tuple):
        self.character_id, self.secret_token, self.display_name, self.public_serial, self.public_handle = identity_tuple[:5]
        if len(identity_tuple) >= 9:
            self.account_uuid = identity_tuple[5] or ""
            self.slot_index = int(identity_tuple[6])
            self.slot_fingerprint = identity_tuple[7] or ""
            self.identity_status = identity_tuple[8] or "active"
            if len(identity_tuple) >= 10:
                self.slot_birth_key = identity_tuple[9] or ""
        self.steam_id64 = get_steam_id_for_account_uuid(self.account_uuid)
        active_ban = moderation.get_active_ban(steam_id64=self.steam_id64, account_uuid=self.account_uuid)
        if active_ban:
            reason = active_ban.get("reason") or "This Steam account is banned from MMOnsterpatch online services."
            send_line(self, f"IDENTITY_ERROR|{b64_encode('This Steam account is banned from MMOnsterpatch online services. Reason: ' + str(reason))}")
            try:
                self.request.close()
            except Exception:
                pass
            return
        with clients_lock:
            # If same identity logs in twice, replace old connection mapping but keep both sockets from crashing.
            old = online_by_character_id.get(self.character_id)
            if old is not None and old is not self:
                clients.pop(old, None)
                try:
                    send_line(old, f"GUILD_ERROR|{b64_encode('This character logged in from another client.')}")
                    old.request.close()
                except Exception:
                    pass
            clients[self] = self.character_id
            online_by_character_id[self.character_id] = self
        print(f"[hello] {self.client_address} user={self.public_handle} display={self.display_name} steam={self.steam_id64 or 'legacy'} id={self.character_id}", flush=True)
        send_identity(self)
        send_line(self, f"WELCOME|{b64_encode(self.public_handle)}")
        send_guild_state(self)
        send_ranked_profile(self)
        send_pending_invites(self)
        broadcast(f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode(self.public_handle + ' joined chat.')}|{int(time.time())}", skip=self)

    def handle(self):
        print(f"[connect] {self.client_address}", flush=True)
        send_line(self, "WELCOME|server-ready")
        try:
            while True:
                raw = self.rfile.readline()
                if not raw:
                    break
                line = raw.decode("utf-8", "replace").strip()
                if not line:
                    continue
                self.handle_line(line)
        except ConnectionResetError:
            pass
        except ConnectionAbortedError:
            pass
        except Exception as ex:
            print(f"[error] {self.client_address}: {ex}", flush=True)
        finally:
            with clients_lock:
                was_known = self in clients
                cid = clients.pop(self, None)
                if cid:
                    current = online_by_character_id.get(cid)
                    if current is self:
                        online_by_character_id.pop(cid, None)
            print(f"[disconnect] {self.client_address} user={self.public_handle}", flush=True)
            if was_known:
                broadcast(f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode(self.public_handle + ' left chat.')}|{int(time.time())}")

    def ensure_registered(self) -> bool:
        if self.character_id:
            return True
        send_line(self, f"GUILD_ERROR|{b64_encode('Social identity is not registered yet.')}")
        return False

    def send_official_world_rates(self, session_token: str):
        try:
            # Validate the same Steam/AIO session and source IP that own official online saves.
            resolve_official_account_from_session(session_token, self.client_address[0])
            vals = [
                OFFICIAL_EXP_RATE,
                OFFICIAL_SATS_RATE,
                OFFICIAL_SHINY_RATE,
                OFFICIAL_CATCH_RATE,
                OFFICIAL_ITEM_DROP_RATE,
                OFFICIAL_RANDOM_ENCOUNTER_RATE,
                OFFICIAL_VISIBLE_SPAWN_RATE,
                OFFICIAL_REWARD_SPAWN_RATE,
                RP_GAIN_RATE,
                RP_LOSS_RATE,
                SEASON_REWARD_RATE,
            ]
            def fmt(v):
                try:
                    return ("%.6f" % float(v)).rstrip('0').rstrip('.')
                except Exception:
                    return "1"
            send_line(self, "OFFICIAL_WORLD_RATES|" + "|".join(fmt(v) for v in vals))
            print(f"[official-world-rates] {self.client_address} exp={OFFICIAL_EXP_RATE} sats={OFFICIAL_SATS_RATE} shinyOddsDenom={OFFICIAL_SHINY_RATE} catch={OFFICIAL_CATCH_RATE} item={OFFICIAL_ITEM_DROP_RATE} random={OFFICIAL_RANDOM_ENCOUNTER_RATE}", flush=True)
        except Exception as ex:
            send_line(self, f"OFFICIAL_SAVE_ERROR|{b64_encode(str(ex))}")
            print(f"[official-world-rates-error] {self.client_address}: {ex}", flush=True)

    def handle_line(self, line: str):
        parts = line.split("|")
        cmd = parts[0].upper()

        if cmd == "OFFICIAL_WORLD_RATES_REQ" and len(parts) >= 2:
            self.send_official_world_rates(b64_decode(parts[1]))
            return

        if cmd == "OFFICIAL_SAVE_SLOTS2_REQ" and len(parts) >= 2:
            session_token = b64_decode(parts[1])
            try:
                lines, account_uuid, occupied = official_save_slots_v2_lines(session_token, self.client_address[0])
                for out_line in lines:
                    send_line(self, out_line)
                send_line(self, f"OFFICIAL_SAVE_SLOTS_DONE|{len(lines)}|{occupied}")
                print(f"[official-save-slots] {self.client_address} account={account_uuid} occupied={occupied}/6 protocol=v2", flush=True)
            except Exception as ex:
                send_line(self, f"OFFICIAL_SAVE_ERROR|{b64_encode(str(ex))}")
                print(f"[official-save-slots-error] {self.client_address}: {ex}", flush=True)
            return

        if cmd == "OFFICIAL_SAVE_SLOTS_REQ" and len(parts) >= 2:
            session_token = b64_decode(parts[1])
            try:
                payload = official_save_slots_payload(session_token, self.client_address[0])
                send_line(self, f"OFFICIAL_SAVE_SLOTS|{b64_encode(payload)}")
                try:
                    info = json.loads(payload)
                    occ = sum(1 for s in info.get('slots', []) if int(s.get('occupied', 0)) != 0)
                except Exception:
                    occ = -1
                print(f"[official-save-slots] {self.client_address} occupied={occ}/6 protocol=legacy", flush=True)
            except Exception as ex:
                send_line(self, f"OFFICIAL_SAVE_ERROR|{b64_encode(str(ex))}")
                print(f"[official-save-slots-error] {self.client_address}: {ex}", flush=True)
            return

        if cmd == "OFFICIAL_SAVE_WRITE" and len(parts) >= 4:
            session_token = b64_decode(parts[1])
            try:
                slot_index = int(parts[2])
            except Exception:
                slot_index = 0
            save_json = b64_decode(parts[3])
            try:
                saved_slot, display_name = write_official_save_slot(session_token, slot_index, save_json, self.client_address[0])
                send_line(self, f"OFFICIAL_SAVE_WRITE_OK|{int(saved_slot)}|{b64_encode(display_name)}")
                meta = extract_official_save_metadata(save_json)
                print(
                    f"[official-save-write] {self.client_address} slot={saved_slot} display={display_name} "
                    f"player={meta.get('player_name')}#{meta.get('player_design')}/{meta.get('player_color1')}/{meta.get('player_color2')} "
                    f"bestFriend={meta.get('best_friend_name')}#{meta.get('best_friend_design')}/{meta.get('best_friend_color1')}/{meta.get('best_friend_color2')} "
                    f"battlesWon={meta.get('battles_won')} battleSpeed={meta.get('battle_speed')} "
                    f"bytes={len(save_json.encode('utf-8', 'ignore'))}",
                    flush=True,
                )
            except Exception as ex:
                send_line(self, f"OFFICIAL_SAVE_ERROR|{b64_encode(str(ex))}")
                print(f"[official-save-write-error] {self.client_address} slot={slot_index}: {ex}", flush=True)
            return

        if cmd == "OFFICIAL_SAVE_DELETE" and len(parts) >= 3:
            session_token = b64_decode(parts[1])
            try:
                slot_index = int(parts[2])
            except Exception:
                slot_index = 0
            try:
                deleted_slot, display_name, archive_path = delete_official_save_slot(session_token, slot_index, self.client_address[0])
                send_line(self, f"OFFICIAL_SAVE_DELETE_OK|{int(deleted_slot)}|{b64_encode(display_name)}|{b64_encode(os.path.basename(archive_path))}")
                print(f"[official-save-delete] {self.client_address} slot={deleted_slot} display={display_name} archive={archive_path}", flush=True)
            except Exception as ex:
                send_line(self, f"OFFICIAL_SAVE_ERROR|{b64_encode(str(ex))}")
                print(f"[official-save-delete-error] {self.client_address} slot={slot_index}: {ex}", flush=True)
            return

        if cmd == "ACCOUNT_SLOT_HELLO" and len(parts) >= 8:
            session_token = b64_decode(parts[1])
            try:
                slot_index = int(parts[2])
            except Exception:
                slot_index = 0
            old_character_id = b64_decode(parts[3])
            old_secret_token = b64_decode(parts[4])
            display_name = sanitize_name(b64_decode(parts[5]))
            slot_fingerprint = b64_decode(parts[6]).strip()
            recovery_snapshot = b64_decode(parts[7])
            slot_birth_key = b64_decode(parts[8]).strip() if len(parts) >= 9 else ''
            try:
                identity = register_or_resume_account_slot(session_token, slot_index, old_character_id, old_secret_token, display_name, slot_fingerprint, recovery_snapshot, slot_birth_key, self.client_address[0])
            except Exception as ex:
                send_line(self, f"IDENTITY_ERROR|{b64_encode(str(ex))}")
                print(f"[account-slot-error] {self.client_address} display={display_name} slot={slot_index}: {ex}", flush=True)
                return
            print(f"[account-slot] {self.client_address} account={identity[5]} slot={identity[6]} display={display_name} handle={identity[4]} id={identity[0]}", flush=True)
            self.attach_identity(identity)
            return

        if cmd == "REGISTER" and len(parts) >= 2:
            display_name = sanitize_name(b64_decode(parts[1]))
            identity = register_new_identity(display_name)
            print(f"[register] {self.client_address} display={display_name} handle={identity[4]} id={identity[0]}", flush=True)
            self.attach_identity(identity)
            return

        if cmd == "HELLO_ID" and len(parts) >= 4:
            character_id = b64_decode(parts[1])
            secret_token = b64_decode(parts[2])
            display_name = sanitize_name(b64_decode(parts[3]))
            identity = validate_identity(character_id, secret_token, display_name)
            if not identity:
                send_line(self, f"IDENTITY_ERROR|{b64_encode('Saved social identity was rejected by the server. Delete the social identity config values to register again.')}")
                print(f"[identity-reject] {self.client_address} display={display_name} id={character_id}", flush=True)
                return
            self.attach_identity(identity)
            return

        # Old testing clients can still register by sending HELLO|name, but they receive a new identity.
        if cmd == "HELLO" and len(parts) >= 2:
            display_name = sanitize_name(b64_decode(parts[1]))
            identity = register_new_identity(display_name)
            print(f"[legacy-register] {self.client_address} display={display_name} handle={identity[4]} id={identity[0]}", flush=True)
            self.attach_identity(identity)
            return

        if cmd == "GUILD_STATE_REQ":
            if self.ensure_registered():
                send_guild_state(self)
            return

        if cmd == "RANKED_PROFILE_REQ":
            if self.ensure_registered():
                send_ranked_profile(self)
            return

        if cmd == "GUILD_CREATE" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            requested = b64_decode(parts[1])
            requested_tag = b64_decode(parts[2]) if len(parts) >= 3 else ""
            ok, msg, membership = create_guild(self, requested, requested_tag)
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                send_guild_state(self)
                return
            gid, gname, rank, gtag = membership
            print(f"[guild-create] {self.public_handle} created guild #{gid} {gname} [{gtag}]", flush=True)
            send_line(self, f"GUILD_CREATED|{gid}|{b64_encode(gname)}|{rank}|{b64_encode(gtag)}")
            send_guild_state(self)
            return

        if cmd == "GUILD_INVITE" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            target_identifier = b64_decode(parts[1])
            ok, msg, membership, target = create_invite(self, target_identifier)
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                return
            gid, gname, rank, gtag = membership
            target_character_id, target_display_name, target_public_handle, _serial = target
            print(f"[guild-invite] {self.public_handle} invited {target_public_handle} to #{gid} {gname} [{gtag}]", flush=True)
            send_line(self, f"GUILD_ERROR|{b64_encode('Invited ' + target_public_handle + ' to ' + gname + ' [' + gtag + '].')}")
            target_handler = find_online_handler(target_character_id)
            if target_handler is not None:
                send_line(target_handler, f"GUILD_INVITE|{gid}|{b64_encode(gname)}|{b64_encode(self.public_handle)}|{b64_encode(gtag)}")
            return

        if cmd == "GUILD_ACCEPT" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            ok, msg, membership = accept_invite(self, parts[1])
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                return
            gid, gname, rank, gtag = membership
            print(f"[guild-join] {self.public_handle} joined #{gid} {gname} [{gtag}] as {rank}", flush=True)
            send_line(self, f"GUILD_JOINED|{gid}|{b64_encode(gname)}|{rank}|{b64_encode(gtag)}")
            send_guild_state(self)
            broadcast_guild(gid, f"CHAT|GUILD|{b64_encode('System')}|{b64_encode(self.public_handle + ' joined the guild.')}|{int(time.time())}")
            return

        if cmd == "GUILD_DECLINE" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            ok, msg, declined = decline_invite(self, parts[1])
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                return
            gid, gname, inviter_character_id, inviter_public_handle = declined
            print(f"[guild-decline] {self.public_handle} declined invite to #{gid} {gname}", flush=True)
            send_line(self, f"GUILD_ERROR|{b64_encode('Declined invite to ' + gname + '.')}")
            inviter_handler = find_online_handler(inviter_character_id)
            if inviter_handler is not None:
                send_line(inviter_handler, f"GUILD_ERROR|{b64_encode(self.public_handle + ' declined the guild invite.')}")
            return

        if cmd == "GUILD_LEAVE":
            if not self.ensure_registered():
                return
            ok, msg, old_membership = leave_guild(self)
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                send_guild_state(self)
                return
            gid, gname = old_membership
            print(f"[guild-leave] {self.public_handle} left guild #{gid} {gname}", flush=True)
            send_line(self, "GUILD_LEFT")
            send_guild_state(self)
            return

        if cmd == "PROFILE_REQ" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            send_public_profile(self, b64_decode(parts[1]))
            return

        if cmd == "REPORT_USER" and len(parts) >= 4:
            if not self.ensure_registered():
                return
            target_identifier = b64_decode(parts[1])
            reason = b64_decode(parts[2])
            details = b64_decode(parts[3])
            ok, msg, report_id = submit_player_report(self, target_identifier, reason, details)
            if not ok:
                send_line(self, f"REPORT_ERROR|{b64_encode(msg)}")
                return
            send_line(self, f"REPORT_SUBMITTED|{int(report_id or 0)}|{b64_encode(msg)}")
            return

        if cmd == "CHAT" and len(parts) >= 3:
            if not self.ensure_registered():
                return
            channel = parts[1].upper()
            msg = b64_decode(parts[2]).strip()
            if not msg:
                return
            if len(msg) > 500:
                msg = msg[:500]

            if channel == "GLOBAL":
                ts = int(time.time())
                membership = get_membership(self.character_id)
                guild_tag = membership[2] if membership else ""
                write_chat_log("GLOBAL", self.display_name, self.public_handle, self.character_id, msg, guild_tag=guild_tag, ts=ts)
                # Keep chat pretty by displaying the character name, while logs/db retain the full handle.
                # Global chat uses the sender's guild tag as the channel label when available.
                broadcast(f"CHAT|GLOBAL|{b64_encode(self.display_name)}|{b64_encode(msg)}|{ts}|{b64_encode(guild_tag)}|{b64_encode(self.public_handle)}|{b64_encode(self.character_id)}")
                return

            if channel == "GUILD":
                membership = get_membership(self.character_id)
                if not membership:
                    send_line(self, f"GUILD_ERROR|{b64_encode('You are not in a Guild.')}")
                    return
                gid, gname, gtag, rank, display_name, public_handle = membership
                ts = int(time.time())
                display = f"{rank} {display_name}"
                write_chat_log("GUILD", display_name, public_handle, self.character_id, msg, guild_id=gid, guild_name=gname, guild_rank=rank, guild_tag=gtag, ts=ts)
                broadcast_guild(gid, f"CHAT|GUILD|{b64_encode(display)}|{b64_encode(msg)}|{ts}|{b64_encode(public_handle)}|{b64_encode(self.character_id)}")
                return

            send_line(self, f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode('Unknown chat channel.')}|{int(time.time())}")
            return

        if cmd == "PING":
            send_line(self, "PONG")
            return

        print(f"[Social] Unknown command from {self.client_address}: {cmd} raw={line}", flush=True)
        send_line(self, f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode('Unknown command received by social server: ' + cmd)}|{int(time.time())}")


def main():
    parser = argparse.ArgumentParser(description=f"MMOnsterpatch Social Server {VERSION}")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=61529)
    parser.add_argument("--db", default=os.path.join(os.path.dirname(__file__), "data", "social.db"))
    args = parser.parse_args()

    init_db(args.db)

    print(f"MMOnsterpatch Social Server v{VERSION}")
    print(f"Listening on {args.host}:{args.port}")
    print(f"Database: {args.db}")
    print("Ctrl+C to stop.")
    print(f"Chat messages are written to daily JSONL files in: {CHAT_LOG_DIR}", flush=True)

    with ThreadedTCPServer((args.host, args.port), SocialHandler) as server:
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\nStopping social server...", flush=True)


if __name__ == "__main__":
    main()
