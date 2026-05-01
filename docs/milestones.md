# Milestones

Each milestone should be fully playable and testable before moving to the next.

---

## Milestone 1 — A unit on a map ✓
- [x] Single normal-ground tile map
- [x] Circle unit with directional arrow
- [x] Right-click to move (crisp, snappy, straight line, no pathfinding)
- [x] Unit stops exactly on target
- [x] Camera follows unit
- [x] Space tap to re-center camera
- [x] Space hold to lock camera to unit
- [x] Middle mouse drag to pan
- [x] Edge scrolling to pan

---

## Milestone 2 — Surfaces
- [ ] Slidy surface (fixed speed, fixed turning rate, never stops)
- [ ] Straight surface (no input accepted, exits on surface change)
- [ ] Kill surface (instant death, leaves corpse)
- [ ] Fast surface (Slidy at 2x speed and turn rate)
- [ ] Confusing surface (Slidy but steers away from click)
- [ ] Fast Confusing surface (Confusing at 2x speed and turn rate)
- [ ] Test level using all surface types
- [ ] Smooth visual transitions between surface types (no jagged edges)

---

## Milestone 3 — Win / death loop
- [ ] Start block (players spawn here)
- [ ] End block (touching it beats the level)
- [ ] Death and respawn at start block
- [ ] Corpse left behind on death
- [ ] Level transition screen (auto-advance, shows deaths + who beat the level)
- [ ] 10-second countdown or loading delay between levels
- [ ] Basic HUD: timer counting up, player name/status

---

## Milestone 4 — Abilities
- [ ] Ability bar in HUD (icons, cooldown overlay, + button, Ctrl+key upgrade)
- [ ] Skill point system (earn on level complete or bonus pickup, persist across levels)
- [ ] Advanced ability lock (Warp, Donut, Ethereal require 3 points earned)
- [ ] Boost (T) — normal ground only, no effect/no cooldown if used elsewhere
- [ ] Gack (F)
- [ ] Warp (Q)
- [ ] Donut (E)
- [ ] Ethereal (R)

---

## Milestone 5 — Enemies
- [ ] Enemy unit (any size, configurable)
- [ ] Patrol path (follows predefined waypoints, loops)
- [ ] Random movement (moves randomly within a defined area)
- [ ] Death on player contact

---

## Milestone 6 — Local multiplayer
- [ ] Two players on the same machine (split input)
- [ ] Resurrection by touching corpse (respawns at start block)
- [ ] Team wipe detection (all players dead simultaneously = full run reset)
- [ ] Per-player HUD: `Name spent (available)`, death count, resurrection count, alive/dead
- [ ] Run reset: back to level 1, 1 skill point each

---

## Milestone 7 — Steam networking
- [ ] Steam Relay (Steamworks P2P) integration
- [ ] Public and private lobbies
- [ ] Host selects level set (built-in or Workshop playlist)
- [ ] Ready-up flow, host starts run
- [ ] Up to 8 players
- [ ] Disconnect does not end run — original lobby members can reconnect mid-run

---

## Milestone 8 — Level editor
- [ ] Accessible from main menu
- [ ] Tile painter (select surface type, paint onto grid)
- [ ] Place start block, end block, bonuses
- [ ] Enemy placement + waypoint path editor
- [ ] Trigger system (place button, link to action e.g. open door)
- [ ] Save and load levels locally
- [ ] Publish to Steam Workshop
- [ ] Subscribe to and play Workshop levels
