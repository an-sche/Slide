using Godot;

namespace Slide;

public class OrbiterBehavior : IEnemyBehavior
{
    private readonly Vector2 _center;
    private readonly float   _radius;
    private readonly float   _angularSpeed;
    private float            _angle;

    public OrbiterBehavior(Vector2 center, float radius, float angularSpeed, bool clockwise = true, float startAngle = 0f)
    {
        _center       = center;
        _radius       = radius;
        _angularSpeed = clockwise ? angularSpeed : -angularSpeed;
        _angle        = startAngle;
    }

    public void Process(float delta, Enemy enemy)
    {
        _angle              += _angularSpeed * delta;
        enemy.GlobalPosition = _center + Vector2.FromAngle(_angle) * _radius;
    }

    public void Draw(Enemy enemy)
    {
        Vector2 localCenter = enemy.ToLocal(_center);
        enemy.DrawArc(localCenter, _radius, 0, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.12f), 1f);
    }
}
