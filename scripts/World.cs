using Godot;
using System.Collections.Generic;

namespace Slide;

public partial class World : Node2D
{
    private GameCamera?      _camera;
    private Hud?             _hud;
    private LevelTransition? _transition;
    private Unit?            _localUnit;
    private bool             _levelCompleted;
    private Vector2          _startPosition;

    private const float WipeDelay = 5f;
    private SceneTreeTimer? _wipeTimer;

    // All spawned units (maintained on every peer)
    private readonly Dictionary<long, Unit>      _units      = new();
    // Warp ghosts tracked on clients for removal (keyed by peerId of the owning unit)
    private readonly Dictionary<long, WarpGhost> _warpGhosts = new();

    private static readonly Color[] PlayerColors =
    [
        new Color(0.20f, 0.80f, 1.00f), // cyan
        new Color(1.00f, 0.45f, 0.10f), // orange
        new Color(0.40f, 1.00f, 0.40f), // green
        new Color(1.00f, 1.00f, 0.20f), // yellow
        new Color(0.90f, 0.20f, 0.90f), // magenta
        new Color(0.20f, 0.60f, 1.00f), // blue
        new Color(1.00f, 0.20f, 0.40f), // red
        new Color(0.85f, 0.85f, 0.85f), // white
    ];

    public override void _Ready()
    {
        _transition = new LevelTransition();
        AddChild(_transition);

        Input.MouseMode = Input.MouseModeEnum.Confined;
        CreateTestLevel();

        if (!GameNetwork.IsMultiplayer)
        {
            SpawnUnit(1L, 0);
        }
        else
        {
            foreach (var p in GameSetup.Players)
                SpawnUnit(p.PeerId, p.Index);
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }

        _hud?.SetAllUnits(_units.Values);
    }

    private void SpawnUnit(long peerId, int playerIndex)
    {
        if (_units.ContainsKey(peerId)) return;

        Color color   = PlayerColors[playerIndex % PlayerColors.Length];
        bool  isLocal = !GameNetwork.IsMultiplayer || peerId == Multiplayer.GetUniqueId();

        var unit = new Unit
        {
            Name          = $"Unit_{peerId}",
            PlayerId      = playerIndex,
            PeerId        = peerId,
            UnitColor     = color,
            IsLocalPlayer = isLocal,
        };
        AddChild(unit);
        unit.SetStartPosition(_startPosition + new Vector2(playerIndex * 48f, 0));
        _units[peerId] = unit;

        unit.CorpseTouched += corpse =>
        {
            if (corpse.SourceUnit == null || !corpse.SourceUnit.IsDead) return;
            if (corpse.SourceUnit == unit) return; // can't self-resurrect
            corpse.SourceUnit.ResurrectEarly();
        };

        if (GameNetwork.IsMultiplayer)
        {
            unit.Died += () =>
            {
                if (!Multiplayer.IsServer()) return;
                Rpc(nameof(BroadcastUnitDeath), peerId, unit.GlobalPosition);
                CheckTeamWipe();
            };
            unit.Respawned += () =>
            {
                if (!Multiplayer.IsServer()) return;
                Rpc(nameof(BroadcastUnitRespawn), peerId, _startPosition + new Vector2(playerIndex * 48f, 0));
                CancelWipeTimer();
            };
        }

        if (!isLocal) return;

        _localUnit = unit;

        _camera = new GameCamera();
        AddChild(_camera);
        _camera.Initialize(unit);

        _hud = new Hud();
        AddChild(_hud);
        unit.Died      += _hud.OnUnitDied;
        unit.Respawned += _hud.OnUnitRespawned;
        _hud.SetUnit(unit);
    }

    private void OnPeerDisconnected(long peerId)
    {
        if (_units.TryGetValue(peerId, out var unit))
        {
            unit.QueueFree();
            _units.Remove(peerId);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GameNetwork.IsMultiplayer || !Multiplayer.IsServer() || _units.Count == 0) return;
        foreach (var (peerId, unit) in _units)
            Rpc(nameof(SyncUnitState), peerId, unit.GlobalPosition, unit.Facing, unit.HasTarget, unit.TargetPosition, unit.AbilitiesActiveMask);
    }

