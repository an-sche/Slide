# Multiplayer

## Modes

## Networking Model
Steam Relay (Steamworks P2P). One player hosts, traffic routes through Steam's relay servers. No dedicated infrastructure required. One player acts as the listen server/host; all others connect through Steam. This handles NAT traversal and firewall issues automatically.

## Player Count
Up to 8 players

## Lobby & Matchmaking
Private and public lobbies. Lobby flow:
1. Host creates a lobby and selects the level set (built-in campaign or a Steam Workshop playlist)
2. Players join and ready up
3. Host starts the run when all players are ready

## Mid-Run Joining & Reconnection
Players who were in the original lobby can reconnect mid-run if they disconnect — a DC does not end the run. New players cannot join a run already in progress.

## Camera
Each player has an independent camera. Players can pan freely to observe teammates. Tapping Space re-centers on their own unit; holding Space locks the camera to follow it continuously.

## Sync Model
Player position
Map (enemies)
Player scores / level
Time
