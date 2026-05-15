using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private Dictionary<Vector2I, (Color Before, Color After)>? _activeStroke;

    private void OnPixelClicked(Vector2I px)
    {
        if (_mode != EditorMode.Paint) return;
        Color[] colors = SlotColors[(int)_mode];
        if (_selectedSlot >= colors.Length) return;

        Color paintColor = colors[_selectedSlot];
        _activeStroke ??= new Dictionary<Vector2I, (Color, Color)>();

        foreach (var pixel in _canvas.BrushPixels(px))
        {
            if (!_activeStroke.ContainsKey(pixel))
                _activeStroke[pixel] = (_canvas.GetPixel(pixel), paintColor);
            else
                _activeStroke[pixel] = (_activeStroke[pixel].Before, paintColor);
            _canvas.SetPixel(pixel, paintColor);
        }
        _canvas.FlushTexture();
    }

    private void EndStroke()
    {
        if (_activeStroke == null || _activeStroke.Count == 0) { _activeStroke = null; return; }

        var stroke = _activeStroke;
        _activeStroke = null;

        bool anyChange = false;
        foreach (var kvp in stroke)
            if (kvp.Value.Before != kvp.Value.After) { anyChange = true; break; }
        if (!anyChange) return;

        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { foreach (var kvp in stroke) _canvas.SetPixel(kvp.Key, kvp.Value.After);  _canvas.FlushTexture(); },
            () => { foreach (var kvp in stroke) _canvas.SetPixel(kvp.Key, kvp.Value.Before); _canvas.FlushTexture(); }
        ));
    }

    private void AdjustBrush(int delta)
    {
        _canvas.AdjustBrushRadius(delta);
        _brushLabel.Text = _canvas.BrushRadius.ToString();
    }
}
