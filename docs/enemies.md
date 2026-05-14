# Enemies

Enemies are obstacles rather than intelligent units. They exist on most maps and kill any player they touch, leaving behind a corpse that teammates can resurrect via touch, Donut, or Ethereal.

Enemies are unaffected by anything players do — no ability interacts with them. They simply continue their behavior after a kill.

Enemies pass through each other with no interaction (future: animations on overlap).

Enemies can be any size and color — both are configured per instance. All enemies will eventually use sprites; circles are used as placeholders during development.

---

## Architecture

Every enemy is an `Enemy` node with one `IEnemyBehavior` attached. The behavior drives all movement logic. New enemy types are created by writing a new behavior — the `Enemy` class never changes.

A future `IAttackBehavior` will be injected the same way (torch, grenade, etc.), composing cleanly with any movement behavior.

```
Enemy : Area2D
  + IEnemyBehavior    ← drives movement (PatrolBehavior, RandomWanderBehavior, ...)
  + IAttackBehavior   ← future: drives attacks (TorchBehavior, GrenadeBehavior, ...)
```

The API is designed to be simple for level designers — an enemy is created by instantiating it with a position, size, color, and a behavior. No subclassing required.

---

## Behavior: Patrol

The enemy follows an ordered list of waypoints. Each waypoint carries its own travel speed, so the enemy can move fast on some segments and slow on others.

**Properties:**
- `Waypoint[]` — ordered list; each waypoint has a `Position` (Vector2) and a `Speed` (float) to travel *to* it
- `PatrolEndBehavior` — what happens when the path end is reached:
  - `Loop` — jump back to waypoint[0] and continue indefinitely
  - `Reverse` — reverse direction and ping-pong along the same path
  - `Disappear` — enemy is instantly removed (future: smoke effect)

**Notes:**
- Enemy starts at `waypoint[0]` when placed
- No idle time — the enemy moves continuously

---

## Behavior: Random Wander

The enemy picks random points inside a defined polygon area, moves to them, idles for a random duration, then picks another. Repeats indefinitely.

**Properties:**
- `Vector2[]` polygon — defines the wander area in world space (any convex or concave shape)
- `Speed` — travel speed while moving
- `MinIdleDuration` / `MaxIdleDuration` — each idle phase picks a random duration within this window (seconds)
- `StartPosition` *(optional)* — if provided, enemy starts here; otherwise a random point inside the polygon is chosen

**State machine:**
`Idle → Moving → Idle → ...`

- **Idle**: wait for a random duration between min and max, then pick a new target point inside the polygon
- **Moving**: travel to the target at `Speed`; on arrival, enter Idle

**Notes:**
- Random points are sampled uniformly using triangle decomposition (`Geometry2D.TriangulatePolygon`): pick a random triangle weighted by area, then a uniform random point within it. Guaranteed termination with exactly 3 random numbers, no loops.
- The polygon is defined in world space

---

## Future Enemy Types

These are not yet implemented. Each will be an `IAttackBehavior` that can be combined with any movement behavior:

- **Torch** — periodically emits a fire hazard in a direction or area
- **Grenade** — lobs projectiles toward player positions
- **Others TBD**

---

## Level Designer Notes

See `editor.md` for the full editor workflow. Summary of what each enemy needs:

**Any enemy:** position, radius, color, behavior type.

**Patrol:** waypoints (position + speed each), end behavior (Loop / Reverse / Disappear).

**Wander:** polygon (3+ vertices), speed, min/max idle duration, optional start position.

**Orbiter:** center position, orbit radius, angular speed, direction, start angle.
