using Godot;

namespace Slide;

public partial class WarpGhost : Node2D
{
    public float   Fraction { get; set; } = 1f;
    public Vector2 Facing   { get; set; } = Vector2.Right;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        float t     = (float)(Time.GetTicksMsec() % 1000) / 1000f;
        float pulse = (Mathf.Sin(t * Mathf.Tau) + 1f) * 0.5f;
        float a     = Fraction;

        // Outer pulsing glow
        DrawArc(Vector2.Zero, Unit.Radius + 4f + pulse * 3f, 0, Mathf.Tau, 32,
            new Color(0.7f, 0.5f, 1f, a * 0.35f), 1.5f);

        // Main ring
        DrawArc(Vector2.Zero, Unit.Radius, 0, Mathf.Tau, 32,
            new Color(0.85f, 0.7f, 1f, a * 0.85f), 2f);

        // Facing arrow
        DrawLine(Vector2.Zero, Facing * (Unit.Radius + 10f),
            new Color(1f, 1f, 1f, a * 0.9f), 2.5f);
    }
}
