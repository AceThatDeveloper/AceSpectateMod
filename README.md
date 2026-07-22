# Ace Spectating Mod

A BepInEx spectate mod for Gorilla Tag. Lets you watch other players in the lobby from third or first person, with a styled in-game menu.

## Installation

1. Make sure [BepInEx](https://github.com/BepInEx/BepInEx) is installed for Gorilla Tag.
2. Drop the `.dll` into your `BepInEx/plugins` folder.
3. Launch the game.

No config file is generated — all settings live in the in-game menu and reset when the game closes.

## Controls

| Key | Action |
|---|---|
| `J` | Toggle the menu open/closed |
| `ESC` | Stop spectating |
| `[` / `]` | Cycle to the previous/next player |
| `F` | Toggle first person / third person view |

## Features

- **Players tab** — search the lobby, star favorite players, click Spectate on anyone
- **Camera tab** — adjust distance, height, follow smoothness, rotation smoothness, and view mode
- **Effects tab** — hide/show your own rig, manage your favorites list
- **Info tab** — current status, target, and control reference

## Notes

- While spectating, your movement is locked and your rig is repositioned to follow the target — this is a client-side modification of your player state.
- This is not safe from anti-cheat detection. Use in private/offline lobbies if you want to avoid any risk of action being taken on public servers.
- Built for BepInEx 5.4.23.5 on Gorilla Tag's Unity 6000.2.9 build. API references (`VRRig`, `GTPlayer`, etc.) may break on future game updates.

## Stuff

- i am NOT Responsible For Any Bans When Using This Mod
- Low Risk Of Getting Banned Using This
- Report Bugs To 🔗- https://discord.gg/C4KqCxh7kW
