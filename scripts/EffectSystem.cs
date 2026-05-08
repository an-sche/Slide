using Godot;
using System.Collections.Generic;

namespace Slide;

public partial class EffectSystem : Node
{
    private readonly Dictionary<long, WarpGhost> _warpGhosts = new();

    public void SpawnGooZone(Vector2 position)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        AddChild(new GooZone { GlobalPosition = position });
        if (GameNetwork.IsMultiplayer)
            Rpc(nameof(ClientSpawnGooZone), position);
    }

    public void SpawnWarpGhost(long peerId, Vector2 position, Vector2 facing, float duration)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        if (_warpGhosts.TryGetValue(peerId, out var old)) { old.QueueFree(); _warpGhosts.Remove(peerId); }
        var ghost = new WarpGhost { GlobalPosition = position, Facing = facing };
        AddChild(ghost);
        _warpGhosts[peerId] = ghost;
        if (GameNetwork.IsMultiplayer)
            Rpc(nameof(ClientSpawnWarpGhost), peerId, position, facing, duration);
    }

    public void UpdateWarpGhostFraction(long peerId, float fraction)
    {
        if (_warpGhosts.TryGetValue(peerId, out var ghost))
            ghost.Fraction = fraction;
    }

    public Vector2 GetWarpGhostPosition(long peerId) =>
        _warpGhosts.TryGetValue(peerId, out var g) ? g.GlobalPosition : Vector2.Zero;

    public Vector2 GetWarpGhostFacing(long peerId) =>
        _warpGhosts.TryGetValue(peerId, out var g) ? g.Facing : Vector2.Right;

    public void RemoveWarpGhost(long peerId)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        if (_warpGhosts.TryGetValue(peerId, out var ghost)) { ghost.QueueFree(); _warpGhosts.Remove(peerId); }
        if (GameNetwork.IsMultiplayer)
            Rpc(nameof(ClientRemoveWarpGhost), peerId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnGooZone(Vector2 position) =>
        AddChild(new GooZone { GlobalPosition = position });

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnWarpGhost(long peerId, Vector2 position, Vector2 facing, float duration)
    {
        if (_warpGhosts.TryGetValue(peerId, out var old)) { old.QueueFree(); _warpGhosts.Remove(peerId); }
        var ghost = new WarpGhost { GlobalPosition = position, Facing = facing, Duration = duration };
        AddChild(ghost);
        _warpGhosts[peerId] = ghost;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientRemoveWarpGhost(long peerId)
    {
        if (_warpGhosts.TryGetValue(peerId, out var ghost)) { ghost.QueueFree(); _warpGhosts.Remove(peerId); }
    }
}
