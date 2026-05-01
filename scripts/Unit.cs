using Godot;

public partial class Unit : Node2D
{
	private const float Speed = 200f;
	private const float ArrivalThreshold = 1f;
	private const float Radius = 16f;

	private Vector2 _targetPosition;
	private bool _isMoving;
	private Vector2 _facing = Vector2.Right;

	public Color UnitColor { get; set; } = new Color(0.2f, 0.8f, 1f);

	public override void _Ready()
	{
		_targetPosition = GlobalPosition;
	}

	public override void _Process(double delta)
	{
		if (!_isMoving) return;

		Vector2 toTarget = _targetPosition - GlobalPosition;
		float distance = toTarget.Length();
		float step = Speed * (float)delta;

		if (distance <= step)
		{
			GlobalPosition = _targetPosition;
			_isMoving = false;
		}
		else
		{
			_facing = toTarget.Normalized();
			GlobalPosition += _facing * step;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		// Body
		DrawCircle(Vector2.Zero, Radius, UnitColor);
		// Outline
		DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, Colors.White, 1.5f);
		// Direction indicator
		DrawLine(Vector2.Zero, _facing * (Radius + 10f), Colors.White, 3f);

		// Move target marker
		if (_isMoving)
		{
			Vector2 localTarget = ToLocal(_targetPosition);
			DrawCircle(localTarget, 4f, new Color(1f, 1f, 0f, 0.8f));
			DrawArc(localTarget, 8f, 0, Mathf.Tau, 16, new Color(1f, 1f, 0f, 0.5f), 1f);
		}
	}

	public void SetTarget(Vector2 worldPosition)
	{
		if (worldPosition.DistanceTo(GlobalPosition) < ArrivalThreshold) return;
		_targetPosition = worldPosition;
		_isMoving = true;
		QueueRedraw();
	}
}
