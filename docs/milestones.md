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

## Milestone 2 — Surfaces ✓
- [x] Slidy surface (fixed speed, fixed turning rate, never stops)
- [x] Straight surface (no input accepted, exits on surface change)
- [x] Kill surface (instant death — corpse deferred to Milestone 3)
- [x] Fast surface (Slidy at 2x speed, same turning radius)
- [x] Confusing surface (Slidy but steers away from click)
- [x] Fast Confusing surface (Confusing at 2x speed, same turning radius)
- [x] Test level using all surface types
- [x] Smooth visual transitions between surface types (no jagged edges)

---

## Milestone 3a — Death & respawn ✓
- [x] Start block (players spawn here)
- [x] Kill surface leaves a corpse instead of instant reset
- [x] Respawn at start block after short delay
- [x] End block (touching it beats the level)

---

## Milestone 3b — HUD ✓
- [x] Timer counting up from 0:00
- [x] Player name, alive/dead status
- [x] Death counter

---

## Milestone 3c — Level transition ✓
- [x] Auto-advance to next level
- [x] Loading screen showing deaths + who beat the level
- [x] Short delay before next level loads

---

## Milestone 4a — Ability bar & skill points ✓
- [x] Ability bar in HUD (5 slots: Q, W, E, R, F — key label, level dots)
- [x] Skill point system (earn 1 on level complete, persist across levels via RunState)
- [x] Spend points to level up an ability (+ button or Ctrl+key)
- [x] Advanced ability lock (W, E, R grayed out until Lv.3)

---

## Milestone 4b — Starter abilities: Boost & Gack ✓
- [x] Boost (Q) — speed boost on ground, activatable on any surface, full duration regardless of surface
- [x] Gack (F) — leaves a distance-based goo trail that boosts speed 40% on non-ground surfaces

---

## Milestone 4c — Warp ✓
- [x] Warp (W) — places a ghost at the unit's current position; reactivating warps the unit back to it
- [x] Ghost fades and disappears after its duration expires
- [x] Ghost is visible to all players (multiplayer-ready visual)

---

## Milestone 4d — Donut ✓
- [x] Donut (E) — fires a ring projectile in the unit's facing direction at fixed speed; stationary on ground; passes through and resurrects all corpses it touches; unaffected by surfaces

---

## Milestone 4e — Ethereal ✓
- [x] Ethereal (R) — unit becomes ethereal; touching a corpse resurrects it at the ethereal unit's position with matching velocity; can resurrect multiple corpses per activation

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

---

## Deferred Items

- Upgrade UI feedback: flash or sound when a point is spent (deferred from 4a, low priority)
- Waypoint cleanup: when a unit reaches its waypoint on ground, the waypoint marker should be removed from the display
- Slidy surface steering: holding right-click should orbit the unit around the target point, not steer toward a point on the tangent of the unit's circle — needs tuning to feel correct
