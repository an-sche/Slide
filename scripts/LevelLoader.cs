using Godot;
using System;
using System.Globalization;
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

    private static LoadResult Populate(LevelData data, string levelDir, Node parent)
    {
        var result = new LoadResult();

        result.LevelBounds = BuildSurfaces(data, levelDir, parent);

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

        foreach (var spawner in data.Spawners)
        {
            bool immediate = spawner.Condition.ValueKind == JsonValueKind.String
                && spawner.Condition.GetString() == "immediate";
            if (!immediate) continue;

            foreach (int idx in spawner.EnemyIndices)
            {
                if (idx < data.Enemies.Length)
                    parent.AddChild(BuildEnemy(data.Enemies[idx]));
            }
        }

        return result;
    }

    // --- Surface zone construction ---

    private static Rect2 BuildSurfaces(LevelData data, string levelDir, Node parent)
    {
        string imagePath = levelDir + "/" + data.Bitmap;
        var    image     = Image.LoadFromFile(imagePath);
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
        var behavior = BuildBehavior(data.Behavior);
        var enemy = new Enemy
        {
            Radius   = data.Radius,
            Color    = ParseColor(data.Color),
            Behavior = behavior,
        };
        if (data.Behavior.Type == "patrol" && data.Behavior.Waypoints?.Length > 0)
            enemy.Position = new Vector2(data.Behavior.Waypoints[0].X, data.Behavior.Waypoints[0].Y);
        return enemy;
    }

    private static IEnemyBehavior BuildBehavior(BehaviorData b) => b.Type switch
    {
        "patrol"  => BuildPatrol(b),
        "wander"  => BuildWander(b),
        "orbiter" => BuildOrbiter(b),
        _         => throw new InvalidOperationException($"Unknown enemy behavior type: '{b.Type}'"),
    };

    private static IEnemyBehavior BuildPatrol(BehaviorData b)
    {
        var waypoints = Array.ConvertAll(b.Waypoints!, w => new Waypoint(new Vector2(w.X, w.Y), w.Speed));
        var end = b.EndBehavior == "loop" ? PatrolEndBehavior.Loop : PatrolEndBehavior.Disappear;
        return new PatrolBehavior(waypoints, end);
    }

    private static IEnemyBehavior BuildWander(BehaviorData b)
    {
        var polygon = Array.ConvertAll(b.Polygon!, v => new Vector2(v.X, v.Y));
        Vector2? start = (b.StartX.HasValue && b.StartY.HasValue)
            ? new Vector2(b.StartX.Value, b.StartY.Value)
            : (Vector2?)null;
        return new RandomWanderBehavior(polygon, b.Speed, b.MinIdle, b.MaxIdle, start, b.Seed);
    }

    private static IEnemyBehavior BuildOrbiter(BehaviorData b) =>
        new OrbiterBehavior(new Vector2(b.CenterX, b.CenterY), b.Radius, b.AngularSpeed, b.Clockwise, b.StartAngle);

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        return new Color(
            int.Parse(hex[0..2], NumberStyles.HexNumber) / 255f,
            int.Parse(hex[2..4], NumberStyles.HexNumber) / 255f,
            int.Parse(hex[4..6], NumberStyles.HexNumber) / 255f);
    }
}
