# Slide — In-Game Controls

## Movement

| Input | Action |
|-------|--------|
| Right-click | Set move target — the unit slides toward the clicked point |
| Right-click + hold | Continuously update move target while the button is held |

Movement is point-and-click. The unit accelerates toward the target and is affected by the surface type it is currently on (see `surfaces.md`).

---

## Abilities

| Key | Ability |
|-----|---------|
| `Q` | Boost |
| `W` | Warp |
| `E` | Donut |
| `R` | Ethereal |
| `F` | Gack |

Abilities trigger on key press. Each ability has its own cooldown shown on the ability bar. See `abilities.md` for full descriptions.

---

## Camera

| Input | Action |
|-------|--------|
| `C` | Toggle camera lock to your character on/off |
| `Space` (hold) | Snap camera to your character while held; releases without changing lock state |
| Middle mouse drag | Pan camera freely |
| Scroll wheel | Zoom in / out |
| Mouse at screen edge | Scroll camera in that direction |

**Camera lock** starts enabled — the camera follows your character automatically. Any manual camera movement (edge scroll, middle mouse pan) disables the lock. Press `C` to re-enable it; press `C` again to disable it without moving the camera.

---

## Meta

| Input | Context | Action |
|-------|---------|--------|
| `Escape` | Playtest (launched from editor) | Return to the editor |
