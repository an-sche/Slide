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

    // Set by the editor before launching World; null means normal gameplay flow.
    public static string? PlaytestPath { get; set; }
    public static bool    IsPlaytest   => PlaytestPath != null;

    // Persists the last level opened in the editor so it survives scene reloads.
    public static string? LastEditorLevelPath { get; set; }

    // In-memory snapshot taken before playtesting so unsaved changes survive the round-trip.
    public record EditorSnapshot(string LevelPath, LevelData LevelData, Godot.Image Image, bool WasDirty);
    public static EditorSnapshot? PlaytestRestore { get; set; }
}
