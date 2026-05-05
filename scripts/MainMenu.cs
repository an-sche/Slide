using Godot;

namespace Slide;

public partial class MainMenu : Control
{
    private VBoxContainer _joinPanel = null!;
    private LineEdit      _ipInput   = null!;
    private Label         _status    = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = new Color(0.08f, 0.08f, 0.10f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(340, 0) };
        vbox.AddThemeConstantOverride("separation", 14);
        center.AddChild(vbox);

        // Title
        var title = new Label { Text = "SLIDE", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 80);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        title.AddThemeConstantOverride("outline_size", 7);
        vbox.AddChild(title);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });

        var playBtn = MakeButton("Play Solo");
        playBtn.Pressed += OnPlaySolo;
        vbox.AddChild(playBtn);

        var hostBtn = MakeButton("Host");
        hostBtn.Pressed += OnHost;
        vbox.AddChild(hostBtn);

        var joinBtn = MakeButton("Join");
        joinBtn.Pressed += OnJoinToggle;
        vbox.AddChild(joinBtn);

        // Join sub-panel (hidden until Join is clicked)
        _joinPanel = new VBoxContainer { Visible = false };
        _joinPanel.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_joinPanel);

        _ipInput = new LineEdit
        {
            Text            = "127.0.0.1",
            PlaceholderText = "IP address",
            CustomMinimumSize = new Vector2(340, 44),
        };
        _ipInput.AddThemeFontSizeOverride("font_size", 18);
        _joinPanel.AddChild(_ipInput);

        var connectBtn = MakeButton("Connect");
        connectBtn.Pressed += OnConnect;
        _joinPanel.AddChild(connectBtn);

        // Status / error label
        _status = new Label
        {
            Text                = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Word,
            CustomMinimumSize   = new Vector2(340, 0),
        };
        _status.AddThemeFontSizeOverride("font_size", 14);
        _status.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.45f));
        vbox.AddChild(_status);
    }

    private void OnPlaySolo()
    {
        GameNetwork.IsMultiplayer = false;
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }

    private void OnHost()
    {
        var peer = new ENetMultiplayerPeer();
        Error err = peer.CreateServer(GameNetwork.Port);
        if (err != Error.Ok)
        {
            _status.Text = $"Failed to start server: {err}";
            return;
        }
        Multiplayer.MultiplayerPeer = peer;
        GameNetwork.IsMultiplayer   = true;
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }

    private void OnJoinToggle()
    {
        _joinPanel.Visible = !_joinPanel.Visible;
        _status.Text       = "";
    }

    private void OnConnect()
    {
        string ip = _ipInput.Text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        GameNetwork.JoinIp = ip;

        var peer = new ENetMultiplayerPeer();
        Error err = peer.CreateClient(ip, GameNetwork.Port);
        if (err != Error.Ok)
        {
            _status.Text = $"Connection error: {err}";
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GameNetwork.IsMultiplayer   = true;
        _status.Text                = "Connecting…";

        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed  += OnConnectionFailed;
    }

    private void OnConnectedToServer()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ConnectionFailed  -= OnConnectionFailed;
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }

    private void OnConnectionFailed()
    {
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
        Multiplayer.ConnectionFailed  -= OnConnectionFailed;
        _status.Text                  = "Connection failed.";
        Multiplayer.MultiplayerPeer   = null;
        GameNetwork.IsMultiplayer     = false;
    }

    private static Button MakeButton(string text)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(340, 52) };
        btn.AddThemeFontSizeOverride("font_size", 22);
        return btn;
    }
}
