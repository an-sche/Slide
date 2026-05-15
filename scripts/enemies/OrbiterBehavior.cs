using Godot;

namespace Slide;

public class OrbiterBehavior : IEnemyBehavior
{
    private readonly Vector2 _center;
    private readonly float   _radius;
    private float            _angularSpeed;  // mutable — flips sign on arc bounce
    private float            _angle;

    // Arc mode (endAngle supplied)
    private readonly bool  _hasArc;
    private float          _arcFrom;  // current "start" endpoint (swaps on each bounce)
    private float          _arcTo;    // current "target" endpoint (swaps on each bounce)
    private readonly float _drawFrom; // fixed lower angle for in-game arc draw
    private readonly float _drawTo;   // fixed upper angle for in-game arc draw

    public OrbiterBehavior(Vector2 center, float radius, float angularSpeed, bool clockwise = true,
                           float startAngle = 0f, float? endAngle = null)
    {
        _center       = center;
        _radius       = radius;
        _angularSpeed = clockwise ? angularSpeed : -angularSpeed;
        _angle        = startAngle;

        if (endAngle.HasValue)
        {
            _hasArc  = true;
            _arcFrom = startAngle;
            _arcTo   = endAngle.Value;

            // Normalize _arcTo to lie "past" startAngle in the initial travel direction
            if (_angularSpeed > 0)
            {
                while (_arcTo <= _arcFrom) _arcTo += Mathf.Tau;
                if (_arcTo > _arcFrom + Mathf.Tau) _arcTo = _arcFrom + Mathf.Tau;
            }
            else
            {
                while (_arcTo >= _arcFrom) _arcTo -= Mathf.Tau;
                if (_arcTo < _arcFrom - Mathf.Tau) _arcTo = _arcFrom - Mathf.Tau;
            }

            _drawFrom = Mathf.Min(_arcFrom, _arcTo);
            _drawTo   = Mathf.Max(_arcFrom, _arcTo);
        }
    }

    public void Process(float delta, Enemy enemy)
    {
        _angle += _angularSpeed * delta;

        if (_hasArc)
        {
            bool pastTarget = _angularSpeed > 0 ? _angle >= _arcTo : _angle <= _arcTo;
            if (pastTarget)
            {
                _angle             = _arcTo;
                _angularSpeed      = -_angularSpeed;
                (_arcFrom, _arcTo) = (_arcTo, _arcFrom);
            }
        }

        enemy.GlobalPosition = _center + Vector2.FromAngle(_angle) * _radius;
    }

    public void Draw(Enemy enemy)
    {
        Vector2 localCenter = enemy.ToLocal(_center);
        if (_hasArc)
            enemy.DrawArc(localCenter, _radius, _drawFrom, _drawTo, 32, new Color(1f, 1f, 1f, 0.12f), 1f);
        else
            enemy.DrawArc(localCenter, _radius, 0, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.12f), 1f);
    }
}
