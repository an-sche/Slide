using System.Text.Json;

namespace Slide;

public class LevelData
{
    public int     Version     { get; set; }
    public string  Name        { get; set; } = "";
    public string  Author      { get; set; } = "";
    public string? Description { get; set; }
    public float   TileSize    { get; set; }
    public int     Width       { get; set; }
    public int     Height      { get; set; }

    public string[][]    Corners  { get; set; } = [];
    public EntityData[]  Entities { get; set; } = [];
    public EnemyData[]   Enemies  { get; set; } = [];
    public SpawnerData[] Spawners { get; set; } = [];
}

public class EntityData
{
    public string Kind { get; set; } = "";
    public float  X    { get; set; }
    public float  Y    { get; set; }
}

public class EnemyData
{
    public float        Radius   { get; set; }
    public string       Color    { get; set; } = "#e63333";
    public BehaviorData Behavior { get; set; } = new();
}

public class BehaviorData
{
    public string Type { get; set; } = "";

    // patrol
    public WaypointData[]? Waypoints    { get; set; }
    public string?         EndBehavior  { get; set; }

    // wander
    public Vec2Data[]? Polygon  { get; set; }
    public float       Speed    { get; set; }
    public float       MinIdle  { get; set; }
    public float       MaxIdle  { get; set; }
    public ulong       Seed     { get; set; }
    public float?      StartX   { get; set; }
    public float?      StartY   { get; set; }

    // orbiter
    public float CenterX      { get; set; }
    public float CenterY      { get; set; }
    public float Radius       { get; set; }
    public float AngularSpeed { get; set; }
    public bool  Clockwise    { get; set; }
    public float StartAngle   { get; set; }
}

public class WaypointData
{
    public float X     { get; set; }
    public float Y     { get; set; }
    public float Speed { get; set; }
}

public class Vec2Data
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class SpawnerData
{
    public int[]       EnemyIndices { get; set; } = [];
    public JsonElement Condition    { get; set; }  // "immediate" | { type, delay } | { type, triggerId }
}
