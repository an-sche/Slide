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

## Milestone 4f — Bonus pickups ✓
- [x] `Bonus` node placed on the map — star placeholder visual, sprite later
- [x] First unit to touch it gains +1 `PlayerLevel` (skill point); bonus disappears immediately, no respawn
- [x] Shared: one bonus, one point — whichever player reaches it first gets it
- [x] Add several bonuses to the test level

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

### 6b — Network transport & roles ✓
- [x] Integrate Godot's built-in ENet transport (no Steam yet)
- [x] Main menu: "Play Solo", "Host", and "Join" buttons; Join reveals an IP field (default `127.0.0.1`)
- [x] Lobby: players see each other, click Ready; game starts when all are ready (guaranteed simultaneous World load → deterministic enemy sync from tick 0)
- [x] Peer disconnect removes that player's unit from the scene

### 6c — Input pipeline ✓
- [x] Client sends right-click target to host via `SetMoveTarget` RPC; host applies it to that peer's unit
- [x] Host is sole authority on all movement and game state; clients suppress local simulation
- [x] Host player's unit simulates directly with no RPC round-trip
- [x] Waypoint indicator appears immediately on the clicking player's screen and clears when the host clears the target
- [x] Ability keypresses (Q/W/E/R/F) forwarded from client to host via `UseAbility` RPC; host activates on the authoritative unit

