using Godot;

namespace Slide;

public partial class Editor
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
            new Color(0.18f, 0.80f, 0.28f),
            new Color(1.00f, 0.85f, 0.00f),
            new Color(1.00f, 0.88f, 0.20f),
        ],
        [
            new Color(0.88f, 0.28f, 0.18f),
            new Color(0.88f, 0.18f, 0.48f),
            new Color(0.28f, 0.80f, 0.48f),
            new Color(0.80f, 0.48f, 0.08f),
            new Color(0.18f, 0.58f, 0.90f),
            new Color(0.90f, 0.80f, 0.08f),
            new Color(0.68f, 0.18f, 0.90f),
        ],
        [
            new Color(0.18f, 0.78f, 0.78f),
            new Color(0.58f, 0.58f, 0.68f),
        ],
    ];

    private static readonly Color ActiveTabBg      = new(0.22f, 0.22f, 0.30f);
    private static readonly Color InactiveTabBg    = new(0.12f, 0.12f, 0.15f);
    private static readonly Color SelectedBorder   = new(1.00f, 0.85f, 0.00f);
    private static readonly Color UnselectedBorder = new(0.32f, 0.32f, 0.38f);

    private void SetMode(EditorMode mode)
    {
        FinalizePlacementSilent();
        _mode = mode;

        _brushSection.Visible = mode == EditorMode.Paint;
        ClearSelection();

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
}
