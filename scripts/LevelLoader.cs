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
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static LoadResult Load(string path, Node parent)
    {
        string    json = FileAccess.GetFileAsString(path);
        LevelData data = JsonSerializer.Deserialize<LevelData>(json, JsonOptions)!;
        return Populate(data, parent);
    }

    private static LoadResult Populate(LevelData data, Node parent)
    {
        var result = new LoadResult();

        BuildSurfaces(data, parent);

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

    private static void BuildSurfaces(LevelData data, Node parent)
    {
        float T = data.TileSize;

        for (int r = 0; r < data.Height; r++)
        for (int c = 0; c < data.Width;  c++)
        {
            SurfaceType? tl = Corner(data.Corners, r,     c    );
            SurfaceType? tr = Corner(data.Corners, r,     c + 1);
            SurfaceType? bl = Corner(data.Corners, r + 1, c    );
            SurfaceType? br = Corner(data.Corners, r + 1, c + 1);

            float x0 = c * T, x1 = x0 + T, xm = (x0 + x1) * 0.5f;
            float y0 = r * T, y1 = y0 + T, ym = (y0 + y1) * 0.5f;

            if (tl == tr && tl == bl && tl == br)
            {
                if (tl != null)
                    AddZone(parent, tl.Value, new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), new Vector2(T, T));
            }
            else
            {
                // Split into quadrants — each is half the tile in both dimensions
                float qs = T * 0.5f;
                if (tl != null) AddZone(parent, tl.Value, new Vector2((x0 + xm) * 0.5f, (y0 + ym) * 0.5f), new Vector2(qs, qs));
                if (tr != null) AddZone(parent, tr.Value, new Vector2((xm + x1) * 0.5f, (y0 + ym) * 0.5f), new Vector2(qs, qs));
                if (bl != null) AddZone(parent, bl.Value, new Vector2((x0 + xm) * 0.5f, (ym + y1) * 0.5f), new Vector2(qs, qs));
                if (br != null) AddZone(parent, br.Value, new Vector2((xm + x1) * 0.5f, (ym + y1) * 0.5f), new Vector2(qs, qs));
            }
        }
    }

    private static void AddZone(Node parent, SurfaceType type, Vector2 center, Vector2 size)
    {
        parent.AddChild(new SurfaceZone { Type = type, Size = size, Position = center });
    }

    private static SurfaceType? Corner(string[][] corners, int r, int c)
    {
        if (r >= corners.Length || c >= corners[r].Length) return null;
        return corners[r][c] switch
        {
            "g"  => SurfaceType.Ground,
            "s"  => SurfaceType.Slidy,
            "f"  => SurfaceType.Fast,
            "c"  => SurfaceType.Confusing,
            "fc" => SurfaceType.FastConfusing,
            "st" => SurfaceType.Straight,
            "k"  => SurfaceType.Kill,
            _    => null,  // "v" or unknown → void, no zone
        };
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
