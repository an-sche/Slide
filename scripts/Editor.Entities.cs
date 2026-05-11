using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void OnPixelRightClicked(Vector2I px, Vector2 screenPos)
    {
        if (_placementMode != EnemyPlacementMode.None) return;
        if (_mode is not (EditorMode.Entities or EditorMode.Enemies) || _levelData == null) return;

        float cellSize    = GameplayConstants.CellSize;
        var   world       = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);
        float pickRadiusSq = (cellSize * 4f) * (cellSize * 4f);

        var candidates = new System.Collections.Generic.List<(int OverlayIndex, string Label)>();

        for (int i = 0; i < _levelData.Entities.Length; i++)
        {
            var   e    = _levelData.Entities[i];
            float d    = new Vector2(e.X, e.Y).DistanceSquaredTo(world);
            if (d > pickRadiusSq) continue;
            string kind  = EntityKindLabel(e.Kind);
            string label = string.IsNullOrEmpty(e.Name)
                ? $"{kind} ({(int)(e.X / cellSize)}, {(int)(e.Y / cellSize)})"
                : $"{kind} - {e.Name}";
            candidates.Add((i, label));
        }

        for (int i = 0; i < _levelData.Enemies.Length; i++)
        {
            var    e    = _levelData.Enemies[i];
            var    pos  = EnemyOrigin(e.Behavior);
            float  d    = pos.DistanceSquaredTo(world);
            if (d > pickRadiusSq) continue;
            string kind  = EnemyKindLabel(e.Behavior);
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

        popup.IndexPressed += idx => Select(candidates[(int)idx].OverlayIndex);
        popup.PopupHide    += () => popup.QueueFree();

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

        string kind = _selectedIndex < _levelData.Entities.Length
            ? EntityKindLabel(_levelData.Entities[_selectedIndex].Kind)
            : EnemyKindLabel(_levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Behavior);

        _selectionKindLabel.Text = string.IsNullOrEmpty(name) ? kind : $"{kind} - {name}";
        RefreshOverlays();
        SetDirty();
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _levelData == null) return;

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var list = new List<EntityData>(_levelData.Entities);
            list.RemoveAt(_selectedIndex);
            _levelData.Entities = [.. list];
        }
        else
        {
            var list = new List<EnemyData>(_levelData.Enemies);
            list.RemoveAt(_selectedIndex - _levelData.Entities.Length);
            _levelData.Enemies = [.. list];
        }

        SetDirty();
        ClearSelection();
    }

    private void OnPixelLeftPressed(Vector2I px)
    {
        if (_levelData == null) return;
        if (_mode == EditorMode.Enemies) { OnEnemyLeftPressed(px); return; }
        if (_mode != EditorMode.Entities || !_placementArmed) return;

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
        _placementArmed = false;

        RefreshOverlays();
        SetDirty();
    }

    private void SyncPositionFields()
    {
        if (_selectedIndex < 0 || _levelData == null)
            return;

        float   cellSize = GameplayConstants.CellSize;
        Vector2 pos      = _selectedIndex < _levelData.Entities.Length
            ? new Vector2(_levelData.Entities[_selectedIndex].X, _levelData.Entities[_selectedIndex].Y)
            : EnemyOrigin(_levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Behavior);

        _syncingFields       = true;
        _selectionXEdit.Text = ((int)(pos.X / cellSize)).ToString();
        _selectionYEdit.Text = ((int)(pos.Y / cellSize)).ToString();
        _syncingFields       = false;
    }

    private void OnSelectionPositionChanged()
    {
        if (_syncingFields || _selectedIndex < 0 || _levelData == null) return;
        if (!int.TryParse(_selectionXEdit.Text, out int tx)) return;
        if (!int.TryParse(_selectionYEdit.Text, out int ty)) return;

        float   cellSize = GameplayConstants.CellSize;
        Vector2 world    = new Vector2((tx + 0.5f) * cellSize, (ty + 0.5f) * cellSize);

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var e = _levelData.Entities[_selectedIndex];
            e.X = world.X;
            e.Y = world.Y;
        }
        else
        {
            SetEnemyOrigin(_levelData.Enemies[_selectedIndex - _levelData.Entities.Length], world);
        }

        RefreshCanvasOverlays();
        SetDirty();
    }

    private static void SetEnemyOrigin(EnemyData enemy, Vector2 world)
    {
        switch (enemy.Behavior)
        {
            case PatrolBehaviorData  p when p.Waypoints.Length > 0: p.Waypoints[0].X = world.X; p.Waypoints[0].Y = world.Y; break;
            case OrbiterBehaviorData o: o.CenterX = world.X; o.CenterY = world.Y; break;
            case WanderBehaviorData  w when w.StartX.HasValue: w.StartX = world.X; w.StartY = world.Y; break;
            case WanderBehaviorData  w when w.Polygon.Length > 0: w.Polygon[0].X = world.X; w.Polygon[0].Y = world.Y; break;
        }
    }
}
