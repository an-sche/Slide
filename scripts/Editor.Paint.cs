using Godot;

namespace Slide;

public partial class Editor
{
    private void OnPixelClicked(Vector2I px)
    {
        if (_mode != EditorMode.Paint) return;
        Color[] colors = SlotColors[(int)_mode];
        if (_selectedSlot >= colors.Length) return;
        _canvas.PaintBrush(px, colors[_selectedSlot]);
        SetDirty();
    }

    private void AdjustBrush(int delta)
    {
        _canvas.AdjustBrushRadius(delta);
        _brushLabel.Text = _canvas.BrushRadius.ToString();
    }
}
