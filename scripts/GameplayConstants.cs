namespace Slide;

public static class GameplayConstants
{
    // Unit movement
    public const float UnitRadius     = 16f;
    public const float GroundSpeed    = 200f;
    public const float SlidySpeed     = 400f;
    public const float SlidyTurnRate  = 15f;  // radians/sec

    // Timing
    public const float RespawnDelay       = 3f;
    public const float WipeDelay          = 5f;
    public const float TransitionDuration = 4f;
    public const float TelegraphDuration  = 0.5f;

    // Abilities
    public const float DonutSpeed       = 900f;
    public const float GackDropDistance = 40f;
    public const int   AbilityUnlockLevel = 3;
}
