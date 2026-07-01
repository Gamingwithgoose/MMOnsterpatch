#!/usr/bin/env python3
"""WoW-style single-realm config loader for MMOnsterpatch Official Server.

The config file is intentionally INI-based and readable, similar to a
worldserver.conf/worldserver.ini.  Values in the file become the public server
owner knobs; Python constants stay as safe defaults.
"""

from __future__ import annotations

import configparser
import os
from dataclasses import dataclass
from typing import Optional


def _clean_path(path: str) -> str:
    return os.path.abspath(os.path.expandvars(os.path.expanduser(path or "")))


def _server_dir() -> str:
    return os.path.dirname(os.path.abspath(__file__))


def default_config_path() -> str:
    return os.path.join(_server_dir(), "configs", "worldserver.ini")


def _get(cp: configparser.ConfigParser, section: str, key: str, default):
    try:
        if cp.has_option(section, key):
            return cp.get(section, key)
    except Exception:
        pass
    return default


def _get_str(cp, section, key, default="") -> str:
    return str(_get(cp, section, key, default) or "").strip()


def _get_int(cp, section, key, default=0) -> int:
    try:
        return int(str(_get(cp, section, key, default)).strip())
    except Exception:
        return int(default)


def _get_float(cp, section, key, default=0.0) -> float:
    try:
        return float(str(_get(cp, section, key, default)).strip())
    except Exception:
        return float(default)


def _get_bool01(cp, section, key, default=True) -> bool:
    raw = str(_get(cp, section, key, "1" if default else "0")).strip().lower()
    return raw in ("1", "true", "yes", "on", "enabled")


def _rel_to_server(path: str) -> str:
    if not path:
        return ""
    path = os.path.expandvars(path)
    if os.path.isabs(path):
        return _clean_path(path)
    return _clean_path(os.path.join(_server_dir(), path))


@dataclass
class WorldConfig:
    config_path: str

    realm_name: str = "Goose Official"
    realm_type: str = "Official"
    realm_enabled: bool = True

    host: str = "0.0.0.0"
    mmo_port: int = 61528
    social_port: int = 61529
    trading_post_port: int = 61526
    openid_http_host: str = "0.0.0.0"
    openid_http_port: int = 61527
    openid_public_base_url: str = "https://mon-auth.gamingwithgoose.com"
    openid_realm: str = "https://mon-auth.gamingwithgoose.com"
    steam_web_api_key: str = ""

    social_db: str = ""
    chat_log_dir: str = ""
    user_report_dir: str = ""
    archive_dir: str = ""

    snapshot_hz: float = 30.0
    socket_idle_timeout_seconds: float = 0.0

    aio_session_hours: float = 12.0
    require_same_ip_for_session: bool = True

    exp_rate: float = 1.0
    sats_rate: float = 1.0
    # Shiny odds are denominator-style: 1000 = 1/1000, 500 = 1/500, 1 = guaranteed shiny.
    shiny_odds_denominator: float = 1000.0
    catch_rate: float = 1.0
    item_drop_rate: float = 1.0
    random_encounter_rate: float = 1.0
    visible_spawn_rate: float = 2.0
    reward_spawn_rate: float = 1.0
    rp_gain_rate: float = 1.0
    rp_loss_rate: float = 1.0
    season_reward_rate: float = 1.0

    visible_spawns_enabled: bool = True
    visible_spawn_base_chance_percent: int = 25
    visible_spawn_topup_seconds: float = 120.0
    no_player_despawn_seconds: float = 60.0
    max_reported_spawn_zones: int = 128
    pending_spawn_request_timeout_seconds: float = 12.0

    ranked_enabled: bool = False
    rp_writes_enabled: bool = False
    starting_rp: int = 0
    max_rp: int = 1000
    ranked_required_mons: int = 4
    ranked_min_level: int = 50
    ranked_rank_gap_limit: int = 2

    def ensure_dirs(self) -> None:
        for p in [
            os.path.dirname(self.social_db),
            self.chat_log_dir,
            self.user_report_dir,
            self.archive_dir,
            os.path.join(_server_dir(), "logs"),
            os.path.join(_server_dir(), "backups"),
        ]:
            if p:
                os.makedirs(p, exist_ok=True)

    @property
    def visible_spawn_chance_percent(self) -> int:
        value = int(round(float(self.visible_spawn_base_chance_percent) * float(self.visible_spawn_rate)))
        return max(0, min(100, value))


