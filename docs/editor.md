# Slide Level Editor

## Layout

```
┌──────────────────────────────────────────────────────────────────┐
│  [New] [Open] [Save] [Play]  │  Paint │ Entities │ Enemies │ Triggers  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                         VIEWPORT                                 │
│                      (map canvas)                                │
│                                                    ┌───────────┐ │
│                                                    │ Properties│ │
│                                                    │   Panel   │ │
│                                                    │ (appears  │ │
│                                                    │ on select)│ │
│                                                    └───────────┘ │
├──────────────────────────────────────────────────────────────────┤
│   [1] [2] [3] [4] [5] [6] [7] [8]   ←  hotbar, changes per mode │
└──────────────────────────────────────────────────────────────────┘
```

---

## Top Bar

The top bar has two sections:

**Left — global actions:**

| Button | Shortcut | Action |
|--------|----------|--------|
| New    | Ctrl+N   | Create a blank level (prompts to save if unsaved changes exist) |
| Open   | Ctrl+O   | Open the level file browser |
| Save   | Ctrl+S   | Save current level to `user://levels/` |
| Play   | F5       | Launch the current level in-game; Escape returns to the editor |

**Right — mode tabs:**

| Tab | Shortcut | Purpose |
|-----|----------|---------|
| Paint    | Tab (cycle) or `M` | Paint surface types onto corner vertices |
| Entities | Tab (cycle) or `M` | Place start block, end block, and bonuses |
| Enemies  | Tab (cycle) or `M` | Place and configure enemies |
| Triggers | Tab (cycle) or `M` | Place buttons and doors; wire actions |

The active mode tab is highlighted. Switching mode clears any in-progress placement (e.g. a patrol path being drawn).

---

## Bottom Hotbar

The hotbar shows the items available in the current mode. Press the number key shown to select that item. The selected slot is highlighted (bright border). Scroll the mouse wheel over the hotbar to cycle through slots.

### Paint mode

| Key | Surface | Color hint |
|-----|---------|------------|
| `1` | Ground | Muted green |
| `2` | Slidy | Blue |
| `3` | Fast | Cyan |
| `4` | Confusing | Purple |
| `5` | Fast Confusing | Magenta |
| `6` | Straight | Orange |
| `7` | Kill | Red |
| `8` | Void | Dark / transparent |

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
| `4` | Chaser |
| `5` | Bouncer |
| `6` | Sniper |
| `7` | Guard |

### Triggers mode

| Key | Object |
|-----|--------|
| `1` | Button |
| `2` | Door |

---

## Viewport

### Camera controls

| Input | Action |
|-------|--------|
| Middle mouse drag | Pan |
| Scroll wheel | Zoom in / out |
| `Home` | Fit entire level in view |

### Grid

The grid displays tile boundaries and corner vertex points. The corner points (intersections) are what get painted in Paint mode — they are larger hit targets than the grid lines themselves.

Grid lines and corner markers scale with zoom so they remain usable at any level of detail.

---

## Paint mode

Painting modifies **corner vertices** of the tile grid (see `map.md` for why this produces smooth diagonal boundaries).

| Input | Action |
|-------|--------|
| Left-click | Paint hovered corner with selected surface type |
| Left-click + drag | Paint continuously while dragging |
| Right-click | Set hovered corner to Void (erase) |

**Hover highlight:** The nearest corner vertex to the cursor is highlighted with the color of the selected surface type before you click, giving immediate feedback.

**Live preview:** The viewport renders the marching-squares polygon outline in real time as you paint. You see the exact physics boundary that will be used in-game — including smooth diagonals — without needing to bake or export first.

---

## Entities mode

Entities snap to the tile grid (center of the nearest tile cell) by default. Hold `Alt` to place at free-float world position.

| Input | Action |
|-------|--------|
| Left-click | Place selected entity at cursor |
| Right-click entity | Delete entity |
| Left-click + drag entity | Reposition |

**Constraints:**
- Only one Start Block and one End Block are allowed per level. Placing a second replaces the existing one.
- Bonuses are unlimited.

**Indicators:** Start Block shows a green arrow pointing right. End Block shows the gold star pattern used in-game. Bonuses show the star placeholder. All match their in-game visuals exactly.

---

## Enemies mode

### Placing an enemy

