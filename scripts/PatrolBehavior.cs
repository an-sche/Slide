using Godot;

namespace Slide;

public record Waypoint(Vector2 Position, float Speed);

public enum PatrolEndBehavior { Loop, Reverse, Disappear }

public class PatrolBehavior : IEnemyBehavior
{
    private readonly Waypoint[]        _waypoints;
    private readonly PatrolEndBehavior _endBehavior;
    private int _current;
    private int _direction = 1;

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
        _current += _direction;

        if (_current >= 0 && _current < _waypoints.Length) return;

        switch (_endBehavior)
        {
            case PatrolEndBehavior.Loop:
                _current = 0;
                break;
            case PatrolEndBehavior.Reverse:
                _direction = -_direction;
                _current   = Mathf.Clamp(_current, 0, _waypoints.Length - 1);
                break;
            default:
                enemy.QueueFree();
                break;
        }
    }
}