def load_world_config(path: Optional[str] = None) -> WorldConfig:
    path = _clean_path(path or default_config_path())
    cp = configparser.ConfigParser(inline_comment_prefixes=("#", ";"), strict=False)
    cp.optionxform = str
    if os.path.exists(path):
        cp.read(path, encoding="utf-8")

    cfg = WorldConfig(config_path=path)

    cfg.realm_name = _get_str(cp, "Realm", "RealmName", cfg.realm_name)
    cfg.realm_type = _get_str(cp, "Realm", "RealmType", cfg.realm_type)
    cfg.realm_enabled = _get_bool01(cp, "Realm", "RealmEnabled", cfg.realm_enabled)

    cfg.host = _get_str(cp, "Network", "Host", cfg.host)
    cfg.mmo_port = _get_int(cp, "Network", "MMO Port", cfg.mmo_port)
    cfg.social_port = _get_int(cp, "Network", "Social Port", cfg.social_port)
    cfg.trading_post_port = _get_int(cp, "Network", "Trading Post Port", cfg.trading_post_port)
    cfg.openid_http_host = _get_str(cp, "Network", "Steam OpenID HTTP Host", cfg.openid_http_host)
    cfg.openid_http_port = _get_int(cp, "Network", "Steam OpenID HTTP Port", cfg.openid_http_port)
    cfg.openid_public_base_url = _get_str(cp, "Network", "Steam OpenID Public Base URL", cfg.openid_public_base_url)
    cfg.openid_realm = _get_str(cp, "Network", "Steam OpenID Realm", cfg.openid_realm)
    cfg.steam_web_api_key = _get_str(cp, "Network", "Steam Web API Key", os.environ.get("STEAM_WEB_API_KEY", ""))

    cfg.social_db = _rel_to_server(_get_str(cp, "Paths", "Social Database", os.path.join("data", "social.db")))
    cfg.chat_log_dir = _rel_to_server(_get_str(cp, "Paths", "Chat Logs", os.path.join("data", "chat_logs")))
    cfg.user_report_dir = _rel_to_server(_get_str(cp, "Paths", "User Reports", os.path.join("data", "user_reports")))
    cfg.archive_dir = _rel_to_server(_get_str(cp, "Paths", "Archived Characters", os.path.join("data", "Archived Characters")))

    cfg.snapshot_hz = _get_float(cp, "Performance", "Snapshot Hz", cfg.snapshot_hz)
    cfg.socket_idle_timeout_seconds = _get_float(cp, "Performance", "Socket Idle Timeout Seconds", cfg.socket_idle_timeout_seconds)

    cfg.aio_session_hours = _get_float(cp, "Auth", "AIO Session Hours", cfg.aio_session_hours)
    cfg.require_same_ip_for_session = _get_bool01(cp, "Auth", "Require Same IP For Session", cfg.require_same_ip_for_session)

    cfg.exp_rate = _get_float(cp, "Rates", "EXP Rate", cfg.exp_rate)
    cfg.sats_rate = _get_float(cp, "Rates", "SATS Rate", cfg.sats_rate)
    # v0.9.8: shiny odds now use denominator-style config.
    # Preferred key: Shiny Odds Denominator = 1000 means 1/1000; 1 means guaranteed.
    # Backward compatibility: if old Shiny Rate exists and new key does not, map old multiplier to denominator.
    old_shiny_rate = _get_float(cp, "Rates", "Shiny Rate", 1.0)
    cfg.shiny_odds_denominator = _get_float(cp, "Rates", "Shiny Odds Denominator", cfg.shiny_odds_denominator)
    try:
        if not cp.has_option("Rates", "Shiny Odds Denominator") and cp.has_option("Rates", "Shiny Rate"):
            r = float(old_shiny_rate)
            cfg.shiny_odds_denominator = 1000.0 if r <= 0 else max(1.0, 1000.0 / r)
    except Exception:
        pass
    cfg.catch_rate = _get_float(cp, "Rates", "Catch Rate", cfg.catch_rate)
    cfg.item_drop_rate = _get_float(cp, "Rates", "Item Drop Chance Rate", _get_float(cp, "Rates", "Item Drop Rate", cfg.item_drop_rate))
    cfg.random_encounter_rate = _get_float(cp, "Rates", "Random Encounter Rate", cfg.random_encounter_rate)
    cfg.visible_spawn_rate = _get_float(cp, "Rates", "Visible Spawn Rate", cfg.visible_spawn_rate)
    cfg.reward_spawn_rate = _get_float(cp, "Rates", "Reward Spawn Rate", cfg.reward_spawn_rate)
    cfg.rp_gain_rate = _get_float(cp, "Rates", "RP Gain Rate", cfg.rp_gain_rate)
    cfg.rp_loss_rate = _get_float(cp, "Rates", "RP Loss Rate", cfg.rp_loss_rate)
    cfg.season_reward_rate = _get_float(cp, "Rates", "Season Reward Rate", cfg.season_reward_rate)

    cfg.visible_spawns_enabled = _get_bool01(cp, "Overworld Spawns", "Visible Spawns Enabled", cfg.visible_spawns_enabled)
    cfg.visible_spawn_base_chance_percent = _get_int(cp, "Overworld Spawns", "Visible Spawn Base Chance Percent", cfg.visible_spawn_base_chance_percent)
    cfg.visible_spawn_topup_seconds = _get_float(cp, "Overworld Spawns", "Visible Spawn Topup Seconds", cfg.visible_spawn_topup_seconds)
    cfg.no_player_despawn_seconds = _get_float(cp, "Overworld Spawns", "No Player Despawn Seconds", cfg.no_player_despawn_seconds)
    cfg.max_reported_spawn_zones = _get_int(cp, "Overworld Spawns", "Max Reported Spawn Zones", cfg.max_reported_spawn_zones)
    cfg.pending_spawn_request_timeout_seconds = _get_float(cp, "Overworld Spawns", "Pending Spawn Request Timeout Seconds", cfg.pending_spawn_request_timeout_seconds)

    cfg.ranked_enabled = _get_bool01(cp, "Ranked", "Ranked Enabled", cfg.ranked_enabled)
    cfg.rp_writes_enabled = _get_bool01(cp, "Ranked", "RP Writes Enabled", cfg.rp_writes_enabled)
    cfg.starting_rp = _get_int(cp, "Ranked", "Starting RP", cfg.starting_rp)
    cfg.max_rp = _get_int(cp, "Ranked", "Max RP", cfg.max_rp)
    cfg.ranked_required_mons = _get_int(cp, "Ranked", "Required Ranked Mons", cfg.ranked_required_mons)
    cfg.ranked_min_level = _get_int(cp, "Ranked", "Minimum Ranked Level", cfg.ranked_min_level)
    cfg.ranked_rank_gap_limit = _get_int(cp, "Ranked", "Rank Gap Limit", cfg.ranked_rank_gap_limit)

    cfg.ensure_dirs()
    return cfg


