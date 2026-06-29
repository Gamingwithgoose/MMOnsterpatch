#!/usr/bin/env python3
"""Shared moderation helpers for MMOnsterpatch server builds.

The ban list lives in the Social database because Social owns the Steam-backed
account/character identity layer. Trading Post and the combined server import
this module so SteamID bans can be enforced before sessions are accepted.
"""
import os
import re
import sqlite3
import time
from typing import Dict, List, Optional

ROOT = os.path.dirname(os.path.abspath(__file__))
DB_PATH = os.path.join(ROOT, "data", "social.db")
DB_LOCK_TIMEOUT = 10


def set_db_path(path: str):
    global DB_PATH
    DB_PATH = path


def _conn():
    if not DB_PATH:
        raise RuntimeError("Moderation DB path is not initialized")
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    return sqlite3.connect(DB_PATH, timeout=DB_LOCK_TIMEOUT)


def normalize_steam_id64(raw: str) -> str:
    value = (raw or "").strip()
    # Admins sometimes paste SteamID64 with punctuation/labels. Keep only digits.
    digits = re.sub(r"\D+", "", value)
    if len(digits) < 10:
        return ""
    return digits


def ensure_moderation_schema(path: Optional[str] = None):
    if path:
        set_db_path(path)
    with _conn() as con:
        con.execute("PRAGMA journal_mode=WAL")
        con.execute(
            """
            CREATE TABLE IF NOT EXISTS account_bans (
                ban_id INTEGER PRIMARY KEY AUTOINCREMENT,
                steam_id64 TEXT NOT NULL,
                account_uuid TEXT NOT NULL DEFAULT '',
                reason TEXT NOT NULL DEFAULT '',
                evidence_note TEXT NOT NULL DEFAULT '',
                banned_by TEXT NOT NULL DEFAULT 'console',
                banned_at INTEGER NOT NULL,
                expires_at INTEGER NOT NULL DEFAULT 0,
                active INTEGER NOT NULL DEFAULT 1,
                unbanned_by TEXT NOT NULL DEFAULT '',
                unbanned_at INTEGER NOT NULL DEFAULT 0,
                unban_reason TEXT NOT NULL DEFAULT ''
            )
            """
        )
        con.execute("CREATE INDEX IF NOT EXISTS idx_account_bans_steam_active ON account_bans(steam_id64, active)")
        con.execute("CREATE INDEX IF NOT EXISTS idx_account_bans_account_active ON account_bans(account_uuid, active)")
        con.commit()


def _known_account_uuid(con, steam_id64: str) -> str:
    try:
        row = con.execute("SELECT account_uuid FROM accounts WHERE steam_id64=?", (steam_id64,)).fetchone()
        if row:
            return str(row[0] or "")
    except Exception:
        pass
    return ""


def get_active_ban(steam_id64: str = "", account_uuid: str = "") -> Optional[Dict[str, object]]:
    steam_id64 = normalize_steam_id64(steam_id64)
    account_uuid = (account_uuid or "").strip()
    if not steam_id64 and not account_uuid:
        return None
    now = int(time.time())
    ensure_moderation_schema()
    with _conn() as con:
        if steam_id64:
            row = con.execute(
                """
                SELECT ban_id, steam_id64, account_uuid, reason, evidence_note, banned_by, banned_at, expires_at, active
                FROM account_bans
                WHERE steam_id64=? AND active=1 AND (expires_at=0 OR expires_at>?)
                ORDER BY banned_at DESC, ban_id DESC
                LIMIT 1
                """,
                (steam_id64, now),
            ).fetchone()
        else:
            row = con.execute(
                """
                SELECT ban_id, steam_id64, account_uuid, reason, evidence_note, banned_by, banned_at, expires_at, active
                FROM account_bans
                WHERE account_uuid=? AND active=1 AND (expires_at=0 OR expires_at>?)
                ORDER BY banned_at DESC, ban_id DESC
                LIMIT 1
                """,
                (account_uuid, now),
            ).fetchone()
    if not row:
        return None
    return {
        "ban_id": int(row[0]),
        "steam_id64": str(row[1] or ""),
        "account_uuid": str(row[2] or ""),
        "reason": str(row[3] or ""),
        "evidence_note": str(row[4] or ""),
        "banned_by": str(row[5] or ""),
        "banned_at": int(row[6] or 0),
        "expires_at": int(row[7] or 0),
        "active": bool(int(row[8] or 0)),
    }


def is_banned(steam_id64: str = "", account_uuid: str = "") -> bool:
    return get_active_ban(steam_id64, account_uuid) is not None


def ban_steam(steam_id64: str, reason: str = "", banned_by: str = "console", evidence_note: str = "", expires_at: int = 0) -> Dict[str, object]:
    steam_id64 = normalize_steam_id64(steam_id64)
    if not steam_id64:
        raise ValueError("A valid SteamID64 is required.")
    ensure_moderation_schema()
    now = int(time.time())
    with _conn() as con:
        account_uuid = _known_account_uuid(con, steam_id64)
        # Keep the old ban history, but make repeated ban commands update/replace the active ban state cleanly.
        con.execute(
            """
            UPDATE account_bans
            SET active=0, unbanned_by=?, unbanned_at=?, unban_reason=?
            WHERE steam_id64=? AND active=1
            """,
            (banned_by or "console", now, "replaced by new active ban", steam_id64),
        )
        cur = con.execute(
            """
            INSERT INTO account_bans(steam_id64, account_uuid, reason, evidence_note, banned_by, banned_at, expires_at, active)
            VALUES(?,?,?,?,?,?,?,1)
            """,
            (steam_id64, account_uuid, reason or "No reason provided.", evidence_note or "", banned_by or "console", now, int(expires_at or 0)),
        )
        con.commit()
        ban_id = int(cur.lastrowid)
    return {
        "ban_id": ban_id,
        "steam_id64": steam_id64,
        "account_uuid": account_uuid,
        "reason": reason or "No reason provided.",
        "banned_by": banned_by or "console",
        "banned_at": now,
        "expires_at": int(expires_at or 0),
        "active": True,
    }


def unban_steam(steam_id64: str, unban_reason: str = "", unbanned_by: str = "console") -> int:
    steam_id64 = normalize_steam_id64(steam_id64)
    if not steam_id64:
        raise ValueError("A valid SteamID64 is required.")
    ensure_moderation_schema()
    now = int(time.time())
    with _conn() as con:
        cur = con.execute(
            """
            UPDATE account_bans
            SET active=0, unbanned_by=?, unbanned_at=?, unban_reason=?
            WHERE steam_id64=? AND active=1
            """,
            (unbanned_by or "console", now, unban_reason or "Unbanned by admin.", steam_id64),
        )
        con.commit()
        return int(cur.rowcount or 0)


def list_active_bans(limit: int = 50) -> List[Dict[str, object]]:
    ensure_moderation_schema()
    now = int(time.time())
    with _conn() as con:
        rows = con.execute(
            """
            SELECT ban_id, steam_id64, account_uuid, reason, banned_by, banned_at, expires_at
            FROM account_bans
            WHERE active=1 AND (expires_at=0 OR expires_at>?)
            ORDER BY banned_at DESC, ban_id DESC
            LIMIT ?
            """,
            (now, int(limit or 50)),
        ).fetchall()
    return [
        {
            "ban_id": int(r[0]),
            "steam_id64": str(r[1] or ""),
            "account_uuid": str(r[2] or ""),
            "reason": str(r[3] or ""),
            "banned_by": str(r[4] or ""),
            "banned_at": int(r[5] or 0),
            "expires_at": int(r[6] or 0),
        }
        for r in rows
    ]
