using Godot;
using System;
using System.Text.Json;

namespace Slide;

public static class LevelLoader
{
    public struct LoadResult
    {
        public Vector2   StartPosition;
        public EndBlock? EndBlock;
        public Rect2     LevelBounds;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static LoadResult Load(string path, Node parent)
    {
        string    json = FileAccess.GetFileAsString(path);
        LevelData data = JsonSerializer.Deserialize<LevelData>(json, JsonOptions)!;
        return Populate(data, path.GetBaseDir(), parent);
    }

    public static LoadResult Load(LevelData data, Image image, Node parent) =>
        Populate(data, image, parent);

    private static LoadResult Populate(LevelData data, string levelDir, Node parent) =>
        Populate(data, Image.LoadFromFile(levelDir + "/" + data.Bitmap), parent);

    private static LoadResult Populate(LevelData data, Image image, Node parent)
    {
        var result = new LoadResult();

        result.LevelBounds = BuildSurfaces(image, parent);

        foreach (var e in data.Entities)
        {
            var pos = new Vector2(e.X, e.Y);
            switch (e.Kind)
            {
                case "start":
                    result.StartPosition = pos;
                    parent.AddChild(new StartBlock { Position = pos });
                    break;
                case "end":
                    var endBlock = new EndBlock { Position = pos };
                    parent.AddChild(endBlock);
                    result.EndBlock = endBlock;
                    break;
                case "bonus":
                    parent.AddChild(new Bonus { Position = pos });
                    break;
            }
        }

        foreach (var e in data.Enemies)
        {
            if (e.Spawn is null or ImmediateSpawnData)
                parent.AddChild(BuildEnemy(e));
            // TimedSpawn and TriggerSpawn handled when those systems are built
        }

        return result;
    }

    // --- Surface zone construction ---

    private static Rect2 BuildSurfaces(Image image, Node parent)
    {
        float  cs        = GameplayConstants.CellSize;
        int    w         = image.GetWidth();
        int    h         = image.GetHeight();

        for (int y = 0; y < h; y++)
        {
            int x = 0;
            while (x < w)
            {
                SurfaceType? type = SurfaceConstants.FromColor(image.GetPixel(x, y));
                if (type == null) { x++; continue; }

                // Merge adjacent same-type cells into one zone (row run-length encoding)
                int start = x;
                while (x < w && SurfaceConstants.FromColor(image.GetPixel(x, y)) == type)
                    x++;
                int run = x - start;

                parent.AddChild(new SurfaceZone
                {
                    Type     = type.Value,
                    Size     = new Vector2(run * cs, cs),
                    Position = new Vector2((start + run * 0.5f) * cs, (y + 0.5f) * cs),
                });
            }
        }

        return new Rect2(0, 0, w * cs, h * cs);
    }

    // --- Enemy construction ---

    private static Enemy BuildEnemy(EnemyData data)
    {
        var enemy = new Enemy
        {
            Radius   = data.Radius,
            Color    = Color.FromHtml(data.Color),
            Behavior = BuildBehavior(data.Behavior),
        };
        if (data.Behavior is PatrolBehaviorData p && p.Waypoints.Length > 0)
            enemy.Position = new Vector2(p.Waypoints[0].X, p.Waypoints[0].Y);
        return enemy;
    }

    private static IEnemyBehavior BuildBehavior(BehaviorData b) => b switch
    {
        PatrolBehaviorData  p => BuildPatrol(p),
        WanderBehaviorData  w => BuildWander(w),
        OrbiterBehaviorData o => BuildOrbiter(o),
        _ => throw new InvalidOperationException($"Unknown behavior type: {b.GetType().Name}"),
    };

    private static IEnemyBehavior BuildPatrol(PatrolBehaviorData b)
    {
        var waypoints = Array.ConvertAll(b.Waypoints, w => new Waypoint(new Vector2(w.X, w.Y), w.Speed));
        var end = b.EndBehavior == "loop" ? PatrolEndBehavior.Loop : PatrolEndBehavior.Disappear;
        return new PatrolBehavior(waypoints, end);
    }

    private static IEnemyBehavior BuildWander(WanderBehaviorData b)
    {
        var polygon = Array.ConvertAll(b.Polygon, v => new Vector2(v.X, v.Y));
        Vector2? start = (b.StartX.HasValue && b.StartY.HasValue)
            ? new Vector2(b.StartX.Value, b.StartY.Value)
            : (Vector2?)null;
        return new RandomWanderBehavior(polygon, b.Speed, b.MinIdle, b.MaxIdle, start, b.Seed);
    }

    private static IEnemyBehavior BuildOrbiter(OrbiterBehaviorData b) =>
        new OrbiterBehavior(new Vector2(b.CenterX, b.CenterY), b.Radius, b.AngularSpeed, b.Clockwise, b.StartAngle);
}
