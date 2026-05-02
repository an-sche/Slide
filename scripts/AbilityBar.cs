using Godot;

namespace Slide;

public partial class AbilityBar : Control
{
    private static readonly (string Key, string Name, bool Advanced, int MaxLevel)[] Slots =
    {
        ("Q", "Boost",    false, 4),
        ("W", "Warp",     true,  4),
        ("E", "Donut",    true,  4),
        ("R", "Ethereal", true,  4),
        ("F", "Gack",     false, 1),
    };

    private const float SlotWidth    = 64f;
    private const float SlotHeight   = 88f;
    private const float Spacing      = 8f;
    private const float BottomMargin = 14f;
    private const float PlusBtnH     = 24f;
    private const float PlusGap      = 4f;
    private const int   UnlockLevel  = 3;

    private readonly StyleBoxFlat[] _styles          = new StyleBoxFlat[5];
    private readonly Label[]        _keyLabels        = new Label[5];
    private readonly Label[]        _nameLabels       = new Label[5];
    private readonly Label[]        _dotLabels        = new Label[5];
    private readonly Button[]       _plusBtns         = new Button[5];
    private readonly ColorRect[]    _cooldownOverlays = new ColorRect[5];

    public override void _Ready()
    {
        float totalWidth  = Slots.Length * SlotWidth + (Slots.Length - 1) * Spacing;
        float totalHeight = PlusBtnH + PlusGap + SlotHeight;

        SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        OffsetLeft   = -totalWidth / 2f;
        OffsetRight  =  totalWidth / 2f;
        OffsetBottom = -BottomMargin;
        OffsetTop    = -(totalHeight + BottomMargin);

        for (int i = 0; i < Slots.Length; i++)
        {
            float x = i * (SlotWidth + Spacing);
            BuildPlusButton(i, x);
            BuildSlot(i, x);
        }

        SetLevel(RunState.PlayerLevel);
        for (int i = 0; i < Slots.Length; i++)
            UpdateSlotState(i, 0f, false);
    }

