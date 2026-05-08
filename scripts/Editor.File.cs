using Godot;
using System.Text.Json;

namespace Slide;

public partial class Editor
{
    private void OnNew() { } // TODO

    private void OnOpen()
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

    private void LoadLevelFile(string path)
    {
        GameSetup.LastEditorLevelPath = path;
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<LevelData>(FileAccess.GetFileAsString(path), opts)!;
        _levelData       = data;
        _levelPath       = path;
        _levelDir        = path.GetBaseDir();
        _titleLabel.Text = string.IsNullOrEmpty(data.Name) ? "(unnamed)" : data.Name;

        string pngPath = path.GetBaseName() + ".png";
        var    image   = Image.LoadFromFile(pngPath);
        _canvas.LoadImage(image);
        _hint.Visible = false;
        RefreshOverlays();
    }

    private void OnSave()
    {
        if (_levelData == null || string.IsNullOrEmpty(_levelPath)) return;

        var image = _canvas.GetImage();
        if (image == null) return;

        string pngPath = _levelPath.GetBaseName() + ".png";
        var err = image.SavePng(pngPath);
        if (err != Error.Ok)
            GD.PrintErr($"Save failed: {err}");
    }

    private void OnPlay()
    {
        if (string.IsNullOrEmpty(_levelPath)) return;

        // Auto-save the PNG so the World loads the current painted state.
        _canvas.GetImage()?.SavePng(_levelPath.GetBaseName() + ".png");

        GameSetup.PlaytestPath = _levelPath;
        RunState.Reset();
        GetTree().ChangeSceneToFile("res://scenes/World.tscn");
    }
}