### 6d — State synchronization ✓
- [x] Host broadcasts `GlobalPosition`, `Facing`, and move target for every unit each physics tick via `SyncUnitState` RPC (unreliable channel)
- [x] `IsDead` synced via separate reliable `BroadcastUnitDeath` / `BroadcastUnitRespawn` RPCs
- [x] Clients apply received state; no client-side prediction
- [x] Camera ignores input and stops updating when its window is not focused
- [x] Enemy positions synced via deterministic lockstep — per-instance seeded RNG, fixed physics tick, identical simulation on all peers; kill detection host-authoritative only (reconnection not supported)
- [ ] Velocity not synced (not needed; clients don't simulate movement)

### 6e — Game flow
- [x] Resurrection by touching a teammate's corpse — host-authoritative; broadcasts respawn to all clients; units cannot self-resurrect
- [x] Multiplayer death: units stay dead indefinitely until resurrected (no auto-respawn timer); solo mode keeps the 3-second auto-respawn
- [x] Team wipe detection: all players dead for 5 s → full run reset (level 1, 1 skill point each); timer cancels if any resurrection occurs during the window
- [x] Level complete triggers advance for all players simultaneously

### 6f — Per-player HUD
- [x] Each client shows only their own unit's ability bar and status (camera and HUD created only for local unit)
- [x] All clients show a shared scoreboard: player name, alive/dead, death count

---

## Milestone 7 — More enemy types

All new behaviors implement `IEnemyBehavior` and are fully compatible with the existing `Enemy` class and multiplayer determinism model.

### 7a — Orbiter ✓
- [x] `OrbiterBehavior` — circles a fixed center point at a configurable radius and angular speed (radians/sec); clockwise or counter-clockwise
- [x] Multiple orbiters can share the same center point

### 7b — Chaser
- [ ] `ChaserBehavior` — idles until any player enters a detection radius; switches to pursuit at fixed speed
- [ ] Telegraph: ring flash on the enemy for 0.5 s before it starts moving (mirrors RandomWander telegraph)
- [ ] Gives up and returns to idle if no player is within an extended give-up radius for N seconds

### 7c — Bouncer
- [ ] `BouncerBehavior` — moves in a straight line at fixed speed; bounces off a defined rectangular bounding box; configurable start position and initial direction

### 7d — Sniper
- [ ] `SniperBehavior` — stationary; aims a visible warning ray at the nearest player for 1 s; fires an instant-kill line projectile (`SniperBeam`) along that ray; configurable cooldown between shots
- [ ] `SniperBeam` node — thin line that persists for ~0.15 s then disappears; kills any player it overlaps on the frame it fires

### 7e — Guard
- [ ] `GuardBehavior` — wraps a `PatrolBehavior`; enters chase mode when a player steps inside a detection radius; returns to patrol when all players exit a larger give-up radius or N seconds pass with no player in range

---

## Milestone 8 — Level editor

Levels are stored as JSON files (`user://levels/<name>.json`). The format encodes a tile grid, entity list, and metadata. Workshop upload (Milestone 9) reuses the same files.

### 8a — Level file format & runtime loader
- [ ] Full JSON schema defined in `docs/map.md`: vertex-based corner grid (`(height+1) × (width+1)` surface codes), entities, enemies, spawners, triggers, doors
- [ ] At load time, `LevelLoader` runs marching squares on the corner grid → traces closed polygons per contiguous surface region → instantiates `SurfaceZone` nodes with polygon collision shapes; no tile grid exists at runtime
- [ ] Nearest-corner rule: each tile quadrant takes its nearest corner's surface type; boundary edges are exact diagonals — players die precisely where the visual edge appears
- [ ] `LevelLoader` wires up entities, enemies (inactive until spawner fires), spawner conditions (immediate / timed / trigger), trigger actions, and doors
- [ ] `World` accepts an optional level path; falls back to `res://levels/test.json` if none provided
- [ ] Export the existing hardcoded test level to `res://levels/test.json` and load it through `LevelLoader`

### 8b — Editor scene & tile painter
- [ ] Separate `Editor` scene accessible from main menu ("Edit Levels")
- [ ] Grid overlay showing tile boundaries; left-click/drag to paint the hovered corner vertex with the selected surface type
- [ ] Palette panel listing all surface types with color swatches; shortcut keys for common types
- [ ] Real-time preview: editor renders the marching-squares boundary as you paint so you see the exact physics polygon

### 8c — Entity placement
- [ ] Toolbar mode toggle: Corners | Entities | Enemies | Triggers
- [ ] Place / delete: StartBlock, EndBlock, Bonus — single-instance constraint on Start and End
- [ ] Left-click to place selected entity; right-click to remove

### 8d — Enemy placement & spawner/trigger wiring
- [ ] Place any enemy type from the enemy palette; configure radius, color, and behavior params in a side panel
- [ ] Patrol path editor: click to add waypoints sequentially; drag to reposition; per-waypoint speed field
- [ ] Wander polygon editor: click to add vertices; drag to reposition; close polygon with double-click
- [ ] Chaser / guard detection and give-up radii shown as overlay circles in the editor viewport
- [ ] Spawner panel: assign enemies to spawner slots, set condition (immediate / timed / trigger)
- [ ] Trigger panel: place button triggers, link actions (open/close door, spawn wave, fire trigger)

### 8e — Save, load, and play
- [ ] Save button writes JSON to `user://levels/`; load button opens a file list
- [ ] "Play" button from editor launches the level in `World` using `LevelLoader`; Escape returns to editor
- [ ] Level select screen (accessible from main menu) lists all saved levels

### 8f — Playlists
- [ ] `PlaylistData` JSON schema: `{ name, levels: [<level filename>, ...] }` stored in `user://playlists/`
- [ ] Playlist editor: create/rename a playlist, add levels from the saved-levels list, reorder with drag-and-drop, remove entries
- [ ] `RunState` tracks the active playlist and current index; level complete advances to the next entry
- [ ] Main menu shows playlists alongside individual levels; selecting a playlist starts from its first level
- [ ] Built-in playlists (shipped with the game) stored in `res://playlists/` and shown alongside user playlists

---

## Milestone 9 — Steam networking
- [ ] Steam Relay (Steamworks P2P) integration
- [ ] Public and private lobbies
- [ ] Host selects level set (built-in or Workshop playlist)
- [ ] Ready-up flow, host starts run
- [ ] Up to 8 players
- [ ] Disconnect does not end run — original lobby members can reconnect mid-run
- [ ] Publish individual levels and playlists to Steam Workshop
- [ ] Subscribe to and play Workshop levels and playlists

### Architecture notes

**Simulation model: host-authoritative using Godot's built-in multiplayer**
- Host runs all game logic (unit movement, enemy behaviors, ability effects, kill detection)
- Clients send right-click inputs to host via `@rpc`; host simulates and broadcasts state back via manual RPCs (`SyncUnitState` unreliable, death/respawn reliable)
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

## Deferred Items

- Upgrade UI feedback: flash or sound when a point is spent (deferred from 4a, low priority)
- Waypoint cleanup: when a unit reaches its waypoint on ground, the waypoint marker should be removed from the display
- Slidy surface steering: holding right-click should orbit the unit around the target point, not steer toward a point on the tangent of the unit's circle — needs tuning to feel correct
