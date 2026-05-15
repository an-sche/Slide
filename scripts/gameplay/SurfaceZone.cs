using Godot;

namespace Slide;

public partial class SurfaceZone : Area2D
{
    public SurfaceType Type { get; set; } = SurfaceType.Ground;
    public Vector2 Size { get; set; } = new Vector2(100, 100);

    public override void _Ready()
    {
        CollisionLayer = Layers.Surfaces;
        CollisionMask  = 0;
        Monitoring = false;
        Monitorable = true;

        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size } });
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-Size / 2, Size), GetColor());
    }

    private Color GetColor() => SurfaceConstants.ForType(Type);
}
