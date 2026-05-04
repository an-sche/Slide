using Godot;

namespace Slide;

public partial class DonutProjectile : Area2D
{
    private const float VisualRadius = 20f;

    public Vector2 MoveVelocity { get; set; }
    public float   Lifetime     { get; set; }

    private float _remaining;

    public override void _Ready()
    {
        _remaining = Lifetime;

        CollisionLayer = Layers.Donuts;
        CollisionMask  = Layers.Corpses;
        Monitoring     = true;
        Monitorable    = false;

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = VisualRadius } });

        AreaEntered += area => { if (area is Corpse c) c.OnResurrect?.Invoke(); };
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f)
        {
            QueueFree();
            return;
        }
        GlobalPosition += MoveVelocity * (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float a     = Lifetime > 0f ? _remaining / Lifetime : 0f;
        float t     = (float)(Time.GetTicksMsec() % 1000) / 1000f;
        float pulse = (Mathf.Sin(t * Mathf.Tau) + 1f) * 0.5f;

        DrawArc(Vector2.Zero, VisualRadius + 5f + pulse * 2f, 0, Mathf.Tau, 32,
            new Color(0.9f, 0.4f, 1f, a * 0.3f), 3f);
        DrawArc(Vector2.Zero, VisualRadius, 0, Mathf.Tau, 32,
            new Color(0.95f, 0.55f, 1f, a * 0.9f), 7f);
    }
}
