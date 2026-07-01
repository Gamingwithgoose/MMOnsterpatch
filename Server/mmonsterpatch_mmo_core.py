#!/usr/bin/env python3
import argparse
import os
import socketserver
import threading
import time
import random
from dataclasses import dataclass, field
from urllib.parse import quote, unquote

@dataclass
class PlayerState:
    conn_id: str
    player_id: str
    name: str
    cluster: str
    scene: str
    x: float
    y: float
    z: float
    facing: int
    moving: bool
    design: int
    color1: int
    color2: int
    status: str
    follower_enabled: bool = False
    follower_x: float = 0.0
    follower_y: float = 0.0
    follower_facing: int = 0
    follower_moving: bool = False
    follower_sprite: str = ""
    follower_flip_x: bool = False
    last_seen: float = 0.0
    riding_broom: bool = False
    water_walking: bool = False
    jumping_or_bouncing: bool = False
    bouncing_name: str = ""

@dataclass
class WorldSpawn:
    spawn_id: str
    cluster: str
    scene: str
    x: float
    y: float
    z: float
    mon_id: int
    mon_key: str
    shiny: bool
    level: int
    state: str = "available"
    owner_id: str = ""
    created_at: float = 0.0
    updated_at: float = 0.0
    zone_id: str = ""
    lock_state: str = "public"          # public | owner_only
    personal_owner_id: str = ""        # player who owns a personal encounter reward spawn
    mon_save_b64: str = ""             # optional exact Mon save payload for personal encounter reward spawns
    version_requirement: str = "None"   # None | Skyfarer | Aurora | Eldenwood | unknown

@dataclass
class MapSpawnState:
    cluster: str
    scene: str
    zones: list = field(default_factory=list)
    signature: str = ""
    last_zone_report: float = 0.0
    last_spawn_pass: float = 0.0
    no_players_since: float = 0.0

@dataclass
class PendingWorldSpawnRequest:
    request_id: str
    cluster: str
    scene: str
    zone_id: str
    x: float
    y: float
    z: float
    rarity: str
    requested_at: float
    requester_conn_id: str = ""

players_lock = threading.Lock()
players_by_conn = {}
handlers_by_conn = {}

# ---------------------------------------------------------------------------
# Server-owned visible overworld spawns
# ---------------------------------------------------------------------------
# Vanilla WildMonSpawnManager rolls 25% per spawnZone. The server owns the
# rate and applies it as chance per empty deduped spawn tile. Clients
# only report/dedupe map spawnZone positions and generate a species/level payload when
# the server asks; clients do not control rate, caps, or timing.
world_spawns_lock = threading.RLock()
world_spawns_by_id = {}
world_map_states = {}
pending_world_spawn_requests = {}
world_spawn_seq = 0
world_spawn_request_seq = 0

VISIBLE_WORLD_SPAWNS_ENABLED = True
VANILLA_VISIBLE_SPAWN_CHANCE_PERCENT = 25
SERVER_VISIBLE_SPAWN_MULTIPLIER = 2
SERVER_VISIBLE_SPAWN_CHANCE_PERCENT = min(100, VANILLA_VISIBLE_SPAWN_CHANCE_PERCENT * SERVER_VISIBLE_SPAWN_MULTIPLIER)
MAP_NO_PLAYER_DESPAWN_SECONDS = 300.0
SPAWN_TOPUP_INTERVAL_SECONDS = 120.0
MAX_REPORTED_SPAWN_ZONES = 128
PENDING_SPAWN_REQUEST_TIMEOUT_SECONDS = 12.0
NO_PERMISSION_CAPTURE_MESSAGE = "You don't have permission to capture this MoN."

def enc(s): return quote(str(s), safe="")
def dec(s): return unquote(s)
def parse_bool(s): return s == "1" or s.lower() == "true"

def fmt_player(p):
    return "|".join([
        enc(p.player_id), enc(p.name), enc(p.cluster), enc(p.scene),
        f"{p.x:.4f}", f"{p.y:.4f}", f"{p.z:.4f}",
        str(int(p.facing)), "1" if p.moving else "0",
        str(int(p.design)), str(int(p.color1)), str(int(p.color2)), enc(getattr(p, "status", "available")),
        "1" if getattr(p, "follower_enabled", False) else "0",
        f"{getattr(p, 'follower_x', 0.0):.4f}",
        f"{getattr(p, 'follower_y', 0.0):.4f}",
        str(int(getattr(p, "follower_facing", 0))),
        "1" if getattr(p, "follower_moving", False) else "0",
        enc(getattr(p, "follower_sprite", "")),
        "1" if getattr(p, "follower_flip_x", False) else "0",
        "1" if getattr(p, "riding_broom", False) else "0",
        "1" if getattr(p, "water_walking", False) else "0",
        "1" if getattr(p, "jumping_or_bouncing", False) else "0",
        enc(getattr(p, "bouncing_name", "") or ""),
    ])

def make_snapshot(for_conn_id):
    now = time.time()
    with players_lock:
        stale = [cid for cid, p in players_by_conn.items() if now - p.last_seen > 30.0]
        for cid in stale:
            print(f"[timeout] {players_by_conn[cid].name} / {players_by_conn[cid].player_id}")
            del players_by_conn[cid]
            handlers_by_conn.pop(cid, None)

        me = players_by_conn.get(for_conn_id)
        if not me:
            return "SNAP|"

        remotes = [
            p for cid, p in players_by_conn.items()
            if cid != for_conn_id and p.cluster == me.cluster and p.scene == me.scene and p.scene not in ("", "unknown")
        ]

    return "SNAP|" + ";".join(fmt_player(p) for p in remotes)

def fmt_spawn(s):
    return "|".join([
        enc(s.spawn_id), enc(s.cluster), enc(s.scene),
        f"{s.x:.4f}", f"{s.y:.4f}", f"{s.z:.4f}",
        str(int(s.mon_id)), "1" if s.shiny else "0", str(int(s.level)),
        enc(s.state), enc(s.owner_id or ""), enc(getattr(s, "mon_key", "") or ""),
        enc(getattr(s, "lock_state", "public") or "public"),
        enc(getattr(s, "personal_owner_id", "") or ""),
        enc(getattr(s, "mon_save_b64", "") or ""),
        enc(getattr(s, "version_requirement", "None") or "None")
    ])

def visible_world_spawns_for_conn(conn_id):
    with players_lock:
        me = players_by_conn.get(conn_id)
    if not me or me.scene in ("", "unknown"):
        return []

    with world_spawns_lock:
        return [
            s for s in world_spawns_by_id.values()
            if s.cluster == me.cluster
            and s.scene == me.scene
            and s.state == "available"
        ]

def make_world_spawn_snapshot(conn_id):
    spawns = visible_world_spawns_for_conn(conn_id)
    return "WORLD_SPAWNS|" + ";".join(fmt_spawn(s) for s in spawns)

