using Godot;

namespace Slide;

public partial class StartBlock : Node2D
{
    private const float Size = 100f;
    private static readonly Color BlockColor = new(0.2f, 1.0f, 0.3f);

    public override void _Draw()
    {
        DrawRect(new Rect2(-Size / 2, -Size / 2, Size, Size), new Color(BlockColor, 0.15f));
        DrawRect(new Rect2(-Size / 2, -Size / 2, Size, Size), BlockColor, filled: false, width: 3f);
        DrawLine(new Vector2(-20, 0), new Vector2(20, 0), BlockColor, 2f);
        DrawLine(new Vector2(0, -20), new Vector2(0, 20), BlockColor, 2f);
    }
}
