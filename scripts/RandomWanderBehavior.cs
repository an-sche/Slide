using Godot;

namespace Slide;

public class RandomWanderBehavior : IEnemyBehavior
{
    private const float TelegraphDuration = 0.5f;

    private readonly Vector2[] _polygon;
    private readonly float     _speed;
    private readonly float     _minIdle;
    private readonly float     _maxIdle;
    private readonly Vector2?  _startPosition;
    private readonly Vector2   _centroid;

    // Precomputed triangulation for O(1) uniform point sampling
    private readonly int[]   _triIndices;
    private readonly float[] _cumulativeAreas;
    private readonly float   _totalArea;

    private enum WanderState { Idle, Telegraph, Moving }
    private WanderState _state       = WanderState.Idle;
    private float       _idleTimer;
    private float       _telegraphTimer;
    private Vector2     _target;
    private bool        _initialized;

    public RandomWanderBehavior(
        Vector2[] polygon,
        float     speed,
        float     minIdleDuration,
        float     maxIdleDuration,
        Vector2?  startPosition = null)
    {
        _polygon       = polygon;
        _speed         = speed;
        _minIdle       = minIdleDuration;
        _maxIdle       = maxIdleDuration;
        _startPosition = startPosition;

        var sum = Vector2.Zero;
        foreach (var p in polygon) sum += p;
        _centroid = sum / polygon.Length;

        _triIndices = Geometry2D.TriangulatePolygon(polygon);
        int triCount = _triIndices.Length / 3;
        _cumulativeAreas = new float[triCount];
        float running = 0f;
        for (int i = 0; i < triCount; i++)
        {
            Vector2 a = _polygon[_triIndices[i * 3]];
            Vector2 b = _polygon[_triIndices[i * 3 + 1]];
            Vector2 c = _polygon[_triIndices[i * 3 + 2]];
            running += Mathf.Abs((b - a).Cross(c - a)) * 0.5f;
            _cumulativeAreas[i] = running;
        }
        _totalArea = running;
    }

    public void Process(float delta, Enemy enemy)
    {
        if (!_initialized)
        {
            _initialized         = true;
            enemy.GlobalPosition = _startPosition ?? GetRandomPoint();
            EnterIdle();
        }

        switch (_state)
        {
            case WanderState.Idle:
                _idleTimer -= delta;
                if (_idleTimer <= 0f)
                {
                    _target         = GetRandomPoint();
                    _state          = WanderState.Telegraph;
                    _telegraphTimer = TelegraphDuration;
                }
                break;

            case WanderState.Telegraph:
                _telegraphTimer -= delta;
                if (_telegraphTimer <= 0f)
                    _state = WanderState.Moving;
                break;

            case WanderState.Moving:
                Vector2 toTarget = _target - enemy.GlobalPosition;
                float   distance = toTarget.Length();
                float   step     = _speed * delta;
                if (distance <= step)
                {
                    enemy.GlobalPosition = _target;
                    EnterIdle();
                }
                else
                {
                    enemy.GlobalPosition += toTarget.Normalized() * step;
                }
                break;
        }
    }

    public void Draw(Enemy enemy)
    {
        if (_state != WanderState.Telegraph) return;

        float progress = 1f - (_telegraphTimer / TelegraphDuration);
        enemy.DrawArc(Vector2.Zero, enemy.Radius + 6f, 0, Mathf.Tau, 32,
            new Color(1f, 0.85f, 0.1f, progress), 2.5f);
    }

    private void EnterIdle()
    {
        _state     = WanderState.Idle;
        _idleTimer = (float)GD.RandRange(_minIdle, _maxIdle);
    }

    // Picks a random triangle weighted by area, then a uniform random point within it.
    // Guarantees even coverage of the polygon with exactly 3 random numbers, no loops.
    private Vector2 GetRandomPoint()
    {
        if (_totalArea <= 0f) return _centroid;

        float pick = (float)GD.RandRange(0.0, _totalArea);
        int tri = _cumulativeAreas.Length - 1;
        for (int i = 0; i < _cumulativeAreas.Length; i++)
        {
            if (pick <= _cumulativeAreas[i]) { tri = i; break; }
        }

        Vector2 a = _polygon[_triIndices[tri * 3]];
        Vector2 b = _polygon[_triIndices[tri * 3 + 1]];
        Vector2 c = _polygon[_triIndices[tri * 3 + 2]];

        float r1 = (float)GD.RandRange(0.0, 1.0);
        float r2 = (float)GD.RandRange(0.0, 1.0);
        if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
        return a + r1 * (b - a) + r2 * (c - a);
    }
}