def broadcast_world_spawn_remove(spawn, exclude_conn_id=None):
    if spawn is None:
        return
    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if p.cluster == spawn.cluster and p.scene == spawn.scene
            and (exclude_conn_id is None or cid != exclude_conn_id)
        ]
    line = "WORLD_SPAWN_REMOVE|" + enc(spawn.spawn_id)
    for cid in targets:
        send_to_conn(cid, line)

def broadcast_world_spawn_add(spawn, exclude_conn_id=None):
    if spawn is None:
        return
    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if p.cluster == spawn.cluster and p.scene == spawn.scene
            and (exclude_conn_id is None or cid != exclude_conn_id)
        ]
    line = "WORLD_SPAWN_ADD|" + fmt_spawn(spawn)
    for cid in targets:
        send_to_conn(cid, line)

def broadcast_world_spawn_lock_update(spawn, reason="OWNER_LEFT_MAP", exclude_conn_id=None):
    if spawn is None:
        return
    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if p.cluster == spawn.cluster and p.scene == spawn.scene
            and (exclude_conn_id is None or cid != exclude_conn_id)
        ]
    line = "WORLD_SPAWN_LOCK_UPDATE|" + "|".join([
        enc(spawn.spawn_id),
        enc(getattr(spawn, "lock_state", "public") or "public"),
        enc(getattr(spawn, "personal_owner_id", "") or ""),
        enc(reason or ""),
    ])
    for cid in targets:
        send_to_conn(cid, line)

def world_spawn_version_requirement_is_shared(spawn):
    try:
        req = (getattr(spawn, "version_requirement", "") or "").strip().lower()
    except Exception:
        req = ""
    # VersionRequirement.None means the encounter entry is shared by all versions.
    # Unknown/blank personal reward metadata is treated as not safely shareable,
    # so it will be deleted instead of unlocked when the owner leaves the map.
    return req in ("none", "shared", "all", "both")

def unlock_personal_spawns_for_owner_left_map(player_id, cluster, scene, reason="OWNER_LEFT_MAP"):
    if not player_id or not cluster or scene in ("", "unknown"):
        return
    unlocked = []
    removed = []
    with world_spawns_lock:
        for sid, spawn in list(world_spawns_by_id.items()):
            if spawn.cluster != cluster or spawn.scene != scene:
                continue
            if getattr(spawn, "lock_state", "public") != "owner_only":
                continue
            if (getattr(spawn, "personal_owner_id", "") or "") != player_id:
                continue

            if world_spawn_version_requirement_is_shared(spawn):
                spawn.lock_state = "public"
                spawn.personal_owner_id = ""
                spawn.updated_at = time.time()
                unlocked.append(spawn)
            else:
                spawn.state = "despawned"
                spawn.updated_at = time.time()
                removed.append(spawn)
                world_spawns_by_id.pop(sid, None)

    for spawn in removed:
        print(f"[personal-spawn-delete] spawn={spawn.spawn_id} owner={player_id} scene={scene} versionRequirement={getattr(spawn, 'version_requirement', '')} reason={reason}; owner left map, deleting non-shared prize spawn")
        broadcast_world_spawn_remove(spawn)

    for spawn in unlocked:
        print(f"[personal-spawn-unlock] spawn={spawn.spawn_id} owner={player_id} scene={scene} reason={reason}; shared-version prize remains visible and flips public")
        # Lock updates must not be treated like removal/despawn. Send a small
        # metadata-only update for clients that already have the MonObject, then
        # also send a normal add/update record so late or stale clients can repair
        # their local copy from the server snapshot path.
        broadcast_world_spawn_lock_update(spawn, reason=reason)
        broadcast_world_spawn_add(spawn)

def world_spawn_is_locked_for_player(spawn, player_id):
    if spawn is None:
        return False
    if getattr(spawn, "lock_state", "public") != "owner_only":
        return False
    owner = getattr(spawn, "personal_owner_id", "") or ""
    return bool(owner and owner != (player_id or ""))

def broadcast_world_spawn_catch_start(spawn, owner_player_id, exclude_conn_id=None):
    if spawn is None:
        return
    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if p.cluster == spawn.cluster and p.scene == spawn.scene
            and (exclude_conn_id is None or cid != exclude_conn_id)
        ]
    line = "WORLD_SPAWN_CATCH_START|" + enc(owner_player_id or "") + "|" + enc(spawn.spawn_id)
    for cid in targets:
        send_to_conn(cid, line)

def broadcast_world_spawn_catch_end(spawn, owner_player_id, result, exclude_conn_id=None):
    if spawn is None:
        return
    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if p.cluster == spawn.cluster and p.scene == spawn.scene
            and (exclude_conn_id is None or cid != exclude_conn_id)
        ]
    line = "WORLD_SPAWN_CATCH_END|" + enc(owner_player_id or "") + "|" + enc(spawn.spawn_id) + "|" + enc(result or "")
    for cid in targets:
        send_to_conn(cid, line)

def map_key(cluster, scene):
    return (cluster or "", scene or "")

def active_player_count_for_map(cluster, scene):
    with players_lock:
        return len([
            p for p in players_by_conn.values()
            if p.cluster == cluster and p.scene == scene and p.scene not in ("", "unknown")
        ])

def choose_world_spawn_generator(cluster, scene, preferred_conn_id=None):
    with players_lock:
        if preferred_conn_id:
            p = players_by_conn.get(preferred_conn_id)
            if p and p.cluster == cluster and p.scene == scene:
                return preferred_conn_id
        for cid, p in players_by_conn.items():
            if p.cluster == cluster and p.scene == scene and p.scene not in ("", "unknown"):
                return cid
    return None

def world_spawn_tile_key(x, y):
    # Tile/position identity used by server-owned visible spawns.
    # This intentionally dedupes duplicate vanilla spawnZone objects at the same tile
    # so Visible Spawn Rate changes chance only, never quantity on one tile.
    try:
        return f"{round(float(x), 2):.2f}:{round(float(y), 2):.2f}"
    except Exception:
        return ""

def parse_world_spawn_zones(zone_body):
    zones = []
    seen_tiles = set()
    if not zone_body:
        return zones
    records = [r for r in zone_body.split(";") if r.strip()]
    for rec in records[:MAX_REPORTED_SPAWN_ZONES]:
        parts = rec.split(",")
        if len(parts) < 4:
            continue
        try:
            client_zone_id = dec(parts[0]).strip()
            x = float(parts[1])
            y = float(parts[2])
            z = float(parts[3])
            tile_key = world_spawn_tile_key(x, y) or client_zone_id or str(len(zones))
            if tile_key in seen_tiles:
                continue
            seen_tiles.add(tile_key)
            zones.append({"zone_id": tile_key, "client_zone_id": client_zone_id, "x": x, "y": y, "z": z})
        except Exception:
            continue
    return zones

def get_occupied_world_spawn_zone_ids(cluster, scene):
    occupied = set()
    for s in world_spawns_by_id.values():
        if s.cluster == cluster and s.scene == scene and s.state in ("available", "busy"):
            occupied.add(getattr(s, "zone_id", "") or "")
    for req in pending_world_spawn_requests.values():
        if req.cluster == cluster and req.scene == scene:
            occupied.add(req.zone_id or "")
    occupied.discard("")
    return occupied

