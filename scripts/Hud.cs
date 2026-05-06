using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Slide;

public partial class Hud : CanvasLayer
{
    private Label _timerLabel = null!;
    private VBoxContainer _scoreboardBox = null!;
    private AbilityBar _abilityBar = null!;
    private Unit? _unit;
    private PlayerState _playerState = RunState.GetPlayer(0);
    private float _elapsed;

    private readonly List<(Unit unit, Label label)> _scoreboardRows = [];

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _scoreboardBox = new VBoxContainer();
        _scoreboardBox.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _scoreboardBox.OffsetLeft   = 10;
        _scoreboardBox.OffsetTop    = 10;
        _scoreboardBox.OffsetRight  = 500;
        _scoreboardBox.OffsetBottom = 300;
        _scoreboardBox.AddThemeConstantOverride("separation", 2);
        root.AddChild(_scoreboardBox);

        _timerLabel = CreateLabel(root, Control.LayoutPreset.TopRight,
            offsetLeft: -130, offsetTop: 10, offsetRight: -10, offsetBottom: 40,
            align: HorizontalAlignment.Right);

        _abilityBar = new AbilityBar();
        root.AddChild(_abilityBar);

        _elapsed = RunState.ElapsedSeconds;
        UpdateTimerLabel();
    }

    public void SetUnit(Unit unit)
    {
        _unit        = unit;
        _playerState = unit.PlayerState;
        _abilityBar.SetPlayerState(_playerState);
    }

    public void SetAllUnits(IEnumerable<Unit> units)
    {
        foreach (var child in _scoreboardBox.GetChildren())
            child.QueueFree();
        _scoreboardRows.Clear();

        foreach (var unit in units.OrderBy(u => u.PlayerId))
        {
            var label = new Label();
            label.AddThemeFontSizeOverride("font_size", 16);
            label.AddThemeColorOverride("font_color", unit.UnitColor);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            _scoreboardBox.AddChild(label);
            _scoreboardRows.Add((unit, label));
        }

        UpdateScoreboard();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        RunState.ElapsedSeconds = _elapsed;
        UpdateTimerLabel();
        UpdateScoreboard();

        if (_unit != null)
        {
            for (int i = 0; i < 5; i++)
            {
                var (fraction, active) = _unit.GetAbilityState(i);
                _abilityBar.UpdateSlotState(i, fraction, active);
            }
        }
    }

    public void OnUnitDied()      { }
    public void OnUnitRespawned() { }

    private void UpdateScoreboard()
    {
        foreach (var (unit, label) in _scoreboardRows)
        {
            string prefix = unit.IsLocalPlayer ? ">" : " ";
            string status = unit.IsDead ? "☠" : "♥";
            label.Text = $"{prefix} Slider {unit.PlayerId + 1}  Lv.{unit.PlayerState.PlayerLevel}  {status}  Deaths: {unit.PlayerState.TotalDeaths}";
        }
    }

    private void UpdateTimerLabel()
    {
        int minutes = (int)_elapsed / 60;
        int seconds = (int)_elapsed % 60;
        _timerLabel.Text = $"{minutes}:{seconds:D2}";
    }

    private static Label CreateLabel(
        Control parent,
        Control.LayoutPreset preset,
        float offsetLeft, float offsetTop, float offsetRight, float offsetBottom,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { HorizontalAlignment = align };
        label.SetAnchorsPreset(preset);
        label.OffsetLeft   = offsetLeft;
        label.OffsetTop    = offsetTop;
        label.OffsetRight  = offsetRight;
        label.OffsetBottom = offsetBottom;
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        parent.AddChild(label);
        return label;
    }
}
