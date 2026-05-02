namespace Slide;

public static class RunState
{
    public static float ElapsedSeconds { get; set; }
    public static int TotalDeaths { get; set; }

    public static void Reset()
    {
        ElapsedSeconds = 0;
        TotalDeaths = 0;
    }
}
