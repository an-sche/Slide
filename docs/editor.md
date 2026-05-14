# Slide Level Editor

## Layout

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  [New] [Open] [Save] [Save As] [Settings] [Play] [Fit]  [Undo] [Redo]           │
│  │ Paint │ Entities │ Enemies │ Triggers │              (level name *)           │
├─────────────────────────────────────────────────────────────────────┬────────────┤
│                                                                     │  Options   │
│                          VIEWPORT                                   │   Panel    │
│                       (map canvas)                                  │  (260 px)  │
│                                                                     │            │
├─────────────────────────────────────────────────────────────────────┴────────────┤
│              [1] [2] [3] [4] [5] [6] [7] [8]   ←  hotbar, changes per mode      │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Top Bar

| Button   | Shortcut       | Action |
|----------|----------------|--------|
| New      | Ctrl+N         | Create a blank level (prompts if unsaved) |
| Open     | Ctrl+O         | Open the level file browser |
| Save     | Ctrl+S         | Save current level |
| Save As  | Ctrl+Shift+S   | Save to a new path |
| Settings | Ctrl+,         | Edit level metadata (name, author, description) |
| Play     | F5             | Launch level in-game; Escape returns to editor |
| Fit      | F              | Zoom/pan canvas to fit the whole level in view |
| Undo     | Ctrl+Z         | Undo last action |
| Redo     | Ctrl+Y         | Redo |

The active mode tab is highlighted. Switching mode clears any in-progress placement and selection.

**Right-click auto-switching:** Right-clicking an entity or enemy on the canvas from *any* mode selects it and automatically switches to the correct tab (Entities or Enemies). This means you can edit enemies from Paint mode without manually switching first.

---

## Bottom Hotbar

Press the number key shown on a slot to select it. In Entities / Enemies mode, the selected slot arms a placement — the next left-click drops that item.

### Paint mode

| Key | Surface |
|-----|---------|
| `1` | Ground |
| `2` | Slidy |
| `3` | Fast |
| `4` | Confusing |
| `5` | Fast Confusing |
| `6` | Straight |
| `7` | Kill |
| `8` | Void |

### Entities mode

| Key | Entity |
|-----|--------|
| `1` | Start Block |
| `2` | End Block |
| `3` | Bonus |

### Enemies mode

| Key | Behavior |
|-----|---------|
| `1` | Patrol |
| `2` | Wander |
| `3` | Orbiter |

---

## Viewport

### Camera controls

| Input | Action |
|-------|--------|
| Middle-mouse drag | Pan |
| Scroll wheel | Zoom in / out |
| `F` or Fit button | Fit entire level in view |

A pixel grid appears at zoom ≥ 8× showing individual cell boundaries.

### Overlays

All entities and enemies are drawn as colored overlays on the canvas regardless of which mode tab is active:

- **Diamond** — Start / End / Bonus entities; also the "start position" marker on Wander enemies
- **Circle** — Enemies (filled with the enemy's color)
- **Lines** — Patrol paths, Wander polygon edges, Orbiter orbit circle
- **Spoke + diamond** — Orbiter start angle marker (shows where on the circle the enemy begins)
- **Yellow ring** — Currently selected entity or enemy
- **Ghost line** — Dashed line from last-placed point to cursor during multi-click placement

---

## Paint mode

Left-click or drag to paint surface types onto the bitmap. Each pixel encodes one surface type as an RGB color (see `map.md`).

| Input | Action |
|-------|--------|
| Left-click / drag | Paint pixel(s) with selected surface |
| `[` / `]` | Decrease / increase brush radius |

The options panel shows the current brush size. Brush preview circle follows the cursor.

---

## Entities mode

| Input | Action |
|-------|--------|
| Left-click | Place selected entity (snaps to tile center) |
| Right-click | Select nearest entity or enemy within ~4 cells |
| Delete | Remove selected entity |

Only one Start and one End are allowed per level — placing a second replaces the existing one. Right-clicking near multiple overlapping items shows a disambiguation popup.

**Options panel when selected:** kind label, tile X/Y (editable), Name field, Delete button. Names appear as "Kind - Name" on the canvas overlay.

---

## Enemies mode

### Placing

Select a behavior type from the hotbar (1/2/3), then left-click the canvas to begin placement. Patrol and Wander require multiple canvas clicks to build their path/polygon; press Enter or click Done when finished. Orbiter is placed in one click.

Right-click an existing enemy to select it. Delete removes the selected enemy.

### Common panel fields

| Field | Notes |
|-------|-------|
| Radius | Collision and visual radius; `−` / `+` buttons |
| Color | Color picker; changes take effect immediately on the canvas |
| Name | Optional label shown on the canvas overlay |
| X / Y | Tile position of the enemy's origin; editable |

### Patrol

The enemy walks an ordered list of waypoints, each with its own travel speed.

**Waypoint list** — each row shows:
- Index number
- X / Y tile position (editable; live-updates the canvas)
- Speed (`s:` field)
- ↑ / ↓ reorder buttons
- ✏ pick-on-canvas button — click it, then click anywhere on the canvas to move that waypoint
- × delete button

**All speeds** — bulk field above the list; sets every waypoint to the same speed at once.

**Add Waypoint** button — appends waypoints by clicking the canvas; press Enter or Done when finished. The ghost line shows the path being built.

**End behavior:**
- **Loop** — return to waypoint 1 and repeat
- **Reverse** — ping-pong back and forth along the path
- **Disappear** — enemy is removed after reaching the last waypoint

### Wander

The enemy picks random destinations inside a polygon and idles between moves.

**Polygon** — drawn by clicking vertices on the canvas (3 minimum). Use Edit Polygon to modify an existing one; click near the first vertex or press Enter to close.

**Vertex list** — each row shows:
- Index number
- X / Y tile position (editable; live-updates the canvas)
- ✏ pick-on-canvas button
- × delete button (disabled when 3 or fewer vertices remain)

**Other fields:** Speed, Idle min/max (seconds), Seed (randomises movement; Rand button regenerates).

**Start Position** — optional; if set, the enemy starts here instead of a random polygon point. Set button picks via canvas click; × clears it.

### Orbiter

The enemy orbits a center point at a fixed radius.

**Pick Center** — click canvas to set the orbit center (shown as the labeled circle overlay).

**Other fields:** Orbit Radius (`−` / `+`), Speed (rad/s), Direction (CW / CCW), Start Angle (degrees).

The orbit circle, spoke, and start-angle diamond are always visible on the canvas. The spoke and diamond update in real time as you type the start angle.

---

## Options Panel

Fixed 260 px right sidebar. Content changes based on mode and selection:

| State | Shows |
|-------|-------|
| Paint mode | Brush size controls |
| Entity/Enemy/Trigger mode — nothing selected | "Right-click to select" hint |
| Entity selected | Kind, position, name, Delete |
| Enemy selected | Kind, position, name, Delete + behavior-specific config |

---

## Undo / Redo

Nearly every edit is undoable:

- Painting pixels
- Placing, moving, or deleting entities and enemies
- Waypoint and polygon edits (add, delete, reorder, move)
- Enemy property changes (radius, color, speed, end behavior, etc.)
- Name changes

Undo history is per-session and is cleared when a new level is opened.

---

## Unsaved Changes

A `*` appears next to the level name when there are unsaved changes. The following actions prompt a confirmation if the level is dirty:

- Escape key (exit to main menu)
- New (discard current level)
- Open (replace current level)
- Play (if auto-save on play is disabled)
