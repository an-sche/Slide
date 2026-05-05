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

## Milestone 5a — Enemy foundation ✓
- [x] `IEnemyBehavior` interface (`void Process(float delta, Enemy enemy)`)
- [x] `Enemy` class — configurable radius and color, holds one `IEnemyBehavior`, kills player on contact, added to `"enemies"` group
- [x] Circle placeholder visual (same style as Unit)

---

## Milestone 5b — Patrol behavior ✓
- [x] `Waypoint` record — `Position` (Vector2) + `Speed` (float)
- [x] `PatrolEndBehavior` enum — `Loop` / `Disappear`
- [x] `PatrolBehavior` — moves through waypoints in order at per-waypoint speed; starts at waypoint[0]; loops or disappears at end

---

## Milestone 5c — Random wander behavior ✓
- [x] `RandomWanderBehavior` — polygon area (Vector2[]), speed, min/max idle duration, optional start position
- [x] Idle → Telegraph → Moving → Idle state machine; telegraph flashes a ring on the enemy for 0.5s before it moves
- [x] Random target points sampled uniformly via triangle decomposition — no rejection sampling, no loops
- [x] If no start position provided, begin at a random point inside the polygon

---

## Milestone 6 — Multiplayer foundations (localhost)

**Testing:** Use Godot's "Debug → Run Multiple Instances" to launch host + client(s) on the same machine.

### 6a — Fixed timestep & simulation cleanup ✓
- [x] Move `Unit` and `Enemy` simulation from `_Process` to `_PhysicsProcess`
- [x] Verify movement feels identical before and after the switch

### 6b — Network transport & roles
- [ ] Integrate Godot's built-in ENet transport (no Steam yet)
- [ ] Main menu: "Host" and "Join" buttons (join connects to localhost by default)
- [ ] Host assigns peer IDs; each peer controls exactly one `Unit`

### 6c — Input pipeline
- [ ] Client sends right-click target and ability keypresses to host via `@rpc`
- [ ] Host applies inputs, simulates, and is the sole authority on all game state
- [ ] Local unit on host simulates directly (no RPC round-trip for host player)

### 6d — State synchronization
- [ ] `MultiplayerSynchronizer` on each `Unit` — broadcasts `GlobalPosition`, `Velocity`, `Facing`, `IsDead`
- [ ] Enemy positions broadcast from host each tick
- [ ] Clients render received state; no client-side prediction yet

### 6e — Game flow
- [ ] Resurrection by touching a teammate's corpse (respawns at start block)
- [ ] Team wipe detection: all players dead simultaneously → full run reset (level 1, 1 skill point each)
- [ ] Level complete triggers advance for all players simultaneously

### 6f — Per-player HUD
- [ ] Each client shows only their own unit's ability bar and status
- [ ] All clients show a shared scoreboard: player name, alive/dead, death count

---

## Milestone 7 — Steam networking
- [ ] Steam Relay (Steamworks P2P) integration
- [ ] Public and private lobbies
- [ ] Host selects level set (built-in or Workshop playlist)
- [ ] Ready-up flow, host starts run
- [ ] Up to 8 players
- [ ] Disconnect does not end run — original lobby members can reconnect mid-run

### Architecture notes

**Simulation model: host-authoritative using Godot's built-in multiplayer**
- Host runs all game logic (unit movement, enemy behaviors, ability effects, kill detection)
- Clients send inputs to host via `@rpc`; host simulates and syncs state back via `MultiplayerSynchronizer`
- Steam Relay (via GodotSteam addon) routes traffic between players — same role as Battle.net for StarCraft
- Host has zero latency advantage; acceptable for a co-op puzzle game

**GodotSteam addon required**
- Provides Steamworks integration (lobbies, relay, authentication) for Godot
- Works alongside Godot's built-in `MultiplayerAPI`

**Fixed timestep (done in 6a)**
- Unit, Enemy, DonutProjectile, and GooZone simulation moved to `_PhysicsProcess`; rendering stays in `_Process`
- Physics interpolation enabled project-wide for smooth visuals at any framerate

**Enemy sync**
- Patrol enemies are deterministic — host broadcasts config once at level load; clients can simulate locally
- Wander enemies and all kill decisions are host-authoritative only; results broadcast to clients

**Bandwidth**
- `MultiplayerSynchronizer` broadcasts positions each tick; 100 enemies × 8 bytes × 20 ticks/sec ≈ 16KB/s — well within Steam relay limits
- Player inputs (target point, ability key) are small RPCs from each client to host

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
