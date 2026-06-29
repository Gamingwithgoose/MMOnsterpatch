# Changelog

All notable MMOnsterpatch AIO changes will be tracked here.

## [v0.8.3] - 2026-06-29

### Added

- Added server-side safety work for database updates and moderation.
- Added automatic `social.db` backup support before migration/schema checks.
- Added clearer migration logging during server startup.
- Added daily chat log files for moderation and audit review.
- Chat logs are saved as daily JSONL files under `Server/data/chat_logs/`.
- Chat log entries include timestamp information, channel, sender identity, guild context when available, and the message text.
- Added server-side SteamID ban support.
- Added an `account_bans` moderation table for active bans and ban history.
- Added admin console commands:
  - `/bansteam <SteamID64> <reason>`
  - `/unbansteam <SteamID64> <reason>`
  - `/bancheck <SteamID64>`
  - `/banlist`
  - `/adminhelp`

### Changed

- Server banner/version output now clearly identifies the v0.8.3 server-safety/moderation build.
- Chat messages are saved to log files but are not echoed into the live server console.
- Banned SteamIDs are blocked during Steam/session login and social character registration.
- Existing active sessions for a banned SteamID are revoked when possible.
- Active banned Social and Trading Post sessions are disconnected when possible.
- Unbanning clears active bans without deleting ban history.

### Fixed

- Improved unknown social command logging so the exact command is visible in server output.
- Improved server startup visibility around migration checks and database safety steps.

### Safety

- Database migrations are intended to be additive and non-destructive.
- Existing account, character, guild, ranked, and Trading Post records should not be deleted by normal migration checks.
- SteamID bans target verified account identity instead of character names or public handles.
- Ban history remains available after unbanning.
- Chat log write errors are reported without printing every chat message to the live server console.

### Tested

- Confirmed admin ban commands parse correctly from the server console.
- Confirmed `/bansteam`, `/bancheck`, `/banlist`, and `/unbansteam` work as expected.
- Confirmed banning a real SteamID prevents that account from connecting in-game.
- Confirmed unbanning restores access.

### Notes

- This is a server-safety and moderation update.
- No client patcher update is required if already using the v0.8.2 client.
- Recommended before updating a public server: keep a manual copy of `Server/data/social.db` in addition to the automatic backup system.

## [v0.8.2] - 2026-06-28

### Added

- Added the first foundation for the future Ranked system.
- Added a new **Ranked** section inside the AIO chat window.
- Added read-only ranked character information:
  - Current Rank
  - RP
  - Wins
  - Losses
  - Highest Rank
  - Season status
- Added **Season 0** as the first planned ranked season.
- Season 0 is planned to begin on **October 1st, 2026**.
- Added ranked requirement display for future Ranked battles:
  - Ranked battles will require **4 battle-ready MoN**
  - All 4 MoN must be **level 50 or higher**
  - Ranked battles will use a maximum rank-gap rule
- Added the early server-side database foundation for ranked profiles, ranked seasons, ranked match records, and ranked audit history.
- Added safer database migration support so future ranked/social updates can be added without wiping the server database.
- Added automatic server database backup support before migrations.
- Added guild tags during guild creation.
- Guild tags support **3–4 letters or numbers**.
- Guild tags do not allow spaces and display in uppercase.
- Guild names now support letters, numbers, and spaces.
- Guild names now support up to **18 characters**.
- Added a new chat channel dropdown menu.
- The chat bar now uses:
  - Current chat dropdown
  - Trading Post button
- The dropdown currently includes:
  - Global
  - Guild
  - Ranked

### Changed

- Global, Guild, and Ranked are now selected through the chat dropdown instead of separate top-level buttons.
- The Ranked tab is read-only for now and disables chat input while viewing it.
- Ranked information is split into cleaner Status and Ruleset pages.
- The Ranked tab now stays visible while being viewed and does not fade out from inactivity until the player leaves it, minimizes chat, opens Trading Post, or presses Cancel/Esc.
- Global chat no longer shows the `[GLOBAL]` prefix.
- Guild chat no longer shows the `[GUILD]` prefix because Guild messages already live inside the Guild tab.
- Global chat can now show a player’s guild tag when that player is in a guild.
- Players without a guild tag do not show a tag in Global chat.
- Inline chat icons/emojis have improved spacing and vertical alignment.
- Emoji/icon position was adjusted so inline icons sit better with the game font.

### Fixed

- Fixed chat spacing when inline icons/emojis were used.
- Emoji messages previously caused large gaps between words because the icon rendering path split normal text into separate layout pieces.
- Fixed Guild tab message formatting so guild messages look cleaner.
- Fixed Ranked tab content cutting off by adding paging and scroll support.
- Improved server logging for unknown social commands so future issues are easier to track.
- Improved server version/banner output so it is easier to confirm the correct server build is running.

### Safety

- Ranked records are tied to the character/account foundation instead of only the player name.
- Ranked progress is designed to belong to the character record, not just a display name.
- Old character/account data is intended to be preserved through migrations.
- Server database changes should now be handled through migrations instead of requiring a fresh database.
- Database backups are created before migration when possible.

### Notes

- Ranked battles do **not** change RP yet.
- Ranked buttons/actions are still disabled.
- The Ranked tab is currently for viewing the foundation and planned rules only.
- This update requires both the updated AIO client patcher and updated MMOnsterpatch server.
- Existing guilds created before guild tags may not have a tag unless recreated or updated later.
