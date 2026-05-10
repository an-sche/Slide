using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Slide;

public class LevelData
{
    public int     Version     { get; set; }
    public string  Name        { get; set; } = "";
    public string  Author      { get; set; } = "";
    public string? Description { get; set; }
    public string  Bitmap      { get; set; } = "";

    public EntityData[]  Entities { get; set; } = [];
    public EnemyData[]   Enemies  { get; set; } = [];
    public SpawnerData[] Spawners { get; set; } = [];
}

public class EntityData
{
    public string  Id   { get; set; } = Guid.NewGuid().ToString();
    public string  Kind { get; set; } = "";
    public string? Name { get; set; }
    public float   X    { get; set; }
    public float   Y    { get; set; }
}

public class EnemyData
{
    public string       Id       { get; set; } = Guid.NewGuid().ToString();
    public string?      Name     { get; set; }
    public float        Radius   { get; set; }
    public string       Color    { get; set; } = "#e63333";
    public BehaviorData Behavior { get; set; } = null!;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PatrolBehaviorData),  "patrol")]
[JsonDerivedType(typeof(WanderBehaviorData),  "wander")]
[JsonDerivedType(typeof(OrbiterBehaviorData), "orbiter")]
[JsonDerivedType(typeof(ChaserBehaviorData),  "chaser")]
[JsonDerivedType(typeof(BouncerBehaviorData), "bouncer")]
[JsonDerivedType(typeof(SniperBehaviorData),  "sniper")]
[JsonDerivedType(typeof(GuardBehaviorData),   "guard")]
public abstract class BehaviorData { }

public class PatrolBehaviorData : BehaviorData
{
    public WaypointData[] Waypoints   { get; set; } = [];
    public string         EndBehavior { get; set; } = "reverse";
}

public class WanderBehaviorData : BehaviorData
{
    public Vec2Data[] Polygon { get; set; } = [];
    public float      Speed   { get; set; }
    public float      MinIdle { get; set; }
    public float      MaxIdle { get; set; }
    public ulong      Seed    { get; set; }
    public float?     StartX  { get; set; }
    public float?     StartY  { get; set; }
}

public class OrbiterBehaviorData : BehaviorData
{
    public float CenterX      { get; set; }
    public float CenterY      { get; set; }
    public float Radius       { get; set; }
    public float AngularSpeed { get; set; }
    public bool  Clockwise    { get; set; }
    public float StartAngle   { get; set; }
}

public class ChaserBehaviorData : BehaviorData
{
    public float StartX          { get; set; }
    public float StartY          { get; set; }
    public float Speed           { get; set; }
    public float DetectionRadius { get; set; }
    public float GiveUpRadius    { get; set; }
}

public class BouncerBehaviorData : BehaviorData
{
    public float StartX { get; set; }
    public float StartY { get; set; }
    public float Angle  { get; set; }
    public float Speed  { get; set; }
}

public class SniperBehaviorData : BehaviorData
{
    public float X            { get; set; }
    public float Y            { get; set; }
    public float Range        { get; set; }
    public float FireInterval { get; set; }
    public float AimDuration  { get; set; }
}

public class GuardBehaviorData : BehaviorData
{
    public WaypointData[] Waypoints       { get; set; } = [];
    public float          DetectionRadius { get; set; }
    public float          GiveUpRadius    { get; set; }
    public float          AlertSpeed      { get; set; }
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
    public JsonElement Condition    { get; set; }
}
