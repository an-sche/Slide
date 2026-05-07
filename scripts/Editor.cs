using Godot;

namespace Slide;

public partial class Editor : Control
{
    private enum EditorMode { Paint, Entities, Enemies, Triggers }

    private static readonly string[][] SlotLabels =
    [
        ["Ground", "Slidy", "Fast", "Confusing", "FastConf", "Straight", "Kill", "Void"],
        ["Start", "End", "Bonus"],
        ["Patrol", "Wander", "Orbiter", "Chaser", "Bouncer", "Sniper", "Guard"],
        ["Button", "Door"],
    ];

    private static readonly Color[][] SlotColors =
    [
        [
            SurfaceConstants.Ground,
            SurfaceConstants.Slidy,
            SurfaceConstants.Fast,
            SurfaceConstants.Confusing,
            SurfaceConstants.FastConfusing,
            SurfaceConstants.Straight,
            SurfaceConstants.Kill,
            SurfaceConstants.Void,
        ],
        [
            new Color(0.18f, 0.80f, 0.28f), // Start
            new Color(1.00f, 0.85f, 0.00f), // End
            new Color(1.00f, 0.88f, 0.20f), // Bonus
        ],
        [
            new Color(0.88f, 0.28f, 0.18f), // Patrol
            new Color(0.88f, 0.18f, 0.48f), // Wander
            new Color(0.28f, 0.80f, 0.48f), // Orbiter
            new Color(0.80f, 0.48f, 0.08f), // Chaser
            new Color(0.18f, 0.58f, 0.90f), // Bouncer
            new Color(0.90f, 0.80f, 0.08f), // Sniper
            new Color(0.68f, 0.18f, 0.90f), // Guard
        ],
        [
            new Color(0.18f, 0.78f, 0.78f), // Button
            new Color(0.58f, 0.58f, 0.68f), // Door
        ],
    ];

    private static readonly Color ActiveTabBg      = new(0.22f, 0.22f, 0.30f);
    private static readonly Color InactiveTabBg    = new(0.12f, 0.12f, 0.15f);
    private static readonly Color SelectedBorder   = new(1.00f, 0.85f, 0.00f);
    private static readonly Color UnselectedBorder = new(0.32f, 0.32f, 0.38f);

    private const int TopBarH    = 48;
    private const int BottomBarH = 88;
    private const int SlotW      = 78;
    private const int SlotH      = 66;
    private const int PropW      = 260;

    private EditorMode _mode         = EditorMode.Paint;
    private int        _selectedSlot = 0;

    private readonly Button[]        _modeTabs      = new Button[4];
    private readonly StyleBoxFlat[]  _modeTabStyles = new StyleBoxFlat[4];
    private readonly Control[]       _slotRoots     = new Control[8];
    private readonly StyleBoxFlat[]  _slotStyles    = new StyleBoxFlat[8];
    private readonly ColorRect[]     _slotSwatches  = new ColorRect[8];
    private readonly Label[]         _slotNames     = new Label[8];

    private Control _propertiesPanel = null!;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect { Color = new Color(0.10f, 0.10f, 0.12f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 0);
        AddChild(vbox);

        vbox.AddChild(BuildTopBar());

        var viewportArea = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(viewportArea);
        BuildViewportArea(viewportArea);

        vbox.AddChild(BuildBottomBar());

        SetMode(EditorMode.Paint);
    }

