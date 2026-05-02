namespace Slide;

public static class RunState
{
    public static float ElapsedSeconds { get; set; }
    public static int TotalDeaths { get; set; }
    public static int PlayerLevel { get; set; } = 1;

    // One entry per ability slot (Q, W, E, R, F), all start at level 0 (unspent)
    public static int[] AbilityLevels { get; } = { 0, 0, 0, 0, 0 };

    public static int SpentPoints
    {
        get
        {
            int total = 0;
            foreach (int level in AbilityLevels) total += level;
            return total;
        }
    }

    public static int AvailablePoints => PlayerLevel - SpentPoints;

    public static void Reset()
    {
        ElapsedSeconds = 0;
        TotalDeaths = 0;
        PlayerLevel = 1;
        for (int i = 0; i < AbilityLevels.Length; i++) AbilityLevels[i] = 0;
    }
}
