using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slide;

public partial class Editor
{
    private void ConfirmDiscardIfDirty(Action callback)
    {
        if (!IsDirty) { callback(); return; }

        var dlg = new ConfirmationDialog
        {
            Title            = "Unsaved Changes",
            DialogText       = "You have unsaved changes. Discard them?",
            OkButtonText     = "Discard",
            CancelButtonText = "Cancel",
        };
        dlg.Confirmed += () => { callback(); dlg.QueueFree(); };
        dlg.Canceled  += () => dlg.QueueFree();
        AddChild(dlg);
        dlg.PopupCentered();
    }

    private void OnNew() => ConfirmDiscardIfDirty(ShowNewLevelDialog);

    private void ShowNewLevelDialog()
    {
        var dlg = new ConfirmationDialog
        {
            Title        = "New Level",
            OkButtonText = "Create",
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        dlg.AddChild(vbox);

        void AddRow(string labelText, SpinBox spin, out SpinBox result)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var lbl = new Label { Text = labelText, CustomMinimumSize = new Vector2(60, 0) };
            row.AddChild(lbl); row.AddChild(spin);
            vbox.AddChild(row);
            result = spin;
        }

        AddRow("Width",  new SpinBox { MinValue = 8, MaxValue = 2048, Step = 1, Value = 500 }, out var widthSpin);
        AddRow("Height", new SpinBox { MinValue = 8, MaxValue = 2048, Step = 1, Value = 500 }, out var heightSpin);

        var hint = new Label
        {
            Text         = "Player unit is ~10×10 px",
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        hint.AddThemeFontSizeOverride("font_size", 11);
        hint.AddThemeColorOverride("font_color", new Color(0.60f, 0.60f, 0.65f));
        vbox.AddChild(hint);

        dlg.Confirmed += () =>
        {
            CreateNewLevel((int)widthSpin.Value, (int)heightSpin.Value);
            dlg.QueueFree();
        };
        dlg.Canceled += () => dlg.QueueFree();
        AddChild(dlg);
        dlg.PopupCentered(new Vector2I(380, 140));
    }

    private void CreateNewLevel(int width, int height)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgb8);
        image.Fill(new Color(0.04f, 0.04f, 0.04f)); // void

        _levelData = new LevelData
        {
            Version = 1,
            Name    = "",
            Bitmap  = "",
        };
        _levelPath = "";
        _levelDir  = "";
        GameSetup.LastEditorLevelPath = "";

