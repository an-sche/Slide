using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void OnPixelRightClicked(Vector2I px, Vector2 screenPos)
    {
        if (_mode is not (EditorMode.Entities or EditorMode.Enemies) || _levelData == null) return;

        float cellSize    = GameplayConstants.CellSize;
        var   world       = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);
        float pickRadiusSq = (cellSize * 4f) * (cellSize * 4f);

        var candidates = new System.Collections.Generic.List<(int OverlayIndex, string Label)>();

        for (int i = 0; i < _levelData.Entities.Length; i++)
        {
            var e = _levelData.Entities[i];
            float d = new Vector2(e.X, e.Y).DistanceSquaredTo(world);
            if (d > pickRadiusSq) continue;
            string kind = e.Kind switch { "start" => "Start", "end" => "End", "bonus" => "Bonus", var k => k };
            string label = string.IsNullOrEmpty(e.Name)
                ? $"{kind} ({(int)(e.X / cellSize)}, {(int)(e.Y / cellSize)})"
                : $"{kind} - {e.Name}";
            candidates.Add((i, label));
        }

        for (int i = 0; i < _levelData.Enemies.Length; i++)
        {
            var e   = _levelData.Enemies[i];
            var pos = e.Behavior.Type switch
            {
                "patrol"  => e.Behavior.Waypoints?.Length > 0
                                 ? new Vector2(e.Behavior.Waypoints[0].X, e.Behavior.Waypoints[0].Y)
                                 : Vector2.Zero,
                "orbiter" => new Vector2(e.Behavior.CenterX, e.Behavior.CenterY),
                "wander"  => e.Behavior.StartX.HasValue
                                 ? new Vector2(e.Behavior.StartX.Value, e.Behavior.StartY!.Value)
                                 : (e.Behavior.Polygon?.Length > 0
                                        ? new Vector2(e.Behavior.Polygon[0].X, e.Behavior.Polygon[0].Y)
                                        : Vector2.Zero),
                _         => Vector2.Zero,
            };
            float d = pos.DistanceSquaredTo(world);
            if (d > pickRadiusSq) continue;
            string t     = e.Behavior.Type;
            string kind  = char.ToUpper(t[0]) + t[1..];
            string label = string.IsNullOrEmpty(e.Name)
                ? $"{kind} ({(int)(pos.X / cellSize)}, {(int)(pos.Y / cellSize)})"
                : $"{kind} - {e.Name}";
            candidates.Add((_levelData.Entities.Length + i, label));
        }

        if (candidates.Count == 0)
        {
            ClearSelection();
            return;
        }

        if (candidates.Count == 1)
        {
            Select(candidates[0].OverlayIndex);
            return;
        }

        ShowPickPopup(candidates, screenPos);
    }

    private void Select(int overlayIndex)
    {
        _selectedIndex = overlayIndex;
        RefreshOverlays();
        SyncNameField();
    }

    private void ShowPickPopup(System.Collections.Generic.List<(int OverlayIndex, string Label)> candidates, Vector2 screenPos)
    {
        var popup = new PopupMenu();
        foreach (var (_, label) in candidates)
            popup.AddItem(label);

        popup.IndexPressed += idx =>
        {
            Select(candidates[(int)idx].OverlayIndex);
            popup.QueueFree();
        };
        popup.PopupHide += () => popup.QueueFree();

        AddChild(popup);
        popup.Position = (Vector2I)screenPos;
        popup.Popup();
    }

    private void OnSelectionNameChanged(string value)
    {
        if (_selectedIndex < 0 || _levelData == null) return;
        string? name = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (_selectedIndex < _levelData.Entities.Length)
            _levelData.Entities[_selectedIndex].Name = name;
        else
            _levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Name = name;

        // Update the kind label live without rebuilding the whole panel.
        string kind;
        if (_selectedIndex < _levelData.Entities.Length)
        {
            kind = _levelData.Entities[_selectedIndex].Kind switch
            {
                "start" => "Start", "end" => "End", "bonus" => "Bonus", var k => k,
            };
        }
        else
        {
            string t = _levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Behavior.Type;
            kind = char.ToUpper(t[0]) + t[1..];
        }

        _selectionKindLabel.Text = string.IsNullOrEmpty(name) ? kind : $"{kind} - {name}";
        RefreshOverlays();
        SetDirty();
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _levelData == null) return;

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var list = new System.Collections.Generic.List<EntityData>(_levelData.Entities);
            list.RemoveAt(_selectedIndex);
            _levelData.Entities = [.. list];
        }
        else
        {
            int ei   = _selectedIndex - _levelData.Entities.Length;
            var list = new System.Collections.Generic.List<EnemyData>(_levelData.Enemies);
            list.RemoveAt(ei);
            _levelData.Enemies = [.. list];
        }

        _selectedIndex = -1;
        RefreshOverlays();
        SetDirty();
    }

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
