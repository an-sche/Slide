# Multiplayer Design

## Overview

Slide supports up to 8 players over Steam relay (Milestone 7). Milestone 6 implements the full simulation and networking model using Godot's built-in ENet transport on localhost, so the architecture is production-ready before Steam is introduced.

---

## Simulation Model

**Host-authoritative.** The host runs all game logic: unit movement, enemy behaviors, ability effects, kill detection, bonus pickup, level completion. Clients send inputs to the host and receive state back.

- Host player simulates locally with no round-trip
- Clients send right-click target and ability keypresses via `@rpc` to host
- Host applies inputs, simulates, and broadcasts state via `MultiplayerSynchronizer`
- All kill decisions and game-flow events (wipe, level complete) originate on host

---

## Transport

**Milestone 6:** Godot's built-in ENet (`ENetMultiplayerPeer`)  
**Milestone 7:** Replaced with GodotSteam relay — same `MultiplayerAPI`, different peer

Default port: `7777`

---

## Main Menu

The game boots to a main menu scene (`MainMenu.tscn`) rather than directly into `World.tscn`.

**Layout:**
- Game title
- "Host" button — starts a server and enters the world as the host player
- "Join" button — reveals an IP address field (default: `127.0.0.1`) with a "Connect" button below it
- IP field is editable so players can connect across two real machines during testing

**Flow:**
```
MainMenu
  ├── Host → start ENet server → load World → spawn Unit for host
  └── Join → enter IP → Connect → load World → wait for host to spawn Unit
```

---

## Player Identity

Players are identified by their Godot `MultiplayerPeer` ID, assigned by host on connection.

**Names:** Auto-assigned as `Slider 1`, `Slider 2`, ... `Slider 8` based on join order. No name entry for now.

**Peer → Unit mapping:** Host maintains a dictionary of `peerId → Unit`. When a peer connects, the host spawns a Unit for them and registers it. When a peer disconnects, their Unit is removed.

---

## Input Pipeline

Clients do not simulate — they send discrete input events to the host:

| Event | RPC direction | Data |
|---|---|---|
| Right-click target | Client → Host | `Vector2 worldPosition` |
| Ability keypress | Client → Host | `AbilitySlot slot` |

The host applies these to the correct Unit on the next physics tick. The host player's input is applied directly with no RPC.

---

## State Synchronization

Each `Unit` has a `MultiplayerSynchronizer` broadcasting:
- `GlobalPosition`
- `Velocity`
- `Facing`
- `IsDead`

Enemy positions are broadcast from the host each tick. All enemy movement runs on host only — clients render received positions.

**Bandwidth estimate:** 100 enemies × 8 bytes × 60 ticks/sec ≈ 48 KB/s, well within Steam relay limits.

---

## Game Flow (Multiplayer)

**Level complete:** When any unit touches the end block, the host triggers level advance for all players simultaneously via RPC broadcast.

**Team wipe:** Host detects when all connected units are dead simultaneously → full run reset: back to level 1, 1 skill point each.

**Bonus pickup:** First unit to touch a bonus claims the point (host-authoritative). The bonus node is freed on the host; the removal propagates to clients via `MultiplayerSpawner`.

**Disconnect:** The disconnecting player's Unit is removed. The run continues for remaining players. Reconnection is a Milestone 7 feature (Steam lobby required).

---

## Camera

Each player has an independent camera. Players can pan freely to observe teammates. Tapping Space re-centers on their own unit; holding Space locks the camera to follow it.

---

## HUD (Per-player)

Each client displays:
- Their own unit's ability bar and status (level, alive/dead, deaths)
- A shared scoreboard showing all connected players: name, alive/dead, death count

---

## Player Count

Up to 8 players.

---

## Lobby & Matchmaking (Milestone 7)

- Private and public lobbies via Steam
- Host creates lobby and selects the level set (built-in campaign or Workshop playlist)
- Players join and ready up; host starts the run
- Players who were in the original lobby can reconnect mid-run — a disconnect does not end the run
- New players cannot join a run already in progress

---

## Testing (Milestone 6)

Use **Debug → Run Multiple Instances** in the Godot editor to launch host + client(s) on the same machine. Set instance count to 2 or more, run, Host on one window, Join on the other with IP `127.0.0.1`.
