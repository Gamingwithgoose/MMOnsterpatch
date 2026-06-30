# MMOnsterpatch Official Server

MMOnsterpatch Official Server is the online server and client patcher setup for Monsterpatch multiplayer and server-owned online save features.

The v0.9.0 build moves the project from the older AIO-only flow into the Official Server direction: players remain offline by default, then choose **Switch to Online Mode** on the native save-select screen. Online saves are loaded from and written to the server, while normal local saves stay local.

## Current Version

```text
Current release: v0.9.0
Branch focus: Official Server foundation
Client output: MMOnsterpatchOfficialServerPatcher.dll
Install location: BepInEx\patchers
```

## What v0.9.0 Adds

### Native Online Save Select

The save-select screen now has an Official Server Online Mode path.

- Offline mode shows normal local saves.
- Online mode shows server-owned online save slots.
- The mode button switches between:
  - `Switch to Online Mode`
  - `Switch to Offline Mode`
- Online mode uses an embedded pink online save-select background.
- Server status text appears on the save-select screen.

### Server-Owned Online Saves

Online save slots are owned by the Official Server.

- Online save-slot data is requested from the server after Steam authentication.
- New online characters are created in the selected online slot.
- `SaveSystem.SaveGame` is redirected to the server during an Official Online Save Session.
- Local disk writes are blocked while an online save session is active.
- Returning to title force-saves online progress to the server before disconnecting.

### Local Save Protection

The v0.9.0 client uses a stronger Official Online Save Session guard so local save files are not overwritten by online gameplay saves.

This guard survives the transition from save select into gameplay. It does not rely only on the save-select menu object staying alive.

### Steam Authentication and Cached Session Token

Steam authentication is handled through the Official Server auth flow.

The client can cache the session token in config as base64 text so the token is not shown as plain raw data at a glance.

Server-side validation is still authoritative:

- session lifetime is 12 hours
- session must come from the same source IP that created it
- different IP requires Steam re-authentication
- expired session requires Steam re-authentication
- invalid or legacy sessions are rejected

Config section:

```ini
[Official Server Auth]
CachedSessionToken =
CachedSessionExpiresUtc =
CachedSteamID64 =
CachedAccountUUID =
```

### Online-Only Delete Save Flow

The custom Delete button appears only in Online Mode.

The button uses the game's official delete process:

1. Player clicks **Delete** in Online Mode.
2. The save-select screen enters the official delete mode.
3. Player selects an online slot.
4. The official delete confirmation appears.
5. Confirming delete sends the server-side archive/delete request.
6. The online save is archived and removed from the live online slot table.

Offline mode does not show the custom Delete button.

### Archived Online Characters

Deleted online save data is archived under the server data folder instead of being silently discarded.

```text
Server/data/Archived Characters/
```

The archive keeps the deleted online save information available for audit/recovery review while freeing the live slot for a truly new online character later.

### Online Chat Activation

The chat window is tied to an active Official Online Save Session. It should become available once an online save is loaded and should disconnect/clean up when returning to title.

---

# Existing AIO Online Features

The Official Server package still includes the broader MMOnsterpatch AIO online systems:

- online player presence
- remote player and follower sync
- server-owned visible overworld spawns
- personal reward spawn locking/unlocking
- global chat
- guilds and guild chat
- Trading Post and GTS systems
- server bank storage
- Global Box quality-of-life storage
- PvP request and battle command routing hooks
- Steam-backed account foundation
- character records under each account by save slot
- ranked database foundation
- moderation and SteamID ban support

---

# Account and Character Direction

The long-term identity model is:

```text
Steam identity
  -> AccountUUID
    -> online save slot
      -> registered PvP character identity
        -> CharacterName#0000 public handle
        -> ranked profile / RP / rewards / history
```

The PvP/Register flow is planned but not live in v0.9.0.

Planned PvP tab behavior:

- PvP tab shows **Register** before showing ranked profile details.
- Register creates a unique PvP identity for the currently loaded online save-slot character.
- Public handles use `CharacterName#xxxx`.
- The `xxxx` tag is randomly generated from `0000` to `9999`.
- The full handle must be unique for that character name.
- Deleting an online save archives the old live identity so a new character in the same slot starts clean.

---

# Ranked / RP Roadmap

Ranked is still foundation-level in v0.9.0.

Current foundation:

- Ranked tab exists as a read-only foundation.
- Ranked database tables exist or are staged.
- Season 0 is planned.
- Ranked buttons/actions remain disabled.
- Real RP writes are not live.

Planned ranked structure:

| RP Range | Rank |
| --- | --- |
| 0 - 199 | E |
| 200 - 399 | D |
| 400 - 599 | C |
| 600 - 799 | B |
| 800 - 999 | A |
| 1000 | S |

Season 0 is planned to begin on October 1st, 2026.

---

# Repository Layout

```text
Source/
  Client-side Official Server patcher source.

Server/
  Python server source for Steam auth, official online saves, chat/social, Trading Post/GTS, guilds, MMO presence, PvP routing, and moderation.
```

Both sides should be updated together when testing authentication, online save slots, delete/archive behavior, Trading Post, social identity, or PvP/ranked features.

---

# Build

From the `Source` folder:

```bat
build_all.bat
```

The build script expects Monsterpatch/BepInEx/Unity reference DLLs to be available through `MONSTERPATCH_LIB_DIR`, `..\lib`, `..\..\lib`, or `C:\Monsterpatch_Mods\lib`.

The built patcher is copied to:

```text
patchers/MMOnsterpatchOfficialServerPatcher.dll
```

For game install testing, place the DLL in:

```text
Monsterpatch/BepInEx/patchers/
```

---

# Server Run

From the `Server` folder:

```bat
start_mmonsterpatch_server.bat
```

Default public setup expects:

```text
Trading Post/GTS TCP: 61526
Steam OpenID HTTP:   61527
MMO multiplayer TCP: 61528
Social chat TCP:     61529
```

Set your Steam Web API key using one of these environment variables if display-name lookup is wanted:

```text
STEAM_WEB_API_KEY
PBO_STEAM_WEB_API_KEY
PBO_STEAM_API_KEY
STEAM_API_KEY
```

---

# Data Safety Goals

The Official Server should fail safely, not destructively.

Current and planned safety rules:

- offline/local saves stay local
- online saves are saved to the server only while online session guard is active
- local disk writes are blocked during online sessions
- online delete archives before removing live online save data
- Steam/account identity is server-owned
- PvP/ranked identity should be character-owned, not name-owned
- risky systems such as ranked, SATS wagers, and MoN wagers should use server-owned transaction/audit records
- database migrations should be additive and non-destructive
- if the server cannot validate an online action, it should block the action instead of guessing

---

# Current Development Status

v0.9.0 is still an alpha Official Server foundation build.

Working/foundation areas:

- native save-select Online Mode
- Steam auth from save-select
- cached 12-hour same-IP session token
- server-owned online save slots
- online save write redirect
- local save protection during online sessions
- online-only official delete confirmation flow
- server archive/delete for online saves
- embedded online save-select background
- chat activation during online save sessions
- existing AIO social/chat/guild/Trading Post/GTS/MMO/PvP hooks

Planned next areas:

- PvP Register button and character-owned PvP IDs
- live ranked eligibility checks
- ranked match dry-run records
- real RP writes after dry-run validation
- ranked history and rewards
- SATS wager battles
- possible MoN wager battles
- moderation/admin UI polish