    public override void _Process(double delta)
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Right)) return;
        var target = GetGlobalMousePosition();
        // Always set locally so the waypoint appears immediately on this screen.
        _localUnit?.SetTarget(target);
        // Clients also forward to the host for authoritative movement.
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer())
            RpcId(1, nameof(SetMoveTarget), target);
    }

    // Client → host: activate an ability on the sender's unit.
    // level is sent so the host uses the client's actual upgraded level, not its own stale copy.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UseAbility(int slot, int level)
    {
        if (!Multiplayer.IsServer()) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (!_units.TryGetValue(sender, out var unit)) return;
        unit.PlayerState.AbilityLevels[slot] = level;
        unit.TryActivateAbility(slot);
    }

    // Client → host: set this client's unit move target.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetMoveTarget(Vector2 position)
    {
        if (!Multiplayer.IsServer()) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (_units.TryGetValue(sender, out var unit))
            unit.SetTarget(position);
    }

    // Host → all clients: push position, facing, target, and active ability states for one unit per call.
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncUnitState(long peerId, Vector2 position, Vector2 facing, bool hasTarget, Vector2 target, byte abilitiesActive)
    {
        if (!_units.TryGetValue(peerId, out var unit)) return;
        unit.GlobalPosition = position;
        unit.Facing         = facing;
        if (hasTarget) unit.SetTarget(target);
        else           unit.ClearTarget();
        unit.SetAbilitiesActive(abilitiesActive);
    }

    // Host → all clients: a unit just died.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void BroadcastUnitDeath(long peerId, Vector2 deathPosition)
    {
        if (!_units.TryGetValue(peerId, out var unit) || unit.IsDead) return;
        unit.ApplyRemoteDeath(deathPosition);
    }

    // Host → all clients: a unit just respawned.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void BroadcastUnitRespawn(long peerId, Vector2 spawnPosition)
    {
        if (!_units.TryGetValue(peerId, out var unit) || !unit.IsDead) return;
        unit.ApplyRemoteRespawn(spawnPosition);
    }

    // Host → all clients: spawn a goo zone.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnGooZone(Vector2 position)
    {
        AddChild(new GooZone { GlobalPosition = position });
    }

    // Host → all clients: a unit placed a warp ghost.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnWarpGhost(long peerId, Vector2 position, Vector2 facing, float duration)
    {
        if (_warpGhosts.TryGetValue(peerId, out var old)) { old.QueueFree(); _warpGhosts.Remove(peerId); }
        var ghost = new WarpGhost { GlobalPosition = position, Facing = facing, Duration = duration };
        AddChild(ghost);
        _warpGhosts[peerId] = ghost;
    }

    // Host → all clients: a warp ghost was consumed or expired.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientRemoveWarpGhost(long peerId)
    {
        if (_warpGhosts.TryGetValue(peerId, out var ghost)) { ghost.QueueFree(); _warpGhosts.Remove(peerId); }
    }

    // Host → all clients: spawn a donut projectile.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnDonut(Vector2 position, Vector2 velocity, float lifetime)
    {
        AddChild(new DonutProjectile { GlobalPosition = position, MoveVelocity = velocity, Lifetime = lifetime });
    }

    private void CheckTeamWipe()
    {
        if (_wipeTimer != null) return; // already counting
        foreach (var unit in _units.Values)
            if (!unit.IsDead) return; // someone is still alive

        _wipeTimer          = GetTree().CreateTimer(WipeDelay);
        _wipeTimer.Timeout += OnWipeTimerExpired;
    }

    private void CancelWipeTimer()
    {
        if (_wipeTimer == null) return;
        _wipeTimer.Timeout -= OnWipeTimerExpired;
        _wipeTimer          = null;
    }

    private void OnWipeTimerExpired()
    {
        _wipeTimer = null;
        // Double-check — a Donut or Ethereal may have resurrected someone during the delay.
        foreach (var unit in _units.Values)
            if (!unit.IsDead) return;

        Rpc(nameof(ResetRun));
        ResetRun();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ResetRun()
    {
        RunState.Reset(); // resets to PlayerLevel = 1, all ability levels 0
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }

    private void OnLevelCompleted(int finisherPlayerId)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        if (_levelCompleted) return;
        _levelCompleted = true;

        if (GameNetwork.IsMultiplayer)
            Rpc(nameof(BroadcastLevelComplete), finisherPlayerId, RunState.ElapsedSeconds, RunState.TotalDeaths);
        BroadcastLevelComplete(finisherPlayerId, RunState.ElapsedSeconds, RunState.TotalDeaths);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void BroadcastLevelComplete(int finisherPlayerId, float elapsed, int deaths)
    {
        RunState.GetPlayer(finisherPlayerId).PlayerLevel++;
        string finisherName = $"Slider {finisherPlayerId + 1}";
        _transition!.ShowTransition(finisherName, elapsed, deaths);
    }

    private void CreateTestLevel()
    {
        (SurfaceType type, int col, int row)[] grid =
        {
            (SurfaceType.Ground,        0, 0),
            (SurfaceType.Slidy,         1, 0),
            (SurfaceType.Fast,          2, 0),
            (SurfaceType.Confusing,     0, 1),
            (SurfaceType.FastConfusing, 1, 1),
            (SurfaceType.Straight,      2, 1),
        };

        var tileSize = new Vector2(1200, 1200);

        foreach (var (type, col, row) in grid)
        {
            var center = new Vector2(
                (col - 1) * tileSize.X,
                (row - 0.5f) * tileSize.Y);
            AddChild(new SurfaceZone { Type = type, Size = tileSize, Position = center });
        }

        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(4400, 400),  Position = new Vector2(0, -1600) });
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(4400, 400),  Position = new Vector2(0,  1600) });
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(400,  2400), Position = new Vector2(-2200, 0) });
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(400,  2400), Position = new Vector2( 2200, 0) });

        _startPosition = new Vector2(-1400, -800);
        AddChild(new StartBlock { Position = _startPosition });

        var endBlock = new EndBlock { Position = new Vector2(-1000, -400) };
        AddChild(endBlock);
        endBlock.LevelCompleted += OnLevelCompleted;

        AddChild(new Bonus { Position = new Vector2(-1200, -400) });
        AddChild(new Bonus { Position = new Vector2(  200, -900) });
        AddChild(new Bonus { Position = new Vector2( 1100, -400) });
        AddChild(new Bonus { Position = new Vector2(-1100,  400) });
        AddChild(new Bonus { Position = new Vector2(  100,  700) });

        AddChild(new Enemy
        {
            Position = new Vector2(-500, -600),
            Radius   = 32f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(-500, -600), 250f),
                new Waypoint(new Vector2( 500, -600), 250f),
            ], PatrolEndBehavior.Loop),
        });
        AddChild(new Enemy
        {
            Position = new Vector2(200, -800),
            Radius   = 24f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(200, -800), 350f),
                new Waypoint(new Vector2(200, -400), 350f),
            ], PatrolEndBehavior.Loop),
        });
        AddChild(new Enemy
        {
            Position = new Vector2(-200, -400),
            Radius   = 20f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(-200, -400), 180f),
                new Waypoint(new Vector2( 400, -400), 180f),
                new Waypoint(new Vector2( 400, -800), 180f),
                new Waypoint(new Vector2(-200, -800), 180f),
            ], PatrolEndBehavior.Loop),
        });

        Vector2[] fastTileArea =
        [
            new(680, -1120), new(1720, -1120),
            new(1720, -80),  new(680,  -80),
        ];

        AddChild(new Enemy { Radius = 36f, Color = new Color(0.85f, 0.3f, 0.1f),
            Behavior = new RandomWanderBehavior(fastTileArea, speed: 120f, minIdleDuration: 1.5f, maxIdleDuration: 4f,   seed: 1001) });
        AddChild(new Enemy { Radius = 22f, Color = new Color(0.9f, 0.15f, 0.3f),
            Behavior = new RandomWanderBehavior(fastTileArea, speed: 280f, minIdleDuration: 0.3f, maxIdleDuration: 1.5f, seed: 1002) });
        AddChild(new Enemy { Radius = 28f, Color = new Color(0.8f, 0.2f, 0.5f),
            Behavior = new RandomWanderBehavior(fastTileArea, speed: 180f, minIdleDuration: 0.8f, maxIdleDuration: 3f,   seed: 1003) });
        AddChild(new Enemy { Radius = 18f, Color = new Color(0.95f, 0.4f, 0.1f),
            Behavior = new RandomWanderBehavior(fastTileArea, speed: 350f, minIdleDuration: 0.2f, maxIdleDuration: 1f,   seed: 1004) });
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-10000, -10000, 20000, 20000), new Color(0.18f, 0.32f, 0.14f));
    }

#if DEBUG
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Quoteleft }) return;
        if (_localUnit == null) return;

        var ps = _localUnit.PlayerState;
        ps.PlayerLevel                            = 20;
        ps.AbilityLevels[(int)AbilitySlot.Boost]    = 4;
        ps.AbilityLevels[(int)AbilitySlot.Warp]     = 4;
        ps.AbilityLevels[(int)AbilitySlot.Donut]    = 4;
        ps.AbilityLevels[(int)AbilitySlot.Ethereal] = 4;
        ps.AbilityLevels[(int)AbilitySlot.Gack]     = 1;
        _localUnit.ResetAbilityCooldowns();
        GetViewport().SetInputAsHandled();
    }
#endif
}
