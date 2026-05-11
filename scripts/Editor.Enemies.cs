using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private enum EnemyPlacementMode { None, PlacingWaypoints }

    private EnemyData[]?    _enemyPlacementSnapshot;
    private WaypointData[]? _waypointAddSnapshot;

    // ── Placement ────────────────────────────────────────────────────────────

    private void OnEnemyLeftPressed(Vector2I px)
    {
        if (_levelData == null) return;
        float cellSize = GameplayConstants.CellSize;
        var   world    = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);

        if (_placementMode == EnemyPlacementMode.PlacingWaypoints && _placementTarget != null)
        {
            AppendPatrolWaypoint(world);
            return;
        }

        if (!_placementArmed) return;

        EnemyData? enemy = _selectedSlot switch
        {
            0 => CreatePatrolEnemy(world),
            _ => null,
        };
        if (enemy == null) return;

        _enemyPlacementSnapshot = _levelData.Enemies;
        _placementTarget        = enemy;
        var list = new List<EnemyData>(_levelData.Enemies) { enemy };
        _levelData.Enemies = [..list];
        _selectedIndex     = _levelData.Entities.Length + _levelData.Enemies.Length - 1;

        RefreshOverlays();
        SyncNameField();
    }

    private EnemyData CreatePatrolEnemy(Vector2 world)
    {
        _placementMode = EnemyPlacementMode.PlacingWaypoints;
        _canvas.SetGhostLine(world);
        return new EnemyData
        {
            Radius   = 12f,
            Color    = "#e63333",
            Behavior = new PatrolBehaviorData
            {
                Waypoints   = [new WaypointData { X = world.X, Y = world.Y, Speed = 100f }],
                EndBehavior = "loop",
            },
        };
    }

    private void AppendPatrolWaypoint(Vector2 world)
    {
        var patrol    = (PatrolBehaviorData)_placementTarget!.Behavior;
        var waypoints = new List<WaypointData>(patrol.Waypoints)
        {
            new WaypointData { X = world.X, Y = world.Y, Speed = 100f },
        };
        patrol.Waypoints = [..waypoints];
        _canvas.SetGhostLine(world);
        RefreshOverlays();
    }

    private void FinalizePlacement()
    {
        if (_placementMode == EnemyPlacementMode.None) return;

        if (_enemyPlacementSnapshot != null)
        {
            var before = _enemyPlacementSnapshot;
            var after  = _levelData!.Enemies;
            int selIdx = _selectedIndex;
            _enemyPlacementSnapshot = null;
            FinalizePlacementSilent();
            Select(selIdx);  // apply initial display: RefreshOverlays + SyncNameField
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { _levelData!.Enemies = after;  Select(selIdx);   },
                () => { _levelData!.Enemies = before; ClearSelection(); }
            ));
            return;
        }

        if (_waypointAddSnapshot != null && _placementTarget != null)
        {
            var patrol = (PatrolBehaviorData)_placementTarget.Behavior;
            var before = _waypointAddSnapshot;
            var after  = patrol.Waypoints;
            _waypointAddSnapshot = null;
            FinalizePlacementSilent();
            RefreshOverlays();  // rebuild panel without "placing waypoints" hint
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { patrol.Waypoints = after;  RefreshOverlays(); },
                () => { patrol.Waypoints = before; RefreshOverlays(); }
            ));
            return;
        }

        FinalizePlacementSilent();
        RefreshOverlays();
    }

    private void FinalizePlacementSilent()
    {
        _placementMode           = EnemyPlacementMode.None;
        _placementTarget         = null;
        _placementArmed          = false;
        _enemyPlacementSnapshot  = null;
        _waypointAddSnapshot     = null;
        _canvas.SetGhostLine(null);
        RefreshSlotBorders();
    }

    // ── Options panel ─────────────────────────────────────────────────────────

    private void PopulateBehaviorConfig()
    {
        foreach (Node child in _behaviorConfigContainer.GetChildren())
        {
            _behaviorConfigContainer.RemoveChild(child);
            child.QueueFree();
        }

        if (_selectedIndex < 0 || _levelData == null ||
            _selectedIndex < _levelData.Entities.Length)
        {
            _behaviorConfigContainer.Visible = false;
            return;
        }

        int idx = _selectedIndex - _levelData.Entities.Length;
        if (idx >= _levelData.Enemies.Length)
        {
            _behaviorConfigContainer.Visible = false;
            return;
        }

        var enemy = _levelData.Enemies[idx];
        _behaviorConfigContainer.Visible = true;

        // Common: Radius
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Radius"));
        var radiusRow = new HBoxContainer();
        radiusRow.AddThemeConstantOverride("separation", 4);
        var radiusMinus = new Button { Text = "−", CustomMinimumSize = new Vector2(28, 24) };
        var radiusLabel = new Label
        {
            Text                = ((int)enemy.Radius).ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        radiusLabel.AddThemeFontSizeOverride("font_size", 14);
        var radiusPlus = new Button { Text = "+", CustomMinimumSize = new Vector2(28, 24) };

        radiusMinus.Pressed += () =>
        {
            float before = enemy.Radius;
            float after  = Mathf.Max(4f, before - 2f);
            if (after == before) return;
            _undoStack.Execute(new SimpleCommand(
                () => { enemy.Radius = after;  RefreshOverlays(); },
                () => { enemy.Radius = before; RefreshOverlays(); }
            ));
        };
        radiusPlus.Pressed += () =>
        {
            float before = enemy.Radius;
            float after  = before + 2f;
            _undoStack.Execute(new SimpleCommand(
                () => { enemy.Radius = after;  RefreshOverlays(); },
                () => { enemy.Radius = before; RefreshOverlays(); }
            ));
        };

        radiusRow.AddChild(radiusMinus);
        radiusRow.AddChild(radiusLabel);
        radiusRow.AddChild(radiusPlus);
        _behaviorConfigContainer.AddChild(radiusRow);

        // Common: Color
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Color"));
        Color colorBefore  = Color.FromHtml(enemy.Color);
        bool  colorEditing = false;
        var   colorBtn     = new ColorPickerButton
        {
            Color               = colorBefore,
            CustomMinimumSize   = new Vector2(0, 28),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        colorBtn.ColorChanged += c =>
        {
            if (!colorEditing)
            {
                colorBefore  = Color.FromHtml(enemy.Color);
                colorEditing = true;
            }
            enemy.Color = "#" + c.ToHtml(false).ToLower();
            RefreshCanvasOverlays();
        };
        colorBtn.PopupClosed += () =>
        {
            if (!colorEditing) return;
            colorEditing = false;
            Color before = colorBefore;
            Color after  = colorBtn.Color;
            if (before == after) return;
            // Color already applied via ColorChanged — just track for undo.
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { enemy.Color = "#" + after.ToHtml(false).ToLower();  RefreshOverlays(); },
                () => { enemy.Color = "#" + before.ToHtml(false).ToLower(); RefreshOverlays(); }
            ));
        };
        _behaviorConfigContainer.AddChild(colorBtn);

        _behaviorConfigContainer.AddChild(new HSeparator());

        if (enemy.Behavior is PatrolBehaviorData patrol)
            BuildPatrolConfig(patrol, enemy);
    }

    private void BuildPatrolConfig(PatrolBehaviorData patrol, EnemyData enemy)
    {
        if (_placementMode == EnemyPlacementMode.PlacingWaypoints)
        {
            var hint = new Label
            {
                Text         = $"Click canvas to add waypoints\n({patrol.Waypoints.Length} placed)",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontSizeOverride("font_size", 12);
            hint.AddThemeColorOverride("font_color", new Color(0.65f, 0.75f, 0.90f));
            _behaviorConfigContainer.AddChild(hint);

            var doneBtn = new Button
            {
                Text                = "Done",
                CustomMinimumSize   = new Vector2(0, 28),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            doneBtn.Pressed += FinalizePlacement;
            _behaviorConfigContainer.AddChild(doneBtn);
            return;
        }

        // End Behavior
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("End Behavior"));
        var endRow = new HBoxContainer();
        endRow.AddThemeConstantOverride("separation", 4);

        var loopBtn      = new Button { Text = "Loop",      ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var disappearBtn = new Button { Text = "Disappear", ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        loopBtn.AddThemeFontSizeOverride("font_size", 12);
        disappearBtn.AddThemeFontSizeOverride("font_size", 12);

        var btnGroup = new ButtonGroup();
        loopBtn.ButtonGroup      = btnGroup;
        disappearBtn.ButtonGroup = btnGroup;
        loopBtn.ButtonPressed      = patrol.EndBehavior != "disappear";
        disappearBtn.ButtonPressed = patrol.EndBehavior == "disappear";

        loopBtn.Pressed += () =>
        {
            if (patrol.EndBehavior == "loop") return;
            string before = patrol.EndBehavior;
            _undoStack.Execute(new SimpleCommand(
                () => { patrol.EndBehavior = "loop";      RefreshOverlays(); },
                () => { patrol.EndBehavior = before;      RefreshOverlays(); }
            ));
        };
        disappearBtn.Pressed += () =>
        {
            if (patrol.EndBehavior == "disappear") return;
            string before = patrol.EndBehavior;
            _undoStack.Execute(new SimpleCommand(
                () => { patrol.EndBehavior = "disappear"; RefreshOverlays(); },
                () => { patrol.EndBehavior = before;      RefreshOverlays(); }
            ));
        };

        endRow.AddChild(loopBtn);
        endRow.AddChild(disappearBtn);
        _behaviorConfigContainer.AddChild(endRow);

        // Waypoint list
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel($"Waypoints ({patrol.Waypoints.Length})"));

        float cellSize = GameplayConstants.CellSize;
        var   wpList   = new VBoxContainer();
        wpList.AddThemeConstantOverride("separation", 3);
        _behaviorConfigContainer.AddChild(wpList);

        for (int w = 0; w < patrol.Waypoints.Length; w++)
        {
            int captured = w;
            var wp       = patrol.Waypoints[w];
            int tx = (int)(wp.X / cellSize);
            int ty = (int)(wp.Y / cellSize);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 2);

            var numLabel = new Label
            {
                Text              = $"{w + 1}",
                CustomMinimumSize = new Vector2(16, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            numLabel.AddThemeFontSizeOverride("font_size", 11);
            numLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.60f));

            var posLabel = new Label
            {
                Text              = $"{tx},{ty}",
                CustomMinimumSize = new Vector2(44, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            posLabel.AddThemeFontSizeOverride("font_size", 11);

            var sLabel = new Label { Text = "s:", VerticalAlignment = VerticalAlignment.Center };
            sLabel.AddThemeFontSizeOverride("font_size", 11);
            sLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.60f));

            var speedEdit = new LineEdit
            {
                Text                = ((int)wp.Speed).ToString(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            speedEdit.AddThemeFontSizeOverride("font_size", 11);

            float speedBefore = 0f;
            speedEdit.FocusEntered += () => speedBefore = patrol.Waypoints[captured].Speed;
            speedEdit.FocusExited  += () =>
            {
                string text = speedEdit.Text;
                if (!float.TryParse(text, out float speedAfter)) return;
                speedAfter = Mathf.Max(1f, speedAfter);
                if (speedAfter == speedBefore) return;
                float sb = speedBefore, sa = speedAfter;
                int   wi = captured;
                // Speed already applied via TextChanged — just track for undo.
                _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                    () => { patrol.Waypoints[wi].Speed = sa; RefreshSelectionPanel(); },
                    () => { patrol.Waypoints[wi].Speed = sb; RefreshSelectionPanel(); }
                ));
            };
            speedEdit.TextChanged += val =>
            {
                if (float.TryParse(val, out float spd))
                    patrol.Waypoints[captured].Speed = Mathf.Max(1f, spd);
            };

            var delBtn = new Button { Text = "×", CustomMinimumSize = new Vector2(22, 0) };
            delBtn.AddThemeFontSizeOverride("font_size", 13);
            delBtn.Pressed += () =>
            {
                var before = patrol.Waypoints;
                var wps    = new List<WaypointData>(patrol.Waypoints);
                wps.RemoveAt(captured);
                var after  = wps.ToArray();
                _undoStack.Execute(new SimpleCommand(
                    () => { patrol.Waypoints = after;  RefreshOverlays(); },
                    () => { patrol.Waypoints = before; RefreshOverlays(); }
                ));
            };

            row.AddChild(numLabel);
            row.AddChild(posLabel);
            row.AddChild(sLabel);
            row.AddChild(speedEdit);
            row.AddChild(delBtn);
            wpList.AddChild(row);
        }

        // Add Waypoint button
        var addBtn = new Button
        {
            Text                = "+ Add Waypoint",
            CustomMinimumSize   = new Vector2(0, 26),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        addBtn.AddThemeFontSizeOverride("font_size", 12);
        addBtn.Pressed += () =>
        {
            _waypointAddSnapshot = patrol.Waypoints;
            if (patrol.Waypoints.Length > 0)
            {
                var last = patrol.Waypoints[^1];
                _canvas.SetGhostLine(new Vector2(last.X, last.Y));
            }
            _placementMode   = EnemyPlacementMode.PlacingWaypoints;
            _placementTarget = enemy;
            RefreshOverlays();
        };
        _behaviorConfigContainer.AddChild(addBtn);
    }

    private static Label MakeBehaviorLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.60f, 0.60f, 0.65f));
        return label;
    }
}
