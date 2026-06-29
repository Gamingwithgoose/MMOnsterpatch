# MMOnsterpatch AIO Server

MMOnsterpatch AIO is the online server and client patcher setup for Monsterpatch multiplayer features. The goal is to keep the base game feeling like Monsterpatch while adding optional online systems: player presence, shared overworld spawns, chat, guilds, Trading Post storage, GTS trades, PvP hooks, and future ranked seasons.

The server is built around manual connection. Players stay offline by default, then choose when to connect through the AIO chat window. That keeps normal single-player sessions clean and avoids surprise online behavior at game start.

This repository currently covers the v0.8.1 account and character foundation branch.

## Current Server Focus

The current server is focused on three things:

1. Keeping online play intentional and lightweight.
2. Moving important online identity and gameplay ownership to the server.
3. Preparing the database foundation for ranked PvP, seasons, rewards, and wager battles.

The ranked and wager systems are planned features. The current build lays the account, character, and slot identity structure they will sit on.

---

# Connection and Online World

## 1. Manual Connect and Disconnect

Players connect on purpose through the AIO chat window instead of auto-joining when the game starts.

This keeps offline play clean, makes testing easier, and gives players control over when they appear online.

## 2. Overworld Player Presence

Players on the same map can see each other online with synced character details, including name, design, colors, facing direction, movement state, and busy/available status.

## 3. Step-Based Movement Sync

The server relays real step movement instead of trying to guess where a player should be. This keeps remote movement closer to what the local player actually did and helps avoid awkward sliding or fake interpolation.

## 4. Map-Scoped Online Snapshots

Players only receive live player and spawn data for the map or cluster they are currently using.

The server does not need to send unrelated map data to everyone, which keeps the online world lighter and cleaner.

## 5. Follower State Relay

Follower state is included with online player updates. The server can relay follower position, facing, movement, sprite identity, shiny/normal state, and flip state so remote followers display correctly.

## 6. Remote Visual Events

The server can relay one-shot visual events for remote actions, including broom state, casting, water/bounce state, and tool-style actions.

The client replays these using the game's normal visual behavior where possible, instead of using random custom effects.

---

# Account and Character Identity

## 7. Steam-Backed Server Account

The v0.8.1 foundation creates or loads a permanent MMOnsterpatch account for the authenticated Steam identity.

That account receives an internal AccountUUID. The AccountUUID is the main online account record, while characters are added under that account by save slot.

## 8. No Steam Passwords Sent to the Server

Steam authentication does not send a player's Steam username or password to the MMOnsterpatch server.

Authentication is handled through Steam. The server receives the verified Steam identity result and uses that to create or load the matching MMOnsterpatch account.

## 9. Characters Under the Account

Each save slot can have its own active character under the player's MMOnsterpatch account.

Each character receives:

- CharacterUUID
- secret token
- display name
- public handle
- save slot index
- slot fingerprint
- active or archived status

This lets ranked records, guild data, trade history, rewards, and future wager history belong to the character instead of only the Steam account.

## 10. Public Handles

Public handles use the character name plus a four-digit tag, such as:

```text
CharName#1234
```

The four-digit tag is generated randomly from 0000 to 9999.

The same number can be reused by different names. The full handle is what matters.

```text
CharName#1234
OtherName#1234
```

Those are different handles because the name in front is different.

## 11. Slot Fingerprint Protection

The client sends a compact fingerprint for the active save slot when registering with the server.

If the fingerprint matches the active character record, the server keeps the same CharacterUUID, public handle, and character-linked data.

If the fingerprint does not match, the server treats it as a different character in that slot.

## 12. Archived Characters Instead of Hard Deletes

When a save slot changes, the old character is archived instead of deleted.

For example, if slot0 used to belong to one character and the player creates a new character in slot0, the server can archive the old slot0 character as inactive and create a new active CharacterUUID for the new save.

This is important for player safety. Ranked history, rewards, and recovery data should not be wiped just because a slot changed.

## 13. Recovery Foundation

The server keeps archived character records and stores a compact recovery snapshot/fingerprint summary for future recovery tooling.

Full character save restoration is not live yet. The current goal is to make sure old character records are preserved instead of destroyed.

---

# Server-Owned Visible Overworld Spawns

## 14. Server-Controlled Spawn Rates

Visible overworld spawns are owned by the server. Players do not have client config access to server spawn multipliers, caps, or timing.

## 15. 2x Vanilla Visible Spawn Rate

Vanilla visible overworld spawnZone rolls are treated as 25% per spawnZone. The server currently uses a locked 2x rate, making that 50% per reported spawnZone.

## 16. Official-Feeling Rarity

Visible spawn rarity stays close to the normal game feel:

- 65% Common
- 30% Uncommon
- 5% Rare

This system does not change normal random grass or cave encounter rates.

## 17. Stationary SpawnZone Spawns

