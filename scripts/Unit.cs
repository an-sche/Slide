using Godot;
using System.Collections.Generic;

namespace Slide;

public partial class Unit : Area2D
{
	private const float Radius = 16f;
	private const float GroundSpeed = 200f;
	private const float SlidySpeed = 400f;
	private const float SlidyTurnRate = 15.0f; // radians/sec

	private const float RespawnDelay = 3f;

	private static readonly float[] BoostMultipliers = { 0.50f, 0.65f, 0.80f, 1.00f };
	private static readonly float[] BoostDurations   = { 5f, 6f, 7f, 8f };
	private static readonly float[] BoostCooldowns   = { 40f, 35f, 30f, 25f };

	private Vector2? _target;
	private Vector2 _facing = Vector2.Right;
	private Vector2 _velocity = Vector2.Zero;
	private SurfaceType _currentSurface = SurfaceType.Ground;
	private Vector2 _startPosition;
	private readonly HashSet<SurfaceZone> _overlappingZones = new();
	private bool _isDead;
	private Corpse? _corpse;

	private float _boostDuration;
	private float _boostCooldown;
	private float _boostMaxCooldown;
	private float _boostMultiplier;

	public float BoostCooldownFraction => _boostMaxCooldown > 0f ? _boostCooldown / _boostMaxCooldown : 0f;
	public bool IsBoostActive => _boostDuration > 0f;

	public Color UnitColor { get; set; } = new Color(0.2f, 0.8f, 1f);

	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void RespawnedEventHandler();

