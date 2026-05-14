using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Slide;

public partial class Editor
{
    private enum EnemyPlacementMode { None, PlacingWaypoints, PlacingPolygon, PickingCenter, PickingStartPos, PickingWaypointPos }

    private EnemyData[]?    _enemyPlacementSnapshot;
    private WaypointData[]? _waypointAddSnapshot;
    private Vec2Data[]?     _polygonEditSnapshot;
    private int             _pickingWaypointIndex = -1;

    // ── Placement ────────────────────────────────────────────────────────────

    private void OnEnemyLeftPressed(Vector2I px)
    {
        if (_levelData == null) return;
        float cellSize = GameplayConstants.CellSize;
        var   world    = new Vector2((px.X + 0.5f) * cellSize, (px.Y + 0.5f) * cellSize);

        switch (_placementMode)
        {
            case EnemyPlacementMode.PlacingWaypoints:   AppendPatrolWaypoint(world);   return;
            case EnemyPlacementMode.PlacingPolygon:     AppendOrClosePolygon(world);   return;
            case EnemyPlacementMode.PickingCenter:      ApplyPickedCenter(world);      return;
            case EnemyPlacementMode.PickingStartPos:    ApplyPickedStartPos(world);    return;
            case EnemyPlacementMode.PickingWaypointPos: ApplyPickedWaypointPos(world); return;
        }

        if (!_placementArmed) return;

        switch (_selectedSlot)
        {
            case 0: BeginPatrolPlacement(world);  break;
            case 1: BeginWanderPlacement(world);  break;
            case 2: PlaceOrbiterEnemy(world);     break;
        }
    }

    private void BeginPatrolPlacement(Vector2 world)
    {
        var enemy = new EnemyData
        {
            Radius   = 12f,
            Color    = "#e63333",
            Behavior = new PatrolBehaviorData
            {
                Waypoints   = [new WaypointData { X = world.X, Y = world.Y, Speed = 100f }],
                EndBehavior = "reverse",
            },
        };
        BeginMultiClickPlacement(enemy);
        _placementMode = EnemyPlacementMode.PlacingWaypoints;
        _canvas.SetGhostLine(world);
    }

    private void BeginWanderPlacement(Vector2 world)
    {
        var enemy = new EnemyData
        {
            Radius   = 12f,
            Color    = "#dd3377",
            Behavior = new WanderBehaviorData
            {
                Polygon = [new Vec2Data { X = world.X, Y = world.Y }],
                Speed   = 100f,
                MinIdle = 0.5f,
                MaxIdle = 2f,
                Seed    = ((ulong)GD.Randi() << 32) | (ulong)GD.Randi(),
            },
        };
        BeginMultiClickPlacement(enemy);
        _placementMode = EnemyPlacementMode.PlacingPolygon;
        _canvas.SetGhostLine(world);
    }

    private void BeginMultiClickPlacement(EnemyData enemy)
    {
        _enemyPlacementSnapshot = _levelData!.Enemies;
        _placementTarget        = enemy;
        var list = new List<EnemyData>(_levelData.Enemies) { enemy };
        _levelData.Enemies = [..list];
        _selectedIndex     = _levelData.Entities.Length + _levelData.Enemies.Length - 1;
        RefreshOverlays();
        SyncNameField();
    }

