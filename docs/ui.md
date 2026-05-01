# UI

## Screens
- Main Menu
- Level Select
- In-Game HUD
- Pause Menu
- Settings
- Lobby
- Level Transition / Loading Screen

## Level Transition Screen
Shown automatically between levels while the next level loads. No player input required — it auto-advances. Displays:
- Total team deaths this level
- Which players reached the end (beat the level)
Players can still spend skill points mid-level before the end is touched, not on this screen.

## HUD

### Ability Bar
Displayed at the bottom of the screen, one icon per ability (Boost, Gack, Warp, Donut, Ethereal). Each icon shows:
- Cooldown overlay (like WoW/League of Legends)
- A **+** button above the icon when the player has an unspent skill point and the ability is eligible to upgrade
- Advanced abilities (Warp, Donut, Ethereal) are grayed out / locked until 3 skill points have been earned

Upgrading an ability: click the **+** button, or press **Ctrl + ability key** (e.g. Ctrl+T for Boost). No panel, no pause — inline upgrade like League of Legends. Only affects that player; the game keeps running.

### Player Info
Each player (self and teammates) is displayed as:
`[Name] [spent] ([available])` — e.g. **Vampire 1 (3)** means Vampire has spent 1 skill point and has 3 available.

Also shown per player:
- Death count
- Resurrection count
- Alive / dead status

### Timer
Counts up from 0:00 infinitely during the level. Shown in the HUD.

## Settings

No control options for now. 
