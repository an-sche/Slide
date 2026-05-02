using Godot;

namespace Slide;

public partial class EndBlock : Area2D
{
    private const float Size = 150f;
    private static readonly Color BlockColor = new(1.0f, 0.85f, 0.0f);

    [Signal]
    public delegate void LevelCompletedEventHandler();

    public override void _Ready()
    {
        CollisionLayer = 4;
        CollisionMask = 2; // detects units
        Monitoring = true;
        Monitorable = false;

        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(Size, Size) } });

        AreaEntered += OnAreaEntered;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-Size / 2, -Size / 2, Size, Size), new Color(BlockColor, 0.2f));
        DrawRect(new Rect2(-Size / 2, -Size / 2, Size, Size), BlockColor, filled: false, width: 3f);

        // 8-pointed star
        for (int i = 0; i < 8; i++)
            DrawLine(Vector2.Zero, Vector2.FromAngle(i * Mathf.Pi / 4f) * 30f, BlockColor, 2f);
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is Unit)
            EmitSignal(SignalName.LevelCompleted);
    }
}
