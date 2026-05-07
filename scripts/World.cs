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

    private const float WipeDelay = GameplayConstants.WipeDelay;
    private SceneTreeTimer? _wipeTimer;

    private readonly Dictionary<long, Unit>      _units      = new();
    private readonly Dictionary<long, WarpGhost> _warpGhosts = new();

    public override void _Ready()
    {
        _transition = new LevelTransition();
        AddChild(_transition);

        Input.MouseMode = Input.MouseModeEnum.Confined;

        var result = LevelLoader.Load("res://levels/test.json", this);
        _startPosition = result.StartPosition;
        if (result.EndBlock != null)
            result.EndBlock.LevelCompleted += OnLevelCompleted;

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

        Color color   = PlayerConstants.Colors[playerIndex % PlayerConstants.Colors.Length];
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
            if (corpse.SourceUnit == unit) return;
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
        _localUnit?.SetTarget(target);
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer())
            RpcId(1, nameof(SetMoveTarget), target);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UseAbility(int slot, int level)
    {
        if (!Multiplayer.IsServer()) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (!_units.TryGetValue(sender, out var unit)) return;
        unit.PlayerState.AbilityLevels[slot] = level;
        unit.TryActivateAbility(slot);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SetMoveTarget(Vector2 position)
    {
        if (!Multiplayer.IsServer()) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (_units.TryGetValue(sender, out var unit))
            unit.SetTarget(position);
    }

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

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void BroadcastUnitDeath(long peerId, Vector2 deathPosition)
    {
        if (!_units.TryGetValue(peerId, out var unit) || unit.IsDead) return;
        unit.ApplyRemoteDeath(deathPosition);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void BroadcastUnitRespawn(long peerId, Vector2 spawnPosition)
    {
        if (!_units.TryGetValue(peerId, out var unit) || !unit.IsDead) return;
        unit.ApplyRemoteRespawn(spawnPosition);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnGooZone(Vector2 position)
    {
        AddChild(new GooZone { GlobalPosition = position });
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnWarpGhost(long peerId, Vector2 position, Vector2 facing, float duration)
    {
        if (_warpGhosts.TryGetValue(peerId, out var old)) { old.QueueFree(); _warpGhosts.Remove(peerId); }
        var ghost = new WarpGhost { GlobalPosition = position, Facing = facing, Duration = duration };
        AddChild(ghost);
        _warpGhosts[peerId] = ghost;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientRemoveWarpGhost(long peerId)
    {
        if (_warpGhosts.TryGetValue(peerId, out var ghost)) { ghost.QueueFree(); _warpGhosts.Remove(peerId); }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnDonut(Vector2 position, Vector2 velocity, float lifetime)
    {
        AddChild(new DonutProjectile { GlobalPosition = position, MoveVelocity = velocity, Lifetime = lifetime });
    }

    private void CheckTeamWipe()
    {
        if (_wipeTimer != null) return;
        foreach (var unit in _units.Values)
            if (!unit.IsDead) return;

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
        foreach (var unit in _units.Values)
            if (!unit.IsDead) return;

        Rpc(nameof(ResetRun));
        ResetRun();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ResetRun()
    {
        RunState.Reset();
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
        ps.PlayerLevel                              = 20;
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
