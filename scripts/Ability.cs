namespace Slide;

public abstract class Ability
{
    protected readonly Unit Unit;

    public float CooldownFraction { get; protected set; }
    public bool IsActive { get; protected set; }

    // Override in abilities that modify ground movement speed
    public virtual float GroundSpeedMultiplier => 1f;

    protected Ability(Unit unit) { Unit = unit; }

    public abstract void TryActivate();
    public abstract void Process(float delta);
    public virtual void DrawOnUnit() { }
    public virtual void OnRespawn() { }
}
