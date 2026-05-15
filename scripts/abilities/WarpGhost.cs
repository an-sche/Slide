using Godot;

namespace Slide;

public partial class WarpGhost : Node2D
{
    public float   Fraction { get; set; } = 1f;
    public Vector2 Facing   { get; set; } = Vector2.Right;
    // When > 0 the ghost manages its own fade-out (used on clients that don't run WarpAbility).
    public float   Duration { get; set; } = 0f;

    private float _elapsed;

    public override void _Process(double delta)
    {
        if (Duration > 0f)
        {
            _elapsed += (float)delta;
            Fraction  = Mathf.Max(0f, 1f - _elapsed / Duration);
            if (Fraction <= 0f) { QueueFree(); return; }
        }
        QueueRedraw();
    }

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