def roll_visible_world_spawn_rarity():
    # Official visible overworld table from WildMonSpawnManager:
    # rand < 5 = rare, rand >= 35 = common, otherwise uncommon.
    r = random.randrange(0, 100)
    if r < 5:
        return "rare"
    if r >= 35:
        return "common"
    return "uncommon"

def request_world_spawn_generation(cluster, scene, zone, preferred_conn_id=None):
    global world_spawn_request_seq
    generator_conn = choose_world_spawn_generator(cluster, scene, preferred_conn_id)
    if not generator_conn:
        return False

    world_spawn_request_seq += 1
    request_id = f"owreq-{int(time.time())}-{world_spawn_request_seq}"
    req = PendingWorldSpawnRequest(
        request_id=request_id,
        cluster=cluster,
        scene=scene,
        zone_id=str(zone.get("zone_id", "")),
        x=float(zone.get("x", 0.0)),
        y=float(zone.get("y", 0.0)),
        z=float(zone.get("z", 0.0)),
        rarity=roll_visible_world_spawn_rarity(),
        requested_at=time.time(),
        requester_conn_id=generator_conn,
    )
    pending_world_spawn_requests[request_id] = req
    send_to_conn(generator_conn, "WORLD_SPAWN_GEN_REQ|" + "|".join([
        enc(req.request_id), enc(req.scene), enc(req.rarity), enc(req.zone_id),
        f"{req.x:.4f}", f"{req.y:.4f}", f"{req.z:.4f}",
    ]))
    print(f"[world-spawn-gen-req] scene={scene} zone={req.zone_id} rarity={req.rarity} to={generator_conn}")
    return True

def run_world_spawn_pass_for_map(cluster, scene, preferred_conn_id=None, force=False):
    if not VISIBLE_WORLD_SPAWNS_ENABLED or scene in ("", "unknown"):
        return
    key = map_key(cluster, scene)
    state = world_map_states.get(key)
    if not state or not state.zones:
        return
    if active_player_count_for_map(cluster, scene) <= 0:
        return

    now = time.time()
    if not force and now - state.last_spawn_pass < SPAWN_TOPUP_INTERVAL_SECONDS:
        return

    occupied = get_occupied_world_spawn_zone_ids(cluster, scene)
    made_requests = 0
    for zone in list(state.zones):
        zone_id = str(zone.get("zone_id", ""))
        if zone_id and zone_id in occupied:
            continue
        if random.randrange(0, 100) >= SERVER_VISIBLE_SPAWN_CHANCE_PERCENT:
            continue
        if request_world_spawn_generation(cluster, scene, zone, preferred_conn_id=preferred_conn_id):
            made_requests += 1
            if zone_id:
                occupied.add(zone_id)

    state.last_spawn_pass = now
    if made_requests:
        print(f"[world-spawn-pass] scene={scene} zones={len(state.zones)} chance={SERVER_VISIBLE_SPAWN_CHANCE_PERCENT}% requested={made_requests}")

def receive_world_spawn_zones(conn_id, player_id, cluster, scene, signature, zone_body):
    if not VISIBLE_WORLD_SPAWNS_ENABLED or scene in ("", "unknown"):
        return

    with players_lock:
        p = players_by_conn.get(conn_id)
    if not p:
        return
    if p.cluster != cluster or p.scene != scene:
        print(f"[world-spawn-zones-ignored] conn={conn_id} player map={p.cluster}/{p.scene} packet map={cluster}/{scene}")
        return

    zones = parse_world_spawn_zones(zone_body)
    if not zones:
        return

    now = time.time()
    key = map_key(cluster, scene)
    with world_spawns_lock:
        state = world_map_states.get(key)
        first_report = state is None
        signature_changed = first_report or state.signature != (signature or "")
        if state is None:
            state = MapSpawnState(cluster=cluster, scene=scene)
            world_map_states[key] = state
        state.zones = zones
        state.signature = signature or ""
        state.last_zone_report = now
        if state.no_players_since:
            state.no_players_since = 0.0

    if signature_changed or first_report:
        print(f"[world-spawn-zones] scene={scene} zones={len(zones)} signature={signature}")
        run_world_spawn_pass_for_map(cluster, scene, preferred_conn_id=conn_id, force=True)

def complete_world_spawn_generation(conn_id, player_id, request_id, mon_id, mon_key, shiny, level, version_requirement="None"):
    global world_spawn_seq
    with world_spawns_lock:
        req = pending_world_spawn_requests.pop(request_id, None)
        if req is None:
            print(f"[world-spawn-gen-result-ignored] missing request {request_id}")
            return

    with players_lock:
        player = players_by_conn.get(conn_id)
    if not player or player.cluster != req.cluster or player.scene != req.scene:
        print(f"[world-spawn-gen-result-ignored] wrong map request={request_id}")
        return

    try:
        mon_id = int(mon_id)
    except Exception:
        mon_id = -1
    try:
        level = max(1, int(level))
    except Exception:
        level = 1

    version_requirement = str(version_requirement or "None").strip() or "None"
    if version_requirement.lower() not in ("none", "shared", "all", "both"):
        print(f"[world-spawn-gen-result-rejected] request={request_id} scene={req.scene} mon={mon_id}/{mon_key} versionRequirement={version_requirement}; server overworld spawns exclude version-exclusive mons")
        return

    with world_spawns_lock:
        world_spawn_seq += 1
        spawn_id = f"ow-{req.cluster}-{req.scene}-{int(time.time())}-{world_spawn_seq}"
        now = time.time()
        spawn = WorldSpawn(
            spawn_id=spawn_id,
            cluster=req.cluster,
            scene=req.scene,
            x=float(req.x),
            y=float(req.y),
            z=float(req.z) if abs(float(req.z)) > 0.0001 else float(req.y) * 0.01,
            mon_id=mon_id,
            mon_key=str(mon_key or ""),
            shiny=bool(shiny),
            level=level,
            state="available",
            owner_id="",
            created_at=now,
            updated_at=now,
            zone_id=req.zone_id,
            lock_state="public",
            personal_owner_id="",
            mon_save_b64="",
            version_requirement="None",
        )
        world_spawns_by_id[spawn_id] = spawn

    print(f"[world-spawn-add] {spawn.spawn_id} scene={spawn.scene} zone={req.zone_id} pos=({spawn.x:.2f},{spawn.y:.2f}) mon={spawn.mon_id} mon_key={spawn.mon_key} shiny={spawn.shiny} level={spawn.level}")
    broadcast_world_spawn_add(spawn)