Overworld MoNs remain stationary, matching vanilla visible spawn behavior.

The system does not currently add roaming or wandering logic.

## 18. Active Map Spawn Management

The server only keeps overworld spawn records active for maps players are actually using.

If players remain on a map, the server can run controlled top-up passes. If no players remain on a map long enough, the server can clean those spawns from memory.

## 19. Shared Capture Ownership

When a public server spawn is being captured, the server marks it busy so two players cannot capture the same visible MoN at the same time.

Catch start, catch end, and final catch results can be broadcast so other players see the same spawn disappear or return to an available state.

---

# Personal Random Encounter Reward Spawns

## 20. Battle Reward Spawns Are Online

When a player wins a normal random encounter and opens the reward crystal, those reward MoNs can spawn visibly online for everyone on the map.

## 21. Exact Battle Result Data

Personal reward spawns use the exact defeated encounter MoNs from the battle result instead of rerolling them.

That preserves species, level, shiny state, and saved MoN data.

## 22. Anti-Steal Protection

Reward spawns are visible to everyone, but only the player who won the battle can capture them while that player remains on the map.

If another player tries to capture a locked personal reward spawn, they receive a Monsterpatch-style message explaining that they do not have permission to capture that MoN.

## 23. Unlock When Owner Leaves

If the owner leaves the map or disconnects, the same reward spawn can stay visible and flip from owner-only to public.

This keeps the spawn from being wasted while still protecting the player who earned it first.

## 24. Server-Side Enforcement

The client blocks invalid interaction locally, but the server also rejects invalid capture claims.

The protection does not rely only on client behavior.

---

# Chat, Guilds, and Social Features

## 25. Global Chat

Connected players can use the global chat channel with join/leave system messages and clean display names.

## 26. Guild Creation and Joining

Players can create or join a guild. The chat window includes a dedicated Guild section for guild state, invites, and Guild Chat.

## 27. Guild Invites

Guild leaders can invite players by public handle or unique display name.

Invited players receive an Accept/Decline popup.

## 28. Guild Chat

Guild messages only go to members of the same guild.

Messages are labeled by rank and display name so guild chat stays readable.

## 29. Guild Leave Handling

Players can leave guilds.

The server can transfer leadership to the oldest remaining member if the leader leaves, or disband the guild if the leader is alone.

## 30. Chat UI Extras

The AIO chat window includes improved scrollback, an active input indicator, tighter message prefixes, and an icon picker for styled inline chat messages.

---

# Trading Post and GTS

## 31. Trading Post Integration

The Trading Post is integrated into the AIO flow and can be opened from the chat window or pause-menu entry.

It is not meant to double-install as a separate old QuickSpell system.

## 32. Server Bank

Each account has a 240-slot server bank for online storage through the Trading Post service.

## 33. GTS Listings

Players can create MoN-for-MoN trade listings, browse paged listings, filter by requested species, view their own listings, and cancel open listings.

## 34. GTS Offers and Claims

When a player offers the requested species, the server completes the trade, gives the buyer the listed MoN, and stores the offered MoN for the listing owner to claim.

## 35. Accepted Trade Notifications

The server can notify players when one or more of their GTS listings were accepted and have claimable rewards waiting.

## 36. Trading Identity Protection

Steam-backed identity helps reduce basic spoofing and scam attempts because Trading Post actions can be tied to a verified player identity instead of a loose client claim.

---

# Global Box and Storage Quality-of-Life

## 37. Native Local and Global Box Buttons

The Mon Box has LOCAL and GLOBAL mode buttons with native-style cursor/navigation behavior.

This avoids the rejected Send To submenu direction and keeps the box flow closer to the game UI.

## 38. Global Box Persistence

The Global Box saves to `globalBox.txt` and supports loading saved MoN data back into the box view.

## 39. Autosave on Transfers

Moving MoNs between Local and Global boxes can immediately save the Global Box and force the normal game save so the local box side is preserved too.

## 40. Official Box UI Feel

The Global Box work uses BoxManager fields and official UI patterns where possible, including the game's TMP font and box count display.

## 41. AIO Storage Note

Global Box is an AIO quality-of-life storage feature.

It is not the public shared server database. Server-side shared storage is handled separately through the Trading Post bank.

---

# Battle and PvP Hooks

## 42. Battle Requests

The MMO server can route 1-to-1 battle requests, accepts, declines, and busy replies between nearby online players.

## 43. Team Payload Exchange

Battle request packets include team payload data so the receiving client can prepare the remote player's team for battle setup.

## 44. Real Battle Command Routing

The server can relay selected move/target commands, battle hits, battle-done packets, and battle state packets between two PvP clients.

## 45. Status Safety

The server checks basic availability and busy state before routing battle requests so players already busy are not pulled into another request.

---

# Planned PvP and Ranked Roadmap

The next PvP expansion is planned around giving players more meaningful battle options, character-based ranking, seasonal progression, and optional high-stakes wager battles.

