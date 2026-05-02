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
        SurfaceType.Slidy        => new Color(0.50f, 0.80f, 1.00f),
        SurfaceType.Fast         => new Color(1.00f, 0.60f, 0.20f),
        SurfaceType.Confusing    => new Color(0.60f, 0.20f, 0.80f),
        SurfaceType.FastConfusing => new Color(0.90f, 0.15f, 0.30f),
        SurfaceType.Straight     => new Color(0.55f, 0.55f, 0.55f),
        SurfaceType.Kill         => new Color(0.75f, 0.08f, 0.08f),
        _                        => new Color(0.18f, 0.32f, 0.14f),
    };
}
