using Godot;

namespace Slide;

public class WarpAbility : Ability
{
    private static readonly float[] Durations = [5f, 6f, 7f, 8f];
    private static readonly float[] Cooldowns = [45f, 40f, 35f, 30f];

    private const int SlotIndex = 1; // W

    private enum WarpState { Idle, GhostPlaced }

    private WarpState  _state = WarpState.Idle;
    private WarpGhost? _ghost;
    private Vector2    _capturedVelocity;
    private float      _ghostRemaining;
    private float      _ghostMaxDuration;
    private float      _cooldown;
    private float      _maxCooldown;

    public WarpAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[SlotIndex];
        if (level <= 0) return;

        if (_state == WarpState.Idle)
        {
            if (Unit.IsDead || _cooldown > 0f) return;

            _capturedVelocity = Unit.Velocity;
            _ghostMaxDuration = Durations[level - 1];
            _ghostRemaining   = _ghostMaxDuration;

            _ghost = new WarpGhost
            {
                GlobalPosition = Unit.GlobalPosition,
                Facing         = Unit.Facing,
            };
            Unit.GetParent().AddChild(_ghost);

            _state   = WarpState.GhostPlaced;
            IsActive = true;
        }
        else
        {
            if (Unit.IsDead) return;

            Unit.GlobalPosition = _ghost!.GlobalPosition;
            Unit.Velocity       = _capturedVelocity;
            Unit.ClearTarget();

            int level2       = Unit.PlayerState.AbilityLevels[SlotIndex];
            _maxCooldown     = Cooldowns[level2 - 1];
            _cooldown        = _maxCooldown;

            _ghost.QueueFree();
            _ghost   = null;
            _state   = WarpState.Idle;
            IsActive = false;
        }
    }

    public override void Process(float delta)
    {
        if (_state == WarpState.GhostPlaced)
        {
            _ghostRemaining -= delta;
            if (_ghostRemaining <= 0f)
            {
                _ghost?.QueueFree();
                _ghost   = null;
                _state   = WarpState.Idle;
                IsActive = false;

                int level = Unit.PlayerState.AbilityLevels[SlotIndex];
                if (level > 0)
                {
                    _maxCooldown = Cooldowns[level - 1];
                    _cooldown    = _maxCooldown;
                }
            }
            else if (_ghost != null)
            {
                _ghost.Fraction = _ghostRemaining / _ghostMaxDuration;
            }
        }

        if (_cooldown > 0f)
            _cooldown = Mathf.Max(0f, _cooldown - delta);

        CooldownFraction = _maxCooldown > 0f ? _cooldown / _maxCooldown : 0f;
    }

    public override void OnRespawn()
    {
        // Ghost stays in the world, cooldown keeps ticking — nothing to reset
    }
}
