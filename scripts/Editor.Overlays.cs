using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void RefreshOverlays()
    {
        if (_levelData == null) return;

        var list = new List<EditorOverlay>();

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
            list.Add(new EditorOverlay(pos, color, shape, label, Selected: i == _selectedIndex));
        }

        for (int i = 0; i < _levelData.Enemies.Length; i++)
        {
            var    e          = _levelData.Enemies[i];
            int    overlayIdx = _levelData.Entities.Length + i;
            string kind       = EnemyKindLabel(e.Behavior);
            string label      = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            list.Add(new EditorOverlay(EnemyOrigin(e.Behavior), Color.FromHtml(e.Color), OverlayShape.Circle, label, Selected: overlayIdx == _selectedIndex));
        }

        _canvas.SetOverlays([.. list], GameplayConstants.CellSize);
        RefreshSelectionPanel();
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
            return;
        }

        _selectionHint.Visible    = false;
        _selectionDetails.Visible = true;

        float cellSize = GameplayConstants.CellSize;

        if (_selectedIndex < _levelData.Entities.Length)
        {
            var    e    = _levelData.Entities[_selectedIndex];
            string kind = EntityKindLabel(e.Kind);
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            _selectionPosLabel.Text  = $"Tile ({(int)(e.X / cellSize)}, {(int)(e.Y / cellSize)})";
        }
        else
        {
            var    e    = _levelData.Enemies[_selectedIndex - _levelData.Entities.Length];
            string kind = EnemyKindLabel(e.Behavior);
            var    pos  = EnemyOrigin(e.Behavior);
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            _selectionPosLabel.Text  = $"Tile ({(int)(pos.X / cellSize)}, {(int)(pos.Y / cellSize)})";
        }
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
        ChaserBehaviorData  => "Chaser",
        BouncerBehaviorData => "Bouncer",
        SniperBehaviorData  => "Sniper",
        GuardBehaviorData   => "Guard",
        _                   => "",
    };

    private static Vector2 EnemyOrigin(BehaviorData b) => b switch
    {
        PatrolBehaviorData  p => p.Waypoints.Length > 0 ? new Vector2(p.Waypoints[0].X, p.Waypoints[0].Y) : Vector2.Zero,
        OrbiterBehaviorData o => new Vector2(o.CenterX, o.CenterY),
        WanderBehaviorData  w => w.StartX.HasValue
                                     ? new Vector2(w.StartX.Value, w.StartY!.Value)
                                     : (w.Polygon.Length > 0 ? new Vector2(w.Polygon[0].X, w.Polygon[0].Y) : Vector2.Zero),
        ChaserBehaviorData  c => new Vector2(c.StartX, c.StartY),
        BouncerBehaviorData n => new Vector2(n.StartX, n.StartY),
        SniperBehaviorData  s => new Vector2(s.X, s.Y),
        GuardBehaviorData   g => g.Waypoints.Length > 0 ? new Vector2(g.Waypoints[0].X, g.Waypoints[0].Y) : Vector2.Zero,
        _                     => Vector2.Zero,
    };
}
