using Godot;

namespace Slide;

public partial class Editor : Control
{
    private EditorMode _mode         = EditorMode.Paint;
    private int        _selectedSlot = 0;

    private readonly Button[]       _modeTabs      = new Button[4];
    private readonly StyleBoxFlat[] _modeTabStyles = new StyleBoxFlat[4];
    private readonly Control[]      _slotRoots     = new Control[8];
    private readonly StyleBoxFlat[] _slotStyles    = new StyleBoxFlat[8];
    private readonly ColorRect[]    _slotSwatches  = new ColorRect[8];
    private readonly Label[]        _slotNames     = new Label[8];

    private CanvasView _canvas     = null!;
    private Label      _hint       = null!;
    private Label      _titleLabel = null!;
    private Label      _brushLabel = null!;
    private LevelData? _levelData;
    private string     _levelDir  = "";
    private string     _levelPath = "";

    public override void _Ready()
    {
        var window = GetTree().Root;
        Size = (Vector2)window.Size;
        window.SizeChanged += () => { if (IsInsideTree()) Size = (Vector2)GetTree().Root.Size; };

        var bg = new ColorRect { Color = new Color(0.10f, 0.10f, 0.12f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 0);
        AddChild(vbox);

        vbox.AddChild(BuildTopBar());

        var viewport = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(viewport);
        BuildViewportArea(viewport);

        vbox.AddChild(BuildBottomBar());

        SetMode(EditorMode.Paint);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        int slot = key.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3,
            Key.Key5 => 4, Key.Key6 => 5, Key.Key7 => 6, Key.Key8 => 7,
            _ => -1,
        };
        if (slot >= 0) { SelectSlot(slot); GetViewport().SetInputAsHandled(); return; }

        if (key.Keycode == Key.Bracketleft)  { AdjustBrush(-1); GetViewport().SetInputAsHandled(); return; }
        if (key.Keycode == Key.Bracketright) { AdjustBrush(+1); GetViewport().SetInputAsHandled(); return; }

        if (key.Keycode == Key.Tab)
        {
            SetMode((EditorMode)(((int)_mode + 1) % 4));
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Escape)
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            GetViewport().SetInputAsHandled();
        }
    }
}
