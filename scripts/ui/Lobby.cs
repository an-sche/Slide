using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Slide;

public partial class Lobby : Control
{
    private readonly record struct LobbySlot(int Index, bool Ready);

    private readonly Dictionary<long, LobbySlot> _players    = new();
    private readonly Dictionary<long, Label>     _readyLabels = new();
    private int    _nextIndex = 1; // host is always index 0
    private bool   _localReady;
    private Button _readyBtn   = null!;
    private Label  _statusLabel = null!;
    private VBoxContainer _playerRows = null!;

    public override void _Ready()
    {
        GameSetup.Clear();
        BuildUi();

        if (!GameNetwork.IsMultiplayer) return;

        if (Multiplayer.IsServer())
        {
            RegisterPlayer(Multiplayer.GetUniqueId(), 0);
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }
        else
        {
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            RpcId(1, nameof(RequestJoin));
        }
    }

    public override void _ExitTree()
    {
        if (GameNetwork.IsMultiplayer)
            Multiplayer.PeerDisconnected -= OnPeerDisconnected;
    }

    // Client → host: I have loaded and am ready to be assigned.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestJoin()
    {
        if (!Multiplayer.IsServer()) return;
        long peerId = Multiplayer.GetRemoteSenderId();
        if (_players.ContainsKey(peerId)) return;

        int index = _nextIndex++;

        // Send all existing players to the new peer first.
        foreach (var (existingId, slot) in _players)
            RpcId(peerId, nameof(AddPlayer), existingId, slot.Index, slot.Ready);

        // Register new player on host, then broadcast to all clients (including the new one).
        RegisterPlayer(peerId, index);
        Rpc(nameof(AddPlayer), peerId, index, false);

        UpdateStatus();
    }

    // Host → all: a player joined (or existing player list catch-up for a new joiner).
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void AddPlayer(long peerId, int index, bool ready)
    {
        if (_players.ContainsKey(peerId)) return;
        RegisterPlayer(peerId, index);
        if (ready)
        {
            _players[peerId] = _players[peerId] with { Ready = true };
            UpdateReadyLabel(peerId, true);
        }
        UpdateStatus();
    }

    // Client → host: toggle ready state.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestSetReady(bool ready)
    {
        if (!Multiplayer.IsServer()) return;
        long peerId = Multiplayer.GetRemoteSenderId();
        if (!_players.TryGetValue(peerId, out var slot)) return;

        _players[peerId] = slot with { Ready = ready };
        UpdateReadyLabel(peerId, ready);
        Rpc(nameof(UpdateReady), peerId, ready);
        UpdateStatus();
        CheckAllReady();
    }

    // Host → all: a player's ready state changed.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void UpdateReady(long peerId, bool ready)
    {
        if (!_players.TryGetValue(peerId, out var slot)) return;
        _players[peerId] = slot with { Ready = ready };
        UpdateReadyLabel(peerId, ready);
        UpdateStatus();
    }

    // Host → all: everyone is ready, load the game.
    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void StartGame()
    {
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }

    // ── private helpers ────────────────────────────────────────────────────────

    private void OnReadyPressed()
    {
        _localReady    = !_localReady;
        _readyBtn.Text = _localReady ? "Unready" : "Ready";

        if (!GameNetwork.IsMultiplayer) return;

        if (Multiplayer.IsServer())
        {
            long myId = Multiplayer.GetUniqueId();
            _players[myId] = _players[myId] with { Ready = _localReady };
            UpdateReadyLabel(myId, _localReady);
            Rpc(nameof(UpdateReady), myId, _localReady);
            UpdateStatus();
            CheckAllReady();
        }
        else
        {
            RpcId(1, nameof(RequestSetReady), _localReady);
        }
    }

    private void OnPeerDisconnected(long peerId)
    {
        if (!_players.ContainsKey(peerId)) return;
        _players.Remove(peerId);
        GameSetup.RemovePlayer(peerId);
        RemovePlayerRow(peerId);
        UpdateStatus();
    }

    private void CheckAllReady()
    {
        if (_players.Count == 0) return;
        if (!_players.Values.All(s => s.Ready)) return;
        _statusLabel.Text = "Starting...";
        Rpc(nameof(StartGame));
        StartGame();
    }

    private void RegisterPlayer(long peerId, int index)
    {
        _players[peerId] = new LobbySlot(index, false);
        GameSetup.AddPlayer(peerId, index);
        AddPlayerRow(peerId, index);

        // Enable ready button once the local player's slot exists.
        if (peerId == Multiplayer.GetUniqueId())
            _readyBtn.Disabled = false;
    }

    private void UpdateStatus()
    {
        int total = _players.Count;
        int ready = _players.Values.Count(s => s.Ready);
        _statusLabel.Text = total == 0 ? "Waiting for players..." : $"{ready} / {total} ready";
    }

    // ── UI ─────────────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = new Color(0.08f, 0.08f, 0.10f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(420, 0) };
        vbox.AddThemeConstantOverride("separation", 16);
        center.AddChild(vbox);

        var title = new Label { Text = "SLIDE", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 72);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        title.AddThemeConstantOverride("outline_size", 7);
        vbox.AddChild(title);

        var subtitle = new Label { Text = "Lobby", HorizontalAlignment = HorizontalAlignment.Center };
        subtitle.AddThemeFontSizeOverride("font_size", 22);
        subtitle.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
        vbox.AddChild(subtitle);

        vbox.AddChild(new HSeparator());

        _playerRows = new VBoxContainer();
        _playerRows.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_playerRows);

        vbox.AddChild(new HSeparator());

        _statusLabel = new Label { Text = "Waiting for players...", HorizontalAlignment = HorizontalAlignment.Center };
        _statusLabel.AddThemeFontSizeOverride("font_size", 15);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        vbox.AddChild(_statusLabel);

        _readyBtn = new Button { Text = "Ready", CustomMinimumSize = new Vector2(420, 52), Disabled = true };
        _readyBtn.AddThemeFontSizeOverride("font_size", 22);
        _readyBtn.Pressed += OnReadyPressed;
        vbox.AddChild(_readyBtn);
    }

    private void AddPlayerRow(long peerId, int index)
    {
        long myId = Multiplayer.GetUniqueId();
        string name = $"Slider {index + 1}" + (peerId == myId ? " (You)" : "");

        var row = new HBoxContainer { Name = $"Row_{peerId}" };

        var nameLabel = new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(nameLabel);

        var readyLabel = new Label { Text = "waiting..." };
        readyLabel.AddThemeFontSizeOverride("font_size", 18);
        readyLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
        row.AddChild(readyLabel);

        _playerRows.AddChild(row);
        _readyLabels[peerId] = readyLabel;
    }

    private void RemovePlayerRow(long peerId)
    {
        _playerRows.GetNodeOrNull<HBoxContainer>($"Row_{peerId}")?.QueueFree();
        _readyLabels.Remove(peerId);
    }

    private void UpdateReadyLabel(long peerId, bool ready)
    {
        if (!_readyLabels.TryGetValue(peerId, out var label)) return;
        label.Text = ready ? "READY" : "waiting...";
        label.AddThemeColorOverride("font_color",
            ready ? new Color(0.4f, 1f, 0.4f) : new Color(0.55f, 0.55f, 0.55f));
    }
}