    public void SetLevel(int level)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            bool locked = Slots[i].Advanced && level < UnlockLevel;
            _keyLabels[i].AddThemeColorOverride("font_color",  locked ? LockedKeyColor  : UnlockedKeyColor);
            _nameLabels[i].AddThemeColorOverride("font_color", locked ? LockedNameColor : UnlockedNameColor);
            _dotLabels[i].AddThemeColorOverride("font_color",  locked ? LockedNameColor : UnlockedNameColor);
        }
        UpdatePlusButtons();
    }

    public void UpdateSlotState(int index, float cooldownFraction, bool active)
    {
        _cooldownOverlays[index].OffsetBottom = SlotHeight * cooldownFraction;

        Color border;
        if (active)
            border = ActiveBorderColor;
        else if (cooldownFraction > 0f)
            border = CooldownBorderColor;
        else if (Slots[index].Advanced && RunState.PlayerLevel < UnlockLevel)
            border = LockedBorderColor;
        else
            border = UnlockedBorderColor;

        _styles[index].BorderColor = border;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (!key.CtrlPressed) return;

        int index = key.Keycode switch
        {
            Key.Q => 0,
            Key.W => 1,
            Key.E => 2,
            Key.R => 3,
            Key.F => 4,
            _ => -1,
        };

        if (index < 0) return;
        TryUpgrade(index);
        GetViewport().SetInputAsHandled();
    }

    private void TryUpgrade(int i)
    {
        if (RunState.AvailablePoints <= 0) return;
        if (RunState.AbilityLevels[i] >= Slots[i].MaxLevel) return;
        if (Slots[i].Advanced && RunState.PlayerLevel < UnlockLevel) return;

        RunState.AbilityLevels[i]++;
        UpdateDots(i);
        UpdatePlusButtons();
    }

    private void UpdatePlusButtons()
    {
        int available = RunState.AvailablePoints;
        for (int i = 0; i < Slots.Length; i++)
        {
            _plusBtns[i].Visible = available > 0
                && RunState.AbilityLevels[i] < Slots[i].MaxLevel
                && (!Slots[i].Advanced || RunState.PlayerLevel >= UnlockLevel);
        }
    }

    private void UpdateDots(int i)
    {
        int level    = RunState.AbilityLevels[i];
        int maxLevel = Slots[i].MaxLevel;
        _dotLabels[i].Text = new string('●', level) + new string('○', maxLevel - level);
    }

    private void BuildPlusButton(int i, float x)
    {
        var normal = new StyleBoxFlat
        {
            BgColor              = new Color(0.1f, 0.35f, 0.1f, 0.9f),
            BorderWidthTop       = 1, BorderWidthBottom = 1,
            BorderWidthLeft      = 1, BorderWidthRight  = 1,
            BorderColor          = Colors.LimeGreen,
            CornerRadiusTopLeft  = 3, CornerRadiusTopRight    = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };
        var hover = new StyleBoxFlat
        {
            BgColor              = new Color(0.2f, 0.5f, 0.2f, 0.9f),
            BorderWidthTop       = 1, BorderWidthBottom = 1,
            BorderWidthLeft      = 1, BorderWidthRight  = 1,
            BorderColor          = Colors.LimeGreen,
            CornerRadiusTopLeft  = 3, CornerRadiusTopRight    = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };

        var btn = new Button { Text = "+" };
        btn.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        btn.OffsetLeft   = x;
        btn.OffsetRight  = x + SlotWidth;
        btn.OffsetTop    = 0f;
        btn.OffsetBottom = PlusBtnH;
        btn.Visible      = false;
        btn.AddThemeStyleboxOverride("normal",  normal);
        btn.AddThemeStyleboxOverride("hover",   hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeColorOverride("font_color", Colors.LimeGreen);
        btn.AddThemeFontSizeOverride("font_size", 16);

        int captured = i;
        btn.Pressed += () => TryUpgrade(captured);

        _plusBtns[i] = btn;
        AddChild(btn);
    }

    private void BuildSlot(int i, float x)
    {
        float slotY = PlusBtnH + PlusGap;

        var style = new StyleBoxFlat
        {
            BgColor                = new Color(0.1f, 0.1f, 0.1f, 0.88f),
            BorderWidthTop         = 2, BorderWidthBottom = 2,
            BorderWidthLeft        = 2, BorderWidthRight  = 2,
            CornerRadiusTopLeft    = 4, CornerRadiusTopRight    = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        _styles[i] = style;

        var panel = new Panel();
        panel.AddThemeStyleboxOverride("panel", style);
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.OffsetLeft   = x;
        panel.OffsetRight  = x + SlotWidth;
        panel.OffsetTop    = slotY;
        panel.OffsetBottom = slotY + SlotHeight;
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        var keyLabel = new Label { Text = Slots[i].Key, HorizontalAlignment = HorizontalAlignment.Center };
        keyLabel.AddThemeFontSizeOverride("font_size", 22);
        keyLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        keyLabel.AddThemeConstantOverride("outline_size", 3);
        vbox.AddChild(keyLabel);
        _keyLabels[i] = keyLabel;

        var nameLabel = new Label { Text = Slots[i].Name, HorizontalAlignment = HorizontalAlignment.Center };
        nameLabel.AddThemeFontSizeOverride("font_size", 11);
        nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        nameLabel.AddThemeConstantOverride("outline_size", 2);
        vbox.AddChild(nameLabel);
        _nameLabels[i] = nameLabel;

        var dotLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        dotLabel.AddThemeFontSizeOverride("font_size", 9);
        dotLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        dotLabel.AddThemeConstantOverride("outline_size", 1);
        vbox.AddChild(dotLabel);
        _dotLabels[i] = dotLabel;

        UpdateDots(i);

        var overlay = new ColorRect { Color = new Color(0f, 0f, 0f, 0.72f) };
        overlay.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        overlay.OffsetLeft   = 0f;
        overlay.OffsetRight  = SlotWidth;
        overlay.OffsetTop    = 0f;
        overlay.OffsetBottom = 0f;
        overlay.MouseFilter  = Control.MouseFilterEnum.Ignore;
        panel.AddChild(overlay);
        _cooldownOverlays[i] = overlay;
    }

    private static readonly Color ActiveBorderColor   = new(1f, 0.8f, 0f);
    private static readonly Color CooldownBorderColor = new(0.25f, 0.25f, 0.25f);

    private static readonly Color UnlockedBorderColor = new(0.65f, 0.65f, 0.65f);
    private static readonly Color UnlockedKeyColor    = Colors.White;
    private static readonly Color UnlockedNameColor   = new(0.78f, 0.78f, 0.78f);

    private static readonly Color LockedBorderColor   = new(0.35f, 0.35f, 0.35f);
    private static readonly Color LockedKeyColor      = new(0.45f, 0.45f, 0.45f);
    private static readonly Color LockedNameColor     = new(0.38f, 0.38f, 0.38f);
}
