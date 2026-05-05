using Godot;

namespace Slide;

public partial class GameCamera : Camera2D
{
    private const float EdgeScrollSpeed = 600f;
    private const float EdgeThreshold   = 20f;
    private const float DefaultZoom     = 1.0f;
    private const float ZoomStep        = 0.1f;
    private const float MinZoom         = 1.0f;
    private const float MaxZoom         = 2.0f;

    private Node2D? _unit;
    private bool    _isLockedToUnit;
    private bool    _isSpaceHeld;
    private bool    _isMiddleMousePanning;
    private Vector2 _panStartMousePos;
    private Vector2 _panStartCameraPos;

    public void Initialize(Node2D unit)
    {
        _unit           = unit;
        _isLockedToUnit = true;
        GlobalPosition  = unit.GlobalPosition;
        Zoom            = Vector2.One * DefaultZoom;
    }

    public override void _Process(double delta)
    {
        if (!GetWindow().HasFocus()) return;
        if (_isSpaceHeld)
        {
            if (_unit != null) GlobalPosition = _unit.GlobalPosition;
            return;
        }

        HandleEdgeScrolling((float)delta);

        if (_isLockedToUnit && _unit != null)
            GlobalPosition = _unit.GlobalPosition;
    }

    public override void _Input(InputEvent @event)
    {
        if (!GetWindow().HasFocus()) return;
        HandleSpaceInput(@event);
        HandleMiddleMousePan(@event);
        HandleZoom(@event);
    }

    private void HandleSpaceInput(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (key.Keycode != Key.Space || key.Echo) return;

        _isSpaceHeld = key.Pressed;

        if (key.Pressed && _unit != null)
            GlobalPosition = _unit.GlobalPosition;
        else if (!key.Pressed)
            _isLockedToUnit = false;
    }

    private void HandleMiddleMousePan(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Middle } mb)
        {
            if (mb.Pressed && !_isSpaceHeld)
            {
                _isMiddleMousePanning = true;
                _isLockedToUnit       = false;
                _panStartMousePos     = mb.Position;
                _panStartCameraPos    = GlobalPosition;
            }
            else if (!mb.Pressed)
            {
                _isMiddleMousePanning = false;
            }
        }

        if (@event is InputEventMouseMotion motion && _isMiddleMousePanning && !_isSpaceHeld)
            GlobalPosition = _panStartCameraPos - (motion.Position - _panStartMousePos);
    }

    private void HandleZoom(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

        float delta = mb.ButtonIndex switch
        {
            MouseButton.WheelUp   =>  ZoomStep,
            MouseButton.WheelDown => -ZoomStep,
            _ => 0f,
        };

        if (delta == 0f) return;

        float newZoom = Mathf.Clamp(Zoom.X + delta, MinZoom, MaxZoom);
        Zoom = Vector2.One * newZoom;
    }

    private void HandleEdgeScrolling(float delta)
    {
        if (_isMiddleMousePanning) return;

        Vector2 mousePos     = GetViewport().GetMousePosition();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scrollDir    = Vector2.Zero;

        if (mousePos.X < EdgeThreshold)                  scrollDir.X = -1;
        else if (mousePos.X > viewportSize.X - EdgeThreshold) scrollDir.X = 1;

        if (mousePos.Y < EdgeThreshold)                  scrollDir.Y = -1;
        else if (mousePos.Y > viewportSize.Y - EdgeThreshold) scrollDir.Y = 1;

        if (scrollDir != Vector2.Zero)
        {
            _isLockedToUnit = false;
            GlobalPosition += scrollDir * EdgeScrollSpeed * delta;
        }
    }
}
