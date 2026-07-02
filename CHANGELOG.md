## v0.11.2 - Launcher-Owned Runtime Confirmed

Needs replaced: **Launcher + Client dev package**. Server is unchanged and included only for convenience.

- Marked v0.11.2 as the confirmed launcher-owned runtime milestone after user testing confirmed the launcher works once the patcher DLL is present in the payload.
- Moved the project from manual `BepInEx\patchers` copying to a launcher-managed online runtime.
- The launcher payload now owns the Windows BepInEx bootstrap/runtime files for the online session:
  - `Payload\Root\winhttp.dll`
  - `Payload\Root\doorstop_config.ini`
  - `Payload\BepInEx\core\BepInEx.Preloader.dll`
  - `Payload\BepInEx\patchers\MMOnsterpatchOfficialServerPatcher.dll` after build
- Root `build_all.bat` builds the client patcher first, copies it into the launcher payload, then publishes the launcher with that payload.
- `Source\build_all.bat` copies `MMOnsterpatchOfficialServerPatcher.dll` to both the package `patchers` folder and `Launcher\Payload\BepInEx\patchers`.
- `Launcher\build_launcher.bat` verifies the published release folder contains the patcher DLL, bundled BepInEx preloader, `winhttp.dll`, and `doorstop_config.ini` before allowing the build to succeed.
- The launcher temporarily isolates the user's existing offline `BepInEx` folder, stages a clean online-only runtime, launches Monsterpatch in Online Mode, hides while the game runs, restores itself on game exit, and restores the user's original offline setup.
- Existing v0.11.1 launcher flow remains confirmed: title takeover works, `Play` becomes `Log In`, online save slot flow works, and the old connect/disconnect popups are suppressed.
- Next planned focus after this milestone: cosmetics and polish.

## v0.11.2 - Launcher Payload SelfCopyFix

- Fixed `Launcher\build_launcher.bat` failing after `Source\build_all.bat` had already copied `MMOnsterpatchOfficialServerPatcher.dll` into the launcher payload.
- The launcher build now prefers the freshly built `Source\bin\Release` DLL, then package `patchers`, then existing payload.
- If the payload DLL is already the selected source, the build skips the Windows self-copy instead of treating it as a failed copy.
- Kept version at `0.11.2` while launcher-owned runtime packaging is being calibrated.

## v0.11.2 - Launcher Payload PublishFix

## v0.11.2 - Launcher Payload MSBuildFix

- Fixed the launcher payload copy for generic build-all workflows that build `.csproj` files directly instead of running `Source\build_all.bat`.
- Added an MSBuild post-build target to `Source\MMOnsterpatchAIOPatcher.csproj` so `MMOnsterpatchOfficialServerPatcher.dll` is copied into `Launcher\Payload\BepInEx\patchers` after every client build.
- The post-build target also copies the patcher into any already-created launcher output or publish folder, including `Launcher\bin\Release\net8.0-windows` and `Launcher\bin\Release\net8.0-windows\win-x64\publish`.
- Added a root `build_all.bat` that runs the client build first, then publishes the launcher with the completed payload.
- Kept version at `0.11.2` while launcher-owned runtime packaging is being calibrated.


Needs replaced: **Launcher + Client dev package**. Server unchanged/included only for convenience.

- Fixed the build scripts so the compiled `MMOnsterpatchOfficialServerPatcher.dll` is copied into `Launcher/Payload/BepInEx/patchers/` every time `Source/build_all.bat` runs.
- `Source/build_all.bat` now searches for the built DLL under `Source/bin/Release` and fails clearly if it cannot find or copy it.
- `Launcher/build_launcher.bat` now builds the client patcher first, locates the DLL from Source/package/payload fallback paths, and copies it into the launcher payload before publish.
- `Launcher/build_launcher.bat` now force-copies the entire `Launcher/Payload` folder into the published release folder after `dotnet publish`.
- The launcher build now hard-fails if the published release folder is missing the patcher DLL, `BepInEx.Preloader.dll`, `winhttp.dll`, or `doorstop_config.ini`.
- Changed launcher payload project copy rules to `Always` instead of `PreserveNewest`.

