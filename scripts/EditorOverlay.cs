using Godot;

namespace Slide;

public enum OverlayShape { Circle, Diamond }

public readonly record struct EditorOverlay(
    Vector2      WorldPos,
    Color        Color,
    OverlayShape Shape,
    string       Label,
    bool         Selected = false
);
