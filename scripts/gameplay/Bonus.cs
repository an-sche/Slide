using Godot;

namespace Slide;

public partial class Bonus : Area2D
{
    private const float OuterRadius = 20f;
    private const float InnerRadius = 9f;
    private const int   StarPoints  = 5;

    private static readonly Color FillColor    = new(1f, 0.85f, 0f);
    private static readonly Color OutlineColor = new(1f, 1f, 0.75f);

    public override void _Ready()
    {
        CollisionLayer = Layers.Bonuses;
        CollisionMask  = Layers.Units;
        Monitoring     = true;
        Monitorable    = false;

        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = OuterRadius } });

        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is not Unit u || u.IsDead) return;
        u.PlayerState.PlayerLevel++;
        QueueFree();
    }

    public override void _Draw()
    {
        var pts = BuildStarPoints(OuterRadius, InnerRadius, StarPoints);
        DrawColoredPolygon(pts, FillColor);

        var outline = new Vector2[pts.Length + 1];
        pts.CopyTo(outline, 0);
        outline[pts.Length] = pts[0];
        DrawPolyline(outline, OutlineColor, 1.5f);
    }

    private static Vector2[] BuildStarPoints(float outer, float inner, int points)
    {
        var verts = new Vector2[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            float angle = i * Mathf.Pi / points - Mathf.Pi / 2f;
            float r     = i % 2 == 0 ? outer : inner;
            verts[i]    = Vector2.FromAngle(angle) * r;
        }
        return verts;
    }
}