def receive_personal_encounter_spawns(conn_id, player_id, cluster, scene, records_body):
    global world_spawn_seq
    if not VISIBLE_WORLD_SPAWNS_ENABLED:
        return
    with players_lock:
        player = players_by_conn.get(conn_id)
    if not player:
        return
    if player.cluster != cluster or player.scene != scene or scene in ("", "unknown"):
        print(f"[personal-spawn-ignored] wrong map packet={cluster}/{scene} actual={getattr(player, 'cluster', '')}/{getattr(player, 'scene', '')}")
        return

    records = [r for r in (records_body or "").split(";") if r.strip()]
    if not records:
        return

    created = []
    now = time.time()
    with world_spawns_lock:
        for rec in records[:4]:
            parts = rec.split(",")
            if len(parts) < 9:
                continue
            try:
                slot = parts[0]
                x = float(parts[1])
                y = float(parts[2])
                z = float(parts[3])
                mon_id = int(parts[4]) if parts[4] else -1
                shiny = parse_bool(parts[5])
                level = int(parts[6]) if parts[6] else 1
                mon_key = dec(parts[7])
                mon_save_b64 = dec(parts[8])
                version_requirement = dec(parts[9]) if len(parts) > 9 else "unknown"
            except Exception:
                continue
            world_spawn_seq += 1
            spawn_id = f"owp-{cluster}-{scene}-{int(now)}-{world_spawn_seq}"
            spawn = WorldSpawn(
                spawn_id=spawn_id,
                cluster=cluster,
                scene=scene,
                x=x,
                y=y,
                z=z if abs(z) > 0.0001 else y * 0.01,
                mon_id=mon_id,
                mon_key=mon_key or "",
                shiny=bool(shiny),
                level=max(1, int(level)),
                state="available",
                owner_id="",
                created_at=now,
                updated_at=now,
                zone_id="personal:" + str(slot),
                lock_state="owner_only",
                personal_owner_id=player_id or player.player_id,
                mon_save_b64=mon_save_b64 or "",
                version_requirement=version_requirement or "unknown",
            )
            world_spawns_by_id[spawn_id] = spawn
            created.append(spawn)

    for spawn in created:
        print(f"[personal-spawn-add] {spawn.spawn_id} owner={spawn.personal_owner_id} scene={scene} mon={spawn.mon_id} shiny={spawn.shiny} level={spawn.level} versionRequirement={getattr(spawn, 'version_requirement', '')}")
        broadcast_world_spawn_add(spawn)

def maintain_world_spawn_maps():
    if not VISIBLE_WORLD_SPAWNS_ENABLED:
        return
    now = time.time()
    with world_spawns_lock:
        # Drop stale generation requests so a failed generator client cannot block a zone forever.
        stale_req_ids = [
            rid for rid, req in pending_world_spawn_requests.items()
            if now - req.requested_at > PENDING_SPAWN_REQUEST_TIMEOUT_SECONDS
        ]
        for rid in stale_req_ids:
            req = pending_world_spawn_requests.pop(rid, None)
            if req:
                print(f"[world-spawn-gen-timeout] {rid} scene={req.scene} zone={req.zone_id}")

        states = list(world_map_states.items())

    for key, state in states:
        cluster, scene = key
        count = active_player_count_for_map(cluster, scene)
        if count <= 0:
            remove_now = False
            with world_spawns_lock:
                current_state = world_map_states.get(key)
                if not current_state:
                    continue
                if current_state.no_players_since <= 0.0:
                    current_state.no_players_since = now
                elif now - current_state.no_players_since >= MAP_NO_PLAYER_DESPAWN_SECONDS:
                    remove_now = True
                    world_map_states.pop(key, None)
                    for rid, req in list(pending_world_spawn_requests.items()):
                        if req.cluster == cluster and req.scene == scene:
                            pending_world_spawn_requests.pop(rid, None)
                    for sid, spawn in list(world_spawns_by_id.items()):
                        if spawn.cluster == cluster and spawn.scene == scene:
                            world_spawns_by_id.pop(sid, None)
            if remove_now:
                print(f"[world-spawn-map-despawn] scene={scene} empty_for={MAP_NO_PLAYER_DESPAWN_SECONDS:g}s")
            continue

        with world_spawns_lock:
            current_state = world_map_states.get(key)
            if current_state:
                current_state.no_players_since = 0.0
                due = current_state.zones and (now - current_state.last_spawn_pass >= SPAWN_TOPUP_INTERVAL_SECONDS)
            else:
                due = False
        if due:
            run_world_spawn_pass_for_map(cluster, scene, force=False)

def claim_world_spawn(conn_id, player_id, spawn_id):
    with players_lock:
        player = players_by_conn.get(conn_id)

    if not player:
        send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_FAIL|" + "|".join([
            enc(spawn_id), enc("Player not registered")
        ]))
        return

    with world_spawns_lock:
        spawn = world_spawns_by_id.get(spawn_id)
        if spawn is None:
            send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_FAIL|" + "|".join([
                enc(spawn_id), enc("Spawn is gone")
            ]))
            return

        if spawn.cluster != player.cluster or spawn.scene != player.scene:
            send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_FAIL|" + "|".join([
                enc(spawn_id), enc("Spawn is not in this area")
            ]))
            return

        expected_owner = player_id or player.player_id
        if world_spawn_is_locked_for_player(spawn, expected_owner):
            send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_FAIL|" + "|".join([
                enc(spawn.spawn_id), enc(NO_PERMISSION_CAPTURE_MESSAGE)
            ]))
            return
        if spawn.state != "available":
            if spawn.state == "busy" and (spawn.owner_id or "") == expected_owner:
                send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_OK|" + enc(spawn.spawn_id))
                return
            send_to_conn(conn_id, "WORLD_SPAWN_BUSY|" + "|".join([
                enc(spawn.spawn_id), enc(spawn.owner_id or "")
            ]))
            return

        # First processed claim wins.
        spawn.state = "busy"
        spawn.owner_id = expected_owner
        spawn.updated_at = time.time()

    print(f"[world-spawn-claim] {player.name}({player.player_id}) claimed {spawn.spawn_id}")
    send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_OK|" + enc(spawn.spawn_id))
    # v0.6.3: claim only locks ownership. Remote catch FX now starts when the
    # claimant actually presses Catch, via WORLD_SPAWN_CATCH_START.

def start_world_spawn_catch_animation(conn_id, player_id, spawn_id):
    with players_lock:
        player = players_by_conn.get(conn_id)

    if not player:
        return

    with world_spawns_lock:
        spawn = world_spawns_by_id.get(spawn_id)
        if spawn is None:
            return

        owner = spawn.owner_id or ""
        expected_owner = player_id or player.player_id
        if world_spawn_is_locked_for_player(spawn, expected_owner):
            send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_FAIL|" + "|".join([
                enc(spawn.spawn_id), enc(NO_PERMISSION_CAPTURE_MESSAGE)
            ]))
            return

        # v0.6.4: catch-start is allowed to be the first packet that reaches the
        # server. If the spawn is still available, claim it for this player and
        # relay the catch FX immediately. TCP order normally sends CLAIM first,
        # but this makes the animation path robust instead of silently ignoring.
        if spawn.state == "available":
            spawn.state = "busy"
            spawn.owner_id = expected_owner
            spawn.updated_at = time.time()
            owner = expected_owner
            send_to_conn(conn_id, "WORLD_SPAWN_CLAIM_OK|" + enc(spawn.spawn_id))
        elif spawn.state != "busy" or (owner and expected_owner and owner != expected_owner):
            print(f"[world-spawn-catch-start-ignored] spawn={spawn_id} state={getattr(spawn, 'state', '')} owner={owner} from={expected_owner}")
            return

    print(f"[world-spawn-catch-start] {player.name}({player.player_id}) started catch animation for {spawn.spawn_id}")
    broadcast_world_spawn_catch_start(spawn, player.player_id, exclude_conn_id=conn_id)

