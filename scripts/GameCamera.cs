using Godot;

public partial class GameCamera : Camera2D
{
    private const float EdgeScrollSpeed = 600f;
    private const float EdgeThreshold = 20f;

    private Node2D? _unit;
    private bool _isLockedToUnit;
    private bool _isMiddleMousePanning;
    private Vector2 _panStartMousePos;
    private Vector2 _panStartCameraPos;

    public void Initialize(Node2D unit)
    {
        _unit = unit;
        _isLockedToUnit = true;
        GlobalPosition = unit.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        // Edge scroll may break the lock — run it first
        HandleEdgeScrolling((float)delta);

        if (_isLockedToUnit && _unit != null)
            GlobalPosition = _unit.GlobalPosition;
    }

    public override void _Input(InputEvent @event)
    {
        HandleSpaceInput(@event);
        HandleMiddleMousePan(@event);
    }

    private void HandleSpaceInput(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (key.Keycode != Key.Space || key.Echo) return;

        if (key.Pressed)
        {
            // Press: snap to unit and lock
            _isLockedToUnit = true;
            if (_unit != null)
                GlobalPosition = _unit.GlobalPosition;
        }
        else
        {
            // Release: unlock so player can pan freely
            _isLockedToUnit = false;
        }
    }

    private void HandleMiddleMousePan(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Middle } mb)
        {
            if (mb.Pressed)
            {
                _isMiddleMousePanning = true;
                _isLockedToUnit = false;
                _panStartMousePos = mb.Position;
                _panStartCameraPos = GlobalPosition;
            }
            else
            {
                _isMiddleMousePanning = false;
            }
        }

        if (@event is InputEventMouseMotion motion && _isMiddleMousePanning)
        {
            GlobalPosition = _panStartCameraPos - (motion.Position - _panStartMousePos);
        }
    }

    private void HandleEdgeScrolling(float delta)
    {
        if (_isMiddleMousePanning) return;

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scrollDir = Vector2.Zero;

        if (mousePos.X < EdgeThreshold) scrollDir.X = -1;
        else if (mousePos.X > viewportSize.X - EdgeThreshold) scrollDir.X = 1;

        if (mousePos.Y < EdgeThreshold) scrollDir.Y = -1;
        else if (mousePos.Y > viewportSize.Y - EdgeThreshold) scrollDir.Y = 1;

        if (scrollDir != Vector2.Zero)
        {
            _isLockedToUnit = false;
            GlobalPosition += scrollDir * EdgeScrollSpeed * delta;
        }
    }
}
