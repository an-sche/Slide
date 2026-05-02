using Godot;

namespace Slide;

public partial class SurfaceZone : Area2D
{
    public SurfaceType Type { get; set; } = SurfaceType.Ground;
    public Vector2 Size { get; set; } = new Vector2(100, 100);

    public override void _Ready()
    {
        CollisionLayer = 1;
        CollisionMask = 0;
        Monitoring = false;
        Monitorable = true;

        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size } });
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-Size / 2, Size), GetColor());
    }

    private Color GetColor() => Type switch
    {
        SurfaceType.Slidy         => new Color(0.55f, 0.82f, 1.00f), // light blue
        SurfaceType.Fast          => new Color(0.10f, 0.25f, 0.75f), // dark blue
        SurfaceType.Confusing     => new Color(0.75f, 0.55f, 0.95f), // light purple
        SurfaceType.FastConfusing => new Color(0.38f, 0.10f, 0.58f), // dark purple
        SurfaceType.Straight      => new Color(0.72f, 0.72f, 0.72f), // light gray
        SurfaceType.Kill          => new Color(0.75f, 0.08f, 0.08f), // dark red
        _                         => new Color(0.25f, 0.65f, 0.25f), // green
    };
}
