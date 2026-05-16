using Godot;
using System;

namespace Slide;

public partial class Unit : Area2D
{
	public const float Radius         = GameplayConstants.UnitRadius;
	private const float GroundSpeed   = GameplayConstants.GroundSpeed;
	private const float SlidySpeed    = GameplayConstants.SlidySpeed;
	private const float SlidyTurnRate = GameplayConstants.SlidyTurnRate;
	private const float RespawnDelay  = GameplayConstants.RespawnDelay;

	private Vector2? _target;
	private Vector2 _facing = Vector2.Right;
	private Vector2 _velocity = Vector2.Zero;
	private SurfaceType _currentSurface = SurfaceType.Ground;
	private Vector2 _startPosition;
	private bool _isDead;
	private Corpse?          _corpse;
	private SceneTreeTimer?  _respawnTimer;

	private readonly Ability?[] _abilities = new Ability?[5];
	private CircleShape2D _wallCheckShape = null!;


	public int         PlayerId      { get; set; } = 0;
	public long        PeerId        { get; set; } = 1L;
	public bool        IsLocalPlayer { get; set; } = true;
	public PlayerState PlayerState  => RunState.GetPlayer(PlayerId);

	public byte AbilitiesActiveMask
	{
		get
		{
			byte mask = 0;
			for (int i = 0; i < _abilities.Length; i++)
				if (_abilities[i]?.IsActive == true) mask |= (byte)(1 << i);
			return mask;
		}
	}

	public void SetAbilitiesActive(byte mask)
	{
		for (int i = 0; i < _abilities.Length; i++)
			_abilities[i]?.SetActiveState((mask & (1 << i)) != 0);
	}

	public bool        IsDead         => _isDead;
	public bool        IsOnGround    => _currentSurface == SurfaceType.Ground;
	public SurfaceType CurrentSurface => _currentSurface;
	public bool    HasTarget    => _target.HasValue;
	public Vector2 TargetPosition => _target ?? Vector2.Zero;
	public Vector2 Velocity   { get => _velocity; set => _velocity = value; }
	public Vector2 Facing     { get => _facing; set => _facing = value.Normalized(); }
	private bool IsInGoo
	{
		get
		{
			var query = new PhysicsPointQueryParameters2D
			{
				Position          = GlobalPosition,
				CollideWithAreas  = true,
				CollideWithBodies = false,
				CollisionMask     = Layers.GooZones,
			};
			return GetWorld2D().DirectSpaceState.IntersectPoint(query).Count > 0;
		}
	}

	public EffectSystem     Effects     { get; set; } = null!;
	public ProjectileSystem Projectiles { get; set; } = null!;

	public event Action<Corpse>? CorpseTouched;
	public event Action<int, int>? AbilityInputForwarded;
	public Color UnitColor { get; set; } = new Color(0.2f, 0.8f, 1f);

	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void RespawnedEventHandler();

	public override void _Ready()
	{
		_startPosition = GlobalPosition;

		CollisionLayer = Layers.Units;
		CollisionMask  = Layers.Corpses;
		ZIndex = 1;

		_wallCheckShape = new CircleShape2D { Radius = Radius * 0.8f };
		AddChild(new CollisionShape2D { Shape = _wallCheckShape });

		AreaEntered += OnZoneEntered;

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

		var posBeforeMove = GlobalPosition;

		UpdateSurfaceFromPoint();

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

		if (!_isDead && TryGetWallNormal(out var wallNormal))
		{
			GlobalPosition = posBeforeMove;
			if (_currentSurface == SurfaceType.Ground)
			{
				_velocity = Vector2.Zero;
				_target   = null;
			}
			else
			{
				SlideAlongWall(wallNormal);
			}
		}
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLocalPlayer) return;
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

		if (slot < 0) return;