    private Control BuildTopBar()
    {
        var panelStyle = new StyleBoxFlat { BgColor = new Color(0.14f, 0.14f, 0.17f) };
        var panel      = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(hbox);

        hbox.AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) });

        var newBtn = MakeTopBarButton("New");   newBtn.Pressed  += OnNew;  hbox.AddChild(newBtn);
        var openBtn = MakeTopBarButton("Open"); openBtn.Pressed += OnOpen; hbox.AddChild(openBtn);
        var saveBtn = MakeTopBarButton("Save"); saveBtn.Pressed += OnSave; hbox.AddChild(saveBtn);
        var playBtn = MakeTopBarButton("Play"); playBtn.Pressed += OnPlay; hbox.AddChild(playBtn);

        var sep = new VSeparator();
        sep.CustomMinimumSize = new Vector2(0, 28);
        hbox.AddChild(sep);

        string[] modeNames = ["Paint", "Entities", "Enemies", "Triggers"];
        for (int i = 0; i < modeNames.Length; i++)
        {
            var (btn, style) = MakeTabButton(modeNames[i]);
            int captured = i;
            btn.Pressed       += () => SetMode((EditorMode)captured);
            _modeTabs[i]      = btn;
            _modeTabStyles[i] = style;
            hbox.AddChild(btn);
        }

        hbox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        return panel;
    }

    private void BuildViewportArea(Control parent)
    {
        var canvas = new ColorRect { Color = new Color(0.18f, 0.32f, 0.14f) };
        canvas.SetAnchorsPreset(LayoutPreset.FullRect);
        parent.AddChild(canvas);

        var hint = new Label
        {
            Text                = "No level loaded — use New or Open to begin",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        hint.SetAnchorsPreset(LayoutPreset.FullRect);
        hint.AddThemeFontSizeOverride("font_size", 18);
        hint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.25f));
        parent.AddChild(hint);

        var propStyle = new StyleBoxFlat
        {
            BgColor          = new Color(0.13f, 0.13f, 0.16f),
            BorderWidthLeft  = 1,
            BorderColor      = new Color(0.30f, 0.30f, 0.38f),
        };
        _propertiesPanel = new Panel { Visible = false };
        _propertiesPanel.SetAnchorsPreset(LayoutPreset.RightWide);
        _propertiesPanel.OffsetLeft = -PropW;
        _propertiesPanel.AddThemeStyleboxOverride("panel", propStyle);
        parent.AddChild(_propertiesPanel);

        var propHint = new Label
        {
            Text                = "Select an object\nto edit its properties",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        propHint.SetAnchorsPreset(LayoutPreset.FullRect);
        propHint.AddThemeFontSizeOverride("font_size", 14);
        propHint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.30f));
        _propertiesPanel.AddChild(propHint);
    }

    private Control BuildBottomBar()
    {
        var panelStyle = new StyleBoxFlat { BgColor = new Color(0.14f, 0.14f, 0.17f) };
        var panel      = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var center = new CenterContainer();
        panel.AddChild(center);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        center.AddChild(hbox);

        for (int i = 0; i < 8; i++)
        {
            var slotStyle = new StyleBoxFlat
            {
                BgColor                = new Color(0.10f, 0.10f, 0.13f),
                BorderWidthTop         = 2, BorderWidthBottom    = 2,
                BorderWidthLeft        = 2, BorderWidthRight     = 2,
                BorderColor            = UnselectedBorder,
                CornerRadiusTopLeft    = 4, CornerRadiusTopRight    = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            };
            _slotStyles[i] = slotStyle;

            var slot = new Panel { CustomMinimumSize = new Vector2(SlotW, SlotH) };
            slot.AddThemeStyleboxOverride("panel", slotStyle);
            hbox.AddChild(slot);
            _slotRoots[i] = slot;

            var vbox = new VBoxContainer();
            vbox.SetAnchorsPreset(LayoutPreset.FullRect);
            vbox.Alignment    = BoxContainer.AlignmentMode.Center;
            vbox.MouseFilter  = MouseFilterEnum.Ignore;
            vbox.AddThemeConstantOverride("separation", 2);
            slot.AddChild(vbox);

            var keyLabel = new Label
            {
                Text                = $"{i + 1}",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            keyLabel.AddThemeFontSizeOverride("font_size", 11);
            keyLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            vbox.AddChild(keyLabel);

            var swatch = new ColorRect { CustomMinimumSize = new Vector2(28, 22) };
            swatch.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            vbox.AddChild(swatch);
            _slotSwatches[i] = swatch;

            var nameLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
            vbox.AddChild(nameLabel);
            _slotNames[i] = nameLabel;

            int captured = i;
            slot.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                    SelectSlot(captured);
            };
        }

        return panel;
    }

    private void SetMode(EditorMode mode)
    {
        _mode = mode;

        for (int i = 0; i < _modeTabs.Length; i++)
            _modeTabStyles[i].BgColor = i == (int)mode ? ActiveTabBg : InactiveTabBg;

        string[] labels = SlotLabels[(int)mode];
        Color[]  colors = SlotColors[(int)mode];

        for (int i = 0; i < 8; i++)
        {
            bool active = i < labels.Length;
            _slotRoots[i].Visible = active;
            if (!active) continue;
            _slotSwatches[i].Color = colors[i];
            _slotNames[i].Text     = labels[i];
        }

        SelectSlot(0);
    }

    private void SelectSlot(int index)
    {
        if (index >= SlotLabels[(int)_mode].Length) return;
        for (int i = 0; i < 8; i++)
            _slotStyles[i].BorderColor = i == index ? SelectedBorder : UnselectedBorder;
        _selectedSlot = index;
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

    private void OnNew()  { } // TODO: prompt to save, then reset to blank level
    private void OnOpen() { } // TODO: open file browser
    private void OnSave() { } // TODO: save level JSON to user://levels/
    private void OnPlay() => GetTree().ChangeSceneToFile("res://scenes/World.tscn");

    private static Button MakeTopBarButton(string text)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(72, 36) };
        btn.AddThemeFontSizeOverride("font_size", 15);
        return btn;
    }

    private static (Button btn, StyleBoxFlat style) MakeTabButton(string text)
    {
        var normal = new StyleBoxFlat
        {
            BgColor                = InactiveTabBg,
            CornerRadiusTopLeft    = 3,
            CornerRadiusTopRight   = 3,
        };
        var hover = new StyleBoxFlat
        {
            BgColor                = new Color(0.20f, 0.20f, 0.26f),
            CornerRadiusTopLeft    = 3,
            CornerRadiusTopRight   = 3,
        };

        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(92, 36) };
        btn.AddThemeFontSizeOverride("font_size", 15);
        btn.AddThemeStyleboxOverride("normal",   normal);
        btn.AddThemeStyleboxOverride("hover",    hover);
        btn.AddThemeStyleboxOverride("pressed",  normal);
        btn.AddThemeStyleboxOverride("focus",    new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("disabled", normal);
        return (btn, normal);
    }
}