def finish_world_spawn_claim(conn_id, player_id, spawn_id, result):
    result = (result or "").strip().lower()
    caught = result in ("caught", "catch", "success", "1", "true", "yes")

    with players_lock:
        player = players_by_conn.get(conn_id)

    if not player:
        return

    with world_spawns_lock:
        spawn = world_spawns_by_id.get(spawn_id)
        if spawn is None:
            return

        owner = spawn.owner_id or ""
        expected_owner = player_id or player.player_id
        if spawn.state != "busy" or (owner and expected_owner and owner != expected_owner):
            print(f"[world-spawn-result-ignored] spawn={spawn_id} result={result} state={getattr(spawn, 'state', '')} owner={owner} from={expected_owner}")
            return

        if caught:
            spawn.state = "despawned"
            spawn.updated_at = time.time()
        else:
            spawn.state = "available"
            spawn.owner_id = ""
            spawn.updated_at = time.time()

    if caught:
        print(f"[world-spawn-caught] {player.name}({player.player_id}) caught {spawn.spawn_id}; despawning globally")
        broadcast_world_spawn_catch_end(spawn, player.player_id, "caught")
        broadcast_world_spawn_remove(spawn)
    else:
        print(f"[world-spawn-released] {player.name}({player.player_id}) failed catch for {spawn.spawn_id}; making available again")
        broadcast_world_spawn_catch_end(spawn, player.player_id, "failed")
        broadcast_world_spawn_add(spawn)

def send_to_conn(conn_id, text):
    with players_lock:
        handler = handlers_by_conn.get(conn_id)
    if not handler:
        return
    try:
        if text.startswith("WORLD_SPAWN_CATCH") or text.startswith("WORLD_SPAWN_CLAIM") or text.startswith("WORLD_SPAWN_REMOVE") or text.startswith("WORLD_SPAWN_LOCK_UPDATE"):
            print(f"[world-spawn-send] to={conn_id} {text}")
        handler.send_line(text)
    except Exception as e:
        print(f"[send-error] {conn_id}: {e}")

def broadcast_step(from_conn_id, step_line, cluster, scene):
    if scene in ("", "unknown"):
        return

    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if cid != from_conn_id and p.cluster == cluster and p.scene == scene
        ]

    for cid in targets:
        send_to_conn(cid, step_line)


def broadcast_visual_event(from_conn_id, visual_line, cluster, scene):
    if scene in ("", "unknown"):
        return

    with players_lock:
        targets = [
            cid for cid, p in players_by_conn.items()
            if cid != from_conn_id and p.cluster == cluster and p.scene == scene
        ]

    for cid in targets:
        send_to_conn(cid, visual_line)

def find_conn_by_player_id(player_id):
    with players_lock:
        for cid, p in players_by_conn.items():
            if p.player_id == player_id:
                return cid
    return None

def get_player_by_conn(conn_id):
    with players_lock:
        return players_by_conn.get(conn_id)

def send_battle_request(from_conn_id, from_id, from_name, cluster, scene, to_id, team_payload):
    target_conn = find_conn_by_player_id(to_id)
    if not target_conn:
        send_to_conn(from_conn_id, f"BATTLE_DECLINE|{enc(to_id)}|{enc('Player offline')}")
        return

    with players_lock:
        target = players_by_conn.get(target_conn)
        sender = players_by_conn.get(from_conn_id)

    if not target or not sender:
        return

    if target.cluster != cluster or target.scene != scene or scene in ("", "unknown"):
        send_to_conn(from_conn_id, f"BATTLE_DECLINE|{enc(to_id)}|{enc('Player unavailable')}")
        return

    target_status = getattr(target, "status", "available")
    if target_status != "available":
        print(f"[battle-busy] {from_name}({from_id}) tried {target.name}({to_id}) status={target_status}")
        send_to_conn(from_conn_id, "BATTLE_BUSY|" + "|".join([
            enc(to_id), enc(target.name), enc(target_status)
        ]))
        return

    print(f"[battle-req] {from_name}({from_id}) -> {target.name}({to_id})")
    send_to_conn(target_conn, "BATTLE_REQ|" + "|".join([
        enc(from_id), enc(from_name), team_payload
    ]))

def send_battle_accept(from_conn_id, requester_id, acceptor_id, acceptor_name, team_payload):
    requester_conn = find_conn_by_player_id(requester_id)
    if not requester_conn:
        return
    print(f"[battle-accept] {acceptor_name}({acceptor_id}) -> {requester_id}")
    send_to_conn(requester_conn, "BATTLE_ACCEPT|" + "|".join([
        enc(acceptor_id), enc(acceptor_name), team_payload
    ]))

def send_battle_decline(from_conn_id, requester_id, decliner_id, decliner_name):
    requester_conn = find_conn_by_player_id(requester_id)
    if not requester_conn:
        return
    print(f"[battle-decline] {decliner_name}({decliner_id}) -> {requester_id}")
    send_to_conn(requester_conn, "BATTLE_DECLINE|" + "|".join([
        enc(decliner_id), enc(decliner_name)
    ]))

def send_battle_busy_reply(from_conn_id, requester_id, busy_id, busy_name, status):
    requester_conn = find_conn_by_player_id(requester_id)
    if not requester_conn:
        return
    print(f"[battle-busy-reply] {busy_name}({busy_id}) -> {requester_id} status={status}")
    send_to_conn(requester_conn, "BATTLE_BUSY|" + "|".join([
        enc(busy_id), enc(busy_name), enc(status)
    ]))

def send_battle_cmd(from_conn_id, from_id, to_id, battle_id, actor_slot, move_slot, target_slot, target_ally):
    target_conn = find_conn_by_player_id(to_id)
    if not target_conn:
        print(f"[battle-cmd-miss] target offline: {to_id}")
        return

    print(f"[battle-cmd] {from_id} -> {to_id} battle={battle_id} actor={actor_slot} move={move_slot} target={target_slot} ally={target_ally}")
    send_to_conn(target_conn, "BATTLE_CMD|" + "|".join([
        enc(from_id), enc(battle_id),
        str(int(actor_slot)), str(int(move_slot)), str(int(target_slot)),
        "1" if target_ally else "0"
    ]))

