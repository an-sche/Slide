using Godot;

namespace Slide;

public partial class Editor : Control
{
    private EditorMode _mode         = EditorMode.Paint;
    private int        _selectedSlot  = 0;
    private int        _selectedIndex = -1;

    private readonly Button[]       _modeTabs      = new Button[4];
    private readonly StyleBoxFlat[] _modeTabStyles = new StyleBoxFlat[4];
    private readonly Control[]      _slotRoots     = new Control[8];
    private readonly StyleBoxFlat[] _slotStyles    = new StyleBoxFlat[8];
    private readonly ColorRect[]    _slotSwatches  = new ColorRect[8];
    private readonly Label[]        _slotNames     = new Label[8];

    private CanvasView    _canvas             = null!;
    private Button        _undoBtn            = null!;
    private Button        _redoBtn            = null!;
    private Label         _hint               = null!;
    private Label         _titleLabel         = null!;
    private Label         _brushLabel         = null!;
    private VBoxContainer _optionsPanelContent  = null!;
    private Control       _brushSection         = null!;
    private Control       _selectionSection     = null!;
    private Label         _selectionHint        = null!;
    private Control       _selectionDetails     = null!;
    private Label         _selectionKindLabel   = null!;
    private LineEdit      _selectionXEdit       = null!;
    private LineEdit      _selectionYEdit       = null!;
    private LineEdit      _selectionNameEdit    = null!;
    private bool          _syncingFields;
    private bool          _placementArmed;
    private bool          _editingPosition;
    private Godot.Vector2 _posEditStart;
    private bool          _editingName;
    private string?       _nameEditStart;
    private VBoxContainer _behaviorConfigContainer = null!;
    private EnemyPlacementMode _placementMode   = EnemyPlacementMode.None;
    private EnemyData?    _placementTarget;
    private LevelData? _levelData;
    private string     _levelDir  = "";
    private string     _levelPath = "";
    // Only set when restoring from a playtest snapshot that was dirty — not from undo operations.
    private bool      _playtestDirty;
    private UndoStack _undoStack = null!;

    private bool IsDirty => _playtestDirty || _undoStack.IsModified;

    private void ClearDirty()
    {
        _playtestDirty = false;
        _undoStack.MarkSavePoint();
        UpdateTitleLabel();
    }

    private void UpdateTitleLabel()
    {
        string name = _levelData?.Name ?? "";
        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(_levelPath))
            name = _levelPath.GetFile().GetBaseName();
        if (string.IsNullOrEmpty(name))
            name = "(unnamed)";
        _titleLabel.Text    = IsDirty ? name + " *" : name;
        _undoBtn.Disabled   = !_undoStack.CanUndo;
        _redoBtn.Disabled   = !_undoStack.CanRedo;
    }

    public override void _Ready()
    {
        _undoStack = new UndoStack(UpdateTitleLabel);
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

        vbox.AddChild(BuildMiddleRow());

        vbox.AddChild(BuildBottomBar());

        SetMode(EditorMode.Paint);

        if (GameSetup.PlaytestRestore != null)
            RestorePlaytestSnapshot();
        else if (!string.IsNullOrEmpty(GameSetup.LastEditorLevelPath))
            LoadLevelFile(GameSetup.LastEditorLevelPath);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (key.CtrlPressed && key.Keycode == Key.Z && !key.ShiftPressed)
        {
            _undoStack.Undo();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (key.CtrlPressed && key.Keycode == Key.Y)
        {
            _undoStack.Redo();
            GetViewport().SetInputAsHandled();
            return;
        }

        int slot = key.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3,
            Key.Key5 => 4, Key.Key6 => 5, Key.Key7 => 6, Key.Key8 => 7,
            _ => -1,
        };
        if (slot >= 0) { SelectSlot(slot); ArmPlacement(); GetViewport().SetInputAsHandled(); return; }

        if (key.Keycode == Key.Bracketleft)  { AdjustBrush(-1); GetViewport().SetInputAsHandled(); return; }
        if (key.Keycode == Key.Bracketright) { AdjustBrush(+1); GetViewport().SetInputAsHandled(); return; }

        if (key.Keycode == Key.Enter && _placementMode != EnemyPlacementMode.None)
        {
            FinalizePlacement();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Delete && _selectedIndex >= 0)
        {
            DeleteSelected();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Tab)
        {
            SetMode((EditorMode)(((int)_mode + 1) % 4));
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Escape)
        {
            if (_placementMode != EnemyPlacementMode.None)
            {
                CancelPlacement();
                GetViewport().SetInputAsHandled();
                return;
            }
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            GetViewport().SetInputAsHandled();
        }
    }
}
