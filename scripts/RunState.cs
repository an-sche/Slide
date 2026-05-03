namespace Slide;

public static class RunState
{
    public static float ElapsedSeconds { get; set; }
    public static int   TotalDeaths    { get; set; }

    private static readonly PlayerState[] _players =
        [new(), new(), new(), new(), new(), new(), new(), new()];

    public static PlayerState GetPlayer(int id) => _players[id];

    public static void LevelUpAll()
    {
        foreach (var p in _players) p.PlayerLevel++;
    }

    public static void Reset()
    {
        ElapsedSeconds = 0;
        TotalDeaths    = 0;
        foreach (var p in _players) p.Reset();
    }
}
