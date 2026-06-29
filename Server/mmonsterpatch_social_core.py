#!/usr/bin/env python3
"""
MMOnsterpatch Social Server v0.3.0-identity
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
  ACCOUNT_SLOT_HELLO|<base64 aio_session>|<slot>|<base64 old_character_id>|<base64 old_secret>|<base64 display_name>|<base64 slot_fingerprint>|<base64 recovery_snapshot>
  REGISTER|<base64 display_name>                 (legacy/debug fallback)
  HELLO_ID|<base64 character_id>|<base64 secret_token>|<base64 display_name>  (legacy/debug fallback)
  GUILD_STATE_REQ
  GUILD_CREATE|<base64 guild name>
  GUILD_INVITE|<base64 public_handle or display_name>
  GUILD_ACCEPT|<guild id>
  GUILD_DECLINE|<guild id>
  GUILD_LEAVE
  CHAT|GLOBAL|<base64 message>
  CHAT|GUILD|<base64 message>

Server replies:
  WELCOME|server-ready
  IDENTITY|<base64 character_id>|<base64 secret_token>|<serial>|<base64 public_handle>|<base64 display_name>
  WELCOME|<base64 public_handle>
  GUILD_STATE|NONE
  GUILD_STATE|IN|<guild id>|<base64 guild name>|Leader|Member
  GUILD_CREATED|<guild id>|<base64 guild name>|Leader
  GUILD_ERROR|<base64 message>
  GUILD_INVITE|<guild id>|<base64 guild name>|<base64 inviter public_handle>
  GUILD_JOINED|<guild id>|<base64 guild name>|Member
  GUILD_LEFT
  CHAT|GLOBAL|<base64 display_name>|<base64 message>|<unix_time>
  CHAT|GUILD|<base64 "Leader DisplayName" or "Member DisplayName">|<base64 message>|<unix_time>

Guild/friend metadata is persistent in SQLite. Chat messages are not written to disk.
"""
import argparse
import base64
import os
import re
import secrets
import socketserver
import sqlite3
import threading
import time
import uuid
from typing import Dict, Optional, Tuple