    private void PlaceOrbiterEnemy(Vector2 world)
    {
        var enemy = new EnemyData
        {
            Radius   = 12f,
            Color    = "#33cc77",
            Behavior = new OrbiterBehaviorData
            {
                CenterX = world.X, CenterY = world.Y,
                Radius = 50f, AngularSpeed = 1.5f, Clockwise = true, StartAngle = 0f,
            },
        };
        var before = _levelData!.Enemies;
        var after  = new List<EnemyData>(before) { enemy }.ToArray();
        int selIdx = _levelData.Entities.Length + after.Length - 1;
        _placementArmed = false;
        RefreshSlotBorders();
        _undoStack.Execute(new SimpleCommand(
            () => { _levelData!.Enemies = after;  Select(selIdx);   },
            () => { _levelData!.Enemies = before; ClearSelection(); }
        ));
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

    private void AppendOrClosePolygon(Vector2 world)
    {
        var   wander   = (WanderBehaviorData)_placementTarget!.Behavior;
        float cellSize = GameplayConstants.CellSize;

        if (wander.Polygon.Length >= 3)
        {
            var first = new Vector2(wander.Polygon[0].X, wander.Polygon[0].Y);
            if (world.DistanceTo(first) < cellSize * 2f)
            {
                FinalizePlacement();
                return;
            }
        }

        var verts = new List<Vec2Data>(wander.Polygon) { new Vec2Data { X = world.X, Y = world.Y } };
        wander.Polygon = [..verts];
        _canvas.SetGhostLine(world);
        RefreshOverlays();
    }

    private void ApplyPickedCenter(Vector2 world)
    {
        if (_placementTarget == null) return;
        var orbiter = (OrbiterBehaviorData)_placementTarget.Behavior;
        var before  = new Vector2(orbiter.CenterX, orbiter.CenterY);
        orbiter.CenterX  = world.X;
        orbiter.CenterY  = world.Y;
        _placementMode   = EnemyPlacementMode.None;
        _placementTarget = null;
        RefreshOverlays();
        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { orbiter.CenterX = world.X;   orbiter.CenterY = world.Y;   RefreshOverlays(); },
            () => { orbiter.CenterX = before.X;  orbiter.CenterY = before.Y;  RefreshOverlays(); }
        ));
    }

    private void ApplyPickedStartPos(Vector2 world)
    {
        if (_placementTarget == null) return;
        var    wander  = (WanderBehaviorData)_placementTarget.Behavior;
        float? beforeX = wander.StartX;
        float? beforeY = wander.StartY;
        wander.StartX    = world.X;
        wander.StartY    = world.Y;
        _placementMode   = EnemyPlacementMode.None;
        _placementTarget = null;
        RefreshOverlays();
        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { wander.StartX = world.X; wander.StartY = world.Y; RefreshOverlays(); },
            () => { wander.StartX = beforeX; wander.StartY = beforeY; RefreshOverlays(); }
        ));
    }

    private void ApplyPickedWaypointPos(Vector2 world)
    {
        if (_placementTarget == null) return;
        var patrol = (PatrolBehaviorData)_placementTarget.Behavior;
        int idx    = _pickingWaypointIndex;
        if (idx < 0 || idx >= patrol.Waypoints.Length) { FinalizePlacementSilent(); RefreshOverlays(); return; }
        var before = new Vector2(patrol.Waypoints[idx].X, patrol.Waypoints[idx].Y);
        patrol.Waypoints[idx].X = world.X;
        patrol.Waypoints[idx].Y = world.Y;
        _placementMode        = EnemyPlacementMode.None;
        _placementTarget      = null;
        _pickingWaypointIndex = -1;
        RefreshOverlays();
        _undoStack.ExecuteAlreadyDone(new SimpleCommand(
            () => { patrol.Waypoints[idx].X = world.X;  patrol.Waypoints[idx].Y = world.Y;  RefreshOverlays(); },
            () => { patrol.Waypoints[idx].X = before.X; patrol.Waypoints[idx].Y = before.Y; RefreshOverlays(); }
        ));
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
            Select(selIdx);
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
            RefreshOverlays();
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { patrol.Waypoints = after;  RefreshOverlays(); },
                () => { patrol.Waypoints = before; RefreshOverlays(); }
            ));
            return;
        }

        if (_polygonEditSnapshot != null && _placementTarget != null)
        {
            var wander = (WanderBehaviorData)_placementTarget.Behavior;
            var before = _polygonEditSnapshot;
            var after  = wander.Polygon;
            _polygonEditSnapshot = null;
            FinalizePlacementSilent();
            RefreshOverlays();
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { wander.Polygon = after;  RefreshOverlays(); },
                () => { wander.Polygon = before; RefreshOverlays(); }
            ));
            return;
        }

        // PickingCenter / PickingStartPos — just cancel
        FinalizePlacementSilent();
        RefreshOverlays();
    }

    // Commit whatever is in progress (Enter / Done button).
    // Cancel (Escape / Delete) — use CancelPlacement instead.
    private void CancelPlacement()
    {
        if (_placementMode == EnemyPlacementMode.None) return;

        // Initial enemy placement: discard the partially-built enemy.
        if (_enemyPlacementSnapshot != null)
        {
            _levelData!.Enemies = _enemyPlacementSnapshot;
            FinalizePlacementSilent();
            ClearSelection();
            return;
        }

        // Waypoint-add or polygon-edit session: restore the pre-session snapshot.
        if (_waypointAddSnapshot != null && _placementTarget != null)
            ((PatrolBehaviorData)_placementTarget.Behavior).Waypoints = _waypointAddSnapshot;

        if (_polygonEditSnapshot != null && _placementTarget != null)
            ((WanderBehaviorData)_placementTarget.Behavior).Polygon = _polygonEditSnapshot;

        FinalizePlacementSilent();
        RefreshOverlays();
    }

    private void FinalizePlacementSilent()
    {
        _placementMode          = EnemyPlacementMode.None;
        _placementTarget        = null;
        _placementArmed         = false;
        _enemyPlacementSnapshot = null;
        _waypointAddSnapshot    = null;
        _polygonEditSnapshot    = null;
        _pickingWaypointIndex   = -1;
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
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { enemy.Color = "#" + after.ToHtml(false).ToLower();  RefreshOverlays(); },
                () => { enemy.Color = "#" + before.ToHtml(false).ToLower(); RefreshOverlays(); }
            ));
        };
        _behaviorConfigContainer.AddChild(colorBtn);

        _behaviorConfigContainer.AddChild(new HSeparator());

        if (enemy.Behavior is PatrolBehaviorData patrol)
            BuildPatrolConfig(patrol, enemy);
        else if (enemy.Behavior is WanderBehaviorData wander)
            BuildWanderConfig(wander, enemy);
        else if (enemy.Behavior is OrbiterBehaviorData orbiter)
            BuildOrbiterConfig(orbiter, enemy);
    }

    private void BuildPatrolConfig(PatrolBehaviorData patrol, EnemyData enemy)
    {
        if (_placementMode == EnemyPlacementMode.PickingWaypointPos)
        {
            var hint = new Label
            {
                Text         = $"Click canvas to set position for waypoint {_pickingWaypointIndex + 1}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            hint.AddThemeFontSizeOverride("font_size", 12);
            hint.AddThemeColorOverride("font_color", new Color(0.65f, 0.75f, 0.90f));
            _behaviorConfigContainer.AddChild(hint);
            var cancelBtn = new Button { Text = "Cancel", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cancelBtn.Pressed += () =>
            {
                _placementMode        = EnemyPlacementMode.None;
                _placementTarget      = null;
                _pickingWaypointIndex = -1;
                RefreshOverlays();
            };
            _behaviorConfigContainer.AddChild(cancelBtn);
            return;
        }

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
        var reverseBtn   = new Button { Text = "Reverse",   ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var disappearBtn = new Button { Text = "Disappear", ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        loopBtn.AddThemeFontSizeOverride("font_size", 12);
        reverseBtn.AddThemeFontSizeOverride("font_size", 12);
        disappearBtn.AddThemeFontSizeOverride("font_size", 12);

        var btnGroup = new ButtonGroup();
        loopBtn.ButtonGroup      = btnGroup;
        reverseBtn.ButtonGroup   = btnGroup;
        disappearBtn.ButtonGroup = btnGroup;
        loopBtn.ButtonPressed      = patrol.EndBehavior == "loop";
        reverseBtn.ButtonPressed   = patrol.EndBehavior == "reverse";
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
        reverseBtn.Pressed += () =>
        {
            if (patrol.EndBehavior == "reverse") return;
            string before = patrol.EndBehavior;
            _undoStack.Execute(new SimpleCommand(
                () => { patrol.EndBehavior = "reverse";   RefreshOverlays(); },
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
        endRow.AddChild(reverseBtn);
        endRow.AddChild(disappearBtn);
        _behaviorConfigContainer.AddChild(endRow);

        // Waypoint list
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel($"Waypoints ({patrol.Waypoints.Length})"));

        // All-speeds bulk field — only useful when there are 2+ waypoints
        if (patrol.Waypoints.Length >= 2)
        {
            float firstSpd = patrol.Waypoints[0].Speed;
            bool  allSame  = true;
            foreach (var wp in patrol.Waypoints)
                if (wp.Speed != firstSpd) { allSame = false; break; }

            var allRow = new HBoxContainer();
            allRow.AddThemeConstantOverride("separation", 4);

            var allLbl = new Label { Text = "All speeds:", VerticalAlignment = VerticalAlignment.Center };
            allLbl.AddThemeFontSizeOverride("font_size", 11);
            allLbl.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.60f));

            var allEdit = new LineEdit
            {
                Text                = allSame ? ((int)firstSpd).ToString() : "",
                PlaceholderText     = allSame ? "" : "mixed",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            allEdit.AddThemeFontSizeOverride("font_size", 11);
            WireIntFilter(allEdit);

            float[]? allBefore     = null;
            bool     allCommitting = false;

            allEdit.FocusEntered += () =>
            {
                allBefore = new float[patrol.Waypoints.Length];
                for (int i = 0; i < patrol.Waypoints.Length; i++)
                    allBefore[i] = patrol.Waypoints[i].Speed;
            };
            allEdit.TextChanged += val =>
            {
                if (float.TryParse(val, out float spd))
                {
                    spd = Mathf.Max(1f, spd);
                    foreach (var wp in patrol.Waypoints) wp.Speed = spd;
                }
            };

            void CommitAllSpeeds()
            {
                if (allCommitting) return;
                if (_placementMode != EnemyPlacementMode.None) return;
                string text = allEdit.Text;
                if (!float.TryParse(text, out float sa)) return;
                sa = Mathf.Max(1f, sa);

                // Apply to data (TextChanged may have already done this, but ensure it).
                for (int i = 0; i < patrol.Waypoints.Length; i++)
                    patrol.Waypoints[i].Speed = sa;

                // Compare against the snapshot taken on FocusEntered, not current data.
                var  before  = allBefore ?? new float[patrol.Waypoints.Length];
                bool changed = before.Length != patrol.Waypoints.Length;
                if (!changed)
                    for (int i = 0; i < patrol.Waypoints.Length; i++)
                        if ((i < before.Length ? before[i] : sa) != sa) { changed = true; break; }

                if (changed)
                {
                    float[]? snapshot = allBefore;
                    // Update allBefore so successive Enter presses diff from here.
                    allBefore = new float[patrol.Waypoints.Length];
                    for (int i = 0; i < patrol.Waypoints.Length; i++)
                        allBefore[i] = sa;
                    float   csa = sa;
                    float[] cb  = snapshot ?? new float[0];
                    _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                        () => { for (int i = 0; i < patrol.Waypoints.Length; i++) patrol.Waypoints[i].Speed = csa;  RefreshSelectionPanel(); },
                        () => { for (int i = 0; i < patrol.Waypoints.Length; i++) patrol.Waypoints[i].Speed = cb[i]; RefreshSelectionPanel(); }
                    ));
                }

                // Always refresh so per-waypoint fields show the committed value.
                // Guard prevents the FocusExited fired when we remove controls from the tree.
                allCommitting = true;
                RefreshSelectionPanel();
                allCommitting = false;
            }

            allEdit.FocusExited   += () => CommitAllSpeeds();
            allEdit.TextSubmitted += _  => CommitAllSpeeds();

            allRow.AddChild(allLbl);
            allRow.AddChild(allEdit);
            _behaviorConfigContainer.AddChild(allRow);
        }

        float cellSize = GameplayConstants.CellSize;
        var   wpList   = new VBoxContainer();
        wpList.AddThemeConstantOverride("separation", 3);
        _behaviorConfigContainer.AddChild(wpList);

        for (int w = 0; w < patrol.Waypoints.Length; w++)
        {
            int captured = w;
            var wp       = patrol.Waypoints[w];

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 2);

            // Index label
            var numLabel = new Label
            {
                Text              = $"{w + 1}",
                CustomMinimumSize = new Vector2(14, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            numLabel.AddThemeFontSizeOverride("font_size", 11);
            numLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.60f));

            // X position edit
            var xEdit = new LineEdit
            {
                Text                = ((int)(wp.X / cellSize)).ToString(),
                PlaceholderText     = "X",
                CustomMinimumSize   = new Vector2(32, 0),
            };
            xEdit.AddThemeFontSizeOverride("font_size", 11);
            WireIntFilter(xEdit);

            // Y position edit
            var yEdit = new LineEdit
            {
                Text                = ((int)(wp.Y / cellSize)).ToString(),
                PlaceholderText     = "Y",
                CustomMinimumSize   = new Vector2(32, 0),
            };
            yEdit.AddThemeFontSizeOverride("font_size", 11);
            WireIntFilter(yEdit);

            // Wire X/Y with live preview + commit-on-leave
            // Initialize from current data so FocusExited during panel teardown doesn't push spurious undo entries.
            Vector2 posBefore = new Vector2(wp.X, wp.Y);

            xEdit.FocusEntered += () => posBefore = new Vector2(patrol.Waypoints[captured].X, patrol.Waypoints[captured].Y);
            yEdit.FocusEntered += () => posBefore = new Vector2(patrol.Waypoints[captured].X, patrol.Waypoints[captured].Y);

            xEdit.TextChanged += val =>
            {
                if (int.TryParse(val, out int tx))
                    patrol.Waypoints[captured].X = (tx + 0.5f) * cellSize;
                RefreshCanvasOverlays();
            };
            yEdit.TextChanged += val =>
            {
                if (int.TryParse(val, out int ty))
                    patrol.Waypoints[captured].Y = (ty + 0.5f) * cellSize;
                RefreshCanvasOverlays();
            };

            void CommitPos()
            {
                // Bail if panel is being rebuilt (e.g. entering a placement mode tore down the old panel).
                if (_placementMode != EnemyPlacementMode.None) return;
                // Wait until focus has left both fields before committing.
                if (xEdit.HasFocus() || yEdit.HasFocus()) return;
                var finalPos = new Vector2(patrol.Waypoints[captured].X, patrol.Waypoints[captured].Y);
                if (finalPos == posBefore) return;
                Vector2 pb = posBefore, pa = finalPos;
                int     wi = captured;
                posBefore = pa;
                _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                    () => { patrol.Waypoints[wi].X = pa.X; patrol.Waypoints[wi].Y = pa.Y; RefreshOverlays(); },
                    () => { patrol.Waypoints[wi].X = pb.X; patrol.Waypoints[wi].Y = pb.Y; RefreshOverlays(); }
                ));
                // No RefreshSelectionPanel needed — the fields already display what the user typed.
            }

            xEdit.FocusExited   += CommitPos;
            yEdit.FocusExited   += CommitPos;
            xEdit.TextSubmitted += _ => CommitPos();
            yEdit.TextSubmitted += _ => CommitPos();

            // Speed label + edit
            var sLabel = new Label { Text = "s:", VerticalAlignment = VerticalAlignment.Center };
            sLabel.AddThemeFontSizeOverride("font_size", 11);
            sLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.60f));

            var speedEdit = new LineEdit
            {
                Text                = ((int)wp.Speed).ToString(),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            speedEdit.AddThemeFontSizeOverride("font_size", 11);
            WireIntFilter(speedEdit);

            float speedBefore = wp.Speed;
            speedEdit.FocusEntered += () => speedBefore = patrol.Waypoints[captured].Speed;
            speedEdit.TextChanged  += val =>
            {
                if (float.TryParse(val, out float spd))
                    patrol.Waypoints[captured].Speed = Mathf.Max(1f, spd);
            };

            void CommitSpeed()
            {
                if (_placementMode != EnemyPlacementMode.None) return;
                if (!float.TryParse(speedEdit.Text, out float sa)) return;
                sa = Mathf.Max(1f, sa);
                if (sa == speedBefore) return;
                float sb = speedBefore, sc = sa;
                int   wi = captured;
                speedBefore = sa;
                _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                    () => { patrol.Waypoints[wi].Speed = sc; RefreshSelectionPanel(); },
                    () => { patrol.Waypoints[wi].Speed = sb; RefreshSelectionPanel(); }
                ));
            }

            speedEdit.FocusExited   += CommitSpeed;
            speedEdit.TextSubmitted += _ => CommitSpeed();

            // Up / Down reorder buttons
            var upBtn = new Button
            {
                Text              = "↑",
                CustomMinimumSize = new Vector2(20, 0),
                Disabled          = w == 0,
            };
            upBtn.AddThemeFontSizeOverride("font_size", 11);
            upBtn.Pressed += () =>
            {
                int i = captured;
                var before = patrol.Waypoints;
                var wps    = new List<WaypointData>(patrol.Waypoints);
                (wps[i], wps[i - 1]) = (wps[i - 1], wps[i]);
                var after = wps.ToArray();
                _undoStack.Execute(new SimpleCommand(
                    () => { patrol.Waypoints = after;  RefreshOverlays(); },
                    () => { patrol.Waypoints = before; RefreshOverlays(); }
                ));
            };

            var downBtn = new Button
            {
                Text              = "↓",
                CustomMinimumSize = new Vector2(20, 0),
                Disabled          = w == patrol.Waypoints.Length - 1,
            };
            downBtn.AddThemeFontSizeOverride("font_size", 11);
            downBtn.Pressed += () =>
            {
                int i = captured;
                var before = patrol.Waypoints;
                var wps    = new List<WaypointData>(patrol.Waypoints);
                (wps[i], wps[i + 1]) = (wps[i + 1], wps[i]);
                var after = wps.ToArray();
                _undoStack.Execute(new SimpleCommand(
                    () => { patrol.Waypoints = after;  RefreshOverlays(); },
                    () => { patrol.Waypoints = before; RefreshOverlays(); }
                ));
            };

            // Pick-on-canvas button
            var pickBtn = new Button { Text = "✏", CustomMinimumSize = new Vector2(22, 0) };
            pickBtn.AddThemeFontSizeOverride("font_size", 13);
            pickBtn.Pressed += () =>
            {
                _pickingWaypointIndex = captured;
                _placementMode        = EnemyPlacementMode.PickingWaypointPos;
                _placementTarget      = enemy;
                RefreshOverlays();
            };

            // Delete button
            var delBtn = new Button { Text = "×", CustomMinimumSize = new Vector2(20, 0) };
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
            row.AddChild(xEdit);
            row.AddChild(yEdit);
            row.AddChild(sLabel);
            row.AddChild(speedEdit);
            row.AddChild(upBtn);
            row.AddChild(downBtn);
            row.AddChild(pickBtn);
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

    private void BuildWanderConfig(WanderBehaviorData wander, EnemyData enemy)
    {
        if (_placementMode == EnemyPlacementMode.PlacingPolygon)
        {
            var hint = new Label
            {
                Text         = $"Click canvas to add vertices\n({wander.Polygon.Length} placed)\nClick near first vertex or Enter to close",
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

        if (_placementMode == EnemyPlacementMode.PickingStartPos)
        {
            var hint = new Label { Text = "Click canvas to set start position" };
            hint.AddThemeFontSizeOverride("font_size", 12);
            hint.AddThemeColorOverride("font_color", new Color(0.65f, 0.75f, 0.90f));
            _behaviorConfigContainer.AddChild(hint);
            var cancelBtn = new Button { Text = "Cancel", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cancelBtn.Pressed += () =>
            {
                _placementMode   = EnemyPlacementMode.None;
                _placementTarget = null;
                RefreshOverlays();
            };
            _behaviorConfigContainer.AddChild(cancelBtn);
            return;
        }

        // Polygon
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel($"Polygon ({wander.Polygon.Length} vertices)"));
        var polyBtnRow = new HBoxContainer();
        polyBtnRow.AddThemeConstantOverride("separation", 4);

        var editPolyBtn = new Button
        {
            Text                = wander.Polygon.Length == 0 ? "Draw Polygon" : "Edit Polygon",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize   = new Vector2(0, 26),
        };
        editPolyBtn.AddThemeFontSizeOverride("font_size", 12);
        editPolyBtn.Pressed += () =>
        {
            _polygonEditSnapshot = wander.Polygon;
            if (wander.Polygon.Length > 0)
                _canvas.SetGhostLine(new Vector2(wander.Polygon[^1].X, wander.Polygon[^1].Y));
            _placementMode   = EnemyPlacementMode.PlacingPolygon;
            _placementTarget = enemy;
            RefreshOverlays();
        };
        polyBtnRow.AddChild(editPolyBtn);

        if (wander.Polygon.Length > 0)
        {
            var clearPolyBtn = new Button { Text = "Clear", CustomMinimumSize = new Vector2(48, 26) };
            clearPolyBtn.AddThemeFontSizeOverride("font_size", 12);
            clearPolyBtn.Pressed += () =>
            {
                var before = wander.Polygon;
                _undoStack.Execute(new SimpleCommand(
                    () => { wander.Polygon = []; RefreshOverlays(); },
                    () => { wander.Polygon = before; RefreshOverlays(); }
                ));
            };
            polyBtnRow.AddChild(clearPolyBtn);
        }

        _behaviorConfigContainer.AddChild(polyBtnRow);

        // Speed
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Speed"));
        var speedEdit = new LineEdit { Text = ((int)wander.Speed).ToString(), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        speedEdit.AddThemeFontSizeOverride("font_size", 11);
        WireIntFilter(speedEdit);
        float speedBefore = 0f;
        speedEdit.FocusEntered += () => speedBefore = wander.Speed;
        speedEdit.FocusExited  += () =>
        {
            string text = speedEdit.Text;
            if (!float.TryParse(text, out float sa)) return;
            sa = Mathf.Max(1f, sa);
            if (sa == speedBefore) return;
            float sb = speedBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { wander.Speed = sa; RefreshSelectionPanel(); },
                () => { wander.Speed = sb; RefreshSelectionPanel(); }
            ));
        };
        speedEdit.TextChanged += val =>
        {
            if (float.TryParse(val, out float v)) wander.Speed = Mathf.Max(1f, v);
        };
        _behaviorConfigContainer.AddChild(speedEdit);

        // Idle min / max
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Idle (min / max s)"));
        var idleRow = new HBoxContainer();
        idleRow.AddThemeConstantOverride("separation", 4);

        var minEdit = new LineEdit { Text = wander.MinIdle.ToString("F1"), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        minEdit.AddThemeFontSizeOverride("font_size", 11);
        WireFloatFilter(minEdit);
        float minBefore = 0f;
        minEdit.FocusEntered += () => minBefore = wander.MinIdle;
        minEdit.FocusExited  += () =>
        {
            string text = minEdit.Text;
            if (!float.TryParse(text, out float ma)) return;
            ma = Mathf.Max(0f, ma);
            if (ma == minBefore) return;
            float mb = minBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { wander.MinIdle = ma; RefreshSelectionPanel(); },
                () => { wander.MinIdle = mb; RefreshSelectionPanel(); }
            ));
        };
        minEdit.TextChanged += val =>
        {
            if (float.TryParse(val, out float v)) wander.MinIdle = Mathf.Max(0f, v);
        };

        var maxEdit = new LineEdit { Text = wander.MaxIdle.ToString("F1"), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        maxEdit.AddThemeFontSizeOverride("font_size", 11);
        WireFloatFilter(maxEdit);
        float maxBefore = 0f;
        maxEdit.FocusEntered += () => maxBefore = wander.MaxIdle;
        maxEdit.FocusExited  += () =>
        {
            string text = maxEdit.Text;
            if (!float.TryParse(text, out float ma)) return;
            ma = Mathf.Max(wander.MinIdle, ma);
            if (ma == maxBefore) return;
            float mb = maxBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { wander.MaxIdle = ma; RefreshSelectionPanel(); },
                () => { wander.MaxIdle = mb; RefreshSelectionPanel(); }
            ));
        };
        maxEdit.TextChanged += val =>
        {
            if (float.TryParse(val, out float v)) wander.MaxIdle = Mathf.Max(0f, v);
        };

        var slashLbl = new Label { Text = "/", VerticalAlignment = VerticalAlignment.Center };
        slashLbl.AddThemeFontSizeOverride("font_size", 11);
        idleRow.AddChild(minEdit);
        idleRow.AddChild(slashLbl);
        idleRow.AddChild(maxEdit);
        _behaviorConfigContainer.AddChild(idleRow);

        // Seed
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Seed"));
        var seedRow = new HBoxContainer();
        seedRow.AddThemeConstantOverride("separation", 4);
        var seedEdit = new LineEdit { Text = wander.Seed.ToString(), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        seedEdit.AddThemeFontSizeOverride("font_size", 11);
        WireIntFilter(seedEdit);
        ulong seedBefore = 0;
        seedEdit.FocusEntered += () => seedBefore = wander.Seed;
        seedEdit.FocusExited  += () =>
        {
            string text = seedEdit.Text;
            if (!ulong.TryParse(text, out ulong sa)) return;
            if (sa == seedBefore) return;
            ulong sb = seedBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { wander.Seed = sa; RefreshSelectionPanel(); },
                () => { wander.Seed = sb; RefreshSelectionPanel(); }
            ));
        };
        seedEdit.TextChanged += val =>
        {
            if (ulong.TryParse(val, out ulong s)) wander.Seed = s;
        };
        var randBtn = new Button { Text = "Rand", CustomMinimumSize = new Vector2(44, 0) };
        randBtn.AddThemeFontSizeOverride("font_size", 11);
        randBtn.Pressed += () =>
        {
            ulong before = wander.Seed;
            ulong after  = ((ulong)GD.Randi() << 32) | (ulong)GD.Randi();
            _undoStack.Execute(new SimpleCommand(
                () => { wander.Seed = after;  RefreshSelectionPanel(); },
                () => { wander.Seed = before; RefreshSelectionPanel(); }
            ));
        };
        seedRow.AddChild(seedEdit);
        seedRow.AddChild(randBtn);
        _behaviorConfigContainer.AddChild(seedRow);

        // Start Position
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Start Position"));
        var startRow = new HBoxContainer();
        startRow.AddThemeConstantOverride("separation", 4);
        float cs = GameplayConstants.CellSize;
        string startText = wander.StartX.HasValue
            ? $"({(int)(wander.StartX.Value / cs)}, {(int)(wander.StartY!.Value / cs)})"
            : "random";
        var startLbl = new Label { Text = startText, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        startLbl.AddThemeFontSizeOverride("font_size", 11);

        var pickStartBtn = new Button { Text = "Set", CustomMinimumSize = new Vector2(36, 0) };
        pickStartBtn.AddThemeFontSizeOverride("font_size", 11);
        pickStartBtn.Pressed += () =>
        {
            _placementMode   = EnemyPlacementMode.PickingStartPos;
            _placementTarget = enemy;
            RefreshOverlays();
        };

        startRow.AddChild(startLbl);
        startRow.AddChild(pickStartBtn);

        if (wander.StartX.HasValue)
        {
            var clearStartBtn = new Button { Text = "×", CustomMinimumSize = new Vector2(22, 0) };
            clearStartBtn.AddThemeFontSizeOverride("font_size", 13);
            clearStartBtn.Pressed += () =>
            {
                float? bx = wander.StartX, by = wander.StartY;
                _undoStack.Execute(new SimpleCommand(
                    () => { wander.StartX = null; wander.StartY = null; RefreshOverlays(); },
                    () => { wander.StartX = bx;   wander.StartY = by;   RefreshOverlays(); }
                ));
            };
            startRow.AddChild(clearStartBtn);
        }

        _behaviorConfigContainer.AddChild(startRow);
    }

    private void BuildOrbiterConfig(OrbiterBehaviorData orbiter, EnemyData enemy)
    {
        if (_placementMode == EnemyPlacementMode.PickingCenter)
        {
            var hint = new Label { Text = "Click canvas to set orbit center" };
            hint.AddThemeFontSizeOverride("font_size", 12);
            hint.AddThemeColorOverride("font_color", new Color(0.65f, 0.75f, 0.90f));
            _behaviorConfigContainer.AddChild(hint);
            var cancelBtn = new Button { Text = "Cancel", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cancelBtn.Pressed += () =>
            {
                _placementMode   = EnemyPlacementMode.None;
                _placementTarget = null;
                RefreshOverlays();
            };
            _behaviorConfigContainer.AddChild(cancelBtn);
            return;
        }

        // Pick Center
        var pickCenterBtn = new Button
        {
            Text                = "Pick Center",
            CustomMinimumSize   = new Vector2(0, 26),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        pickCenterBtn.AddThemeFontSizeOverride("font_size", 12);
        pickCenterBtn.Pressed += () =>
        {
            _placementMode   = EnemyPlacementMode.PickingCenter;
            _placementTarget = enemy;
            RefreshOverlays();
        };
        _behaviorConfigContainer.AddChild(pickCenterBtn);

        // Orbit Radius
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Orbit Radius"));
        var orRow = new HBoxContainer();
        orRow.AddThemeConstantOverride("separation", 4);
        var orMinus = new Button { Text = "−", CustomMinimumSize = new Vector2(28, 24) };
        var orLabel = new Label
        {
            Text                = ((int)orbiter.Radius).ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        orLabel.AddThemeFontSizeOverride("font_size", 14);
        var orPlus = new Button { Text = "+", CustomMinimumSize = new Vector2(28, 24) };
        orMinus.Pressed += () =>
        {
            float before = orbiter.Radius;
            float after  = Mathf.Max(8f, before - 8f);
            if (after == before) return;
            _undoStack.Execute(new SimpleCommand(
                () => { orbiter.Radius = after;  RefreshOverlays(); },
                () => { orbiter.Radius = before; RefreshOverlays(); }
            ));
        };
        orPlus.Pressed += () =>
        {
            float before = orbiter.Radius;
            float after  = before + 8f;
            _undoStack.Execute(new SimpleCommand(
                () => { orbiter.Radius = after;  RefreshOverlays(); },
                () => { orbiter.Radius = before; RefreshOverlays(); }
            ));
        };
        orRow.AddChild(orMinus);
        orRow.AddChild(orLabel);
        orRow.AddChild(orPlus);
        _behaviorConfigContainer.AddChild(orRow);

        // Angular Speed
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Speed (rad/s)"));
        var angEdit = new LineEdit { Text = orbiter.AngularSpeed.ToString("F2"), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        angEdit.AddThemeFontSizeOverride("font_size", 11);
        WireFloatFilter(angEdit);
        float angBefore = 0f;
        angEdit.FocusEntered += () => angBefore = orbiter.AngularSpeed;
        angEdit.FocusExited  += () =>
        {
            string text = angEdit.Text;
            if (!float.TryParse(text, out float sa)) return;
            sa = Mathf.Max(0.01f, sa);
            if (sa == angBefore) return;
            float sb = angBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { orbiter.AngularSpeed = sa; RefreshSelectionPanel(); },
                () => { orbiter.AngularSpeed = sb; RefreshSelectionPanel(); }
            ));
        };
        angEdit.TextChanged += val =>
        {
            if (float.TryParse(val, out float v)) orbiter.AngularSpeed = Mathf.Max(0.01f, v);
        };
        _behaviorConfigContainer.AddChild(angEdit);

        // Direction
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Direction"));
        var dirRow = new HBoxContainer();
        dirRow.AddThemeConstantOverride("separation", 4);
        var cwBtn  = new Button { Text = "CW",  ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var ccwBtn = new Button { Text = "CCW", ToggleMode = true, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        cwBtn.AddThemeFontSizeOverride("font_size", 12);
        ccwBtn.AddThemeFontSizeOverride("font_size", 12);
        var dirGroup = new ButtonGroup();
        cwBtn.ButtonGroup    = dirGroup;
        ccwBtn.ButtonGroup   = dirGroup;
        cwBtn.ButtonPressed  = orbiter.Clockwise;
        ccwBtn.ButtonPressed = !orbiter.Clockwise;
        cwBtn.Pressed += () =>
        {
            if (orbiter.Clockwise) return;
            _undoStack.Execute(new SimpleCommand(
                () => { orbiter.Clockwise = true;  RefreshOverlays(); },
                () => { orbiter.Clockwise = false; RefreshOverlays(); }
            ));
        };
        ccwBtn.Pressed += () =>
        {
            if (!orbiter.Clockwise) return;
            _undoStack.Execute(new SimpleCommand(
                () => { orbiter.Clockwise = false; RefreshOverlays(); },
                () => { orbiter.Clockwise = true;  RefreshOverlays(); }
            ));
        };
        dirRow.AddChild(cwBtn);
        dirRow.AddChild(ccwBtn);
        _behaviorConfigContainer.AddChild(dirRow);

        // Start Angle
        _behaviorConfigContainer.AddChild(MakeBehaviorLabel("Start Angle (degrees)"));
        var angleEdit = new LineEdit
        {
            Text                = ((int)Mathf.RadToDeg(orbiter.StartAngle)).ToString(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        angleEdit.AddThemeFontSizeOverride("font_size", 11);
        WireIntFilter(angleEdit);
        float angleBefore = 0f;
        angleEdit.FocusEntered += () => angleBefore = orbiter.StartAngle;
        angleEdit.FocusExited  += () =>
        {
            string text = angleEdit.Text;
            if (!float.TryParse(text, out float deg)) return;
            float sa = Mathf.DegToRad(deg);
            if (Mathf.IsEqualApprox(sa, angleBefore)) return;
            float sb = angleBefore;
            _undoStack.ExecuteAlreadyDone(new SimpleCommand(
                () => { orbiter.StartAngle = sa; RefreshSelectionPanel(); },
                () => { orbiter.StartAngle = sb; RefreshSelectionPanel(); }
            ));
        };
        angleEdit.TextChanged += val =>
        {
            if (float.TryParse(val, out float deg)) orbiter.StartAngle = Mathf.DegToRad(deg);
        };
        _behaviorConfigContainer.AddChild(angleEdit);
    }

    // Restricts a LineEdit to digits only (no decimals, no signs).
    private static void WireIntFilter(LineEdit edit)
    {
        bool filtering = false;
        edit.TextChanged += val =>
        {
            if (filtering) return;
            string filtered = string.Concat(val.Where(char.IsDigit));
            if (filtered == val) return;
            filtering = true;
            edit.Text        = filtered;
            edit.CaretColumn = Mathf.Min(edit.CaretColumn, filtered.Length);
            filtering = false;
        };
    }

    // Restricts a LineEdit to a non-negative decimal number (digits + at most one dot).
    private static void WireFloatFilter(LineEdit edit)
    {
        bool filtering = false;
        edit.TextChanged += val =>
        {
            if (filtering) return;
            var  sb     = new System.Text.StringBuilder(val.Length);
            bool hasDot = false;
            foreach (char c in val)
            {
                if (char.IsDigit(c)) { sb.Append(c); }
                else if (c == '.' && !hasDot) { sb.Append(c); hasDot = true; }
            }
            string filtered = sb.ToString();
            if (filtered == val) return;
            filtering = true;
            edit.Text        = filtered;
            edit.CaretColumn = Mathf.Min(edit.CaretColumn, filtered.Length);
            filtering = false;
        };
    }

    private static Label MakeBehaviorLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.60f, 0.60f, 0.65f));
        return label;
    }
}
