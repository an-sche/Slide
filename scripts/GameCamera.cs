using Godot;

namespace Slide;

public partial class GameCamera : Camera2D
{
    public const float EdgeScrollSpeed = 600f;
    public const float EdgeThreshold   = 20f;
    public const float DefaultZoom     = 1.0f;
    public const float ZoomStep        = 0.1f;
    public const float MinZoom         = 1.0f;
    public const float MaxZoom         = 2.0f;

    private Node2D? _unit;
    private bool    _isLockedToUnit;
    private bool    _isSpaceHeld;
    private bool    _isMiddleMousePanning;
    private Vector2 _panStartMousePos;
    private Vector2 _panStartCameraPos;
    private Rect2   _levelBounds;
    private bool    _hasLevelBounds;

    public void Initialize(Node2D unit)
    {
        _unit           = unit;
        _isLockedToUnit = true;
        GlobalPosition  = unit.GlobalPosition;
        Zoom            = Vector2.One * DefaultZoom;
    }

    public void SetLevelBounds(Rect2 bounds)
    {
        _levelBounds    = bounds;
        _hasLevelBounds = true;

        // Disable Godot's built-in hard limits — we apply a softer clamp ourselves.
        LimitLeft   = -10_000_000;
        LimitTop    = -10_000_000;
        LimitRight  =  10_000_000;
        LimitBottom =  10_000_000;
    }

    public override void _Process(double delta)
    {
        if (!GetWindow().HasFocus()) return;
        if (_isSpaceHeld)
        {
            if (_unit != null) GlobalPosition = _unit.GlobalPosition;
            ClampPosition();
            return;
        }

        HandleEdgeScrolling((float)delta);

        if (_isLockedToUnit && _unit != null)
            GlobalPosition = _unit.GlobalPosition;

        ClampPosition();
    }

    private void ClampPosition()
    {
        if (!_hasLevelBounds) return;

        var   viewport = GetViewport().GetVisibleRect().Size;
        float halfW    = viewport.X * 0.5f / Zoom.X;
        float halfH    = viewport.Y * 0.5f / Zoom.Y;

        // Allow the camera to go off the edge, but stop before the map
        // leaves the screen entirely. The map must always have at least
        // one tile's worth of pixels visible.
        float margin = GameplayConstants.CellSize;
        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X,
                _levelBounds.Position.X - halfW + margin,
                _levelBounds.End.X      + halfW - margin),
            Mathf.Clamp(GlobalPosition.Y,
                _levelBounds.Position.Y - halfH + margin,
                _levelBounds.End.Y      + halfH - margin)
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (!GetWindow().HasFocus()) return;
        HandleSpaceInput(@event);
        HandleLockToggle(@event);
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
    }

    private void HandleLockToggle(InputEvent @event)
    {
        if (@event is not InputEventKey { Keycode: Key.C, Pressed: true, Echo: false }) return;

        _isLockedToUnit = !_isLockedToUnit;
        if (_isLockedToUnit && _unit != null)
            GlobalPosition = _unit.GlobalPosition;
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
