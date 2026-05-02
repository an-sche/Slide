using Godot;

namespace Slide;

public partial class Corpse : Node2D
{
    private const float Radius = 16f;

    public Color UnitColor { get; set; } = new Color(0.2f, 0.8f, 1f);

    public override void _Draw()
    {
        var faded = new Color(UnitColor.R, UnitColor.G, UnitColor.B, 0.35f);
        DrawCircle(Vector2.Zero, Radius, faded);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, new Color(1f, 1f, 1f, 0.25f), 1.5f);
    }
}
