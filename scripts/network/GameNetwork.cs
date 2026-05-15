namespace Slide;

public static class GameNetwork
{
    public const int Port = 7777;
    public static bool IsMultiplayer { get; set; } = false;
    public static string JoinIp { get; set; } = "127.0.0.1";
}
