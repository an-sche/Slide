using Godot;

namespace Slide;

public class GackAbility : Ability
{
    private static readonly float[] Durations = [4f];
    private static readonly float[] Cooldowns = [30f];

    private const int   SlotIndex    = 4; // F
    private const float DropDistance = 40f;

    private float   _duration;
    private float   _cooldown;
    private float   _maxCooldown;
    private Vector2 _lastDropPosition;

    public GackAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = RunState.AbilityLevels[SlotIndex];
        if (level <= 0 || Unit.IsDead || _cooldown > 0f) return;

        _duration         = Durations[level - 1];
        _maxCooldown      = Cooldowns[level - 1];
        _cooldown         = _maxCooldown;
        _lastDropPosition = Unit.GlobalPosition;
        IsActive          = true;
        SpawnGoo();
    }

    public override void Process(float delta)
    {
        if (_duration > 0f)
        {
            _duration -= delta;
            if (_duration <= 0f)
            {
                _duration = 0f;
                IsActive  = false;
            }
        }

        if (IsActive && Unit.GlobalPosition.DistanceTo(_lastDropPosition) >= DropDistance)
        {
            _lastDropPosition = Unit.GlobalPosition;
            SpawnGoo();
        }

        if (_cooldown > 0f)
            _cooldown = Mathf.Max(0f, _cooldown - delta);

        CooldownFraction = _maxCooldown > 0f ? _cooldown / _maxCooldown : 0f;
    }

    public override void OnRespawn()
    {
        _duration        = 0f;
        _cooldown        = 0f;
        _maxCooldown     = 0f;
        IsActive         = false;
        CooldownFraction = 0f;
    }

    private void SpawnGoo()
    {
        var goo = new GooZone { GlobalPosition = Unit.GlobalPosition };
        Unit.GetParent().AddChild(goo);
    }
}
