using Godot;

namespace Slide;

public partial class LevelTransition : CanvasLayer
{
    private const float DisplayDuration = 4f;

    private Control _overlay = null!;
    private Label _timeLabel = null!;
    private Label _deathsLabel = null!;
    private Label _finisherLabel = null!;
    private float _elapsed;

    public override void _Ready()
    {
        Layer = 10;
        SetProcess(false);

        _overlay = new Control();
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.Visible = false;
        AddChild(_overlay);

        var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.8f) };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.AddChild(center);

        var vbox = new VBoxContainer { CustomMinimumSize = new Vector2(400, 0) };
        center.AddChild(vbox);

        var title = new Label { Text = "LEVEL COMPLETE", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 36);
        title.AddThemeColorOverride("font_color", Colors.Gold);
        title.AddThemeColorOverride("font_outline_color", Colors.Black);
        title.AddThemeConstantOverride("outline_size", 4);
        vbox.AddChild(title);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });

        _timeLabel = MakeLabel(vbox, 22);
        _deathsLabel = MakeLabel(vbox, 22);
        _finisherLabel = MakeLabel(vbox, 22);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });

        var hint = MakeLabel(vbox, 16);
        hint.Text = "Loading next level...";
        hint.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
    }

    public void ShowTransition(string finisherName, float elapsedSeconds, int totalDeaths)
    {
        int mins = (int)elapsedSeconds / 60;
        int secs = (int)elapsedSeconds % 60;
        _timeLabel.Text = $"Time:  {mins}:{secs:D2}";
        _deathsLabel.Text = $"Deaths:  {totalDeaths}";
        _finisherLabel.Text = $"Finished by:  {finisherName}";

        _overlay.Visible = true;
        _elapsed = 0;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= DisplayDuration)
        {
            SetProcess(false);
            GetTree().ReloadCurrentScene();
        }
    }

    private static Label MakeLabel(VBoxContainer parent, int fontSize)
    {
        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        parent.AddChild(label);
        return label;
    }
}
