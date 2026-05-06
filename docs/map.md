# Slide Map Format

## Overview

Maps are stored as JSON files. The design separates **authoring** (tile grid + entities) from **runtime** (polygon physics):

- The tile grid uses a **vertex-based (corner-point) system** rather than storing one surface type per cell. Corner values sit at grid intersection points, shared by up to four adjacent cells. When two neighboring corners differ, the boundary between them runs diagonally through the shared cell — producing smooth, true-diagonal transitions rather than staircases.
- At load time, `LevelLoader` traces the boundaries between same-type regions using a marching-squares algorithm and produces `Area2D` nodes with polygon collision shapes. **The tile grid does not exist at runtime** — only the resulting physics polygons do. This means a Kill/Ground diagonal boundary is a real diagonal line in the collision geometry; the player dies exactly where the visual edge appears.
- Entity and enemy positions are **world coordinates** (float, in units), with `(0, 0)` at the top-left corner of the map's corner grid.

**File locations:**

| Path | Purpose |
|------|---------|
| `res://levels/<name>.json` | Built-in levels shipped with the game |
| `user://levels/<name>.json` | User-created levels from the editor |
| `res://playlists/<name>.json` | Built-in playlists |
| `user://playlists/<name>.json` | User-created playlists |

---

## Top-Level Schema