VERSION = "0.3.0-identity"
clients_lock = threading.RLock()
clients: Dict[object, str] = {}  # handler -> character_id
online_by_character_id: Dict[str, object] = {}
db_lock = threading.RLock()
DB_PATH = None


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
    if len(value) > 24:
        return None, "Guild name must be 24 characters or less."
    if not re.fullmatch(r"[A-Za-z0-9 ]+", value):
        return None, "Guild name can only use letters, numbers, and spaces."
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
        "IDENTITY|{}|{}|{}|{}|{}|{}|{}|{}|{}".format(
            b64_encode(handler.character_id),
            b64_encode(handler.secret_token),
            int(handler.public_serial or 0),
            b64_encode(handler.public_handle),
            b64_encode(handler.display_name),
            b64_encode(getattr(handler, "account_uuid", "") or ""),
            int(getattr(handler, "slot_index", -1) or -1),
            b64_encode(getattr(handler, "slot_fingerprint", "") or ""),
            getattr(handler, "identity_status", "active") or "active",
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


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    daemon_threads = True


def db_conn():
    if not DB_PATH:
        raise RuntimeError("DB_PATH not initialized")
    return sqlite3.connect(DB_PATH, timeout=10)


def init_db(path: str):
    global DB_PATH
    DB_PATH = path
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with db_lock, db_conn() as con:
        con.execute("PRAGMA journal_mode=WAL")
        ensure_account_character_schema(con)
        con.execute("""
            CREATE TABLE IF NOT EXISTS guilds (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                owner_character_id TEXT NOT NULL,
                owner_display_name TEXT NOT NULL,
                owner_public_handle TEXT NOT NULL,
                created_at INTEGER NOT NULL
            )
        """)
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
        con.commit()


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
    con.execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_active_account_slot_unique ON users(account_uuid, slot_index) WHERE status='active' AND slot_index >= 0")


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


def resolve_account_session(session_token: str) -> Optional[Tuple[int, str, str, str]]:
    session_token = (session_token or '').strip()
    if not session_token:
        return None
    try:
        import mmonsterpatch_tradingpost_core as trading_core
        conn = trading_core.get_db()
        try:
            row = trading_core.resolve_aio_session(conn, session_token)
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


def row_to_identity(row) -> Tuple[str, str, str, int, str, str, int, str, str]:
    return (
        str(row[0]), str(row[1]), str(row[2]), int(row[3]), str(row[4]),
        str(row[5] or ''), int(row[6] if row[6] is not None else -1), str(row[7] or ''), str(row[8] or 'active')
    )


def create_character(con, account_uuid: str, slot_index: int, display_name: str, slot_fingerprint: str = '', recovery_snapshot: str = ''):
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
                    public_serial, slot_fingerprint, status, recovery_snapshot_b64,
                    created_at, last_seen_at, archived_at, archive_reason
                ) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?)
                """,
                (character_id, account_uuid, int(slot_index), secret_token, display_name, public_handle,
                 int(serial), slot_fingerprint or '', 'active', b64_encode(recovery_snapshot or ''), now, now, 0, ''),
            )
            return character_id, secret_token, display_name, int(serial), public_handle, account_uuid, int(slot_index), slot_fingerprint or '', 'active'
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


def register_or_resume_account_slot(session_token: str, slot_index: int, old_character_id: str, old_secret_token: str, display_name: str, slot_fingerprint: str, recovery_snapshot: str):
    session = resolve_account_session(session_token)
    if not session:
        raise RuntimeError('Steam account session was not accepted by the server. Please reconnect with Steam.')
    steam_account_id, steam_username, steam_id64, steam_display_name = session
    display_name = sanitize_name(display_name)
    slot_index = max(0, min(5, int(slot_index)))
    now = int(time.time())
    with db_lock, db_conn() as con:
        account_uuid = get_or_create_account(con, steam_id64, steam_account_id, steam_display_name or steam_username)
        active = con.execute(
            """
            SELECT character_id, secret_token, display_name, public_serial, public_handle,
                   account_uuid, slot_index, slot_fingerprint, status
            FROM users
            WHERE account_uuid=? AND slot_index=? AND status='active'
            LIMIT 1
            """,
            (account_uuid, slot_index),
        ).fetchone()
        if active:
            active_id = str(active[0])
            active_fp = str(active[7] or '')
            if (not slot_fingerprint) or (active_fp == slot_fingerprint):
                old_display = str(active[2])
                if display_name and display_name != old_display:
                    con.execute("UPDATE users SET display_name=?, last_seen_at=? WHERE character_id=?", (display_name, now, active_id))
                    con.execute("UPDATE guild_members SET display_name=? WHERE character_id=?", (display_name, active_id))
                    con.execute("UPDATE guilds SET owner_display_name=? WHERE owner_character_id=?", (display_name, active_id))
                    con.execute("UPDATE guild_invites SET invited_display_name=? WHERE invited_character_id=?", (display_name, active_id))
                else:
                    con.execute("UPDATE users SET last_seen_at=? WHERE character_id=?", (now, active_id))
                con.commit()
                refreshed = con.execute(
                    "SELECT character_id, secret_token, display_name, public_serial, public_handle, account_uuid, slot_index, slot_fingerprint, status FROM users WHERE character_id=?",
                    (active_id,),
                ).fetchone()
                return row_to_identity(refreshed)
            archive_active_slot_character(con, active_id, 'slot_fingerprint_changed', recovery_snapshot)

        # If the client presented a valid old character that belongs to this account/slot, allow it to resume even if the slot index mapping was missing.
        old_character_id = (old_character_id or '').strip()
        old_secret_token = (old_secret_token or '').strip()
        if old_character_id and old_secret_token:
            row = con.execute(
                """
                SELECT character_id, secret_token, display_name, public_serial, public_handle,
                       account_uuid, slot_index, slot_fingerprint, status
                FROM users
                WHERE character_id=? AND account_uuid=? AND status='active'
                """,
                (old_character_id, account_uuid),
            ).fetchone()
            if row and secrets.compare_digest(str(row[1]), old_secret_token):
                row_fp = str(row[7] or '')
                if (not slot_fingerprint) or row_fp == slot_fingerprint:
                    con.execute("UPDATE users SET slot_index=?, display_name=?, last_seen_at=? WHERE character_id=?", (slot_index, display_name, now, old_character_id))
                    con.commit()
                    refreshed = con.execute(
                        "SELECT character_id, secret_token, display_name, public_serial, public_handle, account_uuid, slot_index, slot_fingerprint, status FROM users WHERE character_id=?",
                        (old_character_id,),
                    ).fetchone()
                    return row_to_identity(refreshed)

        identity = create_character(con, account_uuid, slot_index, display_name, slot_fingerprint or '', recovery_snapshot or '')
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
    """Find a user by public handle or unique display name. Returns row or error."""
    raw = sanitize_name(identifier)
    with db_lock, db_conn() as con:
        row = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE public_handle = ? COLLATE NOCASE AND status='active'",
            (raw,),
        ).fetchone()
        if row:
            return (str(row[0]), str(row[1]), str(row[2]), int(row[3])), None

        rows = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE display_name = ? COLLATE NOCASE AND status='active' ORDER BY public_handle ASC",
            (raw,),
        ).fetchall()
        if not rows:
            return None, "No registered player found for: " + raw
        if len(rows) > 1:
            handles = ", ".join(str(r[2]) for r in rows[:8])
            return None, "Multiple players named {} found. Use the full handle: {}".format(raw, handles)
        r = rows[0]
        return (str(r[0]), str(r[1]), str(r[2]), int(r[3])), None


def get_user(character_id: str) -> Optional[Tuple[str, str, str, int]]:
    with db_lock, db_conn() as con:
        row = con.execute(
            "SELECT character_id, display_name, public_handle, public_serial FROM users WHERE character_id = ? AND status='active'",
            (character_id,),
        ).fetchone()
    if not row:
        return None
    return str(row[0]), str(row[1]), str(row[2]), int(row[3])


def get_membership(character_id: str) -> Optional[Tuple[int, str, str, str, str]]:
    with db_lock, db_conn() as con:
        row = con.execute(
            """
            SELECT g.id, g.name, gm.rank, gm.display_name, gm.public_handle
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
    return int(row[0]), str(row[1]), str(row[2]), str(row[3]), str(row[4])


