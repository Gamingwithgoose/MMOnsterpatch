# MMOnsterpatch Official Server GM/Admin Commands

Compatibility target: **Monsterpatch Game Version 0.181**  
Build family: **v0.11.0 Event Reward Mail**

These commands are entered in the **Server UI admin command box** or the server console/admin input loop.

## Quick examples

```text
/system Welcome to MMOnsterpatch!
/givemon GOOSE#6143 PIGLIT 5 Shiny
/giveitem GOOSE#6143 Potion 5
/givesats GOOSE#6143 1000
/givemon ALL PIGLIT 5 Shiny
/giveitem ALL Potion 5
/givesats ALL 1000
/bansteam 76561198000000000 Cheating
/unbansteam 76561198000000000 Appeal accepted
/bancheck 76561198000000000
/banlist
/adminhelp
```

## Player targeting

Most reward commands accept either a full character mailbox handle or `ALL`.

### Single character

```text
/givemon Player#1234 PIGLIT 5 Shiny
/giveitem Player#1234 Potion 5
/givesats Player#1234 1000
```

Use the full character handle, including the `#xxxx` tag.

### Everyone

```text
/givemon ALL PIGLIT 5 Shiny
/giveitem ALL Potion 5
/givesats ALL 1000
```

`ALL` sends the reward to every character mailbox the server can resolve. `MMOMailmon` is skipped.

## Reward mail behavior

The reward commands send mailbox rewards from:

```text
MMOMailmon
```

Reward mail uses:

```text
Subject: System Reward
Body: Thank you for playing MMOnsterpatch. Here's a little gift to show our appreciation.
```

The reward is attached to the mail and claimed by the recipient from their mailbox.

This is intended for events, giveaways, compensation, testing, and admin rewards.

## /givemon

Sends a MoN reward attachment from MMOMailmon.

### Syntax

```text
/givemon <Player#1234|ALL> <mon name or mon id> [level] [Shiny]
```

### Examples

```text
/givemon GOOSE#6143 PIGLIT
/givemon GOOSE#6143 PIGLIT 5
/givemon GOOSE#6143 PIGLIT 5 Shiny
/givemon GOOSE#6143 23 10
/givemon ALL PIGLIT 5 Shiny
```

### Arguments

- `<Player#1234|ALL>`: full character mailbox handle or `ALL`.
- `<mon name or mon id>`: MoN name/key or numeric MoN ID.
- `[level]`: optional. Defaults to `1` if omitted.
- `[Shiny]`: optional. Accepted shiny words include `Shiny`, `shiny=true`, `true`, `sparkle`, or `sparkly`.

### Notes

- The command creates mail with a MoN reward attachment.
- The MoN is created when the recipient claims the mail attachment.
- Names with spaces should be wrapped in quotes.

Example:

```text
/givemon GOOSE#6143 "Some MoN Name" 5 Shiny
```

## /giveitem

Sends an item reward attachment from MMOMailmon.

### Syntax

```text
/giveitem <Player#1234|ALL> <item name or item id> [amount]
```

### Examples

```text
/giveitem GOOSE#6143 Potion
/giveitem GOOSE#6143 Potion 5
/giveitem GOOSE#6143 14 5
/giveitem ALL Potion 5
```

### Arguments

- `<Player#1234|ALL>`: full character mailbox handle or `ALL`.
- `<item name or item id>`: item name/key or numeric item ID.
- `[amount]`: optional. Defaults to `1` if omitted.

### Notes

- The command creates mail with an item reward attachment.
- The item is added when the recipient claims the attachment.
- Names with spaces should be wrapped in quotes.

## /givesats

Sends a SATS reward attachment from MMOMailmon.

### Syntax

```text
/givesats <Player#1234|ALL> <amount>
```

### Examples

```text
/givesats GOOSE#6143 1000
/givesats ALL 250
```

### Arguments

- `<Player#1234|ALL>`: full character mailbox handle or `ALL`.
- `<amount>`: SATS amount to attach.

### Notes

- Amount must be greater than `0`.
- The SATS are added when the recipient claims the mail attachment.

## /system

Sends a red Global chat message as `System`.

### Syntax

```text
/system <message>
```

### Examples

```text
/system Server restart in 5 minutes.
/system Event rewards have been sent. Check your mailbox!
```

### Notes

- Sends to the Global channel.
- The sender appears as `System`.
- Client displays it as red system text.
- Players cannot use this command from in-game chat to impersonate System.

## /bansteam

Bans a SteamID64 and disconnects active sessions for that Steam account.

### Syntax

```text
/bansteam <SteamID64> <reason>
```

### Example

```text
/bansteam 76561198000000000 Cheating / exploit abuse
```

### Notes

- Revokes active AIO sessions for that SteamID64.
- Disconnects active social/trading sessions where applicable.
- If no reason is given, the server uses a default reason.

## /unbansteam

Clears active bans for a SteamID64.

### Syntax

```text
/unbansteam <SteamID64> <reason>
```

### Example

```text
/unbansteam 76561198000000000 Appeal accepted
```

### Notes

- If no reason is given, the server uses a default unban reason.

## /bancheck

Checks whether a SteamID64 has an active ban.

### Syntax

```text
/bancheck <SteamID64>
```

### Example

```text
/bancheck 76561198000000000
```

## /banlist

Lists active SteamID bans.

### Syntax

```text
/banlist
```

### Notes

- Shows up to the server’s active-ban display limit.

## /adminhelp or /help

Prints the command help list in the server UI/console output.

### Syntax

```text
/adminhelp
/help
```

## MMOMailmon player test mailbox

Players can send normal mail to:

```text
MMOMailmon
```

MMOMailmon replies with a server-generated message containing MMOnsterpatch support/social links.

### Notes

- MMOMailmon only auto-replies when mailed directly.
- MMOMailmon does not accept attachments from players.
- Admin reward mail is also sent from MMOMailmon.

## Server UI notes

Launch the server UI with:

```text
Server/Start-ServerUI.bat
```

The UI includes:

- Start Server button.
- Stop Server button.
- Auto Restart checkbox.
- Admin command text box.

The launcher is designed to show the server UI without leaving an extra console window open.
