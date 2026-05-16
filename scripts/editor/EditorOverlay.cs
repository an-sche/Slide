using Godot;

namespace Slide;

public enum OverlayShape { Circle, Diamond, Square }

public readonly record struct EditorOverlay(
    Vector2      WorldPos,
    Color        Color,
    OverlayShape Shape,
    string       Label,
    bool         Selected = false
);

public readonly record struct EditorLine(Vector2 WorldFrom, Vector2 WorldTo, Color Color);
