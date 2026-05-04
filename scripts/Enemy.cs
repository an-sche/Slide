using Godot;

namespace Slide;

public partial class Enemy : Area2D
{
    public float          Radius   { get; set; } = 16f;
    public Color          Color    { get; set; } = new Color(0.9f, 0.2f, 0.2f);
    public IEnemyBehavior Behavior { get; set; } = null!;

    public override void _Ready()
    {
        CollisionLayer = 64;
        CollisionMask  = 2; // detects units

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius } });

        AddToGroup("enemies");

        AreaEntered += area => { if (area is Unit u && !u.IsDead) u.TriggerDeath(); };
    }

    public override void _Process(double delta)
    {
        Behavior?.Process((float)delta, this);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, Color);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, new Color(1f, 1f, 1f, 0.4f), 1.5f);
    }
}