def send_battle_hit(from_conn_id, from_id, to_id, battle_id, actor_slot, move_slot, target_side, target_slot, amount, hp_after, shield_after, crit):
    target_conn = find_conn_by_player_id(to_id)
    if not target_conn:
        print(f"[battle-hit-miss] target offline: {to_id}")
        return

    print(f"[battle-hit] {from_id} -> {to_id} battle={battle_id} actor={actor_slot} move={move_slot} target={target_side}{target_slot} amount={amount} hpAfter={hp_after} crit={crit}")
    send_to_conn(target_conn, "BATTLE_HIT|" + "|".join([
        enc(from_id), enc(battle_id),
        str(int(actor_slot)), str(int(move_slot)), enc(target_side or "E"),
        str(int(target_slot)), str(int(amount)), str(int(hp_after)), str(int(shield_after)),
        "1" if str(crit).lower() in ("1", "true", "yes") else "0"
    ]))

def send_battle_done(from_conn_id, from_id, to_id, battle_id, actor_slot, move_slot):
    target_conn = find_conn_by_player_id(to_id)
    if not target_conn:
        print(f"[battle-done-miss] target offline: {to_id}")
        return

    print(f"[battle-done] {from_id} -> {to_id} battle={battle_id} actor={actor_slot} move={move_slot}")
    send_to_conn(target_conn, "BATTLE_DONE|" + "|".join([
        enc(from_id), enc(battle_id), str(int(actor_slot)), str(int(move_slot))
    ]))

def send_battle_state(from_conn_id, from_id, to_id, battle_id, state_payload):
    target_conn = find_conn_by_player_id(to_id)
    if not target_conn:
        print(f"[battle-state-miss] target offline: {to_id}")
        return

    send_to_conn(target_conn, "BATTLE_STATE|" + "|".join([
        enc(from_id), enc(battle_id), state_payload
    ]))

