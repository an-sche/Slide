using Godot;

namespace Slide;

public class WarpAbility : Ability
{
    private static readonly float[] Durations = [5f, 6f, 7f, 8f];
    private static readonly float[] Cooldowns = [45f, 40f, 35f, 30f];

    private const AbilitySlot Slot = AbilitySlot.Warp;

    private enum WarpState { Idle, GhostPlaced }

    private WarpState _state = WarpState.Idle;
    private Vector2   _capturedVelocity;
    private float     _ghostRemaining;
    private float     _ghostMaxDuration;

    public WarpAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[(int)Slot];
        if (level <= 0) return;

        if (_state == WarpState.Idle)
        {
            if (Unit.IsDead || _cooldown > 0f) return;

            _capturedVelocity = Unit.Velocity;
            _ghostMaxDuration = Durations[level - 1];
            _ghostRemaining   = _ghostMaxDuration;
            _state            = WarpState.GhostPlaced;
            IsActive          = true;

            Unit.Effects.SpawnWarpGhost(Unit.PeerId, Unit.GlobalPosition, Unit.Facing, _ghostMaxDuration);
        }
        else
        {
            if (Unit.IsDead) return;

            Unit.ClearTarget();

            // Host/solo: teleport to ghost. Client: position corrected by next SyncUnitState.
            if (!GameNetwork.IsMultiplayer || Unit.Multiplayer.IsServer())
            {
                Unit.GlobalPosition = Unit.Effects.GetWarpGhostPosition(Unit.PeerId);
                Unit.Velocity       = _capturedVelocity;
                Unit.Facing         = Unit.Effects.GetWarpGhostFacing(Unit.PeerId);
            }
            Unit.Effects.RemoveWarpGhost(Unit.PeerId);

            int level2   = Unit.PlayerState.AbilityLevels[(int)Slot];
            _maxCooldown = Cooldowns[level2 - 1];
            _cooldown    = _maxCooldown;
            _state       = WarpState.Idle;
            IsActive     = false;
        }
    }

    public override void Process(float delta)
    {
        if (_state == WarpState.GhostPlaced)
        {
            _ghostRemaining -= delta;
            if (_ghostRemaining <= 0f)
            {
                Unit.Effects.RemoveWarpGhost(Unit.PeerId);
                _state   = WarpState.Idle;
                IsActive = false;

                int level = Unit.PlayerState.AbilityLevels[(int)Slot];
                if (level > 0)
                {
                    _maxCooldown = Cooldowns[level - 1];
                    _cooldown    = _maxCooldown;
                }
            }
            else
            {
                Unit.Effects.UpdateWarpGhostFraction(Unit.PeerId, _ghostRemaining / _ghostMaxDuration);
            }
        }

        TickCooldown(delta);
    }

    public override void OnRespawn()
    {
        // Ghost stays in the world, cooldown keeps ticking — nothing to reset
    }
}
