using Godot;

namespace Slide;

public abstract class Ability
{
    protected readonly Unit Unit;

    public float CooldownFraction { get; protected set; }
    public bool  IsActive         { get; protected set; }

    protected float _cooldown;
    protected float _maxCooldown;

    // Override in abilities that modify ground movement speed
    public virtual float GroundSpeedMultiplier => 1f;

    protected Ability(Unit unit) { Unit = unit; }

    public abstract void TryActivate();
    public abstract void Process(float delta);
    public virtual void DrawOnUnit()      { }
    public virtual void DrawAboveUnit()   { }
    public virtual void OnRespawn()       { }
    public virtual void ResetCooldown() { _cooldown = 0f; CooldownFraction = 0f; }

    protected void TickCooldown(float delta)
    {
        if (_cooldown > 0f)
            _cooldown = Mathf.Max(0f, _cooldown - delta);
        CooldownFraction = _maxCooldown > 0f ? _cooldown / _maxCooldown : 0f;
    }
}
