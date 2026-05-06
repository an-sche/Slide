using System.Collections.Generic;

namespace Slide;

public static class GameSetup
{
    public record Player(long PeerId, int Index);

    private static readonly List<Player> _players = new();
    public static IReadOnlyList<Player> Players => _players;

    public static void Clear()             => _players.Clear();
    public static void AddPlayer(long peerId, int index) => _players.Add(new Player(peerId, index));
    public static void RemovePlayer(long peerId)         => _players.RemoveAll(p => p.PeerId == peerId);
}