def send_guild_state(handler):
    membership = get_membership(handler.character_id)
    if not membership:
        send_line(handler, "GUILD_STATE|NONE")
        return
    gid, gname, rank, _display, _handle = membership
    send_line(handler, f"GUILD_STATE|IN|{gid}|{b64_encode(gname)}|{rank}")


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
            SELECT gi.guild_id, g.name, gi.invited_by_public_handle
            FROM guild_invites gi
            JOIN guilds g ON g.id = gi.guild_id
            WHERE gi.invited_character_id = ?
            ORDER BY gi.created_at DESC
            """,
            (handler.character_id,),
        ).fetchall()
    for row in rows:
        send_line(handler, f"GUILD_INVITE|{int(row[0])}|{b64_encode(str(row[1]))}|{b64_encode(str(row[2]))}")


def create_guild(handler, requested_name: str) -> Tuple[bool, str, Optional[Tuple[int, str, str]]]:
    guild_name, err = sanitize_guild_name(requested_name)
    if err:
        return False, err, None

    existing = get_membership(handler.character_id)
    if existing:
        return False, f"You are already in guild: {existing[1]}.", (existing[0], existing[1], existing[2])

    now = int(time.time())
    with db_lock, db_conn() as con:
        try:
            cur = con.execute(
                "INSERT INTO guilds(name, owner_character_id, owner_display_name, owner_public_handle, created_at) VALUES(?,?,?,?,?)",
                (guild_name, handler.character_id, handler.display_name, handler.public_handle, now),
            )
            gid = int(cur.lastrowid)
            con.execute(
                "INSERT INTO guild_members(guild_id, character_id, display_name, public_handle, rank, joined_at) VALUES(?,?,?,?,?,?)",
                (gid, handler.character_id, handler.display_name, handler.public_handle, "Leader", now),
            )
            con.commit()
            return True, "created", (gid, guild_name, "Leader")
        except sqlite3.IntegrityError:
            return False, "That guild name already exists.", None


def find_online_handler(character_id: str):
    with clients_lock:
        return online_by_character_id.get(character_id)


def create_invite(inviter, target_identifier: str) -> Tuple[bool, str, Optional[Tuple[int, str, str]], Optional[Tuple[str, str, str, int]]]:
    membership = get_membership(inviter.character_id)
    if not membership:
        return False, "You are not in a Guild.", None, None
    gid, gname, rank, _display, _handle = membership
    if rank != "Leader":
        return False, "Only the guild Leader can invite players.", (gid, gname, rank), None

    target, err = find_user(target_identifier)
    if err:
        return False, err, (gid, gname, rank), None
    target_character_id, target_display_name, target_public_handle, target_serial = target

    if target_character_id == inviter.character_id:
        return False, "You cannot invite yourself.", (gid, gname, rank), target
    if get_membership(target_character_id):
        return False, target_public_handle + " is already in a guild.", (gid, gname, rank), target

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
    return True, target_public_handle, (gid, gname, rank), target


def accept_invite(handler, guild_id_text: str) -> Tuple[bool, str, Optional[Tuple[int, str, str]]]:
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
            SELECT g.id, g.name
            FROM guild_invites gi
            JOIN guilds g ON g.id = gi.guild_id
            WHERE gi.guild_id = ? AND gi.invited_character_id = ?
            """,
            (gid, handler.character_id),
        ).fetchone()
        if not row:
            return False, "No invite found for that guild ID.", None
        gname = str(row[1])
        con.execute(
            "INSERT INTO guild_members(guild_id, character_id, display_name, public_handle, rank, joined_at) VALUES(?,?,?,?,?,?)",
            (gid, handler.character_id, handler.display_name, handler.public_handle, "Member", now),
        )
        con.execute("DELETE FROM guild_invites WHERE invited_character_id = ?", (handler.character_id,))
        con.commit()
    return True, "joined", (gid, gname, "Member")


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

    gid, gname, rank, _display, _handle = membership
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
        self.slot_index = -1
        self.slot_fingerprint = ""
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
        print(f"[hello] {self.client_address} user={self.public_handle} display={self.display_name} id={self.character_id}", flush=True)
        send_identity(self)
        send_line(self, f"WELCOME|{b64_encode(self.public_handle)}")
        send_guild_state(self)
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

    def handle_line(self, line: str):
        parts = line.split("|")
        cmd = parts[0].upper()

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
            try:
                identity = register_or_resume_account_slot(session_token, slot_index, old_character_id, old_secret_token, display_name, slot_fingerprint, recovery_snapshot)
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

        if cmd == "GUILD_CREATE" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            requested = b64_decode(parts[1])
            ok, msg, membership = create_guild(self, requested)
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                send_guild_state(self)
                return
            gid, gname, rank = membership
            print(f"[guild-create] {self.public_handle} created guild #{gid} {gname}", flush=True)
            send_line(self, f"GUILD_CREATED|{gid}|{b64_encode(gname)}|{rank}")
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
            gid, gname, rank = membership
            target_character_id, target_display_name, target_public_handle, _serial = target
            print(f"[guild-invite] {self.public_handle} invited {target_public_handle} to #{gid} {gname}", flush=True)
            send_line(self, f"GUILD_ERROR|{b64_encode('Invited ' + target_public_handle + ' to ' + gname + '.')}")
            target_handler = find_online_handler(target_character_id)
            if target_handler is not None:
                send_line(target_handler, f"GUILD_INVITE|{gid}|{b64_encode(gname)}|{b64_encode(self.public_handle)}")
            return

        if cmd == "GUILD_ACCEPT" and len(parts) >= 2:
            if not self.ensure_registered():
                return
            ok, msg, membership = accept_invite(self, parts[1])
            if not ok:
                send_line(self, f"GUILD_ERROR|{b64_encode(msg)}")
                return
            gid, gname, rank = membership
            print(f"[guild-join] {self.public_handle} joined #{gid} {gname} as {rank}", flush=True)
            send_line(self, f"GUILD_JOINED|{gid}|{b64_encode(gname)}|{rank}")
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
                print(f"[global] {self.public_handle}: {msg}", flush=True)
                # Keep chat pretty by displaying the character name, while logs/db retain the full handle.
                broadcast(f"CHAT|GLOBAL|{b64_encode(self.display_name)}|{b64_encode(msg)}|{int(time.time())}")
                return

            if channel == "GUILD":
                membership = get_membership(self.character_id)
                if not membership:
                    send_line(self, f"GUILD_ERROR|{b64_encode('You are not in a Guild.')}")
                    return
                gid, gname, rank, display_name, public_handle = membership
                display = f"{rank} {display_name}"
                print(f"[guild:{gid}:{gname}] {rank} {public_handle}: {msg}", flush=True)
                broadcast_guild(gid, f"CHAT|GUILD|{b64_encode(display)}|{b64_encode(msg)}|{int(time.time())}")
                return

            send_line(self, f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode('Unknown chat channel.')}|{int(time.time())}")
            return

        if cmd == "PING":
            send_line(self, "PONG")
            return

        send_line(self, f"CHAT|GLOBAL|{b64_encode('System')}|{b64_encode('Unknown command received by social server.')}|{int(time.time())}")


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
    print("Chat messages are in-memory only and are not written to disk.", flush=True)

    with ThreadedTCPServer((args.host, args.port), SocialHandler) as server:
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\nStopping social server...", flush=True)


if __name__ == "__main__":
    main()
