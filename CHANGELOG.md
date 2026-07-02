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
