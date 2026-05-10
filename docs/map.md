# Slide Map Format

## Overview

Maps are stored as JSON files paired with a PNG bitmap. The two files share the same base name and live in the same directory (e.g. `test.json` + `test.png`).

- The **PNG bitmap** defines the surface layout. Each pixel is one tile cell; its RGB color determines the surface type. The bitmap is authored directly in the level editor by painting pixels.
- The **JSON file** stores everything else: metadata, cell size, entity positions, enemy definitions, triggers, and doors.
- At load time, `LevelLoader` reads the PNG row by row, run-length encodes adjacent same-type pixels into rectangular `SurfaceZone` nodes (`Area2D` + `RectangleShape2D`), and places all entities from the JSON.
- Surface type detection at runtime uses a **point query** at the unit's center position — not the unit's collision circle. This means the unit's surface is determined by exactly where its center sits, not what its edges are touching.

**File locations:**

| Path | Purpose |
|------|---------|
| `res://levels/<name>.json` + `<name>.png` | Built-in levels shipped with the game |
| `user://levels/<name>.json` + `<name>.png` | User-created levels from the editor |
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
  "bitmap": "my_level.png",
  "cellSize": 4.0,
  "entities":  [...],
  "enemies":   [...],
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
| `bitmap` | string | Filename of the paired PNG (relative to the JSON file). |
| `cellSize` | float | World units per pixel. Default `4.0`. |
| `entities` | array | Start block, end block, bonuses. |
| `enemies` | array | Enemy definitions. Each enemy carries its own spawn condition. |
| `triggers` | array | Interactive objects that fire a list of actions when activated. |
| `doors` | array | Surface zones toggled open/closed by trigger actions. |

---

## Surface Types

Surface types are encoded as specific RGB colors in the PNG bitmap. The colors are defined in `SurfaceConstants.cs` and matched with ±1 tolerance per channel to account for floating-point rounding when the editor saves painted pixels.

| Surface | RGB | Hex | Behavior |
|---------|-----|-----|----------|
| Ground | (64, 166, 64) | `#40A640` | Normal steering; player moves directly toward click target. |
| Slidy | (140, 209, 255) | `#8CD1FF` | Fixed speed, slow turn rate, never stops. |
| Fast | (26, 64, 191) | `#1A40BF` | Slidy at 2× speed. |
| Confusing | (191, 140, 242) | `#BF8CF2` | Slidy but steers *away* from click target. |
| Fast Confusing | (97, 26, 148) | `#611A94` | Confusing at 2× speed. |
| Straight | (184, 184, 184) | `#B8B8B8` | Input ignored; player continues in current direction. |
| Kill | (191, 20, 20) | `#BF1414` | Instant death. |
| Void | any other color | — | No surface zone created. Pixels outside the playable area should be void. |

Any pixel whose RGB does not match one of the above (within ±1 per channel) is treated as void — no `SurfaceZone` is created for it.

---

## Bitmap conventions

- Surround the playable area with Kill pixels for a solid outer border.
- Leave non-playable areas as any non-matching color (the editor uses a near-black default for void).
- `cellSize` maps pixel coordinates to world coordinates: a pixel at `(px, py)` is at world position `(px × cellSize, py × cellSize)`.
- Entity and enemy positions in the JSON are in world coordinates.
- The bitmap is loaded using `Image.LoadFromFile` (reads raw bytes; no color-space conversion). `SurfaceConstants.FromColor` compares `Color.R8 / G8 / B8` (rounded 8-bit values) with ±1 tolerance.

---

## entities

