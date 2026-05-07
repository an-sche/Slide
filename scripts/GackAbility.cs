using Godot;

namespace Slide;

public class GackAbility : Ability
{
    private static readonly float[] Durations = [4f];
    private static readonly float[] Cooldowns = [30f];

    private const AbilitySlot Slot = AbilitySlot.Gack;
    private const float DropDistance = GameplayConstants.GackDropDistance;

    private float   _duration;
    private Vector2 _lastDropPosition;

    public GackAbility(Unit unit) : base(unit) { }

    public override void TryActivate()
    {
        int level = Unit.PlayerState.AbilityLevels[(int)Slot];
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

        TickCooldown(delta);
    }

    public override void OnRespawn()
    {
        _duration = 0f;
        IsActive  = false;
    }


    private void SpawnGoo()
    {
        if (GameNetwork.IsMultiplayer && !Unit.Multiplayer.IsServer()) return;
        var pos = Unit.GlobalPosition;
        var goo = new GooZone { GlobalPosition = pos };
        Unit.GetParent().AddChild(goo);
        if (GameNetwork.IsMultiplayer)
            (Unit.GetParent() as World)?.Rpc(nameof(World.ClientSpawnGooZone), pos);
    }
}
