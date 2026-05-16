using Godot;

namespace Slide;

public partial class Wall : Area2D
{
    public Vector2 WallSize { get; set; } = new(80f, 80f);

    private static readonly Color FillColor   = new(0.20f, 0.20f, 0.28f);
    private static readonly Color BorderColor = new(0.55f, 0.55f, 0.75f);

    public override void _Ready()
    {
        CollisionLayer = Layers.Walls;
        CollisionMask  = 0;
        Monitoring     = false;
        Monitorable    = true;

        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = WallSize } });
    }

    public override void _Draw()
    {
        var h = WallSize / 2f;
        DrawRect(new Rect2(-h, WallSize), FillColor);
        DrawRect(new Rect2(-h, WallSize), BorderColor, filled: false, width: 3f);
    }
}
