# Milestones

Each milestone should be fully playable and testable before moving to the next.

---

## Milestone 6 — Multiplayer foundations (localhost)

**Testing:** Use Godot's "Debug → Run Multiple Instances" to launch host + client(s) on the same machine.

## Milestone 8 — Level editor

Levels are stored as a JSON + PNG pair (`user://levels/<name>.json` + `<name>.png`). The PNG bitmap defines the surface layout (each pixel = one tile cell, RGB = surface type). The JSON stores metadata, entities, enemies (each carrying their own spawn condition), triggers, and doors. Workshop upload (Milestone 9) reuses the same files.

### 8a — Level file format & runtime loader ✓
- [x] Full JSON + PNG schema defined in `docs/map.md`: PNG bitmap for surfaces, JSON for everything else
- [x] At load time, `LevelLoader` reads the PNG row by row, run-length encodes adjacent same-type pixels into rectangular `SurfaceZone` nodes (`Area2D` + `RectangleShape2D`)
- [x] Surface type detection uses `±1` per-channel tolerance in `SurfaceConstants.FromColor` to handle float truncation when saving painted pixels
- [x] Surface type at runtime determined by a point query at the unit's center (not the unit's collision circle)
- [x] `LevelLoader` spawns entities and all enemies whose `spawn` field is null or `"immediate"`; timed and trigger-based spawn conditions are deferred to when those systems are built
- [x] `World` accepts an optional level path via `GameSetup.PlaytestPath`; falls back to `res://levels/test.json` if none provided

### 8b — Editor scene & pixel painter ✓
- [x] Separate `Editor` scene accessible from main menu ("Edit Levels")
- [x] `CanvasView` control: pan (middle mouse drag), zoom (scroll wheel), pixel grid overlay at high zoom
- [x] Left-click / drag to paint pixels with the selected surface type; circle brush of configurable radius
- [x] `[` / `]` keys and `−` / `+` buttons to adjust brush radius
- [x] Palette panel listing all surface types with color swatches and number-key shortcuts
- [x] Fixed 260px right-side options panel: brush controls in Paint mode; entity/enemy properties in other modes
- [x] Editor split into partial classes: `Editor.cs`, `Editor.Layout.cs`, `Editor.Modes.cs`, `Editor.Paint.cs`, `Editor.File.cs`, `Editor.Overlays.cs`, `Editor.Entities.cs`
- [x] Entity overlays rendered on the canvas (start diamond, end diamond, bonus circles, enemy circles) via `EditorOverlay` record

### 8c — Entity placement ✓
- [x] Left-click to place StartBlock, EndBlock, Bonus; right-click to select; Delete key or panel button to remove
- [x] Single-instance constraint on Start and End (placing a second replaces the existing one)
- [x] Placed entities persisted to `_levelData.Entities` and saved to JSON
- [x] `EntityData` and `EnemyData` each get a GUID `Id` (auto-generated at placement) for stable trigger linking
- [x] Optional `Name` field on entities and enemies; displayed as "Kind - Name" in overlays and options panel
- [x] Options panel shows selected entity's kind, tile position, editable name field, and Delete button
- [x] Disambiguation popup when right-clicking near multiple overlapping entities/enemies — lists candidates by name or tile position; click one to select

### 8d — Enemy placement ✓
- [x] Place Patrol, Wander, and Orbiter enemies from the Enemies mode palette
- [x] Configure radius, color, and name per enemy; editable tile position
- [x] Patrol: waypoint list with per-waypoint speed, reorder (↑/↓), position edit (X/Y fields or ✏ pick-on-canvas), add/delete waypoints; end behavior (Loop / Reverse / Disappear); bulk "set all speeds" field
- [x] Wander: click-to-place polygon vertices; Edit Polygon to revise; vertex list with per-vertex position edit and delete (3-vertex minimum); speed, idle min/max, seed, optional start position
- [x] Orbiter: pick center via canvas click; orbit radius, angular speed, direction (CW/CCW), start angle; spoke + diamond overlay shows start angle live
- [x] Right-click from any mode tab selects the nearest entity or enemy and auto-switches to the correct tab
- [ ] Chaser / Guard detection and give-up radii shown as overlay circles (blocked on Milestone 7b/7e)

### 8e — Save, load, and play ✓
- [x] Save button writes the PNG bitmap to disk (JSON save is a TODO — currently only PNG is saved)
- [x] Open button opens a file browser; loaded level auto-reloads on return from playtest
- [x] Play button auto-saves the PNG, loads the level in `World` via `GameSetup.PlaytestPath`; Escape returns to the editor with the same level still loaded
- [x] JSON save on Save button
- [ ] Level select screen (accessible from main menu) lists all saved levels

### 8f — Spawn conditions

Each enemy has an optional `Spawn` field. When null or `"immediate"`, the enemy is present from level start (current behavior). This milestone adds timed and trigger-based spawning.

**Runtime (gameplay):**
- [ ] `TimedSpawnData` — enemy is hidden at level start; a timer begins when the level loads; enemy spawns after `Delay` seconds
- [ ] `TriggerSpawnData` — enemy spawns when the named trigger fires (wired from a button or game event)
- [ ] Spawned enemies appear with a brief flash effect (same visual as the RandomWander telegraph)

**Editor:**
- [ ] Spawn condition selector per enemy in the options panel: Immediate (default) / Timed / On Trigger
- [ ] Timed: float delay field (seconds until spawn)
- [ ] On Trigger: dropdown of all trigger IDs in the level (requires Milestone 8g)
- [ ] Non-immediate enemies shown with a distinct overlay style (e.g. dashed ring) so designers can see at a glance which enemies are deferred

---

### 8g — Trigger system

Buttons and doors allow level designers to create interactive sequences: timed gates, one-shot traps, and enemy waves gated behind player actions.

**Data structures:**
- [ ] `ButtonData` — position (tile), activation mode (touch / proximity radius), one-shot flag, list of `ActionData`
- [ ] `DoorData` — position (tile), width, height, initial state (open/closed), closed surface type (defaults to Kill)
- [ ] `ActionData` — union type: OpenDoor / CloseDoor / ToggleDoor (target door ID) | SpawnEnemy (target enemy ID, overrides spawn condition) | FireTrigger (target trigger ID, for chaining)

**Runtime (gameplay):**
- [ ] `Button` node — `Area2D`; activates when any player overlaps (touch mode) or enters radius (proximity mode); fires all actions; respects one-shot flag
- [ ] `Door` node — `Area2D` + `CollisionShape2D`; when closed, acts as a Kill surface; animates open/close with a short slide; state changes are host-authoritative and broadcast to clients
- [ ] `LevelLoader` spawns buttons and doors from level JSON; wires action targets by ID after all nodes are created

**Editor:**
- [ ] Button placement in Triggers mode (slot 1): left-click to place; options panel shows activation mode, one-shot toggle, and action list
- [ ] Door placement in Triggers mode (slot 2): left-click to place, then drag to set width and height; options panel shows size fields, surface type, and initial state
- [ ] Action list editor: add/remove actions; each entry has a type dropdown and a target picker (ID dropdown populated from current level's doors and enemies)
- [ ] Viewport overlay: dashed lines connect each button to its target doors/enemies; color-coded by action type (open = green, close = red, spawn = orange)
- [ ] Selecting a button highlights its connected targets on the canvas

---

### 8h — Playlists
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