	public override void _Ready()
	{
		_startPosition = GlobalPosition;

		CollisionLayer = 2;
		CollisionMask = 1;
		ZIndex = 1;

		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius * 0.8f } });

		AreaEntered += OnZoneEntered;
		AreaExited += OnZoneExited;
	}

	public void SetStartPosition(Vector2 position)
	{
		_startPosition = position;
		GlobalPosition = position;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_boostDuration > 0f) _boostDuration = Mathf.Max(0f, _boostDuration - dt);
		if (_boostCooldown > 0f) _boostCooldown = Mathf.Max(0f, _boostCooldown - dt);

		if (_isDead) return;

		switch (_currentSurface)
		{
			case SurfaceType.Ground:
				ProcessGroundMovement(dt);
				break;
			case SurfaceType.Slidy:
				ProcessMomentumMovement(dt, SlidySpeed, SlidyTurnRate, invert: false);
				break;
			case SurfaceType.Fast:
				ProcessMomentumMovement(dt, SlidySpeed * 2f, SlidyTurnRate * 2f, invert: false);
				break;
			case SurfaceType.Confusing:
				ProcessMomentumMovement(dt, SlidySpeed, SlidyTurnRate, invert: true);
				break;
			case SurfaceType.FastConfusing:
				ProcessMomentumMovement(dt, SlidySpeed * 2f, SlidyTurnRate * 2f, invert: true);
				break;
			case SurfaceType.Straight:
				ProcessStraightMovement(dt);
				break;
		}

		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
		if (key.CtrlPressed) return;

		if (key.Keycode == Key.Q)
			TryActivateBoost();
	}

	private void TryActivateBoost()
	{
		int level = RunState.AbilityLevels[0];
		if (level <= 0 || _isDead || _boostCooldown > 0f) return;

		_boostMultiplier  = BoostMultipliers[level - 1];
		_boostDuration    = BoostDurations[level - 1];
		_boostMaxCooldown = BoostCooldowns[level - 1];
		_boostCooldown    = _boostMaxCooldown;
	}

	public override void _Draw()
	{
		if (_isDead) return;

		if (IsBoostActive)
		{
			float t = (float)(Time.GetTicksMsec() % 1000) / 1000f;
			float pulse = (Mathf.Sin(t * Mathf.Tau) + 1f) * 0.5f;
			DrawArc(Vector2.Zero, Radius + 6f, 0, Mathf.Tau, 32, new Color(1f, 0.8f, 0f, 0.55f + pulse * 0.45f), 2.5f);
		}

		DrawCircle(Vector2.Zero, Radius, UnitColor);
		DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, Colors.White, 1.5f);
		DrawLine(Vector2.Zero, _facing * (Radius + 10f), Colors.White, 3f);

		if (_target.HasValue && _currentSurface != SurfaceType.Straight)
		{
			Vector2 localTarget = ToLocal(_target.Value);
			DrawCircle(localTarget, 4f, new Color(1f, 1f, 0f, 0.8f));
			DrawArc(localTarget, 8f, 0, Mathf.Tau, 16, new Color(1f, 1f, 0f, 0.5f), 1f);
		}
	}

	public void SetTarget(Vector2 worldPosition)
	{
		if (_isDead || _currentSurface == SurfaceType.Straight) return;
		_target = worldPosition;
		QueueRedraw();
	}

	private void OnZoneEntered(Area2D area)
	{
		if (area is not SurfaceZone zone) return;
		_overlappingZones.Add(zone);
		UpdateCurrentSurface();
	}

	private void OnZoneExited(Area2D area)
	{
		if (area is not SurfaceZone zone) return;
		_overlappingZones.Remove(zone);
		UpdateCurrentSurface();
	}

	private void UpdateCurrentSurface()
	{
		var newSurface = SurfaceType.Ground;
		int highestPriority = -1;
		foreach (var zone in _overlappingZones)
		{
			int priority = GetSurfacePriority(zone.Type);
			if (priority > highestPriority)
			{
				highestPriority = priority;
				newSurface = zone.Type;
			}
		}

		if (newSurface == _currentSurface) return;
		TransitionToSurface(newSurface);
	}

	private void TransitionToSurface(SurfaceType newSurface)
	{
		if (newSurface == SurfaceType.Kill)
		{
			Die();
			return;
		}

		_currentSurface = newSurface;
		_target = null;

		// Entering a momentum surface: carry current direction at the surface's speed
		if (newSurface is not SurfaceType.Ground and not SurfaceType.Straight)
		{
			float speed = GetBaseSpeed(newSurface);
			_velocity = (_velocity.LengthSquared() > 0.001f ? _velocity.Normalized() : _facing) * speed;
		}
	}

	private void Die()
	{
		if (_isDead) return;

		_isDead = true;
		_target = null;
		_velocity = Vector2.Zero;
		_currentSurface = SurfaceType.Ground;
		_overlappingZones.Clear();

		_corpse = new Corpse { Position = GlobalPosition, UnitColor = UnitColor };
		GetParent().AddChild(_corpse);

		GetTree().CreateTimer(RespawnDelay).Timeout += Respawn;
		EmitSignal(SignalName.Died);
		QueueRedraw();
	}

	private void Respawn()
	{
		_isDead = false;
		GlobalPosition = _startPosition;
		_corpse?.QueueFree();
		_corpse = null;
		_boostDuration    = 0f;
		_boostCooldown    = 0f;
		_boostMaxCooldown = 0f;
		_boostMultiplier  = 0f;
		EmitSignal(SignalName.Respawned);
		QueueRedraw();
	}

	private void ProcessGroundMovement(float delta)
	{
		if (_target is not { } target) return;

		float speed = GroundSpeed * (IsBoostActive ? 1f + _boostMultiplier : 1f);
		Vector2 toTarget = target - GlobalPosition;
		float distance = toTarget.Length();
		float step = speed * delta;

		if (distance <= step)
		{
			GlobalPosition = target;
			_target = null;
			_velocity = Vector2.Zero;
		}
		else
		{
			_facing = toTarget.Normalized();
			_velocity = _facing * speed;
			GlobalPosition += _facing * step;
		}
	}

	private void ProcessMomentumMovement(float delta, float speed, float turnRate, bool invert)
	{
		if (_velocity.LengthSquared() < 0.001f)
			_velocity = _facing * speed;
		else
			_velocity = _velocity.Normalized() * speed;

		if (_target is { } target)
		{
			Vector2 toTarget = target - GlobalPosition;
			if (toTarget.LengthSquared() > 0.001f)
			{
				Vector2 desiredDir = invert ? -toTarget.Normalized() : toTarget.Normalized();
				float currentAngle = Mathf.Atan2(_velocity.Y, _velocity.X);
				float desiredAngle = Mathf.Atan2(desiredDir.Y, desiredDir.X);
				float angleDiff = Mathf.AngleDifference(currentAngle, desiredAngle);
				float turn = Mathf.Clamp(angleDiff, -turnRate * delta, turnRate * delta);
				_velocity = Vector2.FromAngle(currentAngle + turn) * speed;
			}
		}

		_facing = _velocity.Normalized();
		GlobalPosition += _velocity * delta;
	}

	private void ProcessStraightMovement(float delta)
	{
		Vector2 dir = _velocity.LengthSquared() > 0.001f ? _velocity.Normalized() : _facing;
		_velocity = dir * SlidySpeed;
		GlobalPosition += _velocity * delta;
		_facing = _velocity.Normalized();
	}

	private static int GetSurfacePriority(SurfaceType type) => type switch
	{
		SurfaceType.Kill          => 100,
		SurfaceType.Straight      => 5,
		SurfaceType.FastConfusing => 4,
		SurfaceType.Confusing     => 3,
		SurfaceType.Fast          => 2,
		SurfaceType.Slidy         => 1,
		_                         => 0,
	};

	private static float GetBaseSpeed(SurfaceType type) => type switch
	{
		SurfaceType.Fast or SurfaceType.FastConfusing => SlidySpeed * 2f,
		_ => SlidySpeed,
	};
}
