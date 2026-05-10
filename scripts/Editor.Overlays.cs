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
            var (color, shape, label) = e.Kind switch
            {
                "start" => (new Color(0.20f, 0.90f, 0.30f), OverlayShape.Diamond, "Start"),
                "end"   => (new Color(1.00f, 0.80f, 0.10f), OverlayShape.Diamond, "End"),
                "bonus" => (new Color(1.00f, 0.85f, 0.10f), OverlayShape.Circle,  "Bonus"),
                _       => (Colors.White,                    OverlayShape.Circle,  e.Kind),
            };
            string displayLabel = string.IsNullOrEmpty(e.Name) ? label : $"{label} - {e.Name}";
            list.Add(new EditorOverlay(pos, color, shape, displayLabel, Selected: i == _selectedIndex));
        }

        // Enemies — drawn at their start position with a label indicating behavior type.
        for (int i = 0; i < _levelData.Enemies.Length; i++)
        {
            var e           = _levelData.Enemies[i];
            int overlayIdx  = _levelData.Entities.Length + i;
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
            string type         = e.Behavior.Type;
            string enemyKind    = char.ToUpper(type[0]) + type[1..];
            string enemyLabel   = string.IsNullOrEmpty(e.Name) ? enemyKind : $"{enemyKind} - {e.Name}";
            list.Add(new EditorOverlay(pos, ParseColor(e.Color), OverlayShape.Circle, enemyLabel, Selected: overlayIdx == _selectedIndex));
        }

        _canvas.SetOverlays([.. list], GameplayConstants.CellSize);
        RefreshSelectionPanel();
    }

    private void ClearSelection()
    {
        _selectedIndex = -1;
        RefreshSelectionPanel();
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
            var e    = _levelData.Entities[_selectedIndex];
            string kind = e.Kind switch
            {
                "start" => "Start",
                "end"   => "End",
                "bonus" => "Bonus",
                _       => e.Kind,
            };
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            int tx = (int)(e.X / cellSize);
            int ty = (int)(e.Y / cellSize);
            _selectionPosLabel.Text = $"Tile ({tx}, {ty})";
        }
        else
        {
            var e   = _levelData.Enemies[_selectedIndex - _levelData.Entities.Length];
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
            string type = e.Behavior.Type;
            string kind = char.ToUpper(type[0]) + type[1..];
            _selectionKindLabel.Text = string.IsNullOrEmpty(e.Name) ? kind : $"{kind} - {e.Name}";
            int tx = (int)(pos.X / cellSize);
            int ty = (int)(pos.Y / cellSize);
            _selectionPosLabel.Text = $"Tile ({tx}, {ty})";
        }
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        return new Color(
            int.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) / 255f,
            int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) / 255f);
    }
}
