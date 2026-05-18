using Godot;

namespace Slide;

public partial class Portal : Area2D
{
    private const float Radius       = 40f;
    private const float CooldownTime = 0.5f;

    private static readonly Color PortalColor = new(0.60f, 0.20f, 1.00f);

    public Portal? LinkedPortal { get; set; }

    private float _cooldown;

    public override void _Ready()
    {
        CollisionLayer = Layers.Portals;
        CollisionMask  = Layers.Units;
        Monitoring  = true;
        Monitorable = false;

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius } });

        AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        if (_cooldown > 0f)
            _cooldown -= (float)delta;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, new Color(PortalColor, 0.18f));
        DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 32, PortalColor, 3f);

        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.Tau / 6f;
            DrawLine(Vector2.FromAngle(a) * (Radius * 0.3f), Vector2.FromAngle(a) * (Radius * 0.75f), PortalColor, 2f);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (_cooldown > 0f || LinkedPortal == null) return;
        if (area is not Unit u || u.IsDead) return;

        u.GlobalPosition    = LinkedPortal.GlobalPosition;
        LinkedPortal._cooldown = CooldownTime;
        _cooldown           = CooldownTime;
    }
}
