# Levels

## Level Structure
<!-- How are levels organized? Worlds, chapters, procedural? -->
Levels are designed by me and players.

## Built-in Levels
<!-- How many ship with the game? Any progression order? -->
10 is a good start. beat the game by beating 10 levels. 

## Level Format
Levels are tile-based on a small grid (e.g. 32×32px tiles). The tile size is small enough that levels feel freeform — adjacent same-surface tiles render seamlessly with no visible grid lines. All placement snaps to the grid.

A level contains:
- **Surfaces** — tiles of any type defined in surfaces.md
- **Start block** — a region of normal ground where players spawn
- **End block** — a region of normal ground; touching it beats the level
- **Enemies** — follow predefined patrol routes or move randomly within a defined area; any size
- **Obstacles** — e.g. buttons that open doors, adding puzzle elements on top of the navigation challenge
- **Bonuses** — collectibles that grant a skill point; defined in abilities.md

## Workshop / Editor
The level editor is a core feature — it ships as a first-class mode accessible from the main menu (no separate launcher).

### Editor Tools
- **Tile painter** — select a surface type from a palette and paint tiles onto the map
- **Enemy placement** — place an enemy and click waypoints to define its patrol route
- **Trigger system** — place a button, then link it to a target (e.g. a door). The trigger → action relationship is designed to be extensible (future actions beyond open/close door can be added)
- **Object placement** — start block, end block, bonuses, obstacles

### Steam Workshop
Players publish levels directly to Steam Workshop from inside the editor. Any level type is allowed (surfaces, enemies, anything the editor supports).
