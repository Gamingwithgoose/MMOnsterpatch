# MMOnsterpatch Official Server v0.11.2 - Launcher-Owned Runtime

Compatibility target: **Monsterpatch Game Version 0.181**.

## Needs replaced

**Launcher + Client dev package**

The server is included for convenience, but this v0.11.2 milestone does not require replacing the server if you are already on the v0.11.x EventRewardMail/LauncherFlow server base.

If you do replace the server folder, back up/keep:

```text
Server/data/
Server/configs/worldserver.ini
```

## What v0.11.2 changes

v0.11.2 moves MMOnsterpatch Official Server from manual mod copying to a launcher-owned online runtime.

The goal is that players do not need to know how to install BepInEx or where to place patcher DLLs. The launcher stages what Online Mode needs, isolates any existing offline mods, launches Monsterpatch, then restores the user's offline setup after the game exits.

## Build order

For the full developer package, run the root build script:

```bat
build_all.bat
```

That script builds in the correct order:

1. `Source\build_all.bat`
2. `Launcher\build_launcher.bat`

The source build produces:

```text
Source\bin\Release\net472\MMOnsterpatchOfficialServerPatcher.dll
```

Then it copies the patcher into:

```text
patchers\MMOnsterpatchOfficialServerPatcher.dll
Launcher\Payload\BepInEx\patchers\MMOnsterpatchOfficialServerPatcher.dll
```

The launcher build publishes the final launcher folder and hard-fails if the published payload is missing required online runtime files.

## Published launcher folder

After a successful build, run the launcher from:

```text
Launcher\bin\Release\net8.0-windows\win-x64\publish\MMOnsterpatchLauncher.exe
```

The published folder should contain:

```text
MMOnsterpatchLauncher.exe
Payload\Root\winhttp.dll
Payload\Root\doorstop_config.ini
Payload\Root\.doorstop_version
Payload\BepInEx\core\BepInEx.Preloader.dll
Payload\BepInEx\core\*.dll
Payload\BepInEx\patchers\MMOnsterpatchOfficialServerPatcher.dll
```

`MMOnsterpatchOfficialServerPatcher.dll` belongs in `BepInEx\patchers`, not `BepInEx\plugins`.

## Launcher behavior

When clicking **Play Online**, the launcher:

1. writes `%LOCALAPPDATA%\MMOnsterpatch\launcher_session.json`
2. requests elevation if Windows blocks staging in a protected Steam folder
3. temporarily moves the user's existing `BepInEx` to `BepInEx.MMOnsterpatchOfflineBackup`
4. stages the launcher-owned online payload into the Monsterpatch folder
5. starts Monsterpatch with launcher/session markers
6. hides/minimizes while Monsterpatch is running
7. restores the launcher window after Monsterpatch exits
8. restores the original offline `BepInEx` setup
9. clears the launcher session file

If a session is interrupted, use the launcher's **Restore** button.

## Offline mod isolation

Normal/offline mods can remain in the player's regular Monsterpatch install. Online launch temporarily isolates them so they do not load in the official online client.

The live online runtime should contain only the launcher-staged BepInEx runtime plus the official server patcher:

```text
BepInEx\core\
BepInEx\config\
BepInEx\plugins\          empty
BepInEx\patchers\MMOnsterpatchOfficialServerPatcher.dll
```

## Diagnostics

Launcher staging diagnostics are written to:

```text
%LOCALAPPDATA%\MMOnsterpatch\launcher_stage_log.txt
```

Use this log when the launcher stages files but the game boots vanilla or refuses to start Online Mode.

## Current confirmed client flow

Confirmed from v0.11.1/v0.11.2 testing:

- launcher detection works
- title screen takeover works
- `Play` becomes `Log In`
- online save slots can be created/selected
- old connect/disconnect popups are suppressed in launcher sessions
- launcher-owned runtime works when the patcher DLL is in the payload
- build scripts now prevent publish folders that are missing required payload files

## Server/admin docs

- `gmlist.md` contains GM/admin command syntax, examples, and behavior.
- `CHANGELOG.md` contains v0.11.2 launcher-owned runtime notes and earlier v0.11.1/v0.11.0 milestone notes.
- `THIRD-PARTY-NOTICES.md` contains the current bundled BepInEx payload notice.

## Runtime server layout

```text
Server/
  MMOnsterpatchServer.py
  MMOnsterpatchServerUI.ps1
  Start-ServerUI.bat
  Start-ServerUI-Hidden.vbs
  configs/
    worldserver.ini
```

The server auto-creates `data/`, `logs/`, and `backups/` folders when needed.

## Next planned focus

Cosmetics and polish after the v0.11.2 launcher-owned runtime milestone.
