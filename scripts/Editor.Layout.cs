using Godot;

namespace Slide;

public partial class Editor
{
    private const int SlotW       = 78;
    private const int SlotH       = 66;
    private const int ConfigWidth = 160;

    private Control BuildTopBar()
    {
        var panelStyle = new StyleBoxFlat { BgColor = new Color(0.14f, 0.14f, 0.17f) };
        var panel      = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(hbox);

        hbox.AddChild(new Control { CustomMinimumSize = new Vector2(8, 0) });

        var newBtn      = MakeTopBarButton("New");      newBtn.Pressed      += OnNew;           hbox.AddChild(newBtn);
        var openBtn     = MakeTopBarButton("Open");     openBtn.Pressed     += OnOpen;          hbox.AddChild(openBtn);
        var saveBtn     = MakeTopBarButton("Save");     saveBtn.Pressed     += OnSave;          hbox.AddChild(saveBtn);
        var saveAsBtn   = MakeTopBarButton("Save As");  saveAsBtn.Pressed   += OnSaveAs;        hbox.AddChild(saveAsBtn);
        var settingsBtn = MakeTopBarButton("Settings"); settingsBtn.Pressed += OnLevelSettings; hbox.AddChild(settingsBtn);
        var playBtn     = MakeTopBarButton("Play");     playBtn.Pressed     += OnPlay;          hbox.AddChild(playBtn);

        hbox.AddChild(new VSeparator { CustomMinimumSize = new Vector2(0, 28) });

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

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Text                = "",
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 15);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.80f, 0.80f, 0.85f));
        hbox.AddChild(_titleLabel);

        hbox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        return panel;
    }

    private void BuildViewportArea(Control parent)
    {
        _canvas = new CanvasView();
        _canvas.SetAnchorsPreset(LayoutPreset.FullRect);
        _canvas.PixelClicked     += OnPixelClicked;
        _canvas.PixelLeftPressed += OnPixelLeftPressed;
        parent.AddChild(_canvas);

        _hint = new Label
        {
            Text                = "No level loaded — use Open to begin",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            MouseFilter         = MouseFilterEnum.Ignore,
        };
        _hint.SetAnchorsPreset(LayoutPreset.FullRect);
        _hint.AddThemeFontSizeOverride("font_size", 18);
        _hint.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.25f));
        parent.AddChild(_hint);
    }

    private Control BuildBottomBar()
    {
        var panelStyle = new StyleBoxFlat { BgColor = new Color(0.14f, 0.14f, 0.17f) };
        var panel      = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        // Three-column layout: mirror spacer | palette (centered) | config panel
        var layout = new HBoxContainer();
        layout.AddThemeConstantOverride("separation", 0);
        panel.AddChild(layout);

        layout.AddChild(new Control { CustomMinimumSize = new Vector2(ConfigWidth + 8, 0) }); // mirror

        var center = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        layout.AddChild(center);

        layout.AddChild(BuildConfigPanel());

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
            vbox.Alignment   = BoxContainer.AlignmentMode.Center;
            vbox.MouseFilter = MouseFilterEnum.Ignore;
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

    private Control BuildConfigPanel()
    {
        var box = new VBoxContainer();
        box.CustomMinimumSize = new Vector2(ConfigWidth, 0);
        box.AddThemeConstantOverride("separation", 6);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top",    8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        margin.AddThemeConstantOverride("margin_left",   8);
        margin.AddThemeConstantOverride("margin_right",  8);
        margin.AddChild(box);

        var brushRow = new VBoxContainer();
        brushRow.AddThemeConstantOverride("separation", 4);
        box.AddChild(brushRow);

        var brushTitle = new Label { Text = "Brush Size" };
        brushTitle.AddThemeFontSizeOverride("font_size", 11);
        brushTitle.AddThemeColorOverride("font_color", new Color(0.60f, 0.60f, 0.65f));
        brushRow.AddChild(brushTitle);

        var brushControls = new HBoxContainer();
        brushControls.AddThemeConstantOverride("separation", 4);
        brushRow.AddChild(brushControls);

        var minusBtn = new Button { Text = "−", CustomMinimumSize = new Vector2(30, 28) };
        minusBtn.AddThemeFontSizeOverride("font_size", 16);
        minusBtn.Pressed += () => AdjustBrush(-1);
        brushControls.AddChild(minusBtn);

        _brushLabel = new Label
        {
            Text                = "0",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _brushLabel.AddThemeFontSizeOverride("font_size", 15);
        brushControls.AddChild(_brushLabel);

        var plusBtn = new Button { Text = "+", CustomMinimumSize = new Vector2(30, 28) };
        plusBtn.AddThemeFontSizeOverride("font_size", 16);
        plusBtn.Pressed += () => AdjustBrush(+1);
        brushControls.AddChild(plusBtn);

        return margin;
    }

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
            BgColor              = InactiveTabBg,
            CornerRadiusTopLeft  = 3,
            CornerRadiusTopRight = 3,
        };
        var hover = new StyleBoxFlat
        {
            BgColor              = new Color(0.20f, 0.20f, 0.26f),
            CornerRadiusTopLeft  = 3,
            CornerRadiusTopRight = 3,
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
