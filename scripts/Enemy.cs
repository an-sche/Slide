using Godot;

namespace Slide;

public partial class Enemy : Area2D
{
    public float           Radius            { get; set; } = 16f;
    public Color           Color             { get; set; } = new Color(0.9f, 0.2f, 0.2f);
    public IEnemyBehavior? Behavior          { get; set; }
    public float           TelegraphProgress { get; set; }

    public override void _Ready()
    {
        CollisionLayer = Layers.Enemies;
        CollisionMask  = Layers.Units;

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius } });

        AddToGroup("enemies");

        AreaEntered += area =>
        {
            if (area is not Unit u || u.IsDead) return;
            if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
            u.TriggerDeath();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        Behavior?.Process((float)delta, this);
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        Behavior?.Draw(this);
        DrawCircle(Vector2.Zero, Radius, Color);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, new Color(1f, 1f, 1f, 0.4f), 1.5f);
        if (TelegraphProgress > 0f)
            DrawArc(Vector2.Zero, Radius + 6f, 0, Mathf.Tau, 32,
                new Color(1f, 0.85f, 0.1f, TelegraphProgress), 2.5f);
    }
}
