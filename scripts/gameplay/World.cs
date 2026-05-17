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
    private Rect2            _levelBounds;

    private const float WipeDelay = GameplayConstants.WipeDelay;
    private SceneTreeTimer? _wipeTimer;

    private EffectSystem     _effects     = null!;
    private ProjectileSystem _projectiles = null!;

    private CanvasLayer? _escapeMenu;

    private readonly Dictionary<long, Unit> _units = new();
    private Enemy[] _enemies = [];

    public override void _Ready()
    {
        _effects     = new EffectSystem();
        _projectiles = new ProjectileSystem();
        AddChild(_effects);
        AddChild(_projectiles);

        _transition = new LevelTransition();
        AddChild(_transition);

        Input.MouseMode = Input.MouseModeEnum.Confined;

        string levelPath = GameSetup.IsPlaytest ? GameSetup.PlaytestPath! : "res://levels/test.json";
        if (GameSetup.IsPlaytest) AddPlaytestBanner();
        ulong levelSeed = ((ulong)GD.Randi() << 32) | GD.Randi();
        var snap   = GameSetup.PlaytestRestore;
        var result = snap != null
            ? LevelLoader.Load(snap.LevelData, snap.Image, this, levelSeed)
            : LevelLoader.Load(levelPath, this, levelSeed);
        _enemies = result.Enemies;
        _startPosition = result.StartPosition;
        _levelBounds   = result.LevelBounds;
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

        var spawnPos = _startPosition + new Vector2(playerIndex * 48f, 0);
        var unit = new Unit
        {
            Name          = $"Unit_{peerId}",
            PlayerId      = playerIndex,
            PeerId        = peerId,
            UnitColor     = color,
            IsLocalPlayer = isLocal,
            Position      = spawnPos,
        };
        AddChild(unit);
        _units[peerId]   = unit;
        unit.Effects     = _effects;
        unit.Projectiles = _projectiles;

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
                Rpc(nameof(BroadcastUnitRespawn), peerId, unit.GlobalPosition);
                CancelWipeTimer();
            };
        }

        if (!isLocal) return;

        _localUnit = unit;
        unit.AbilityInputForwarded += (slot, level) => RpcId(1, nameof(UseAbility), slot, level);

        _camera = new GameCamera();
        AddChild(_camera);
        _camera.Initialize(unit);
        _camera.SetLevelBounds(_levelBounds);

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
        if (!GameNetwork.IsMultiplayer || !Multiplayer.IsServer()) return;

        foreach (var (peerId, unit) in _units)
            Rpc(nameof(SyncUnitState), peerId, unit.GlobalPosition, unit.Facing, unit.HasTarget, unit.TargetPosition, unit.AbilitiesActiveMask);

        if (_enemies.Length > 0)
        {
            var positions  = new Vector2[_enemies.Length];
            var telegraphs = new float[_enemies.Length];
            for (int i = 0; i < _enemies.Length; i++)
            {
                positions[i]  = _enemies[i].GlobalPosition;
                telegraphs[i] = _enemies[i].TelegraphProgress;
            }
            Rpc(nameof(SyncEnemyStates), (Variant)positions, (Variant)telegraphs);
        }
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

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncEnemyStates(Vector2[] positions, float[] telegraphProgress)
    {
        for (int i = 0; i < positions.Length && i < _enemies.Length; i++)
        {
            _enemies[i].GlobalPosition    = positions[i];
            _enemies[i].TelegraphProgress = telegraphProgress[i];
        }
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

    private void AddPlaytestBanner()
    {
        var canvas = new CanvasLayer { Layer = 128 };
        var label  = new Label
        {
            Text                = "PLAYTEST  —  Esc for menu",
            HorizontalAlignment = HorizontalAlignment.Center,
            AnchorLeft          = 0f,
            AnchorRight         = 1f,
            AnchorTop           = 0f,
            OffsetTop           = 6f,
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f, 0.85f));
        canvas.AddChild(label);
        AddChild(canvas);
    }

    private void ShowEscapeMenu()
    {
        var canvas = new CanvasLayer { Layer = 20 };

        var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.6f) };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvas.AddChild(center);

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        center.AddChild(vbox);

        var btn = new Button { CustomMinimumSize = new Vector2(0, 50) };
        btn.Text = GameSetup.IsPlaytest ? "Quit to Editor" : "Quit to Home";
        btn.Pressed += () =>
        {
            if (GameSetup.IsPlaytest)
            {
                GameSetup.PlaytestPath = null;
                Input.MouseMode        = Input.MouseModeEnum.Visible;
                GetTree().ChangeSceneToFile("res://scenes/Editor.tscn");
            }
            else
            {
                GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            }
        };
        vbox.AddChild(btn);

        AddChild(canvas);
        _escapeMenu = canvas;
    }

    private void CloseEscapeMenu()
    {
        _escapeMenu?.QueueFree();
        _escapeMenu = null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (key.Keycode == Key.Escape)
        {
            if (_escapeMenu != null)
                CloseEscapeMenu();
            else
                ShowEscapeMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

#if DEBUG
        if (key.Keycode != Key.Quoteleft || _localUnit == null) return;
        var ps = _localUnit.PlayerState;
        ps.PlayerLevel                              = 20;
        ps.AbilityLevels[(int)AbilitySlot.Boost]    = 4;
        ps.AbilityLevels[(int)AbilitySlot.Warp]     = 4;
        ps.AbilityLevels[(int)AbilitySlot.Donut]    = 4;
        ps.AbilityLevels[(int)AbilitySlot.Ethereal] = 4;
        ps.AbilityLevels[(int)AbilitySlot.Gack]     = 1;
        _localUnit.ResetAbilityCooldowns();
        GetViewport().SetInputAsHandled();
#endif
    }
}
