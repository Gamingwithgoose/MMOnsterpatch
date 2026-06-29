MMOnsterpatch AIO v0.8.2 Ranked Tab Paging Active Hold Test

Built from the v0.8.2 RankedFoundationChatMenuTest source package.

Update required:
- Client patcher update required.
- Server update required for ranked rules tables, ranked profile replies, and Global chat guild-tag labels.

Client changes:
- Emoji inline Y offset remains -6f.
- Guild chat emoji spacing fix kept.
- Guild tab messages still omit the [GUILD] channel prefix.
- Guild name max remains 18 characters.
- Chat menu remains: [current chat dropdown] [Trading Post].
- Current chat dropdown offers Global, Guild, and Ranked.
- Global and Guild remain chat windows.
- Ranked remains a read-only character rank information panel.
- Chat input is disabled while the Ranked tab is selected.
- Ranked panel now shows current rank information plus ranked requirements.
- Ranked requirements display:
  - 4 battle-ready MoN required.
  - All ranked MoN must be level 50 or higher.
  - Max rank gap is 2 ranks apart.
- Ranked buttons/actions remain disabled in this build.
- Global chat still replaces [GLOBAL] with the sender's guild tag when available, or no tag if the sender is not in a guild.

Server changes:
- Social server version updated to v0.3.3-ranked-rules-foundation.
- Combined server banner updated to v1.1.2-ranked-rules-foundation.
- Added automatic best-effort timestamped social.db backup before schema migration.
- Added schema_migrations tracking table.
- Existing ranked_seasons and ranked_profiles tables remain additive.
- Added ranked_rules table.
- Added ranked_rp_rules table.
- Added ranked_performance_modifiers table.
- Added ranked_matches placeholder table for future resolved ranked battles.
- Added ranked_audit_log placeholder table for future RP/history tracking.
- Season 0 remains seeded for October 1st, 2026.
- Season 0 ranked rules are seeded as disabled/inactive:
  - Required team size: 4.
  - Minimum MoN level: 50.
  - Max allowed rank gap: 2.
  - Max RP: 1000.
  - Ranked actions enabled: false.
- Draft Season 0 RP rules are stored server-side:
  - E: +4 / -2
  - D: +5 / -4
  - C: +6 / -6
  - B: +7 / -8
  - A: +8 / -10
  - S: +0 / -12
- Draft performance modifiers are stored server-side:
  - 4 MoN remaining: winner +2, loser +1 extra loss.
  - 3 MoN remaining: winner +1, normal loss.
  - 2 MoN remaining: normal win, loser -1 loss reduction.
  - 1 MoN remaining: winner -1, loser -2 loss reduction.
- Added draft ranked delta helper logic for future ranked result resolution, but it is not applied yet.
- Unknown social commands now print the exact command to the server console and include it in the system message.

Safety notes:
- This build does not enable ranked battles.
- This build does not change RP from battle results.
- This build does not delete or overwrite existing accounts, characters, guilds, or ranked profiles.
- Database changes are additive and use CREATE TABLE IF NOT EXISTS / INSERT OR UPDATE style seeding.
- social.db is backed up before migration when the file already exists.
