# MMOnsterpatch Official Server v0.11.0 Base

Compatibility target: **Monsterpatch Game Version 0.181**.

## Needs replaced

**Client + Server** for this test build.

Keep/back up before replacing server files:

- `Server/data/`
- `Server/configs/worldserver.ini`

## v0.11.0 base changes

- Bakes the latest approved Trading Post layout config as the new default client config.
- Uses the transparent embedded SATS coin icon for listing rows.
- Adds server-backed Browse/My Listings filters:
  - search text
  - offered type
  - requested type
  - listing type
  - time-left bucket
  - seller search
- Adds Trading Post window position/size persistence in `goose.monsterpatch.gts.client.cfg`.
- Adds chat minimized-state persistence in `goose.monsterpatch.mmonsterpatchaio.cfg`.
- Keeps the v0.10.x Trading Post/Mailbox/SATS listing backend.

## Build

Build the client locally with:

```bat
Source\build_all.bat
```

This package includes both `Source/` and `Server/`.