        _canvas.LoadImage(image);
        _hint.Visible  = false;
        _activeStroke  = null;
        _undoStack.Clear();
        ClearDirty();
        ClearSelection();
    }

    private void OnLevelSettings()
    {
        if (_levelData == null) return;
        var image = _canvas.GetImage();
        if (image == null) return;

        var dlg = new ConfirmationDialog { Title = "Level Settings", OkButtonText = "Apply" };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        dlg.AddChild(vbox);

        SpinBox AddSizeField(string labelText, int current)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(new Label { Text = labelText, CustomMinimumSize = new Vector2(100, 0) });
            var spin = new SpinBox { MinValue = 8, MaxValue = 2048, Step = 1, Value = current };
            row.AddChild(spin);
            vbox.AddChild(row);
            return spin;
        }

        var widthSpin  = AddSizeField("Width (px)",  image.GetWidth());
        var heightSpin = AddSizeField("Height (px)", image.GetHeight());

        var hint = new Label { Text = "Player unit is ~10×10 px" };
        hint.AddThemeFontSizeOverride("font_size", 11);
        hint.AddThemeColorOverride("font_color", new Color(0.60f, 0.60f, 0.65f));
        vbox.AddChild(hint);

        dlg.Confirmed += () =>
        {
            int newW = (int)widthSpin.Value;
            int newH = (int)heightSpin.Value;
            bool cropping = newW < image.GetWidth() || newH < image.GetHeight();

            if (cropping)
            {
                var warn = new ConfirmationDialog
                {
                    Title            = "Crop Map?",
                    DialogText       = "The new size is smaller than the current map. Pixels outside the new bounds will be lost. Continue?",
                    OkButtonText     = "Crop",
                    CancelButtonText = "Cancel",
                };
                warn.Confirmed += () => { ApplyResize(newW, newH); warn.QueueFree(); };
                warn.Canceled  += () => warn.QueueFree();
                AddChild(warn);
                warn.PopupCentered();
            }
            else
            {
                ApplyResize(newW, newH);
            }

            dlg.QueueFree();
        };
        dlg.Canceled += () => dlg.QueueFree();
        AddChild(dlg);
        dlg.PopupCentered(new Vector2I(420, 280));
    }

    private void ApplyResize(int newW, int newH)
    {
        var old = _canvas.GetImage()!;
        if (newW == old.GetWidth() && newH == old.GetHeight()) return;

        var before = (Image)old.Duplicate();

        var after = Image.CreateEmpty(newW, newH, false, Image.Format.Rgb8);
        after.Fill(new Color(0.04f, 0.04f, 0.04f));
        after.BlitRect(old, new Rect2I(0, 0, Mathf.Min(old.GetWidth(), newW), Mathf.Min(old.GetHeight(), newH)), Vector2I.Zero);

        _undoStack.Execute(new SimpleCommand(
            () => _canvas.LoadImage((Image)after.Duplicate()),
            () => _canvas.LoadImage((Image)before.Duplicate())
        ));
    }

    private void OnOpen() => ConfirmDiscardIfDirty(ShowOpenDialog);

    private void ShowOpenDialog()
    {
        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access   = FileDialog.AccessEnum.Filesystem,
            Filters  = ["*.json ; Level Files"],
        };
        dialog.FileSelected += path => { LoadLevelFile(path); dialog.QueueFree(); };
        dialog.Canceled     += () => dialog.QueueFree();
        AddChild(dialog);
        dialog.PopupCentered(new Vector2I(700, 500));
    }

    private void RestorePlaytestSnapshot()
    {
        var snap = GameSetup.PlaytestRestore!;
        GameSetup.PlaytestRestore = null;

        _levelData = snap.LevelData;
        _levelPath = snap.LevelPath;
        _levelDir  = snap.LevelPath.GetBaseDir();
        GameSetup.LastEditorLevelPath = snap.LevelPath;

        _canvas.LoadImage(snap.Image);
        _hint.Visible = false;
        _activeStroke = null;
        ClearSelection();
        RefreshOverlays();

        if (snap.WasDirty) { _playtestDirty = true; UpdateTitleLabel(); } else ClearDirty();
    }

    private void LoadLevelFile(string path)
    {
        GameSetup.LastEditorLevelPath = path;
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(path), opts)!;
        _levelData = data;
        _levelPath = path;
        _levelDir  = path.GetBaseDir();

        string pngPath = path.GetBaseName() + ".png";
        var    image   = Image.LoadFromFile(pngPath);
        _canvas.LoadImage(image);
        _hint.Visible  = false;
        _activeStroke  = null;
        _undoStack.Clear();
        ClearDirty();
        ClearSelection();
        RefreshOverlays();
    }

    private void OnSaveAs()
    {
        if (_levelData == null) return;

        var dialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access   = FileDialog.AccessEnum.Filesystem,
            Filters  = ["*.json ; Level Files"],
        };
        dialog.FileSelected += path =>
        {
            if (!path.EndsWith(".json")) path += ".json";
            _levelPath                    = path;
            _levelDir                     = path.GetBaseDir();
            _levelData.Bitmap             = path.GetFile().GetBaseName() + ".png";
            GameSetup.LastEditorLevelPath = path;
            OnSave();
            dialog.QueueFree();
        };
        dialog.Canceled += () => dialog.QueueFree();
        AddChild(dialog);
        dialog.PopupCentered(new Vector2I(700, 500));
    }

    private void OnSave()
    {
        if (_levelData == null || string.IsNullOrEmpty(_levelPath)) return;

        var image = _canvas.GetImage();
        if (image == null) return;

        string pngPath = _levelPath.GetBaseName() + ".png";
        var pngErr = image.SavePng(pngPath);
        if (pngErr != Error.Ok)
            GD.PrintErr($"PNG save failed: {pngErr}");

        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            WriteIndented          = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        File.WriteAllText(_levelPath, JsonSerializer.Serialize(_levelData, opts));
        ClearDirty();
    }

    private void OnPlay()
    {
        if (string.IsNullOrEmpty(_levelPath) || _levelData == null) return;

        var image = _canvas.GetImage();
        if (image == null) return;

        // Hold direct references — no disk writes, GameSetup survives scene changes.
        GameSetup.PlaytestRestore = new GameSetup.EditorSnapshot(_levelPath, _levelData, image, IsDirty);

        GameSetup.PlaytestPath = _levelPath;
        RunState.Reset();
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }
}
