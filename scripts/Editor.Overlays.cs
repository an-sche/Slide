using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void RefreshOverlays()
    {
        RefreshCanvasOverlays();
        RefreshSelectionPanel();
    }

    private void RefreshCanvasOverlays()
    {
        if (_levelData == null) return;

        var overlays = new List<EditorOverlay>();
        var lines    = new List<EditorLine>();

        for (int i = 0; i < _levelData.Entities.Length; i++)
        {
            var e   = _levelData.Entities[i];
            var pos = new Vector2(e.X, e.Y);
            var (color, shape) = e.Kind switch
            {
                "start" => (new Color(0.20f, 0.90f, 0.30f), OverlayShape.Diamond),
                "end"   => (new Color(1.00f, 0.80f, 0.10f), OverlayShape.Diamond),
                "bonus" => (new Color(1.00f, 0.85f, 0.10f), OverlayShape.Circle),
                _       => (Colors.White,                    OverlayShape.Circle),
            };
            string kind  = EntityKindLabel(e.Kind);
            string label = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            overlays.Add(new EditorOverlay(pos, color, shape, label, Selected: i == _selectedIndex));
        }

        for (int i = 0; i < _levelData.Enemies.Length; i++)
        {
            var  e          = _levelData.Enemies[i];
            int  overlayIdx = _levelData.Entities.Length + i;
            bool selected   = overlayIdx == _selectedIndex;
            var  color      = Color.FromHtml(e.Color);

            if (e.Behavior is PatrolBehaviorData patrol)
            {
                for (int w = 0; w < patrol.Waypoints.Length; w++)
                {
                    var    wpPos = new Vector2(patrol.Waypoints[w].X, patrol.Waypoints[w].Y);
                    string label = w == 0
                        ? (string.IsNullOrEmpty(e.Name) ? "Patrol" : e.Name)
                        : (w + 1).ToString();
                    overlays.Add(new EditorOverlay(wpPos, color, OverlayShape.Circle, label,
                        Selected: selected && w == 0));
                }

                for (int w = 0; w < patrol.Waypoints.Length - 1; w++)
                {
                    var from = new Vector2(patrol.Waypoints[w].X, patrol.Waypoints[w].Y);
                    var to   = new Vector2(patrol.Waypoints[w + 1].X, patrol.Waypoints[w + 1].Y);
                    lines.Add(new EditorLine(from, to, new Color(color.R, color.G, color.B, 0.7f)));
                }

                if (patrol.EndBehavior == "loop" && patrol.Waypoints.Length > 1)
                {
                    var from = new Vector2(patrol.Waypoints[^1].X, patrol.Waypoints[^1].Y);
                    var to   = new Vector2(patrol.Waypoints[0].X, patrol.Waypoints[0].Y);
                    lines.Add(new EditorLine(from, to, new Color(color.R, color.G, color.B, 0.30f)));
                }
            }
            else
            {
                string kind  = EnemyKindLabel(e.Behavior);
                string label = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
                overlays.Add(new EditorOverlay(EnemyOrigin(e.Behavior), color, OverlayShape.Circle, label,
                    Selected: selected));
            }
        }

        _canvas.SetOverlays([..overlays], [..lines], GameplayConstants.CellSize);
    }

    private void ClearSelection()
    {
        _selectedIndex = -1;
        RefreshOverlays();
        SyncNameField();
    }

    private void SyncNameField()
    {
        if (_selectedIndex < 0 || _levelData == null) { _selectionNameEdit.Text = ""; return; }

        string? name = _selectedIndex < _levelData.Entities.Length
            ? _levelData.Entities[_selectedIndex].Name
            : _levelData.Enemies[_selectedIndex - _levelData.Entities.Length].Name;

        _selectionNameEdit.Text = name ?? "";
    }

    private void RefreshSelectionPanel()
    {
        bool inEntityMode = _mode is EditorMode.Entities or EditorMode.Enemies;
        _selectionSection.Visible = inEntityMode;

        if (!inEntityMode || _selectedIndex < 0 || _levelData == null)
        {
            _selectionHint.Visible    = true;
            _selectionDetails.Visible = false;
            PopulateBehaviorConfig();
            return;
        }

        _selectionHint.Visible    = false;
        _selectionDetails.Visible = true;

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var    e    = _levelData.Entities[_selectedIndex];
            string kind = EntityKindLabel(e.Kind);
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
        }
        else
        {
            var    e    = _levelData.Enemies[_selectedIndex - _levelData.Entities.Length];
            string kind = EnemyKindLabel(e.Behavior);
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
        }

        SyncPositionFields();
        PopulateBehaviorConfig();
    }

    private static string EntityKindLabel(string kind) => kind switch
    {
        "start" => "Start",
        "end"   => "End",
        "bonus" => "Bonus",
        _       => kind,
    };

    private static string EnemyKindLabel(BehaviorData b) => b switch
    {
        PatrolBehaviorData  => "Patrol",
        WanderBehaviorData  => "Wander",
        OrbiterBehaviorData => "Orbiter",
        _                   => "",
    };

    private static Vector2 EnemyOrigin(BehaviorData b) => b switch
    {
        PatrolBehaviorData  p => p.Waypoints.Length > 0 ? new Vector2(p.Waypoints[0].X, p.Waypoints[0].Y) : Vector2.Zero,
        OrbiterBehaviorData o => new Vector2(o.CenterX, o.CenterY),
        WanderBehaviorData  w => w.StartX.HasValue
                                     ? new Vector2(w.StartX.Value, w.StartY!.Value)
                                     : (w.Polygon.Length > 0 ? new Vector2(w.Polygon[0].X, w.Polygon[0].Y) : Vector2.Zero),
        _                     => Vector2.Zero,
    };
}
