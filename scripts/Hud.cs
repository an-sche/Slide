using Godot;

namespace Slide;

public partial class Hud : CanvasLayer
{
    private const string PlayerName = "Slider 1";

    private Label _timerLabel = null!;
    private Label _statusLabel = null!;
    private AbilityBar _abilityBar = null!;
    private float _elapsed;
    private int _deaths;
    private bool _isAlive = true;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _statusLabel = CreateLabel(root, Control.LayoutPreset.TopLeft,
            offsetLeft: 10, offsetTop: 10, offsetRight: 420, offsetBottom: 40);

        _timerLabel = CreateLabel(root, Control.LayoutPreset.TopRight,
            offsetLeft: -130, offsetTop: 10, offsetRight: -10, offsetBottom: 40,
            align: HorizontalAlignment.Right);

        _abilityBar = new AbilityBar();
        root.AddChild(_abilityBar);

        _elapsed = RunState.ElapsedSeconds;
        _deaths = RunState.TotalDeaths;
        _abilityBar.SetLevel(RunState.PlayerLevel);
        UpdateLabels();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        RunState.ElapsedSeconds = _elapsed;
        UpdateTimerLabel();
    }

    public void OnUnitDied()
    {
        _deaths++;
        RunState.TotalDeaths = _deaths;
        _isAlive = false;
        UpdateStatusLabel();
    }

    public void OnUnitRespawned()
    {
        _isAlive = true;
        UpdateStatusLabel();
    }

    private void UpdateTimerLabel()
    {
        int minutes = (int)_elapsed / 60;
        int seconds = (int)_elapsed % 60;
        _timerLabel.Text = $"{minutes}:{seconds:D2}";
    }

    private void UpdateStatusLabel()
    {
        string status = _isAlive ? "Alive" : "Dead";
        _statusLabel.Text = $"{PlayerName}  |  Lv.{RunState.PlayerLevel}  |  {status}  |  Deaths: {_deaths}";
    }

    private void UpdateLabels()
    {
        UpdateTimerLabel();
        UpdateStatusLabel();
    }

    private static Label CreateLabel(
        Control parent,
        Control.LayoutPreset preset,
        float offsetLeft, float offsetTop, float offsetRight, float offsetBottom,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { HorizontalAlignment = align };
        label.SetAnchorsPreset(preset);
        label.OffsetLeft = offsetLeft;
        label.OffsetTop = offsetTop;
        label.OffsetRight = offsetRight;
        label.OffsetBottom = offsetBottom;
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        parent.AddChild(label);
        return label;
    }
}
