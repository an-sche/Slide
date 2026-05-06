using Godot;

namespace Slide;

public class DonutAbility : Ability
{
    private static readonly float[] Durations = [1.5f, 3.0f, 4.5f, 6.0f];
    private static readonly float[] Cooldowns = [45f, 40f, 35f, 30f];

    private const AbilitySlot Slot = AbilitySlot.Donut;
    private const float DonutSpeed = 900f;

    public DonutAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[(int)Slot];
        if (level <= 0 || Unit.IsDead || _cooldown > 0f) return;

        _maxCooldown = Cooldowns[level - 1];
        _cooldown    = _maxCooldown;

        if (!GameNetwork.IsMultiplayer || Unit.Multiplayer.IsServer())
        {
            var velocity   = Unit.IsOnGround ? Vector2.Zero : Unit.Facing * DonutSpeed;
            var lifetime   = Durations[level - 1];
            var projectile = new DonutProjectile
            {
                GlobalPosition = Unit.GlobalPosition,
                MoveVelocity   = velocity,
                Lifetime       = lifetime,
            };
            Unit.GetParent().AddChild(projectile);
            if (GameNetwork.IsMultiplayer)
                (Unit.GetParent() as World)?.Rpc(nameof(World.ClientSpawnDonut),
                    Unit.GlobalPosition, velocity, lifetime);
        }
    }

    public override void Process(float delta)
    {
        TickCooldown(delta);
    }

    public override void OnRespawn() { }

}
