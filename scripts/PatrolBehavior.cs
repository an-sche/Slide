using Godot;

namespace Slide;

public record Waypoint(Vector2 Position, float Speed);

public enum PatrolEndBehavior { Loop, Disappear }

public class PatrolBehavior : IEnemyBehavior
{
    private readonly Waypoint[]        _waypoints;
    private readonly PatrolEndBehavior _endBehavior;
    private int _current;

    public PatrolBehavior(Waypoint[] waypoints, PatrolEndBehavior endBehavior = PatrolEndBehavior.Loop)
    {
        _waypoints   = waypoints;
        _endBehavior = endBehavior;
    }

    public void Process(float delta, Enemy enemy)
    {
        if (_waypoints.Length == 0) return;

        Vector2 target   = _waypoints[_current].Position;
        float   speed    = _waypoints[_current].Speed;
        Vector2 toTarget = target - enemy.GlobalPosition;
        float   distance = toTarget.Length();
        float   step     = speed * delta;

        if (distance <= step)
        {
            enemy.GlobalPosition = target;
            Advance(enemy);
        }
        else
        {
            enemy.GlobalPosition += toTarget.Normalized() * step;
        }
    }

    private void Advance(Enemy enemy)
    {
        _current++;
        if (_current < _waypoints.Length) return;

        if (_endBehavior == PatrolEndBehavior.Loop)
            _current = 0;
        else
            enemy.QueueFree();
    }
}
