using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void OnPixelRightClicked(Vector2I px, Vector2 screenPos)
    {
        if (_placementMode != EnemyPlacementMode.None) return;
        if (_levelData == null) return;

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
        if (_levelData != null)
        {
            EditorMode target = overlayIndex < _levelData.Entities.Length
                ? EditorMode.Entities
                : EditorMode.Enemies;
            SwitchModeTab(target);
        }
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
        ApplyNameToSelected(value);
        RefreshOverlays();
    }

    private void OnNameEditFocusEntered()
    {
        if (_editingName) return;
        _nameEditStart = GetSelectedName();
        _editingName   = true;
    }

    private void OnNameEditFocusExited()
    {
        _editingName = false;
        if (_selectedIndex < 0 || _levelData == null) return;

        string? finalName = GetSelectedName();
        if (finalName == _nameEditStart) return;

        string? startName = _nameEditStart;
        int     idx       = _selectedIndex;
        // Name already applied live via OnSelectionNameChanged — just track for undo.
        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { ApplyNameAt(idx, finalName); RefreshOverlays(); SyncNameField(); },
            () => { ApplyNameAt(idx, startName); RefreshOverlays(); SyncNameField(); }
        ));
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _levelData == null) return;

        // Initial placement in progress → Delete cancels (discards partial enemy).
        if (_enemyPlacementSnapshot != null)
        {
            CancelPlacement();
            return;
        }

        // Any other active mode → clear it before proceeding with normal deletion.
        if (_placementMode != EnemyPlacementMode.None)
            FinalizePlacementSilent();

        var entitySnapshot = _levelData.Entities;
        var enemySnapshot  = _levelData.Enemies;
        int restoreIdx     = _selectedIndex;

        EntityData[]? newEntities = null;
        EnemyData[]?  newEnemies  = null;

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var list = new List<EntityData>(_levelData.Entities);
            list.RemoveAt(_selectedIndex);
            newEntities = [.. list];
        }
        else
        {
            var list = new List<EnemyData>(_levelData.Enemies);
            list.RemoveAt(_selectedIndex - _levelData.Entities.Length);
            newEnemies = [.. list];
        }

        _undoStack.Execute(new SimpleCommand(
            () =>
            {
                if (newEntities != null) _levelData.Entities = newEntities;
                if (newEnemies  != null) _levelData.Enemies  = newEnemies;
                ClearSelection();
            },
            () =>
            {
                _levelData.Entities = entitySnapshot;
                _levelData.Enemies  = enemySnapshot;
                Select(restoreIdx);
            }
        ));
    }

    private void OnPixelLeftPressed(Vector2I px)
    {
        if (_levelData == null) return;
        // Route to enemy handler if in Enemies mode OR if an enemy placement is already active
        // (the enemy panel is accessible from Entities mode too, so placement can start there).
        if (_mode == EditorMode.Enemies || _placementMode != EnemyPlacementMode.None)
        {
            OnEnemyLeftPressed(px);
            return;
        }
        if (_mode != EditorMode.Entities || !_placementArmed) return;

        float cellSize = GameplayConstants.CellSize;
        var   world    = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);

        if (_selectedSlot == 4)
        {
            BeginPortalPlacement(world);
            return;
        }

        string kind = _selectedSlot switch
        {
            0 => "start",
            1 => "end",
            2 => "bonus",
            3 => "wall",
            _ => "",
        };
        if (string.IsNullOrEmpty(kind)) return;

        var before   = _levelData.Entities;
        var entities = new List<EntityData>(before);
        if (kind is "start" or "end")
            entities.RemoveAll(e => e.Kind == kind);
        entities.Add(new EntityData { Kind = kind, X = world.X, Y = world.Y });
        var after  = entities.ToArray();
        int selIdx = after.Length - 1;

        _undoStack.Execute(new SimpleCommand(
            () => { _levelData.Entities = after;  Select(selIdx);   },
            () => { _levelData.Entities = before; ClearSelection(); }
        ));

        _placementArmed = false;
        RefreshSlotBorders();
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

    private void OnPositionFieldFocusEntered()
    {
        if (_editingPosition) return;
        _posEditStart    = GetSelectedWorldPos();
        _editingPosition = true;
    }

    private void OnPositionFieldFocusExited()
    {
        // Wait until focus has left both fields before committing.
        if (_selectionXEdit.HasFocus() || _selectionYEdit.HasFocus()) return;
        _editingPosition = false;
        if (_selectedIndex < 0 || _levelData == null) return;

        Vector2 finalPos = GetSelectedWorldPos();
        if (finalPos == _posEditStart) return;

        Vector2 startPos = _posEditStart;
        int     idx      = _selectedIndex;
        // Position already applied live via OnSelectionPositionChanged — just track for undo.
        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { ApplyPositionAt(idx, finalPos); RefreshCanvasOverlays(); SyncPositionFields(); },
            () => { ApplyPositionAt(idx, startPos); RefreshCanvasOverlays(); SyncPositionFields(); }
        ));
    }

    private void OnSelectionPositionChanged()
    {
        if (_syncingFields || _selectedIndex < 0 || _levelData == null) return;
        if (!int.TryParse(_selectionXEdit.Text, out int tx)) return;
        if (!int.TryParse(_selectionYEdit.Text, out int ty)) return;

        float   cs    = GameplayConstants.CellSize;
        Vector2 world = new Vector2((tx + 0.5f) * cs, (ty + 0.5f) * cs);
        ApplyPositionAt(_selectedIndex, world);
        RefreshCanvasOverlays();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Vector2 GetSelectedWorldPos()
    {
        if (_selectedIndex < 0 || _levelData == null) return Vector2.Zero;
        return _selectedIndex < _levelData.Entities.Length
            ? new Vector2(_levelData.Entities[_selectedIndex].X, _levelData.Entities[_selectedIndex].Y)
            : EnemyOrigin(_levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Behavior);
    }

    private void ApplyPositionAt(int idx, Vector2 world)
    {
        if (_levelData == null) return;
        if (idx < _levelData.Entities.Length)
        {
            _levelData.Entities[idx].X = world.X;
            _levelData.Entities[idx].Y = world.Y;
        }
        else
        {
            SetEnemyOrigin(_levelData.Enemies[idx - _levelData.Entities.Length], world);
        }
    }

    private string? GetSelectedName()
    {
        if (_selectedIndex < 0 || _levelData == null) return null;
        return _selectedIndex < _levelData.Entities.Length
            ? _levelData.Entities[_selectedIndex].Name
            : _levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Name;
    }

    private void ApplyNameToSelected(string value)
    {
        if (_selectedIndex < 0 || _levelData == null) return;
        ApplyNameAt(_selectedIndex, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }

    private void ApplyNameAt(int idx, string? name)
    {
        if (_levelData == null) return;
        if (idx < _levelData.Entities.Length)
            _levelData.Entities[idx].Name = name;
        else
            _levelData.Enemies[idx - _levelData.Entities.Length].Name = name;

        string kind = idx < _levelData.Entities.Length
            ? EntityKindLabel(_levelData.Entities[idx].Kind)
            : EnemyKindLabel(_levelData.Enemies[idx - _levelData.Entities.Length].Behavior);
        _selectionKindLabel.Text = string.IsNullOrEmpty(name) ? kind : $"{kind} - {name}";
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

    private void BeginPortalPlacement(Vector2 world)
    {
        var portalA = new EntityData { Kind = "portal", X = world.X, Y = world.Y };
        _portalEntitiesSnapshot = _levelData!.Entities;
        _portalAData            = portalA;

        var list = new List<EntityData>(_levelData.Entities) { portalA };
        _levelData.Entities = [..list];
        _selectedIndex      = list.Count - 1;
        _placementMode      = EnemyPlacementMode.PlacingPortalB;
        _placementArmed     = false;
        RefreshSlotBorders();
        RefreshOverlays();
        SyncNameField();
    }

    private void PlacePortalB(Vector2 world)
    {
        if (_portalAData == null) { FinalizePlacementSilent(); return; }

        var portalB = new EntityData { Kind = "portal", X = world.X, Y = world.Y };
        _portalAData.LinkedPortalId = portalB.Id;

        var before = _portalEntitiesSnapshot!;
        var list   = new List<EntityData>(_levelData!.Entities) { portalB };
        var after  = list.ToArray();
        int selIdx = after.Length - 1;

        _portalEntitiesSnapshot = null;
        _portalAData            = null;
        FinalizePlacementSilent();

        _undoStack.Execute(new SimpleCommand(
            () => { _levelData!.Entities = after;  Select(selIdx);   },
            () => { _levelData!.Entities = before; ClearSelection(); }
        ));
    }
}
