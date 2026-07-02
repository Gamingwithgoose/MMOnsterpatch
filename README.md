# MMOnsterpatch Official Server v0.11.0 Event Reward Mail

Compatibility target: **Monsterpatch Game Version 0.181**.

## Needs replaced

**Client + Server**

Back up/keep before replacing server files:

```text
Server/data/
Server/configs/worldserver.ini
```

## GitHub release docs

- `CHANGELOG.md` contains the polished v0.11.0 release changelog.
- `gmlist.md` contains GM/admin command syntax, examples, and behavior.

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

## Build

Build the client locally with:

```bat
Source\build_all.bat
```

---

## Summary

This v0.11.0 package includes the compact server UI, character-scoped mail/listings, stable character handles, Trading Post filters/layout persistence, mailbox compose/attachments, transparent SATS icon, MMOMailmon auto-reply, red System global chat, and MMOMailmon event reward mail commands.
