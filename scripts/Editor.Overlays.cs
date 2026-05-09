using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class Editor
{
    private void RefreshOverlays()
    {
        if (_levelData == null) return;

        var list = new List<EditorOverlay>();

        foreach (var e in _levelData.Entities)
        {
            var pos = new Vector2(e.X, e.Y);
            var (color, shape, label) = e.Kind switch
            {
                "start" => (new Color(0.20f, 0.90f, 0.30f), OverlayShape.Diamond, "Start"),
                "end"   => (new Color(1.00f, 0.80f, 0.10f), OverlayShape.Diamond, "End"),
                "bonus" => (new Color(1.00f, 0.85f, 0.10f), OverlayShape.Circle,  "Bonus"),
                _       => (Colors.White,                    OverlayShape.Circle,  e.Kind),
            };
            list.Add(new EditorOverlay(pos, color, shape, label));
        }

        // Enemies — drawn at their start position with a label indicating behavior type.
        foreach (var e in _levelData.Enemies)
        {
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
            list.Add(new EditorOverlay(pos, ParseColor(e.Color), OverlayShape.Circle, e.Behavior.Type));
        }

        _canvas.SetOverlays([.. list], GameplayConstants.CellSize);
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