class Handler(socketserver.StreamRequestHandler):
    def setup(self):
        super().setup()
        self.conn_id = f"{self.client_address[0]}:{self.client_address[1]}:{time.time_ns()}"
        self.send_lock = threading.Lock()
        with players_lock:
            handlers_by_conn[self.conn_id] = self
        print(f"[connect] {self.client_address}")

    def finish(self):
        with players_lock:
            handlers_by_conn.pop(self.conn_id, None)
            p = players_by_conn.pop(self.conn_id, None)
        if p:
            print(f"[disconnect] {p.name} / {p.player_id}")
            unlock_personal_spawns_for_owner_left_map(p.player_id, p.cluster, p.scene, reason="OWNER_DISCONNECTED")
        else:
            print(f"[disconnect] {self.client_address}")
        super().finish()

    def send_line(self, text):
        with self.send_lock:
            self.wfile.write((text + "\n").encode("utf-8"))
            self.wfile.flush()

    def handle(self):
        self.send_line("WELCOME|server-ready")

        while True:
            try:
                raw = self.rfile.readline()
            except (ConnectionResetError, ConnectionAbortedError, OSError):
                break
            if not raw:
                break

            try:
                line = raw.decode("utf-8", errors="replace").strip()
                if not line:
                    continue

                parts = line.split("|")
                kind = parts[0].upper()
                if kind.startswith("WORLD_SPAWN"):
                    print(f"[world-spawn-recv] from={self.conn_id} {line}")

                if kind == "HELLO":
                    player_id = dec(parts[1]) if len(parts) > 1 else self.conn_id
                    name = dec(parts[2]) if len(parts) > 2 else "Player"
                    cluster = dec(parts[3]) if len(parts) > 3 else "1"
                    design = int(parts[4]) if len(parts) > 4 and parts[4] else 0
                    color1 = int(parts[5]) if len(parts) > 5 and parts[5] else 0
                    color2 = int(parts[6]) if len(parts) > 6 and parts[6] else 0

                    with players_lock:
                        players_by_conn[self.conn_id] = PlayerState(
                            self.conn_id, player_id, name, cluster, "",
                            0.0, 0.0, 0.0, 0, False,
                            design, color1, color2, "available",
                            False, 0.0, 0.0, 0, False, "", False, time.time()
                        )

                    print(f"[hello] {name} id={player_id} cluster={cluster} design={design} colors={color1},{color2}")
                    self.send_line(f"WELCOME|{enc(player_id)}")

                elif kind == "POS":
                    player_id = dec(parts[1])
                    name = dec(parts[2])
                    cluster = dec(parts[3])
                    scene = dec(parts[4])
                    x = float(parts[5])
                    y = float(parts[6])
                    z = float(parts[7])
                    facing = int(parts[8])
                    moving = parse_bool(parts[9])
                    design = int(parts[10]) if len(parts) > 10 and parts[10] else 0
                    color1 = int(parts[11]) if len(parts) > 11 and parts[11] else 0
                    color2 = int(parts[12]) if len(parts) > 12 and parts[12] else 0
                    status = dec(parts[13]) if len(parts) > 13 and parts[13] else "available"
                    follower_enabled = parse_bool(parts[14]) if len(parts) > 14 and parts[14] else False
                    follower_x = float(parts[15]) if len(parts) > 15 and parts[15] else 0.0
                    follower_y = float(parts[16]) if len(parts) > 16 and parts[16] else 0.0
                    follower_facing = int(parts[17]) if len(parts) > 17 and parts[17] else facing
                    follower_moving = parse_bool(parts[18]) if len(parts) > 18 and parts[18] else False
                    follower_sprite = dec(parts[19]) if len(parts) > 19 and parts[19] else ""
                    follower_flip_x = parse_bool(parts[20]) if len(parts) > 20 and parts[20] else False
                    riding_broom = parse_bool(parts[21]) if len(parts) > 21 and parts[21] else False
                    water_walking = parse_bool(parts[22]) if len(parts) > 22 and parts[22] else False
                    jumping_or_bouncing = parse_bool(parts[23]) if len(parts) > 23 and parts[23] else False
                    bouncing_name = dec(parts[24]) if len(parts) > 24 and parts[24] else ""

                    previous_cluster = ""
                    previous_scene = ""
                    with players_lock:
                        previous_state = players_by_conn.get(self.conn_id)
                        if previous_state:
                            previous_cluster = previous_state.cluster
                            previous_scene = previous_state.scene
                        players_by_conn[self.conn_id] = PlayerState(
                            self.conn_id, player_id, name, cluster, scene,
                            x, y, z, facing, moving,
                            design, color1, color2, status,
                            follower_enabled, follower_x, follower_y, follower_facing, follower_moving, follower_sprite, follower_flip_x, time.time(),
                            riding_broom=riding_broom,
                            water_walking=water_walking,
                            jumping_or_bouncing=jumping_or_bouncing,
                            bouncing_name=bouncing_name
                        )
                    if previous_scene and (previous_cluster != cluster or previous_scene != scene):
                        unlock_personal_spawns_for_owner_left_map(player_id, previous_cluster, previous_scene, reason="OWNER_LEFT_MAP")

                    # Server-owned visible overworld spawns are driven by WORLD_SPAWNZONES reports.

                    # VMS-style: do not reply immediately to each POS.
                    # A fixed-rate broadcaster sends snapshots to every client.

                elif kind == "VIS_EVENT":
                    # VIS_EVENT|id|name|cluster|scene|eventType|x|y|z|facing|dir|riding|water|jump|bouncing
                    if len(parts) < 11:
                        raise ValueError("VIS_EVENT packet too short")

                    player_id = dec(parts[1])
                    name = dec(parts[2])
                    cluster = dec(parts[3])
                    scene = dec(parts[4])
                    event_type = dec(parts[5])
                    x = float(parts[6])
                    y = float(parts[7])
                    z = float(parts[8])
                    facing = int(parts[9])
                    event_dir = int(parts[10])
                    riding_broom = parse_bool(parts[11]) if len(parts) > 11 and parts[11] else False
                    water_walking = parse_bool(parts[12]) if len(parts) > 12 and parts[12] else False
                    jumping_or_bouncing = parse_bool(parts[13]) if len(parts) > 13 and parts[13] else False
                    bouncing_name = dec(parts[14]) if len(parts) > 14 and parts[14] else ""

                    with players_lock:
                        previous = players_by_conn.get(self.conn_id)
                        if previous:
                            previous.riding_broom = riding_broom
                            previous.water_walking = water_walking
                            previous.jumping_or_bouncing = jumping_or_bouncing
                            previous.bouncing_name = bouncing_name
                            previous.last_seen = time.time()

                    out = "VIS_EVENT|" + "|".join([
                        enc(player_id), enc(name), enc(cluster), enc(scene), enc(event_type),
                        f"{x:.4f}", f"{y:.4f}", f"{z:.4f}",
                        str(int(facing)), str(int(event_dir)),
                        "1" if riding_broom else "0",
                        "1" if water_walking else "0",
                        "1" if jumping_or_bouncing else "0",
                        enc(bouncing_name),
                    ])
                    broadcast_visual_event(self.conn_id, out, cluster, scene)

                elif kind == "STEP":
                    # STEP|id|name|cluster|scene|originX|originY|targetX|targetY|facing|duration|design|color1|color2|seq
                    if len(parts) < 15:
                        raise ValueError("STEP packet too short")

                    player_id = dec(parts[1])
                    name = dec(parts[2])
                    cluster = dec(parts[3])
                    scene = dec(parts[4])
                    ox = float(parts[5])
                    oy = float(parts[6])
                    tx = float(parts[7])
                    ty = float(parts[8])
                    facing = int(parts[9])
                    duration = float(parts[10])
                    design = int(parts[11]) if parts[11] else 0
                    color1 = int(parts[12]) if parts[12] else 0
                    color2 = int(parts[13]) if parts[13] else 0
                    seq = parts[14]
                    riding_broom = parse_bool(parts[15]) if len(parts) > 15 and parts[15] else False

                    previous_cluster = ""
                    previous_scene = ""
                    with players_lock:
                        previous = players_by_conn.get(self.conn_id)
                        status = getattr(previous, "status", "available") if previous else "available"
                        if previous:
                            previous_cluster = previous.cluster
                            previous_scene = previous.scene
                        players_by_conn[self.conn_id] = PlayerState(
                            self.conn_id, player_id, name, cluster, scene,
                            tx, ty, ty * 0.01, facing, True,
                            design, color1, color2, status,
                            getattr(previous, "follower_enabled", False) if previous else False,
                            getattr(previous, "follower_x", 0.0) if previous else 0.0,
                            getattr(previous, "follower_y", 0.0) if previous else 0.0,
                            getattr(previous, "follower_facing", 0) if previous else 0,
                            getattr(previous, "follower_moving", False) if previous else False,
                            getattr(previous, "follower_sprite", "") if previous else "",
                            getattr(previous, "follower_flip_x", False) if previous else False,
                            time.time(),
                            riding_broom=riding_broom,
                            water_walking=getattr(previous, "water_walking", False) if previous else False,
                            jumping_or_bouncing=getattr(previous, "jumping_or_bouncing", False) if previous else False,
                            bouncing_name=getattr(previous, "bouncing_name", "") if previous else ""
                        )
                    if previous_scene and (previous_cluster != cluster or previous_scene != scene):
                        unlock_personal_spawns_for_owner_left_map(player_id, previous_cluster, previous_scene, reason="OWNER_LEFT_MAP")

                    out = "STEP|" + "|".join([
                        enc(player_id), enc(name), enc(cluster), enc(scene),
                        f"{ox:.4f}", f"{oy:.4f}", f"{tx:.4f}", f"{ty:.4f}",
                        str(int(facing)), f"{duration:.4f}",
                        str(int(design)), str(int(color1)), str(int(color2)), str(seq),
                        "1" if riding_broom else "0"
                    ])
                    broadcast_step(self.conn_id, out, cluster, scene)

                elif kind == "BATTLE_REQ":
                    # BATTLE_REQ|fromId|fromName|cluster|scene|toId|teamPayload
                    if len(parts) < 7:
                        raise ValueError("BATTLE_REQ packet too short")

                    from_id = dec(parts[1])
                    from_name = dec(parts[2])
                    cluster = dec(parts[3])
                    scene = dec(parts[4])
                    to_id = dec(parts[5])
                    team_payload = parts[6]

                    send_battle_request(self.conn_id, from_id, from_name, cluster, scene, to_id, team_payload)

                elif kind == "BATTLE_ACCEPT":
                    # BATTLE_ACCEPT|requesterId|acceptorId|acceptorName|teamPayload
                    if len(parts) < 5:
                        raise ValueError("BATTLE_ACCEPT packet too short")

                    requester_id = dec(parts[1])
                    acceptor_id = dec(parts[2])
                    acceptor_name = dec(parts[3])
                    team_payload = parts[4]

                    send_battle_accept(self.conn_id, requester_id, acceptor_id, acceptor_name, team_payload)

                elif kind == "BATTLE_DECLINE":
                    # BATTLE_DECLINE|requesterId|declinerId|declinerName
                    if len(parts) < 4:
                        raise ValueError("BATTLE_DECLINE packet too short")

                    requester_id = dec(parts[1])
                    decliner_id = dec(parts[2])
                    decliner_name = dec(parts[3])

                    send_battle_decline(self.conn_id, requester_id, decliner_id, decliner_name)

                elif kind == "BATTLE_BUSY_REPLY":
                    # BATTLE_BUSY_REPLY|requesterId|busyId|busyName|status
                    if len(parts) < 5:
                        raise ValueError("BATTLE_BUSY_REPLY packet too short")

                    requester_id = dec(parts[1])
                    busy_id = dec(parts[2])
                    busy_name = dec(parts[3])
                    status = dec(parts[4])

                    send_battle_busy_reply(self.conn_id, requester_id, busy_id, busy_name, status)

                elif kind == "BATTLE_CMD":
                    # BATTLE_CMD|fromId|toId|battleId|actorSlot|moveSlot|targetSlot|targetAlly
                    if len(parts) < 8:
                        raise ValueError("BATTLE_CMD packet too short")

                    from_id = dec(parts[1])
                    to_id = dec(parts[2])
                    battle_id = dec(parts[3])
                    actor_slot = int(parts[4])
                    move_slot = int(parts[5])
                    target_slot = int(parts[6])
                    target_ally = parse_bool(parts[7])

                    send_battle_cmd(self.conn_id, from_id, to_id, battle_id, actor_slot, move_slot, target_slot, target_ally)

                elif kind == "BATTLE_HIT":
                    # BATTLE_HIT|fromId|toId|battleId|actorSlot|moveSlot|targetSide|targetSlot|amount|hpAfter|shieldAfter|crit
                    if len(parts) < 12:
                        raise ValueError("BATTLE_HIT packet too short")

                    from_id = dec(parts[1])
                    to_id = dec(parts[2])
                    battle_id = dec(parts[3])
                    actor_slot = int(parts[4])
                    move_slot = int(parts[5])
                    target_side = dec(parts[6])
                    target_slot = int(parts[7])
                    amount = int(parts[8])
                    hp_after = int(parts[9])
                    shield_after = int(parts[10])
                    crit = dec(parts[11])

                    send_battle_hit(self.conn_id, from_id, to_id, battle_id, actor_slot, move_slot, target_side, target_slot, amount, hp_after, shield_after, crit)

                elif kind == "BATTLE_DONE":
                    # BATTLE_DONE|fromId|toId|battleId|actorSlot|moveSlot
                    if len(parts) < 6:
                        raise ValueError("BATTLE_DONE packet too short")

                    from_id = dec(parts[1])
                    to_id = dec(parts[2])
                    battle_id = dec(parts[3])
                    actor_slot = int(parts[4])
                    move_slot = int(parts[5])

                    send_battle_done(self.conn_id, from_id, to_id, battle_id, actor_slot, move_slot)

                elif kind == "BATTLE_STATE":
                    # BATTLE_STATE|fromId|toId|battleId|payload
                    if len(parts) < 5:
                        raise ValueError("BATTLE_STATE packet too short")

                    from_id = dec(parts[1])
                    to_id = dec(parts[2])
                    battle_id = dec(parts[3])
                    payload = parts[4]

                    send_battle_state(self.conn_id, from_id, to_id, battle_id, payload)

                elif kind == "WORLD_SPAWNZONES":
                    # WORLD_SPAWNZONES|playerId|cluster|scene|signature|zone0,x,y,z;zone1,x,y,z
                    if len(parts) < 6:
                        raise ValueError("WORLD_SPAWNZONES packet too short")
                    player_id = dec(parts[1])
                    cluster = dec(parts[2])
                    scene = dec(parts[3])
                    signature = dec(parts[4])
                    zone_body = parts[5]
                    receive_world_spawn_zones(self.conn_id, player_id, cluster, scene, signature, zone_body)

                elif kind == "WORLD_SPAWN_GEN_RESULT":
                    # WORLD_SPAWN_GEN_RESULT|playerId|requestId|monId|monKey|shiny|level|versionRequirement
                    if len(parts) < 7:
                        raise ValueError("WORLD_SPAWN_GEN_RESULT packet too short")
                    player_id = dec(parts[1])
                    request_id = dec(parts[2])
                    mon_id = int(parts[3]) if parts[3] else -1
                    mon_key = dec(parts[4])
                    shiny = parse_bool(parts[5])
                    level = int(parts[6]) if parts[6] else 1
                    version_requirement = dec(parts[7]) if len(parts) > 7 else "None"
                    complete_world_spawn_generation(self.conn_id, player_id, request_id, mon_id, mon_key, shiny, level, version_requirement)

                elif kind == "WORLD_SPAWN_PERSONAL_ADD":
                    # WORLD_SPAWN_PERSONAL_ADD|playerId|cluster|scene|slot,x,y,z,monId,shiny,level,monKey,monSaveB64,versionRequirement;...
                    if len(parts) < 5:
                        raise ValueError("WORLD_SPAWN_PERSONAL_ADD packet too short")
                    player_id = dec(parts[1])
                    cluster = dec(parts[2])
                    scene = dec(parts[3])
                    records_body = parts[4]
                    receive_personal_encounter_spawns(self.conn_id, player_id, cluster, scene, records_body)

                elif kind == "WORLD_SPAWN_CLAIM":
                    # WORLD_SPAWN_CLAIM|playerId|spawnId
                    if len(parts) < 3:
                        raise ValueError("WORLD_SPAWN_CLAIM packet too short")
                    player_id = dec(parts[1])
                    spawn_id = dec(parts[2])
                    claim_world_spawn(self.conn_id, player_id, spawn_id)

                elif kind == "WORLD_SPAWN_CATCH_START":
                    # WORLD_SPAWN_CATCH_START|playerId|spawnId
                    if len(parts) < 3:
                        raise ValueError("WORLD_SPAWN_CATCH_START packet too short")
                    player_id = dec(parts[1])
                    spawn_id = dec(parts[2])
                    start_world_spawn_catch_animation(self.conn_id, player_id, spawn_id)

                elif kind == "WORLD_SPAWN_RESULT":
                    # WORLD_SPAWN_RESULT|playerId|spawnId|caught|failed
                    if len(parts) < 4:
                        raise ValueError("WORLD_SPAWN_RESULT packet too short")
                    player_id = dec(parts[1])
                    spawn_id = dec(parts[2])
                    result = dec(parts[3])
                    finish_world_spawn_claim(self.conn_id, player_id, spawn_id, result)

                elif kind == "PING":
                    self.send_line("PONG")

                elif kind == "BYE":
                    break

            except Exception as e:
                print(f"[packet-error] {e} :: {line!r}")
                self.send_line(f"ERR|{enc(str(e))}")

