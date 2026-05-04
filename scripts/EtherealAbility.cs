using Godot;

namespace Slide;

public class EtherealAbility : Ability
{
    private static readonly float[] Durations = [3f, 4f, 5f, 6f];
    private static readonly float[] Cooldowns = [45f, 40f, 35f, 30f];

    private const AbilitySlot Slot = AbilitySlot.Ethereal;

    private float _duration;

    public EtherealAbility(Unit unit) : base(unit)
    {
        unit.CorpseTouched += OnCorpseTouched;
    }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[(int)Slot];
        if (level <= 0 || Unit.IsDead || _cooldown > 0f) return;

        _duration    = Durations[level - 1];
        _maxCooldown = _duration + Cooldowns[level - 1];
        _cooldown    = _maxCooldown;
        IsActive     = true;
    }

    public override void Process(float delta)
    {
        if (_duration > 0f)
        {
            _duration = Mathf.Max(0f, _duration - delta);
            if (_duration <= 0f)
                IsActive = false;
        }

        TickCooldown(delta);
    }

    public override void DrawOnUnit()
    {
        if (!IsActive) return;
        float t     = (float)(Time.GetTicksMsec() % 1000) / 1000f;
        float pulse = (Mathf.Sin(t * Mathf.Tau) + 1f) * 0.5f;
        Unit.DrawArc(Vector2.Zero, Unit.Radius + 6f, 0, Mathf.Tau, 32,
            new Color(0.2f, 0.9f, 0.8f, 0.55f + pulse * 0.45f), 2.5f);
    }

    public override void DrawAboveUnit()
    {
        if (!IsActive) return;
        Unit.DrawCircle(Vector2.Zero, Unit.Radius, new Color(0f, 0f, 0f, 0.35f));
    }

    public override void OnRespawn() { }


    private void OnCorpseTouched(Corpse corpse)
    {
        if (!IsActive || corpse.SourceUnit == null) return;
        corpse.SourceUnit.ResurrectAt(Unit.GlobalPosition, Unit.Velocity);
    }
}