```json
"entities": [
  { "kind": "start", "x": 1600.0, "y": 1600.0 },
  { "kind": "end",   "x": 2000.0, "y": 2000.0 },
  { "kind": "bonus", "x": 1800.0, "y": 2000.0 },
  { "kind": "bonus", "x": 3200.0, "y": 1500.0 }
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

Each entry defines one enemy's appearance, behavior, and when it enters the scene. Enemies with no `spawn` field (or `"immediate"`) are placed at level load. Timed and trigger-based enemies wait until their condition is met.

```json
"enemies": [
  {
    "id": "abc123",
    "radius": 28.0,
    "color": "#cc3311",
    "behavior": { ... }
  },
  {
    "id": "def456",
    "radius": 20.0,
    "color": "#cc3311",
    "behavior": { ... },
    "spawn": { "type": "timed", "delay": 15.0 }
  },
  {
    "id": "ghi789",
    "radius": 20.0,
    "color": "#cc3311",
    "behavior": { ... },
    "spawn": { "type": "trigger", "triggerId": "btn_gate" }
  }
]
```

| Field | Type | Notes |
|-------|------|-------|
| `id` | string | GUID, auto-assigned by the editor. Used by `despawnEnemies` trigger actions. |
| `radius` | float | Collision and visual radius in world units. |
| `color` | string | Hex `"#rrggbb"`. |
| `behavior` | object | One of the behavior objects below. |
| `spawn` | object? | Omit (or use `"immediate"`) to spawn at level load. See spawn conditions below. |

### Spawn conditions

| `type` | Additional fields | Effect |
|--------|-------------------|--------|
| *(omitted)* | — | Spawns at level load. This is the default for all editor-placed enemies. |
| `"immediate"` | — | Explicit immediate spawn; equivalent to omitting the field. |
| `"timed"` | `delay` (float, seconds) | Spawns after a delay from level start. |
| `"trigger"` | `triggerId` (string) | Spawns when the named trigger fires. No separate trigger action needed — the enemy listens for the trigger automatically. |

### patrol

Moves through an ordered list of waypoints. Loops back to the start or disappears at the end.

```json
{
  "type": "patrol",
  "waypoints": [
    { "x": 2500.0, "y": 1800.0, "speed": 250.0 },
    { "x": 3500.0, "y": 1800.0, "speed": 250.0 }
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
    { "x": 3680.0, "y": 1280.0 },
    { "x": 4720.0, "y": 1280.0 },
    { "x": 4720.0, "y": 2320.0 },
    { "x": 3680.0, "y": 2320.0 }
  ],
  "speed": 200.0,
  "minIdle": 1.0,
  "maxIdle": 4.0,
  "seed": 1001
}
```

| Field | Type | Notes |
|-------|------|-------|
| `polygon` | array | Convex polygon vertices defining the wander area (world coords). |
| `speed` | float | Movement speed. |
| `minIdle` / `maxIdle` | float | Idle duration range in seconds. |
| `seed` | int | Per-instance RNG seed. Must be unique across all wander enemies in a level. |
| `startX`, `startY` | float? | Optional. Omit to start at a random polygon point. |

### orbiter

Circles a fixed center point at constant angular speed.

```json
{
  "type": "orbiter",
  "centerX": 1800.0,
  "centerY": 3000.0,
  "radius": 350.0,
  "angularSpeed": 1.1,
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

Idles at a fixed position until a player enters the detection radius, then pursues.

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

### bouncer *(Milestone 7c)*

Moves in a straight line and bounces off a bounding rectangle.

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

### sniper *(Milestone 7d)*

Stationary. Aims a visible warning ray at the nearest player, then fires an instant-kill beam.

```json
{
  "type": "sniper",
  "x": 450.0,
  "y": 300.0,
  "aimDuration": 1.0,
  "cooldown": 3.0
}
```

### guard *(Milestone 7e)*

Patrols a waypoint route but switches to full chase when a player enters the detection radius.

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

---

## triggers

```json
"triggers": [
  {
    "id": "btn_gate",
    "kind": "button",
    "x": 250.0,
    "y": 300.0,
    "oneShot": true,
    "actions": [
      { "type": "openDoor",  "doorId": "gate_0" },
      { "type": "spawnWave", "spawnerIndex": 2 }
    ]
  }
]
```

### Action types

| `type` | Additional fields | Effect |
|--------|-------------------|--------|
| `openDoor` | `doorId` | Sets door to open state. |
| `closeDoor` | `doorId` | Sets door to closed state. |
| `toggleDoor` | `doorId` | Flips door between open and closed. |
| `despawnEnemies` | `enemyIds` (array of strings) | Removes the listed enemies from the scene by GUID. |
| `fireTrigger` | `triggerId` | Fires another trigger's action list. |

> **Trigger-based enemy spawning:** set `"spawn": { "type": "trigger", "triggerId": "..." }` on the enemy itself. No explicit spawn action on the trigger is needed — the enemy listens for the named trigger automatically.

---

## doors

Doors are rectangular surface zones toggled by trigger actions.

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

---

## Playlist format

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
