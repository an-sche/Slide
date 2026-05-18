using Godot;

namespace Slide;

public partial class Editor
{
    private enum EditorMode { Paint, Entities, Enemies, Triggers }

    private static readonly string[][] SlotLabels =
    [
        ["Ground", "Slidy", "Fast", "Confusing", "FastConf", "Straight", "Kill", "Void"],
        ["Start", "End", "Bonus", "Wall", "Portal"],
        ["Patrol", "Wander", "Orbiter"],
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
            new Color(0.55f, 0.55f, 0.75f),
            new Color(0.60f, 0.20f, 1.00f),
        ],
        [
            new Color(0.88f, 0.28f, 0.18f),
            new Color(0.88f, 0.18f, 0.48f),
            new Color(0.28f, 0.80f, 0.48f),
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

        bool isPaint = mode == EditorMode.Paint;
        _brushSection.Visible  = isPaint;
        _canvas.BrushRadius    = isPaint ? 0 : -1;
        if (isPaint) _brushLabel.Text = "0";
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

    // Like SetMode but skips FinalizePlacementSilent/ClearSelection — safe to call from Select().
    private void SwitchModeTab(EditorMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;

        _brushSection.Visible = mode == EditorMode.Paint;
        _canvas.BrushRadius   = mode == EditorMode.Paint ? 0 : -1;

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

        _placementArmed = false;
        _selectedSlot   = 0;
        RefreshSlotBorders();
    }

    private void ArmPlacement()
    {
        if (_mode is not (EditorMode.Entities or EditorMode.Enemies)) return;
        if (_placementMode != EnemyPlacementMode.None)
            FinalizePlacement();
        _placementArmed = true;
        RefreshSlotBorders();
    }

    private void SelectSlot(int index)
    {
        if (index >= SlotLabels[(int)_mode].Length) return;
        _selectedSlot = index;
        RefreshSlotBorders();
    }

    private void RefreshSlotBorders()
    {
        bool entityMode = _mode is EditorMode.Entities or EditorMode.Enemies;
        for (int i = 0; i < 8; i++)
        {
            bool armed = i == _selectedSlot && (!entityMode || _placementArmed);
            _slotStyles[i].BorderColor = armed ? SelectedBorder : UnselectedBorder;
        }
    }
}