```json
{
  "version": 1,
  "name": "My Level",
  "author": "slider_fan",
  "description": "Shown in the level select screen.",
  "tileSize": 64,
  "width": 30,
  "height": 20,
  "corners":  [...],
  "entities":  [...],
  "enemies":   [...],
  "spawners":  [...],
  "triggers":  [...],
  "doors":     [...]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `version` | int | Schema version. Currently `1`. |
| `name` | string | Display name shown in the level select screen. |
| `author` | string | Creator's display name. |
| `description` | string? | Optional. Shown in level select. |
| `tileSize` | int | Size of each tile in world units. Recommended: `64`. |
| `width` | int | Number of tile columns. |
| `height` | int | Number of tile rows. |
| `corners` | array | `(height + 1)` rows × `(width + 1)` columns of surface codes. |
| `entities` | array | Start block, end block, bonuses. |
| `enemies` | array | Enemy definitions (behavior, radius, color). Not placed until a spawner fires. |
| `spawners` | array | Conditions that bring enemies into the scene. |
| `triggers` | array | Interactive objects that fire a list of actions when activated. |
| `doors` | array | Surface zones toggled open/closed by trigger actions. |

---

## Surface Types

Surface types are encoded as short strings everywhere in the file.

| Code | Surface | Physics behavior |
|------|---------|-----------------|
| `"g"` | Ground | Normal steering; player moves directly toward click target. |
| `"s"` | Slidy | Fixed speed, slow turn rate, never stops. |
| `"f"` | Fast | Slidy at 2× speed. |
| `"c"` | Confusing | Slidy but steers *away* from click target. |
| `"fc"` | FastConfusing | Confusing at 2× speed. |
| `"st"` | Straight | Input ignored; player continues in current direction. |
| `"k"` | Kill | Instant death. |
| `"v"` | Void | No physics zone created. Treated as instant death if reached. Use outside the playable border. |

---

## corners

`corners` is a 2D array with **`height + 1` rows** and **`width + 1` columns**. Corner `[row][col]` maps to world position `(col × tileSize, row × tileSize)`.

```json
"corners": [
  ["k","k","k","k","k","k"],
  ["k","g","g","s","s","k"],
  ["k","g","s","s","k","k"],
  ["k","k","k","k","k","k"]
]
```

This encodes a **5 × 3 tile map** (6 × 4 corner grid). The playable area is the inner Ground/Slidy region; Kill corners form the border.

### How corners become physics polygons

For each tile cell bounded by corners `[row][col]`, `[row][col+1]`, `[row+1][col]`, `[row+1][col+1]`:

1. Each cell is divided into four quadrants by its diagonals.
2. Each quadrant takes the surface type of its nearest corner (**nearest-corner rule**).
3. Boundary edges are emitted wherever adjacent quadrants differ.
4. `LevelLoader` collects all edges and traces closed polygons per surface type.
5. One `SurfaceZone` (Area2D + polygon collision shape) is created per contiguous region.

**Result:** A diagonal corner boundary produces a 45° physics edge — not a staircase. At 64 units/tile, diagonal steps are ~45 units wide, well within the player radius (16 units), so transitions feel smooth and exact.

### Practical rules

- Surround the playable area with `"k"` (Kill) corners for a solid outer border.
- Use `"v"` (Void) beyond the Kill border if the map does not fill the full grid — void regions generate no collision and keep the JSON readable.
- The corners array always includes the full `(height + 1) × (width + 1)` grid, even if most values are `"v"`.

---

## entities

```json
"entities": [
  { "kind": "start", "x": 96.0,  "y": 96.0  },
  { "kind": "end",   "x": 800.0, "y": 600.0 },
  { "kind": "bonus", "x": 300.0, "y": 200.0 },
  { "kind": "bonus", "x": 500.0, "y": 350.0 }
]
```

| Field | Type | Notes |
|-------|------|-------|
| `kind` | string | `"start"`, `"end"`, or `"bonus"`. |
| `x`, `y` | float | World position in units. |

- Exactly **one** `"start"` and **one** `"end"` are required per level.
- Any number of `"bonus"` entities are allowed (each grants +1 skill point to the first player who touches it).

---

## enemies

Each entry defines one enemy's appearance and behavior. Enemies are **not placed in the scene** at level load — they wait until a spawner activates them.

```json
"enemies": [
  {
    "radius": 28.0,
    "color": "#cc3311",
    "behavior": { ... }
  }
]
```

| Field | Type | Notes |
|-------|------|-------|
| `radius` | float | Collision and visual radius in world units. |
| `color` | string | Hex `"#rrggbb"`. |
| `behavior` | object | One of the behavior objects below. |

### patrol

Moves through an ordered list of waypoints. Loops back to the start or disappears at the end.

```json
{
  "type": "patrol",
  "waypoints": [
    { "x": 200.0, "y": 150.0, "speed": 250.0 },
    { "x": 600.0, "y": 150.0, "speed": 250.0 },
    { "x": 600.0, "y": 450.0, "speed": 180.0 }
  ],
  "endBehavior": "loop"
}
```

| Field | Type | Notes |
|-------|------|-------|
| `waypoints` | array | `{ x, y, speed }`. Enemy starts at `waypoints[0]`. |
| `waypoints[i].speed` | float | Travel speed for the leg ending at this waypoint. |
| `endBehavior` | string | `"loop"` or `"disappear"`. |

### wander

Idles in a polygon area. Telegraphs with a ring flash before each move. All movement is seeded for deterministic multiplayer sync.

```json
{
  "type": "wander",
  "polygon": [
    { "x": 100.0, "y": 100.0 },
    { "x": 500.0, "y": 100.0 },
    { "x": 500.0, "y": 400.0 },
    { "x": 100.0, "y": 400.0 }
  ],
  "speed": 200.0,
  "minIdle": 1.0,
  "maxIdle": 4.0,
  "seed": 1001,
  "startPosition": { "x": 300.0, "y": 250.0 }
}
```

| Field | Type | Notes |
|-------|------|-------|
| `polygon` | array | Convex polygon vertices defining the wander area (world coords). |
| `speed` | float | Movement speed. |
| `minIdle` / `maxIdle` | float | Idle duration range in seconds. |
| `seed` | int | Per-instance RNG seed. Must be unique across all wander enemies in a level. |
| `startPosition` | object? | Optional `{ x, y }`. Omit to start at a random polygon point. |

### orbiter *(Milestone 7a)*

Circles a fixed center point at constant angular speed.

```json
{
  "type": "orbiter",
  "centerX": 400.0,
  "centerY": 300.0,
  "radius": 150.0,
  "angularSpeed": 1.2,
  "clockwise": true,
  "startAngle": 0.0
}
```

| Field | Type | Notes |
|-------|------|-------|
| `centerX`, `centerY` | float | World position of the orbit center. |
| `radius` | float | Orbit radius in world units. |
| `angularSpeed` | float | Radians per second. |
| `clockwise` | bool | Rotation direction. |
| `startAngle` | float | Starting angle in radians (0 = right / east). |

### chaser *(Milestone 7b)*

Idles at a fixed position until a player enters the detection radius, then pursues. Telegraphs with a ring flash before moving.

```json
{
  "type": "chaser",
  "startX": 300.0,
  "startY": 200.0,
  "speed": 220.0,
  "detectionRadius": 300.0,
  "giveUpRadius": 500.0,
  "giveUpDelay": 3.0
}
```

| Field | Type | Notes |
|-------|------|-------|
| `startX`, `startY` | float | Idle position. |
| `speed` | float | Chase speed. |
| `detectionRadius` | float | Aggro range. |
| `giveUpRadius` | float | Pursuit ends when all players remain beyond this for `giveUpDelay` seconds. |
| `giveUpDelay` | float | Seconds without a target before returning to idle. |

### bouncer *(Milestone 7c)*

Moves in a straight line and bounces off a bounding rectangle. Fully deterministic.

```json
{
  "type": "bouncer",
  "startX": 200.0,
  "startY": 150.0,
  "directionAngle": 0.785,
  "speed": 300.0,
  "bounds": { "x": 64.0, "y": 64.0, "width": 768.0, "height": 512.0 }
}
```

| Field | Type | Notes |
|-------|------|-------|
| `startX`, `startY` | float | Starting position. |
| `directionAngle` | float | Initial direction in radians (0 = right). |
| `speed` | float | Constant speed. |
| `bounds` | object | `{ x, y, width, height }` bounding rectangle in world units. Enemy reflects on contact. |

### sniper *(Milestone 7d)*

Stationary. Aims a visible warning ray at the nearest player for `aimDuration` seconds, then fires an instant-kill beam. Repeats on cooldown.

```json
{
  "type": "sniper",
  "x": 450.0,
  "y": 300.0,
  "aimDuration": 1.0,
  "cooldown": 3.0
}
```

| Field | Type | Notes |
|-------|------|-------|
| `x`, `y` | float | Fixed world position. |
| `aimDuration` | float | Warning ray duration in seconds before firing. |
| `cooldown` | float | Seconds between shot cycles (measured from end of beam). |

### guard *(Milestone 7e)*

Patrols a waypoint route but switches to full chase when a player enters the detection radius. Returns to patrol from the nearest waypoint after losing the player.

```json
{
  "type": "guard",
  "patrol": {
    "waypoints": [
      { "x": 200.0, "y": 200.0, "speed": 180.0 },
      { "x": 500.0, "y": 200.0, "speed": 180.0 }
    ],
    "endBehavior": "loop"
  },
  "detectionRadius": 250.0,
  "giveUpRadius": 420.0,
  "chaseSpeed": 300.0,
  "giveUpDelay": 4.0
}
```

| Field | Type | Notes |
|-------|------|-------|
| `patrol` | object | Same schema as the `patrol` behavior — waypoints + endBehavior. |
| `detectionRadius` | float | Aggro range. |
| `giveUpRadius` | float | Returns to patrol when all players stay beyond this for `giveUpDelay` seconds. |
| `chaseSpeed` | float | Speed during pursuit (typically higher than patrol speed). |
| `giveUpDelay` | float | Seconds without a target before resuming patrol. |

---

## spawners

Spawners control when enemies enter the scene. Each spawner lists enemy indices (into the `enemies` array) and a condition. Multiple spawners can reference the same enemy; the enemy spawns on the **first** activation.

```json
"spawners": [
  {
    "enemyIndices": [0, 1],
    "condition": "immediate"
  },
  {
    "enemyIndices": [2],
    "condition": { "type": "timed", "delay": 30.0 }
  },
  {
    "enemyIndices": [3, 4],
    "condition": { "type": "trigger", "triggerId": "btn_gate" }
  }
]
```

### Condition types

| Condition | Schema | Effect |
|-----------|--------|--------|
| Immediate | `"immediate"` | Spawns when the level loads. |
| Timed | `{ "type": "timed", "delay": N }` | Spawns `N` seconds after level start. |
| Trigger | `{ "type": "trigger", "triggerId": "..." }` | Spawns when the named trigger fires. |

---

## triggers

Triggers are interactive world objects. When activated, they fire an ordered list of actions.

```json
"triggers": [
  {
    "id": "btn_gate",
    "kind": "button",
    "x": 250.0,
    "y": 300.0,
    "oneShot": true,
    "actions": [
      { "type": "openDoor",   "doorId": "gate_0" },
      { "type": "spawnWave",  "spawnerIndex": 2 }
    ]
  }
]
```

| Field | Type | Notes |
|-------|------|-------|
| `id` | string | Unique identifier referenced by spawner conditions and other actions. |
| `kind` | string | `"button"` — a physical object players run over. Future: `"zone"`, `"sequence"`. |
| `x`, `y` | float | World position (for `"button"` kind). |
| `oneShot` | bool | If `true`, the trigger deactivates after firing once. Default `true`. |
| `actions` | array | Ordered list of actions fired simultaneously on activation. |

### Action types

| `type` | Additional fields | Effect |
|--------|-------------------|--------|
| `openDoor` | `doorId` | Sets door to open state. |
| `closeDoor` | `doorId` | Sets door to closed state. |
| `toggleDoor` | `doorId` | Flips door between open and closed. |
| `spawnWave` | `spawnerIndex` | Immediately activates a spawner by index regardless of its own condition. |
| `despawnEnemies` | `enemyIndices` | Removes the listed enemies from the scene. |
| `fireTrigger` | `triggerId` | Fires another trigger's action list (for sequencing). |

---

## doors

Doors are rectangular surface zones that can be toggled between open and closed by trigger actions. When closed, they overlay their `closedSurface` type on top of the corner-grid physics (same priority system as `SurfaceZone`). When opened, they are removed from collision and the underlying corner-grid surface is revealed.

```json
"doors": [
  {
    "id": "gate_0",
    "x": 320.0,
    "y": 128.0,
    "width": 64.0,
    "height": 256.0,
    "closedSurface": "k",
    "initialState": "closed"
  }
]
```

| Field | Type | Notes |
|-------|------|-------|
| `id` | string | Unique identifier referenced by trigger actions. |
| `x`, `y` | float | World position of the door's top-left corner. |
| `width`, `height` | float | Size in world units. Should align to tile grid for clean visuals. |
| `closedSurface` | string | Surface type applied when closed. Usually `"k"`. |
| `initialState` | string | `"open"` or `"closed"` at level start. |

---

## Full example

A compact 8 × 6 tile level (512 × 384 world units at 64 units/tile) demonstrating all sections:

```json
{
  "version": 1,
  "name": "Gate Crossing",
  "author": "dev",
  "description": "Press the button to open the gate — and wake the guard.",
  "tileSize": 64,
  "width": 8,
  "height": 6,

  "corners": [
    ["k","k","k","k","k","k","k","k","k"],
    ["k","g","g","g","k","g","g","g","k"],
    ["k","g","g","g","k","g","s","s","k"],
    ["k","g","g","g","g","g","s","s","k"],
    ["k","g","g","g","k","g","g","g","k"],
    ["k","k","k","k","k","k","k","k","k"],
    ["k","k","k","k","k","k","k","k","k"]
  ],

  "entities": [
    { "kind": "start", "x":  96.0, "y": 192.0 },
    { "kind": "end",   "x": 480.0, "y": 192.0 },
    { "kind": "bonus", "x": 448.0, "y": 320.0 }
  ],

  "enemies": [
    {
      "radius": 24.0,
      "color": "#cc3311",
      "behavior": {
        "type": "patrol",
        "waypoints": [
          { "x": 128.0, "y": 128.0, "speed": 220.0 },
          { "x": 256.0, "y": 256.0, "speed": 220.0 }
        ],
        "endBehavior": "loop"
      }
    },
    {
      "radius": 20.0,
      "color": "#8822cc",
      "behavior": {
        "type": "guard",
        "patrol": {
          "waypoints": [
            { "x": 352.0, "y": 128.0, "speed": 160.0 },
            { "x": 480.0, "y": 128.0, "speed": 160.0 }
          ],
          "endBehavior": "loop"
        },
        "detectionRadius": 200.0,
        "giveUpRadius":    350.0,
        "chaseSpeed":      280.0,
        "giveUpDelay":     3.0
      }
    },
    {
      "radius": 18.0,
      "color": "#cc8800",
      "behavior": {
        "type": "wander",
        "polygon": [
          { "x": 320.0, "y":  64.0 },
          { "x": 512.0, "y":  64.0 },
          { "x": 512.0, "y": 320.0 },
          { "x": 320.0, "y": 320.0 }
        ],
        "speed": 180.0,
        "minIdle": 1.0,
        "maxIdle": 3.5,
        "seed": 42
      }
    }
  ],

  "spawners": [
    { "enemyIndices": [0],    "condition": "immediate" },
    { "enemyIndices": [1, 2], "condition": { "type": "trigger", "triggerId": "btn_gate" } }
  ],

  "triggers": [
    {
      "id": "btn_gate",
      "kind": "button",
      "x": 224.0,
      "y": 192.0,
      "oneShot": true,
      "actions": [
        { "type": "openDoor",  "doorId": "gate" },
        { "type": "spawnWave", "spawnerIndex": 1 }
      ]
    }
  ],

  "doors": [
    {
      "id": "gate",
      "x": 256.0,
      "y": 128.0,
      "width":  64.0,
      "height": 192.0,
      "closedSurface": "k",
      "initialState": "closed"
    }
  ]
}
```

---

## Playlist format

Playlists are separate JSON files listing level paths in play order. `RunState` tracks the active playlist and current index; level completion advances automatically.

```json
{
  "version": 1,
  "name": "Campaign 1",
  "author": "dev",
  "description": "The introductory campaign.",
  "levels": [
    "res://levels/tutorial.json",
    "res://levels/gate_crossing.json",
    "user://levels/my_custom_level.json"
  ]
}
```

| Field | Type | Notes |
|-------|------|-------|
| `version` | int | Schema version. Currently `1`. |
| `name` | string | Display name shown in the playlist select screen. |
| `author` | string | Creator's display name. |
| `description` | string? | Optional. |
| `levels` | array | Ordered list of level file paths. Both `res://` and `user://` paths are valid. |

When the playlist is exhausted, the run ends and a summary screen is shown.
