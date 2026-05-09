using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void OnPixelLeftPressed(Vector2I px)
    {
        if (_mode != EditorMode.Entities || _levelData == null) return;

        float cellSize = GameplayConstants.CellSize;
        var   world    = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);

        string kind = _selectedSlot switch
        {
            0 => "start",
            1 => "end",
            2 => "bonus",
            _ => "",
        };
        if (string.IsNullOrEmpty(kind)) return;

        var entities = new List<EntityData>(_levelData.Entities);

        if (kind is "start" or "end")
            entities.RemoveAll(e => e.Kind == kind);

        entities.Add(new EntityData { Kind = kind, X = world.X, Y = world.Y });
        _levelData.Entities = [.. entities];

        RefreshOverlays();
        SetDirty();
    }
}
