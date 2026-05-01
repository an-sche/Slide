using Godot;

public partial class World : Node2D
{
    private Unit? _unit;
    private GameCamera? _camera;

    public override void _Ready()
    {
        _unit = GetNode<Unit>("Unit");
        _camera = GetNode<GameCamera>("Camera");
        _camera.Initialize(_unit);
        Input.MouseMode = Input.MouseModeEnum.Confined;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            _unit?.SetTarget(GetGlobalMousePosition());
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-10000, -10000, 20000, 20000), new Color(0.18f, 0.32f, 0.14f));
    }
}
