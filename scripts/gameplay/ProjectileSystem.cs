using Godot;

namespace Slide;

public partial class ProjectileSystem : Node
{
    public void SpawnDonut(Vector2 position, Vector2 velocity, float lifetime)
    {
        if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
        AddChild(new DonutProjectile { GlobalPosition = position, MoveVelocity = velocity, Lifetime = lifetime });
        if (GameNetwork.IsMultiplayer)
            Rpc(nameof(ClientSpawnDonut), position, velocity, lifetime);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void ClientSpawnDonut(Vector2 position, Vector2 velocity, float lifetime) =>
        AddChild(new DonutProjectile { GlobalPosition = position, MoveVelocity = velocity, Lifetime = lifetime });
}