Ranked Season 0 is planned to begin on October 1st, 2026.

SATS Wager Battles and possibly MoN Wager Battles are planned before then as pre-season features.

## 46. PvP Battle Menu Expansion

The battle request window is planned to grow from a simple battle prompt into a cleaner PvP mode selector.

Normal battles stay fast and casual.

Special battles open advanced options:

- Ranked (RP)
- Wager (SATS)
- Wager (MoN)

This keeps casual battles simple while giving competitive or high-stakes modes their own place.

## 47. Ranked (RP) Battles

Ranked battles will use Rank Points, or RP, to track competitive progress.

RP is planned to be tied to each character/save slot under the player's MMOnsterpatch account, so different characters can have different rankings.

The maximum RP is planned as 1000.

| RP Range | Rank |
| --- | --- |
| 0 - 199 | E |
| 200 - 399 | D |
| 400 - 599 | C |
| 600 - 799 | B |
| 800 - 999 | A |
| 1000 | S |

Ranked records are planned to track RP, rank, wins, losses, highest RP, highest rank, and season history for each character.

## 48. SATS Wager Battles

SATS Wager Battles are planned as a pre-season PvP feature before Season 0.

A player chooses a SATS amount and sends a wager battle request to another player. If both players accept and both can cover the wager, the SATS are placed into a match pool. The winner receives the full pool after the battle.

This mode is meant for players who want risk and reward without affecting ranked RP.

## 49. MoN Wager Battles

MoN Wager Battles are planned as a possible pre-season feature.

Each player selects one MoN from their team. Only the selected MoN are used for the battle. The winner earns a locked overworld reward spawn that is an exact clone of the defeated MoN.

The losing player's selected MoN is removed from their team after the match is resolved.

This mode is intended to feel rare, risky, and exciting, with real consequences and a meaningful reward for the winner.

## 50. Ranked Season 0

Season 0 is planned to begin October 1st, 2026.

Season 0 will be the first official ranked season. Ranked progress will be tracked per character, with each character having its own RP, rank, wins, losses, and season record.

Seasons are planned to last about three months, following a yearly quarter schedule.

## 51. Ranked Chat Tab

A Ranked tab is planned for the MMOnsterpatch chat window.

Planned ranked tab features include:

- current rank
- current RP
- season information
- win/loss record
- available season rewards
- reward claim buttons

## 52. End-of-Season Rewards

At the end of each season, rewards are planned based on season performance.

Higher ranks should receive better rewards, and top-end ranks may receive special seasonal items.

Rewards are planned to be claimable through the Ranked tab. The goal is for the game to give reward items directly to the player when they claim them.

## 53. Long-Term Seasonal Cycle

After Season 0, ranked seasons are planned to continue on a regular three-month cycle.

Each new season can bring a fresh RP climb, new seasonal rewards, and new competitive goals while keeping character-based records from earlier seasons.

---

# Data Safety Goals

The server should fail safely, not destructively.

For account, character, ranked, reward, and wager systems, the long-term safety rules are:

- do not hard-delete character records automatically
- archive old slot characters instead of wiping them
- keep ranked records tied to CharacterUUID
- keep account identity tied to AccountUUID
- keep public handles as display identity, not the real database identity
- do not mark rewards claimed until the client confirms the reward was actually given
- use transaction-style records for risky systems like SATS and MoN wagers
- if the database has an error, block the affected online feature instead of overwriting player progress

The goal is simple: a server or database problem should not erase player progress.

---

# Repository Layout

This repository is organized around the client patcher and the Python server.

```text
Source/
  Client-side AIO patcher source.

Server/
  MMOnsterpatch server source for online world state, social identity, Trading Post, GTS, guilds, and PvP routing.
```

Both sides should be updated together when testing account, character, Trading Post, or PvP changes.

---

# Current Development Status

This is still an alpha online system.

The current v0.8.1 branch is mainly about getting the account and character foundation in place before ranked progression is added.

Live or working foundation areas include:

- manual Connect/Disconnect
- online player presence
- remote player/follower sync
- server-owned visible overworld spawns
- personal reward spawn locking/unlocking
- global chat
- guilds and guild chat
- Trading Post and GTS systems
- server bank storage
- Global Box quality-of-life storage
- PvP request and command routing hooks
- Steam-backed MMOnsterpatch account foundation
- character records under each account by save slot
- slot fingerprint checks
- archived character records for safer future recovery

Planned next areas include:

- Special Battle menu
- Ranked (RP) battle records
- Ranked chat tab
- SATS Wager Battles
- possible MoN Wager Battles
- Season 0 ranked launch
- end-of-season reward claims
- better recovery tooling for archived characters

Planned first official ranked season:

```text
Season 0 - October 1st, 2026
```

Pre-season PvP focus:

```text
Special Battle menu
SATS Wager Battles
Possible MoN Wager Battles
```
