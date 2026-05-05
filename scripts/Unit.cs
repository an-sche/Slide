using Godot;
using System;
using System.Collections.Generic;

namespace Slide;

public partial class Unit : Area2D
{
	public const float Radius = 16f;
	private const float GroundSpeed = 200f;
	private const float SlidySpeed = 400f;
	private const float SlidyTurnRate = 15.0f; // radians/sec

	private const float RespawnDelay = 3f;

	private Vector2? _target;
	private Vector2 _facing = Vector2.Right;
	private Vector2 _velocity = Vector2.Zero;
	private SurfaceType _currentSurface = SurfaceType.Ground;
	private Vector2 _startPosition;
	private readonly HashSet<SurfaceZone> _overlappingZones = new();
	private bool _isDead;
	private Corpse?          _corpse;
	private SceneTreeTimer?  _respawnTimer;

	private readonly Ability?[] _abilities = new Ability?[5];


	public int         PlayerId      { get; set; } = 0;
	public bool        IsLocalPlayer { get; set; } = true;
	public PlayerState PlayerState  => RunState.GetPlayer(PlayerId);

	public bool    IsDead        => _isDead;
	public bool    IsOnGround   => _currentSurface == SurfaceType.Ground;
	public bool    HasTarget    => _target.HasValue;
	public Vector2 TargetPosition => _target ?? Vector2.Zero;
	public Vector2 Velocity   { get => _velocity; set => _velocity = value; }
	public Vector2 Facing     { get => _facing; set => _facing = value.Normalized(); }
	private bool IsInGoo
	{
		get
		{
			foreach (var area in GetOverlappingAreas())
				if (area is GooZone) return true;
			return false;
		}
	}

	public event Action<Corpse>? CorpseTouched;
	public Color UnitColor { get; set; } = new Color(0.2f, 0.8f, 1f);

	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void RespawnedEventHandler();

