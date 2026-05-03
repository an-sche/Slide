using Godot;

namespace Slide;

public class DonutAbility : Ability
{
    private static readonly float[] Durations = [1.5f, 3.0f, 4.5f, 6.0f];
    private static readonly float[] Cooldowns = [45f, 40f, 35f, 30f];

    private const int   SlotIndex  = 2; // E
    private const float DonutSpeed = 900f;

    private float _cooldown;
    private float _maxCooldown;

    public DonutAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[SlotIndex];
        if (level <= 0 || Unit.IsDead || _cooldown > 0f) return;

        _maxCooldown = Cooldowns[level - 1];
        _cooldown    = _maxCooldown;

        var projectile = new DonutProjectile
        {
            GlobalPosition = Unit.GlobalPosition,
            MoveVelocity   = Unit.IsOnGround ? Vector2.Zero : Unit.Facing * DonutSpeed,
            Lifetime       = Durations[level - 1],
        };
        Unit.GetParent().AddChild(projectile);
    }

    public override void Process(float delta)
    {
        if (_cooldown > 0f)
            _cooldown = Mathf.Max(0f, _cooldown - delta);

        CooldownFraction = _maxCooldown > 0f ? _cooldown / _maxCooldown : 0f;
    }

    public override void OnRespawn() { }
}