class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    allow_reuse_address = True
    request_queue_size = 128
    daemon_threads = True


def snapshot_loop(snapshot_hz):
    interval = 1.0 / max(1.0, float(snapshot_hz))
    print(f"[snapshots] broadcasting at {snapshot_hz:g} Hz")
    while True:
        start = time.time()
        maintain_world_spawn_maps()

        with players_lock:
            conn_ids = list(handlers_by_conn.keys())

        for cid in conn_ids:
            try:
                send_to_conn(cid, make_snapshot(cid))
                send_to_conn(cid, make_world_spawn_snapshot(cid))
            except Exception as e:
                print(f"[snapshot-error] {cid}: {e}")

        elapsed = time.time() - start
        time.sleep(max(0.001, interval - elapsed))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=61528)
    parser.add_argument("--snapshot-hz", type=float, default=30.0)
    args = parser.parse_args()

    print("MMOnsterpatch Server v0.7.8-overworld-spawns")
    print(f"Listening on {args.host}:{args.port}")
    print("Ctrl+C to stop.")

    with ThreadedTCPServer((args.host, args.port), Handler) as server:
        threading.Thread(target=snapshot_loop, args=(args.snapshot_hz,), daemon=True).start()
        server.serve_forever()

if __name__ == "__main__":
    main()