	public override void _Ready()
	{
		_startPosition = GlobalPosition;

		CollisionLayer = Layers.Units;
		CollisionMask  = Layers.Surfaces | Layers.Corpses | Layers.GooZones;
		ZIndex = 1;

		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius * 0.8f } });

		AreaEntered += OnZoneEntered;
		AreaExited += OnZoneExited;

		_abilities[(int)AbilitySlot.Boost]    = new BoostAbility(this);
		_abilities[(int)AbilitySlot.Warp]     = new WarpAbility(this);
		_abilities[(int)AbilitySlot.Donut]    = new DonutAbility(this);
		_abilities[(int)AbilitySlot.Ethereal] = new EtherealAbility(this);
		_abilities[(int)AbilitySlot.Gack]     = new GackAbility(this);
	}

	public (float CooldownFraction, bool IsActive) GetAbilityState(int slot)
	{
		var a = _abilities[slot];
		return a != null ? (a.CooldownFraction, a.IsActive) : (0f, false);
	}

	public void SetStartPosition(Vector2 position)
	{
		_startPosition = position;
		GlobalPosition = position;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		foreach (var a in _abilities) a?.Process(dt);

		// Clients don't simulate — they receive state from the host.
		if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;

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
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLocalPlayer) return;
		if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
		if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
		if (key.CtrlPressed) return;

		int slot = key.Keycode switch
		{
			Key.Q => (int)AbilitySlot.Boost,
			Key.W => (int)AbilitySlot.Warp,
			Key.E => (int)AbilitySlot.Donut,
			Key.R => (int)AbilitySlot.Ethereal,
			Key.F => (int)AbilitySlot.Gack,
			_ => -1,
		};

		if (slot >= 0) _abilities[slot]?.TryActivate();
	}

	public override void _Draw()
	{
		if (_isDead) return;

		foreach (var a in _abilities) a?.DrawOnUnit();

		DrawCircle(Vector2.Zero, Radius, UnitColor);
		DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, Colors.White, 1.5f);
		DrawLine(Vector2.Zero, _facing * (Radius + 10f), Colors.White, 3f);

		foreach (var a in _abilities) a?.DrawAboveUnit();

		if (IsLocalPlayer && _target.HasValue && _currentSurface != SurfaceType.Straight)
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

	public void ClearTarget() => _target = null;

	private void OnZoneEntered(Area2D area)
	{
		if (area is SurfaceZone zone) { _overlappingZones.Add(zone); UpdateCurrentSurface(); }
		else if (area is Corpse c)    CorpseTouched?.Invoke(c);
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

		_corpse = new Corpse { Position = GlobalPosition, UnitColor = UnitColor, OnResurrect = ResurrectEarly, SourceUnit = this };
		GetParent().AddChild(_corpse);

		_respawnTimer          = GetTree().CreateTimer(RespawnDelay);
		_respawnTimer.Timeout += Respawn;
		EmitSignal(SignalName.Died);
		QueueRedraw();
	}

	private void Respawn()
	{
		_respawnTimer  = null;
		_isDead        = false;
		GlobalPosition = _startPosition;
		_corpse?.QueueFree();
		_corpse       = null;
		foreach (var a in _abilities) a?.OnRespawn();
		EmitSignal(SignalName.Respawned);
		QueueRedraw();
	}

	public void TriggerDeath() => Die();

	public void TryActivateAbility(int slot)
	{
		if (slot >= 0 && slot < _abilities.Length)
			_abilities[slot]?.TryActivate();
	}

	// Applied on clients when the host determines this unit has died.
	// Does not start a respawn timer — the host will send a respawn RPC.
	public void ApplyRemoteDeath(Vector2 deathPosition)
	{
		if (_isDead) return;
		_isDead = true;
		_target = null;
		_velocity = Vector2.Zero;
		_currentSurface = SurfaceType.Ground;
		_overlappingZones.Clear();
		GlobalPosition = deathPosition;
		_corpse = new Corpse { Position = GlobalPosition, UnitColor = UnitColor, OnResurrect = ResurrectEarly, SourceUnit = this };
		GetParent().AddChild(_corpse);
		EmitSignal(SignalName.Died);
		QueueRedraw();
	}

	// Applied on clients when the host determines this unit has respawned.
	public void ApplyRemoteRespawn(Vector2 spawnPosition)
	{
		if (_respawnTimer != null) { _respawnTimer.Timeout -= Respawn; _respawnTimer = null; }
		if (!_isDead) return;
		_isDead = false;
		GlobalPosition = spawnPosition;
		_corpse?.QueueFree();
		_corpse = null;
		foreach (var a in _abilities) a?.OnRespawn();
		EmitSignal(SignalName.Respawned);
		QueueRedraw();
	}

	public void ResetAbilityCooldowns() { foreach (var a in _abilities) a?.ResetCooldown(); }

	public void ResurrectEarly()
	{
		if (!_isDead) return;
		if (_respawnTimer != null)
		{
			_respawnTimer.Timeout -= Respawn;
			_respawnTimer          = null;
		}
		Respawn();
	}

	public void ResurrectAt(Vector2 position, Vector2 velocity)
	{
		if (!_isDead) return;
		if (_respawnTimer != null)
		{
			_respawnTimer.Timeout -= Respawn;
			_respawnTimer          = null;
		}
		_isDead        = false;
		GlobalPosition = position;
		_velocity      = velocity;
		_corpse?.QueueFree();
		_corpse       = null;
		foreach (var a in _abilities) a?.OnRespawn();
		EmitSignal(SignalName.Respawned);
		QueueRedraw();
	}

	private void ProcessGroundMovement(float delta)
	{
		if (_target is not { } target) return;

		float speedMult = 1f;
		foreach (var a in _abilities) if (a != null) speedMult *= a.GroundSpeedMultiplier;
		float speed = GroundSpeed * speedMult;
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
		speed *= IsInGoo ? GooZone.SpeedMultiplier : 1f;

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
		float straightSpeed = SlidySpeed * (IsInGoo ? GooZone.SpeedMultiplier : 1f);
		Vector2 dir = _velocity.LengthSquared() > 0.001f ? _velocity.Normalized() : _facing;
		_velocity = dir * straightSpeed;
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
