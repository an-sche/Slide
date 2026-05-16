using System;
using System.Text.Json.Serialization;

namespace Slide;

public class LevelData
{
    public int     Version     { get; set; }
    public string  Name        { get; set; } = "";
    public string  Author      { get; set; } = "";
    public string? Description { get; set; }
    public string  Bitmap      { get; set; } = "";

    public EntityData[] Entities { get; set; } = [];
    public EnemyData[]  Enemies  { get; set; } = [];
}

public class EntityData
{
    public string  Id       { get; set; } = Guid.NewGuid().ToString();
    public string  Kind     { get; set; } = "";
    public string? Name     { get; set; }
    public float   X        { get; set; }
    public float   Y        { get; set; }
    public float?  Width    { get; set; }
    public float?  Height   { get; set; }
    public float?  Rotation { get; set; }  // degrees; wall-only
}

public class EnemyData
{
    public string               Id       { get; set; } = Guid.NewGuid().ToString();
    public string?              Name     { get; set; }
    public float                Radius   { get; set; }
    public string               Color    { get; set; } = "#e63333";
    public BehaviorData         Behavior { get; set; } = null!;
    public SpawnConditionData?  Spawn    { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ImmediateSpawnData), "immediate")]
[JsonDerivedType(typeof(TimedSpawnData),     "timed")]
[JsonDerivedType(typeof(TriggerSpawnData),   "trigger")]
public abstract class SpawnConditionData { }

public class ImmediateSpawnData : SpawnConditionData { }

public class TimedSpawnData : SpawnConditionData
{
    public float Delay { get; set; }
}

public class TriggerSpawnData : SpawnConditionData
{
    public string TriggerId { get; set; } = "";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PatrolBehaviorData),  "patrol")]
[JsonDerivedType(typeof(WanderBehaviorData),  "wander")]
[JsonDerivedType(typeof(OrbiterBehaviorData), "orbiter")]
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
    public float?     StartX  { get; set; }
    public float?     StartY  { get; set; }
}

public class OrbiterBehaviorData : BehaviorData
{
    public float  CenterX      { get; set; }
    public float  CenterY      { get; set; }
    public float  Radius       { get; set; }
    public float  AngularSpeed { get; set; }
    public bool   Clockwise    { get; set; }
    public float  StartAngle   { get; set; }
    public float? EndAngle     { get; set; }  // null = full loop; set = arc bounce between StartAngle and EndAngle
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

