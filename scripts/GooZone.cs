using Godot;

namespace Slide;

public partial class GooZone : Area2D
{
    public const float GooRadius     = 50f;
    public const float Lifetime      = 5f;
    public const float SpeedMultiplier = 1.40f;

    private float _remaining = Lifetime;

    public override void _Ready()
    {
        CollisionLayer = Layers.GooZones;
        CollisionMask  = 0;
        Monitorable    = true;
        Monitoring     = false;

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = GooRadius } });
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f)
            QueueFree();
        else
            QueueRedraw();
    }

    public override void _Draw()
    {
        float alpha = (_remaining / Lifetime) * 0.38f;
        DrawCircle(Vector2.Zero, GooRadius, new Color(0.15f, 0.75f, 0.1f, alpha));
        DrawArc(Vector2.Zero, GooRadius, 0, Mathf.Tau, 32,
            new Color(0.2f, 1f, 0.15f, alpha + 0.1f), 1.5f);
    }
}
