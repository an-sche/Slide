using Godot;

namespace Slide;

public class BoostAbility : Ability
{
    private static readonly float[] Multipliers = [0.50f, 0.65f, 0.80f, 1.00f];
    private static readonly float[] Durations   = [5f, 6f, 7f, 8f];
    private static readonly float[] Cooldowns   = [40f, 35f, 30f, 25f];

    private const AbilitySlot Slot = AbilitySlot.Boost;

    private float _duration;
    private float _multiplier;

    public BoostAbility(Unit unit) : base(unit) { }

    public override float GroundSpeedMultiplier => IsActive ? 1f + _multiplier : 1f;

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[(int)Slot];
        if (level <= 0 || Unit.IsDead || _cooldown > 0f) return;

        _multiplier  = Multipliers[level - 1];
        _duration    = Durations[level - 1];
        _maxCooldown = Cooldowns[level - 1];
        _cooldown    = _maxCooldown;
        IsActive     = true;
    }

    public override void Process(float delta)
    {
        if (_duration > 0f)
        {
            _duration = Mathf.Max(0f, _duration - delta);
            IsActive  = _duration > 0f;
        }

        TickCooldown(delta);
    }

    public override void DrawOnUnit()
    {
        if (!IsActive) return;
        float t = (float)(Time.GetTicksMsec() % 1000) / 1000f;
        float pulse = (Mathf.Sin(t * Mathf.Tau) + 1f) * 0.5f;
        Unit.DrawArc(Vector2.Zero, Unit.Radius + 6f, 0, Mathf.Tau, 32,
            new Color(1f, 0.8f, 0f, 0.55f + pulse * 0.45f), 2.5f);
    }

    public override void OnRespawn()
    {
        _duration   = 0f;
        _multiplier = 0f;
        IsActive    = false;
    }

}