Left-click anywhere in the viewport to drop an enemy of the selected behavior type. A properties panel opens on the right immediately. The enemy is not fully configured until its required behavior fields are filled.

Right-click an existing enemy to select it and reopen its properties panel. Delete key removes the selected enemy.

### Properties panel — common fields

| Field | Notes |
|-------|-------|
| Radius | Visual and collision radius in world units |
| Color | Color picker — sets the enemy's display color |
| Spawn condition | Immediate, Timed (delay in seconds), or On Trigger (pick a trigger ID from dropdown) |

### Properties panel — behavior-specific fields

**Patrol**
- Waypoint list with per-waypoint speed fields.
- Click **Add Waypoint** or click directly in the viewport while the patrol editor is active to append a waypoint at that position. Drag waypoints to reposition. Numbers show the order.
- End behavior: Loop or Disappear.

**Wander**
- Click in viewport to add polygon vertices. Double-click or click the first vertex to close the polygon.
- Speed, min/max idle, seed fields in the panel.
- Seed auto-assigned from a counter; can be manually overridden.
- Start position: optional override; click **Pick** then click in the viewport.

**Orbiter**
- Click **Pick Center** then click in the viewport to set the orbit center.
- Radius, angular speed, clockwise toggle, start angle fields.
- The orbit circle is always visible as an overlay in the viewport.

**Chaser**
- Click in viewport to set idle position, or enter coordinates manually.
- Detection radius and give-up radius shown as two concentric circles on the enemy; drag the circle handles to resize.
- Speed and give-up delay fields.

**Bouncer**
- Click in viewport to set start position.
- Drag the bounding rectangle handles to set bounce area.
- Direction angle dial or numeric field. Initial direction shown as an arrow on the enemy.
- Speed field.

**Sniper**
- Click in viewport to set position, or enter manually.
- Aim duration and cooldown fields.
- A sample aim ray is shown rotating slowly in the editor viewport.

**Guard**
- Combines patrol waypoint editor (same as Patrol above) with detection/give-up radius circles (same as Chaser above).
- Chase speed field (separate from patrol speed).

---

## Triggers mode

### Button

Left-click to place a button in the viewport. Buttons snap to tile grid by default; hold `Alt` for free placement.

**Properties panel:**
- **One-shot:** Toggle — if on, the button deactivates after the first activation.
- **Actions list:** Add/remove actions. Each action entry has a type dropdown and type-specific fields:
  - Open Door / Close Door / Toggle Door → Door ID picker (dropdown of all doors in the level)
  - Spawn Wave → Spawner index picker
  - Despawn Enemies → Multi-select enemy list
  - Fire Trigger → Trigger ID picker

### Door

Left-click to place, then drag to set width and height (or enter in properties panel). Doors snap to tile grid.

**Properties panel:**
- Width and height fields (world units).
- Closed surface type (defaults to Kill).
- Initial state: Open or Closed.

**Viewport overlays:**
- Closed doors are rendered with their surface color and a lock icon.
- Open doors show an outline only.
- Drag corner handles to resize.

### Wiring view

With the Triggers mode active, dashed lines connect each button to the doors and spawners it controls, giving a visual map of the level's event graph. Lines are color-coded by action type.

---

## Properties panel

The properties panel slides in from the right edge when an enemy, entity, or trigger is selected. It closes when you click empty space in the viewport or press `Escape`.

Multiple enemies/triggers can be selected with `Shift+click`. The properties panel shows only the fields common to all selected items (batch editing). Behavior-specific fields are hidden for mixed-type selections.

---

## Level metadata

Accessible via **File → Level Settings** (or a gear icon in the top bar):

| Field | Notes |
|-------|-------|
| Name | Display name shown in level select |
| Author | Auto-filled from editor profile; editable |
| Description | Optional. Shown in level select tooltip |
| Tile size | Default 64. Changing after painting rescales the world coordinate positions of all entities and enemies proportionally |

---

## Unsaved changes

A dot appears next to the level name in the top bar when there are unsaved changes. Attempting to close, create a new level, or launch Play without saving prompts a confirmation dialog.

---

## Planned / future

- Undo / redo (Ctrl+Z / Ctrl+Y)
- Multi-tile brush size for Paint mode
- Copy / paste selection of entities and enemies
- In-editor playtesting with a ghost unit (no HUD, no death, just movement preview)
- Steam Workshop publish button