		if (!GameNetwork.IsMultiplayer || Multiplayer.IsServer())
		{
			_abilities[slot]?.TryActivate();
		}
		else
		{
			// Activate locally so the HUD cooldown display is immediate.
			_abilities[slot]?.TryActivate();
			// Forward to host with current level so host can simulate correctly.
			AbilityInputForwarded?.Invoke(slot, PlayerState.AbilityLevels[slot]);
		}
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
		if (area is Corpse c && !_isDead) CorpseTouched?.Invoke(c);
	}

	private bool TryGetWallNormal(out Vector2 normal)
	{
		var query = new PhysicsShapeQueryParameters2D
		{
			Shape             = _wallCheckShape,
			Transform         = new Transform2D(0, GlobalPosition),
			CollideWithAreas  = true,
			CollideWithBodies = false,
			CollisionMask     = Layers.Walls,
		};
		var results = GetWorld2D().DirectSpaceState.IntersectShape(query, 1);
		if (results.Count == 0) { normal = Vector2.Zero; return false; }

		if (results[0]["collider"].As<Wall>() is not { } wall)
		{
			normal = Vector2.Zero;
			return false;
		}

		// Determine which face was hit by finding the dominant axis in wall-local space.
		var   localPos  = (GlobalPosition - wall.GlobalPosition).Rotated(-wall.GlobalRotation);
		var   halfSize  = wall.WallSize / 2f;
		float nx        = localPos.X / halfSize.X;
		float ny        = localPos.Y / halfSize.Y;
		var   localNorm = Mathf.Abs(nx) >= Mathf.Abs(ny)
			? new Vector2(Mathf.Sign(nx), 0f)
			: new Vector2(0f, Mathf.Sign(ny));
		normal = localNorm.Rotated(wall.GlobalRotation);
		return true;
	}

	private void SlideAlongWall(Vector2 wallNormal)
	{
		// Cancel the into-wall velocity component.
		float dot = _velocity.Dot(wallNormal);
		if (dot < 0f)
			_velocity -= dot * wallNormal;

		// If the result is near-zero (right-angle hit), pick the tangent direction
		// that best matches intent: target direction for steered surfaces, facing for Straight.
		if (_velocity.LengthSquared() < 1f)
		{
			var   tangent   = new Vector2(-wallNormal.Y, wallNormal.X);
			var   intendDir = (_currentSurface == SurfaceType.Straight || !_target.HasValue)
				? _facing
				: (_target.Value - GlobalPosition).Normalized();
			float sign      = intendDir.Dot(tangent) >= 0f ? 1f : -1f;
			_velocity       = tangent * sign * GetBaseSpeed(_currentSurface);
		}

		_target = null;
	}

	private void UpdateSurfaceFromPoint()
	{
		var query = new PhysicsPointQueryParameters2D
		{
			Position          = GlobalPosition,
			CollideWithAreas  = true,
			CollideWithBodies = false,
			CollisionMask     = Layers.Surfaces,
		};
		var results = GetWorld2D().DirectSpaceState.IntersectPoint(query);

		var newSurface = SurfaceType.Kill;
		int highestPriority = -1;
		foreach (var result in results)
		{
			if (result["collider"].As<Area2D>() is SurfaceZone zone)
			{
				int priority = GetSurfacePriority(zone.Type);
				if (priority > highestPriority)
				{
					highestPriority = priority;
					newSurface = zone.Type;
				}
			}
		}

		if (newSurface != _currentSurface)
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
		// In multiplayer, only the host determines death. Clients receive it via BroadcastUnitDeath → ApplyRemoteDeath.
		if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;

		_isDead = true;
		RunState.GetPlayer(PlayerId).TotalDeaths++;
		_target = null;
		_velocity = Vector2.Zero;
		_currentSurface = SurfaceType.Ground;

		_corpse = new Corpse { Position = GlobalPosition, UnitColor = UnitColor, OnResurrect = ResurrectEarly, SourceUnit = this };
		GetParent().AddChild(_corpse);

		// Solo only: auto-respawn after a delay. In multiplayer, units stay dead until resurrected.
		if (!GameNetwork.IsMultiplayer)
		{
			_respawnTimer          = GetTree().CreateTimer(RespawnDelay);
			_respawnTimer.Timeout += Respawn;
		}
		EmitSignal(SignalName.Died);
		QueueRedraw();
	}

	private void Respawn()
	{
		_respawnTimer = null;
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
		RunState.GetPlayer(PlayerId).TotalDeaths++;
		_target = null;
		_velocity = Vector2.Zero;
		_currentSurface = SurfaceType.Ground;
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
		if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
		if (_respawnTimer != null)
		{
			_respawnTimer.Timeout -= Respawn;
			_respawnTimer          = null;
		}
		Respawn();
	}

	public void ResurrectAt(Vector2 position, Vector2 velocity, Vector2 facing, SurfaceType surface)
	{
		if (!_isDead) return;
		if (GameNetwork.IsMultiplayer && !Multiplayer.IsServer()) return;
		if (_respawnTimer != null)
		{
			_respawnTimer.Timeout -= Respawn;
			_respawnTimer          = null;
		}
		_isDead         = false;
		GlobalPosition  = position;
		_facing         = facing;
		_currentSurface = surface;
		// Normalize velocity to the correct speed for the surface we're landing on
		if (surface is not SurfaceType.Ground and not SurfaceType.Straight)
		{
			float speed = GetBaseSpeed(surface);
			_velocity = (velocity.LengthSquared() > 0.001f ? velocity.Normalized() : _facing) * speed;
		}
		else
		{
			_velocity = velocity;
		}
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

		_facing = invert ? -_velocity.Normalized() : _velocity.Normalized();
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