## v0.11.2 - Launcher Full Payload Runtime
- Added bundled Windows BepInEx win_x64 5.4.23.5 runtime payload under `Launcher/Payload`.
- Added `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, and full `BepInEx/core` to the launcher payload.
- The launcher no longer needs an existing game `BepInEx/core` to borrow for Windows testing.
- `Source/build_all.bat` still copies the compiled MMOnsterpatch patcher into `Launcher/Payload/BepInEx/patchers` automatically.
- The final published launcher folder is now the intended player-facing release shape: launcher EXE plus Payload folder.
- Existing local/offline `BepInEx` installs are still backed up and restored for online launch isolation.

## v0.11.2 - Launcher-Owned Runtime BootstrapGuard

Needs replaced: **Launcher only** for existing v0.11.2 client payload tests. Full developer package also updates `Source/build_all.bat`.

- Kept version metadata at `0.11.2` because this is still the launcher-owned runtime milestone.
- Added a hard pre-launch runtime readiness guard so Play Online stops instead of allowing vanilla Monsterpatch to boot when staging is incomplete.
- Verifies live `BepInEx/core/BepInEx.Preloader.dll`, `BepInEx/patchers/MMOnsterpatchOfficialServerPatcher.dll`, `winhttp.dll`, `doorstop_config.ini`, and the launcher runtime marker before starting the game.
- Verifies the staged patcher DLL size and SHA256 hash against the payload/source DLL.
- Added clearer staging diagnostics with patcher source, staged hash, and doorstop readiness in `%LOCALAPPDATA%/MMOnsterpatch/launcher_stage_log.txt`.
- Updated `Launcher/build_launcher.bat` so launcher-only packages fail clearly if the compiled patcher payload is missing.
- Updated developer `Source/build_all.bat` so a successful client build automatically copies the patcher DLL/PDB into `Launcher/Payload/BepInEx/patchers/`.
- This is still a transition build: if `Launcher/Payload/BepInEx/core` and `Launcher/Payload/Root` do not contain a full BepInEx runtime/bootstrap, the launcher still borrows `BepInEx/core`, `winhttp.dll`, and `doorstop_config.ini` from the user's existing BepInEx install.

## v0.11.2 - Launcher-Owned Runtime AccessFix

Needs replaced: **Launcher only** if v0.11.2 StageFix client was already built. Full package still includes client/server for convenience.

- Kept version metadata at `0.11.2` because this is still the launcher-owned runtime milestone.
- Fixed protected Steam library staging by detecting Program Files / write-denied Monsterpatch installs before moving `BepInEx`.
- Added launcher self-elevation for online staging. If Windows blocks access to the Monsterpatch folder, the launcher now relaunches itself with Administrator rights and automatically resumes Play Online.
- Added `--play-online <Monsterpatch.exe>` command-line handling so the elevated launcher instance can continue the same launch without the user reselecting the game.
- Added write-probe diagnostics to `%LOCALAPPDATA%/MMOnsterpatch/launcher_stage_log.txt`.
- Added a clearer fallback message explaining that protected Steam libraries require running the launcher as Administrator or moving Monsterpatch to a non-protected Steam library.
- Kept the StageFix behavior: patcher payload staging, clean online BepInEx swap, hidden launcher while the game runs, restore launcher on game exit, and offline BepInEx restore after exit.

## v0.11.2 - Launcher-Owned Runtime StageFix

Needs replaced: **Launcher + Client only**. Server is included for convenience but unchanged.

- Kept version metadata at `0.11.2` because this is the same launcher-owned runtime milestone.
- Updated `Launcher/build_launcher.bat` so it builds the client patcher first and copies `MMOnsterpatchOfficialServerPatcher.dll` into `Launcher/Payload/BepInEx/patchers/`.
- Added launcher project content rules so `Launcher/Payload/**` is copied beside the built/published launcher.
- Strengthened online runtime staging so launch fails loudly if the patcher is not actually copied into live `BepInEx/patchers`.
- Added launcher staging diagnostics to `%LOCALAPPDATA%/MMOnsterpatch/launcher_stage_log.txt`, including package root, launcher root, patcher source, and staged target.
- Changed launcher behavior from minimize-only to hiding from the taskbar while Monsterpatch is running, then restoring the launcher window after the game exits.
- Added process-tree style waiting so the launcher does not immediately restore if the first process exits and a same-folder Monsterpatch process continues running.
- Added doorstop config safety: backs up `doorstop_config.ini`, forces `enabled=true`, and points `targetAssembly` at `BepInEx\core\BepInEx.Preloader.dll` during online sessions.
- Restores the original `doorstop_config.ini` backup when the online runtime is restored.
- Added safer handling for stale `BepInEx.MMOnsterpatchOfflineBackup` states so the launcher does not accidentally stage into the wrong live BepInEx folder.

## v0.11.2 - Launcher-Owned Online Runtime

Needs replaced: **Launcher + Client only**. Server is included for convenience but unchanged from the v0.11.x reward-mail base.

- Promoted v0.11.1 Launcher Flow Calibration as the confirmed-good title/login flow baseline.
- Added launcher-owned clean online runtime staging for official server sessions.
- Added launcher logic to temporarily move the user's existing `BepInEx` folder to `BepInEx.MMOnsterpatchOfflineBackup` during online launch.
- Added launcher logic to create a clean online `BepInEx` folder containing only borrowed/provided `BepInEx/core`, empty `plugins`, and the MMOnsterpatch official server patcher in `patchers`.
- Added automatic patcher discovery from the package `patchers/` output, `Source/bin/Release/net472/`, or optional `Launcher/Payload/BepInEx/patchers/` payload.
- Added optional `Launcher/Payload` support for future fully clean installs where the launcher owns the BepInEx bootstrap/runtime instead of borrowing from an existing install.
- Added launch-time checks for required BepInEx bootstrap files `winhttp.dll` and `doorstop_config.ini`.
- Added launcher minimize-on-game-launch behavior.
- Added launcher restore-on-game-exit behavior so the window reopens after Monsterpatch closes.
- Added automatic offline BepInEx/mod restore after online game exit.
- Added manual Restore button for recovery if a previous online session was interrupted.
- Added `%LOCALAPPDATA%/MMOnsterpatch/launcher_stage_log.txt` staging log output.
- Updated launcher and client version metadata to `0.11.2` / `v0.11.2-launcher-owned-runtime`.

## v0.11.1 - Launcher Flow Calibration Confirmed

- Marked v0.11.1 Launcher Flow Calibration BuildFix as working as intended based on user testing.
- Confirmed title screen takeover works in launcher mode.
- Confirmed `Play` becomes `Log In` in launcher mode.
- Confirmed the v0.11.1 calibration branch is safe to use as the baseline for launcher-owned runtime work.

## v0.11.1 - Launcher Flow Calibration BuildFix

- Removed duplicate GetTransformPath helper from OfficialServerSaveSelectNative.cs that caused CS0111 during the v0.11.1 client build.
- Kept package/build metadata at v0.11.1 while this launcher calibration pass is still being tested.

## v0.11.1 - Launcher Flow Calibration

- Increased package/build metadata from v0.11.0 to v0.11.1 for launcher-flow calibration testing.
- Fixed the Extras info text replacement so the body message remains the full MMOnsterpatch explanation instead of being overwritten by the GitHub button label.
- Reduced the Extras body-message font size and enabled wrapping for the MMOnsterpatch info text.
- Widened the native Extras Delete Save button and renamed it to Delete Online Save File with no wrapping.
- Added launcher-mode direct native save-slot handling so online slots can create new saves through PickVersion or load existing saves through PlaySaveFile while keeping server save redirection armed.
- Re-enabled/verified native save-slot selectables while online slot visuals are applied.
- Suppressed the old Connected/Disconnected MMOnsterpatch native status popups during launcher sessions.

## v0.11.0 LauncherFlowTest CompileFix

- Added/verified missing launcher flow static fields used by OfficialServerSaveSelectNative.cs.
- Removes Server/__pycache__ and .pyc files from the source package.
- Keeps numeric assembly/package version at 0.11.0 for this v0.11.0 test track.

# Changelog

## v0.11.0 - Event Reward Mail / Solid GitHub Release

Compatibility target: **Monsterpatch Game Version 0.181**.

Needs replaced: **Client + Server**.

### Release highlights

- Promoted the tuned Trading Post layout to the official v0.11.0 base.
- Baked the latest approved Trading Post client config defaults into the generated config, with session/auth values blanked for safety.
- Kept the transparent embedded SATS coin icon for SATS offers/requests in listing rows.
- Added a compact server runtime folder with one bundled server script, server UI, launcher files, and config.
- Added a PowerShell server UI with Start/Stop controls, Auto Restart checkbox, and admin command entry.
- Added hidden server UI launch behavior so `Start-ServerUI.bat` opens the UI without leaving an extra console window visible.
- Added server-backed Trading Post Browse/My Listings filters.
- Added per-control filter bar config for X/Y/width/height tuning.
- Added Trading Post window position/size persistence.
- Added chat minimized-state persistence alongside existing chat position/size persistence.
- Added character-stable `Name#xxxx` handles so character tags no longer regenerate every login.
- Changed mailboxes to be character-scoped instead of Steam/account scoped when active character identity is available.
- Changed new Trading Post listings to use active character identity for seller display, My Listings, cancellation, completion, and return mail.
- Added mailbox compose/reply drawer with per-field layout controls.
- Added mailbox MoN/SATS attachment support for player mail.
- Added `MMOMailmon` test mailbox auto-reply.
- Added red `System` global chat messages from server admin commands.
- Added MMOMailmon reward-mail admin commands for event rewards:
  - `/givemon`
  - `/giveitem`
  - `/givesats`
  - `ALL` targeting for server-wide rewards.

### Compact server folder

The runtime server folder is now intentionally small:

```text
Server/
  MMOnsterpatchServer.py
  MMOnsterpatchServerUI.ps1
  Start-ServerUI.bat
  Start-ServerUI-Hidden.vbs
  configs/
    worldserver.ini
```

The server auto-creates runtime folders when needed:

```text
Server/data/
Server/logs/
Server/backups/
```

### Trading Post updates

- Browse/My Listings filter bar is now backed by server-side filtering.
- Filter controls include:
  - Search
  - Offered
  - Requested
  - Type
  - Time Left
  - Seller
  - Refresh
- Each filter control has its own configurable offset and size.
- Listing rows support MoN icons and SATS icons independently.
- The embedded SATS icon uses the transparent-background asset.
- Seller identity for new listings uses active character handle where available.
- My Listings is scoped to the current character where available.
- Listing mail returns and sale/completion mail are routed to the listing owner character mailbox.

### Mailbox updates

- Mail routing now resolves full character handles such as `GOOSE#7722`.
- Character mailboxes are separate from Steam/account identity when character identity is available.
- Compose Mail uses an attached side drawer.
- Reply autofills the sender’s full character/mail handle.
- Mail view supports Reply, Delete, and Close.
- Compose supports optional attachments:
  - None
  - MoN
  - SATS
- MMOMailmon auto-replies only when a player sends mail directly to `MMOMailmon`.
- MMOMailmon does not accept player attachments.

### Admin reward mail

Admin reward commands now create mailbox rewards from `MMOMailmon` instead of injecting rewards invisibly.

Reward mail uses:

```text
From: MMOMailmon
Subject: System Reward
Body: Thank you for playing MMOnsterpatch. Here's a little gift to show our appreciation.
```

Supported commands:

```text
/givemon Player#1234 PIGLIT 5
/givemon Player#1234 PIGLIT 5 Shiny
/giveitem Player#1234 Potion 5
/givesats Player#1234 1000
```

Server-wide event rewards are supported with `ALL`:

```text
/givemon ALL PIGLIT 5 Shiny
/giveitem ALL Potion 5
/givesats ALL 1000
```

`ALL` sends reward mail to every character mailbox the server can resolve, excluding `MMOMailmon`.

### System chat

Server admins can broadcast to Global chat as red `System` text:

```text
/system Server restart in 5 minutes.
```

Players cannot impersonate the server-side `System` sender.

### GM command documentation

A GM/admin command reference is included in:

```text
gmlist.md
```

### Notes

- Old mail/listings created before character-scoped routing may still show older Steam/account-style identities until recreated or touched by the new flow.
- Existing duplicate character handles created during testing are not deleted automatically. The server should stabilize on the latest active account+slot character identity going forward.
- `/giveitem` item delivery depends on the client’s item lookup by item name or ID when the reward is claimed.

## Previous v0.11.0 testing milestones

### v0.11.0 - Base

- Promoted the tuned Trading Post layout to the new v0.11.0 base.
- Added transparent embedded SATS coin icon.
- Added server-backed Trading Post filters.
- Added Trading Post window position/size persistence.
- Added chat minimized-state persistence.

### v0.11.0 - Solid mail/filter update

- Added independent filter bar config for each visible control.
- Moved Compose Mail into an attached side drawer.
- Added automatic mailbox creation/refresh on login.
- Added Player#0000-style mailbox handles and reply autofill.
- Added MMOMailmon test mailbox auto-reply.

### v0.11.0 - Character mailboxes and listings

- Routed new mail to active character public handles.
- Added character-scoped mailbox columns and character mailbox backfill.
- Added character-aware Trading Post seller, My Listings, cancel, completion, and expiration mail.

### v0.11.0 - Compact server and mail attachments

- Added compact server runtime package.
- Added server UI with Start/Stop and Auto Restart.
- Added mail compose attachments for MoN/SATS.
- Added red System global chat command.

### v0.11.0 - Handle stability fix

- Fixed character handles changing every login.
- Server now reuses latest active account+slot identity.
- Added hidden UI launcher.

### v0.11.0 - Event Reward Mail

- Added `/givemon`, `/giveitem`, and `/givesats` as MMOMailmon reward mail commands.
- Added `ALL` targeting for server-wide event rewards.

## v0.11.0 Launcher Flow Test
- Added `Launcher/` source for `MMOnsterpatchLauncher` test app.
- Launcher writes a session token and launches Monsterpatch with `MMONSTERPATCH_LAUNCHER=1`.
- Client detects launcher sessions.
- Old save-select `Switch to Online Mode` / `Switch to Offline Mode` button is removed for this flow.
- Save-select delete button is removed for this flow; Extras delete is routed toward online delete mode when launched by the launcher.
- Title screen attempts launcher-only takeover: Play text becomes `Log In`, Options is hidden, Extras is retained.
- Extras attempts launcher-only text/link takeover: Discord becomes GitHub and Discord URL routes to the MMOnsterpatch GitHub page.
- Launcher-mode disconnect attempts to close the game after saving/disconnecting instead of returning to title.
- Embedded online-only title logo and save plus icon assets added.

## v0.11.0 - LauncherFlowTest BuildFix
- Fixed missing launcher-flow runtime field declarations in `OfficialServerSaveSelectNative.cs`.
- Fixes compile errors for launcher session state and online menu embedded sprite swap fields.
- No intended behavior changes from the LauncherFlowTest package.