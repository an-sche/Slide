# Gameplay

The game is 2D top-down.

## Core Mechanic
Sliding around on different surfaces. The player navigates complex maps and tricky routes to reach the end. As the player beats levels they get additional abilities that help them beat the levels.

Movement is inspired by StarCraft-style click-to-move: right-clicking sets a destination and the unit moves directly toward it in a straight line. There is no pathfinding — if an obstacle or death surface is in the way, that is the player's fault.

## Controls
| Action | Input |
|--------|-------|
| Right Click | Set move target — unit moves in a straight line toward that point (no pathfinding) |
| Space (tap) | Re-center camera on own unit |
| Space (hold) | Lock camera to follow own unit continuously |
| Middle mouse drag | Pan camera |
| Cursor at screen edge | Pan camera (edge scrolling) |
| Q, E, R, T, F etc | Remappable Abilities (abilities.md) |

## Rules
<!-- What can the player do / not do? Win/loss conditions? -->
The player can only move their unit. They are restricted in their movements based on the types of survace they are on. Surface types are listed in surfaces.md.
The player wins the level by reaching the end location.
The player loses when they touch the wrong surface or get hit by an enemy.
The player can ressurect their friend by touching the dead players body. When a player dies, they will leave behind a body that the others can touch to ressurect. When ressurected this way, the ressurected player will respawn on the start square. 

## Progression
This is a run-based game. Players start every run at level 1 with 1 skill point each.

Skill points are earned by beating a level or collecting bonuses inside a level. Points carry forward across levels within a run — spending them upgrades abilities permanently for that run.

If all players are dead simultaneously, the run ends and everyone resets: back to level 1, 1 skill point, all abilities reset. Full wipe.

Abilities are listed in abilities.md.

## Game Modes
Single player and Multiplayer. The game is recommended for multiplayer.

### Single Player Death
When a player dies in single player, they respawn automatically at the start block after a short delay. The run continues — deaths are tracked and shown on the level transition screen. There is no game over in single player; only a full team wipe in multiplayer ends the run.
