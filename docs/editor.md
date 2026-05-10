# Slide Level Editor

## Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [New] [Open] [Save] [Play]  │  Paint │ Entities │ Enemies │ Triggers   │
├──────────────────────────────────────────────────────────────┬──────────┤
│                                                              │ Options  │
│                         VIEWPORT                             │  Panel   │
│                      (map canvas)                            │ (220 px) │
│                                                              │          │
│                                                              │          │
├──────────────────────────────────────────────────────────────┴──────────┤
│         [1] [2] [3] [4] [5] [6] [7] [8]   ←  hotbar, changes per mode  │
└─────────────────────────────────────────────────────────────────────────┘
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
| Paint    | Tab (cycle) or `M` | Paint surface types onto map pixels |
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

A pixel grid appears at high zoom levels (zoom ≥ 8×) showing individual cell boundaries. Each cell in the editor corresponds to one pixel in the PNG bitmap and one `cellSize`-unit square in the game world.

Grid lines scale with zoom so they remain usable at any level of detail.

---

## Paint mode

Painting writes surface type colors directly into the PNG bitmap. Each pixel encodes one surface type as an RGB color (see `map.md` for the full color table). The PNG is the source of truth — no intermediate representation.

| Input | Action |
|-------|--------|
| Left-click | Paint pixel(s) under cursor with selected surface type |
| Left-click + drag | Paint continuously while dragging |
| `[` / `]` | Decrease / increase brush radius |

**Brush:** The brush is a filled circle. Radius 0 paints a single pixel. Higher radii paint all pixels within that radius. A white circle preview follows the cursor, showing exactly which pixels will be painted.

**Options panel (right sidebar):** In Paint mode shows the current brush size with `−` / `+` buttons as an alternative to the bracket shortcuts.

**What you see:** The viewport renders the PNG bitmap directly — the colors you paint are the colors that appear in-game as surface zones. No baking or export step is needed; Save writes the PNG and JSON together.

---

## Entities mode

| Input | Action |
|-------|--------|
| Left-click | Place selected entity at cursor (snaps to tile center) |
| Right-click | Select nearest entity within ~4 cells; clears selection if none nearby |
| Delete | Remove the selected entity |

**Constraints:**
- Only one Start Block and one End Block are allowed per level. Placing a second replaces the existing one.
- Bonuses are unlimited.

**Selection:** Right-clicking near multiple overlapping entities shows a disambiguation popup listing each candidate by name or tile position — click one to select it. The selected entity gets a yellow ring on the canvas.

**Options panel (right sidebar):** When an entity is selected, the panel shows its kind, tile position, an editable Name field, and a Delete button. Names display as "Kind - Name" (e.g. "Bonus - hidden gem") in both the panel and the canvas overlay label.

**Entity IDs:** Every entity is assigned a GUID at placement time. This ID is stable across renames and is how the trigger system will reference doors and targets.

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

## Options panel

The options panel is a fixed 220px right sidebar. Its content changes based on mode and selection:

| State | Panel shows |
|-------|-------------|
| Paint mode | Brush size (`−` / `+` buttons, or `[` / `]` keys) |
| Entities/Enemies/Triggers — nothing selected | "Right-click to select" hint |
| Entity selected | Kind label (e.g. "Bonus - hidden gem"), tile position, Name field, Delete button |
| Enemy selected | Behavior type label, tile position, Name field, Delete button (behavior config fields coming in Milestone 8d) |

Selection is cleared when switching modes or opening a different level.

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
- Copy / paste selection of entities and enemies
- In-editor playtesting with a ghost unit (no HUD, no death, just movement preview)
- Steam Workshop publish button