def apply_world_config(cfg: WorldConfig, mmo_module=None, social_module=None, trading_module=None) -> None:
    if trading_module is not None:
        setattr(trading_module, "AIO_SESSION_HOURS", float(cfg.aio_session_hours))
        setattr(trading_module, "AIO_SESSION_REQUIRE_SAME_IP", bool(cfg.require_same_ip_for_session))
        setattr(trading_module, "OFFICIAL_ARCHIVE_DIR", cfg.archive_dir)

    if social_module is not None:
        setattr(social_module, "RANKED_MAX_RP", int(cfg.max_rp))
        setattr(social_module, "RANKED_REQUIRED_TEAM_SIZE", int(cfg.ranked_required_mons))
        setattr(social_module, "RANKED_MIN_MON_LEVEL", int(cfg.ranked_min_level))
        setattr(social_module, "RANKED_MAX_RANK_GAP", int(cfg.ranked_rank_gap_limit))
        setattr(social_module, "RANKED_ACTIONS_ENABLED", 1 if cfg.ranked_enabled else 0)
        setattr(social_module, "RANKED_RP_WRITES_ENABLED", 1 if cfg.rp_writes_enabled else 0)
        setattr(social_module, "RANKED_STARTING_RP", int(cfg.starting_rp))
        setattr(social_module, "RP_GAIN_RATE", float(cfg.rp_gain_rate))
        setattr(social_module, "RP_LOSS_RATE", float(cfg.rp_loss_rate))
        setattr(social_module, "SEASON_REWARD_RATE", float(cfg.season_reward_rate))
        setattr(social_module, "OFFICIAL_EXP_RATE", float(cfg.exp_rate))
        setattr(social_module, "OFFICIAL_SATS_RATE", float(cfg.sats_rate))
        setattr(social_module, "OFFICIAL_SHINY_RATE", float(cfg.shiny_odds_denominator))
        setattr(social_module, "OFFICIAL_CATCH_RATE", float(cfg.catch_rate))
        setattr(social_module, "OFFICIAL_ITEM_DROP_RATE", float(cfg.item_drop_rate))
        setattr(social_module, "OFFICIAL_RANDOM_ENCOUNTER_RATE", float(cfg.random_encounter_rate))
        setattr(social_module, "OFFICIAL_VISIBLE_SPAWN_RATE", float(cfg.visible_spawn_rate))
        setattr(social_module, "OFFICIAL_REWARD_SPAWN_RATE", float(cfg.reward_spawn_rate))
        setattr(social_module, "CHAT_LOG_DIR", cfg.chat_log_dir)
        setattr(social_module, "USER_REPORT_DIR", cfg.user_report_dir)
        setattr(social_module, "OFFICIAL_ARCHIVE_DIR", cfg.archive_dir)

    if mmo_module is not None:
        setattr(mmo_module, "VISIBLE_WORLD_SPAWNS_ENABLED", bool(cfg.visible_spawns_enabled))
        setattr(mmo_module, "VANILLA_VISIBLE_SPAWN_CHANCE_PERCENT", int(cfg.visible_spawn_base_chance_percent))
        setattr(mmo_module, "SERVER_VISIBLE_SPAWN_MULTIPLIER", float(cfg.visible_spawn_rate))
        setattr(mmo_module, "SERVER_VISIBLE_SPAWN_CHANCE_PERCENT", int(cfg.visible_spawn_chance_percent))
        setattr(mmo_module, "SPAWN_TOPUP_INTERVAL_SECONDS", float(cfg.visible_spawn_topup_seconds))
        setattr(mmo_module, "MAP_NO_PLAYER_DESPAWN_SECONDS", float(cfg.no_player_despawn_seconds))
        setattr(mmo_module, "MAX_REPORTED_SPAWN_ZONES", int(cfg.max_reported_spawn_zones))
        setattr(mmo_module, "PENDING_SPAWN_REQUEST_TIMEOUT_SECONDS", float(cfg.pending_spawn_request_timeout_seconds))
        setattr(mmo_module, "REWARD_SPAWN_RATE", float(cfg.reward_spawn_rate))
        setattr(mmo_module, "OFFICIAL_EXP_RATE", float(cfg.exp_rate))
        setattr(mmo_module, "OFFICIAL_SATS_RATE", float(cfg.sats_rate))
        setattr(mmo_module, "OFFICIAL_SHINY_RATE", float(cfg.shiny_odds_denominator))
        setattr(mmo_module, "OFFICIAL_CATCH_RATE", float(cfg.catch_rate))
        setattr(mmo_module, "OFFICIAL_ITEM_DROP_RATE", float(cfg.item_drop_rate))
        setattr(mmo_module, "OFFICIAL_RANDOM_ENCOUNTER_RATE", float(cfg.random_encounter_rate))
