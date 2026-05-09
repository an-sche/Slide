using System;
using Godot;

namespace Slide;

public partial class CanvasView : Control
{
    public event Action<Vector2I>? PixelClicked;
    public event Action<Vector2I>? PixelLeftPressed;

    private Image?        _image;
    private ImageTexture? _texture;
    private Vector2       _offset;
    private float         _zoom = 1f;
    private bool          _panning;
    private Vector2       _panStart;
    private Vector2       _offsetAtPanStart;
    private bool          _needsFit;
    private Label         _coordLabel  = null!;
    private int           _brushRadius = 0;
    private float         _cellSize    = 1f;
    private EditorOverlay[] _overlays  = [];

    public CanvasView()
    {
        MouseFilter   = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        ClipContents  = true;
    }

    public override void _Ready()
    {
        SetProcess(true);

        _coordLabel = new Label { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _coordLabel.SetAnchorsPreset(LayoutPreset.BottomLeft);
        _coordLabel.OffsetLeft   = 8;
        _coordLabel.OffsetBottom = -6;
        _coordLabel.AddThemeFontSizeOverride("font_size", 12);
        _coordLabel.AddThemeColorOverride("font_color", new Color(0.70f, 0.70f, 0.70f));
        AddChild(_coordLabel);
    }

    public void LoadImage(Image image)
    {
        _image    = image;
        _texture  = ImageTexture.CreateFromImage(image);
        _needsFit = true;
        QueueRedraw();
    }

    public Image? GetImage() => _image;

    public void SetOverlays(EditorOverlay[] overlays, float cellSize)
    {
        _overlays = overlays;
        _cellSize  = cellSize;
        QueueRedraw();
    }

    public int BrushRadius => _brushRadius;

    public void AdjustBrushRadius(int delta)
    {
        _brushRadius = Mathf.Max(0, _brushRadius + delta);
        QueueRedraw();
    }

    public void PaintBrush(Vector2I center, Color color)
    {
        if (_image == null) return;
        int r = _brushRadius;
        int w = _image.GetWidth(), h = _image.GetHeight();
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int px = center.X + dx, py = center.Y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;
            _image.SetPixel(px, py, color);
        }
        _texture!.Update(_image);
        QueueRedraw();
    }

    public Vector2I ToPixel(Vector2 screenPos) =>
        new(Mathf.FloorToInt((screenPos.X - _offset.X) / _zoom),
            Mathf.FloorToInt((screenPos.Y - _offset.Y) / _zoom));

    private void FitToView()
    {
        if (_image == null || Size == Vector2.Zero) return;
        float sx = (Size.X - 40f) / _image.GetWidth();
        float sy = (Size.Y - 40f) / _image.GetHeight();
        _zoom   = Mathf.Clamp(Mathf.Min(sx, sy), 0.25f, 64f);
        _offset = (Size - new Vector2(_image.GetWidth(), _image.GetHeight()) * _zoom) / 2f;
    }

    private void UpdateCoordLabel(Vector2 pos)
    {
        if (_image == null) { _coordLabel.Visible = false; return; }
        var px = ToPixel(pos);
        bool inside = (uint)px.X < (uint)_image.GetWidth() &&
                      (uint)px.Y < (uint)_image.GetHeight();
        _coordLabel.Visible = inside;
        if (inside) _coordLabel.Text = _brushRadius > 0
            ? $"{px.X}, {px.Y}  r:{_brushRadius}"
            : $"{px.X}, {px.Y}";
    }

    public override void _Process(double delta)
    {
        var local = GetLocalMousePosition();
        if (_panning)
            _offset = _offsetAtPanStart + (local - _panStart);
        UpdateCoordLabel(local);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.15f, 0.15f, 0.18f));
        if (_texture == null || _image == null) return;

        if (_needsFit) { FitToView(); _needsFit = false; }

        var imgSize = new Vector2(_image.GetWidth(), _image.GetHeight());
        DrawTextureRect(_texture, new Rect2(_offset, imgSize * _zoom), false);

        if (_zoom >= 8f)
        {
            var grid = new Color(0f, 0f, 0f, 0.3f);
            float x0 = _offset.X, y0 = _offset.Y;
            float x1 = x0 + imgSize.X * _zoom;
            float y1 = y0 + imgSize.Y * _zoom;
            for (int x = 0; x <= _image.GetWidth(); x++)
                DrawLine(new Vector2(x0 + x * _zoom, y0), new Vector2(x0 + x * _zoom, y1), grid);
            for (int y = 0; y <= _image.GetHeight(); y++)
                DrawLine(new Vector2(x0, y0 + y * _zoom), new Vector2(x1, y0 + y * _zoom), grid);
        }

        // Entity overlays
        foreach (var ov in _overlays)
            DrawOverlay(WorldToScreen(ov.WorldPos), ov);

        // Brush preview
        var mouse    = GetLocalMousePosition();
        var cursorPx = ToPixel(mouse);
        if ((uint)cursorPx.X < (uint)_image.GetWidth() && (uint)cursorPx.Y < (uint)_image.GetHeight())
        {
            var   screenCenter = new Vector2(cursorPx.X + 0.5f, cursorPx.Y + 0.5f) * _zoom + _offset;
            float screenRadius = (_brushRadius + 0.5f) * _zoom;
            DrawArc(screenCenter, screenRadius, 0, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.8f), 1.5f);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mb when mb.Pressed && mb.ButtonIndex == MouseButton.Left:
                PixelLeftPressed?.Invoke(ToPixel(mb.Position));
                PixelClicked?.Invoke(ToPixel(mb.Position));
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseMotion motion when motion.ButtonMask.HasFlag(MouseButtonMask.Left):
                PixelClicked?.Invoke(ToPixel(motion.Position));
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseButton mb when mb.ButtonIndex == MouseButton.Middle:
                if (mb.Pressed)
                {
                    _panning          = true;
                    _panStart         = mb.Position;
                    _offsetAtPanStart = _offset;
                }
                else
                {
                    _panning = false;
                }
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseButton mb when mb.Pressed &&
                 mb.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown:
            {
                float   factor = mb.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1f / 1.15f;
                Vector2 pivot  = (mb.Position - _offset) / _zoom;
                _zoom   = Mathf.Clamp(_zoom * factor, 0.25f, 64f);
                _offset = mb.Position - pivot * _zoom;
                QueueRedraw();
                GetViewport().SetInputAsHandled();
                break;
            }
        }
    }

    private Vector2 WorldToScreen(Vector2 worldPos) =>
        (worldPos / _cellSize) * _zoom + _offset;

    private void DrawOverlay(Vector2 sp, EditorOverlay ov)
    {
        const float R     = 8f;
        var         white = new Color(1f, 1f, 1f, 0.7f);

        switch (ov.Shape)
        {
            case OverlayShape.Circle:
                DrawCircle(sp, R, ov.Color);
                DrawArc(sp, R, 0, Mathf.Tau, 32, white, 1.5f);
                break;

            case OverlayShape.Diamond:
                Vector2[] pts =
                [
                    sp + new Vector2(0,  -R),
                    sp + new Vector2(R,   0),
                    sp + new Vector2(0,   R),
                    sp + new Vector2(-R,  0),
                ];
                DrawColoredPolygon(pts, ov.Color);
                DrawPolyline([..pts, pts[0]], white, 1.5f);
                break;
        }

        if (_zoom >= 3f && !string.IsNullOrEmpty(ov.Label))
            DrawString(ThemeDB.FallbackFont, sp + new Vector2(R + 3f, 4f),
                ov.Label, HorizontalAlignment.Left, -1, 11, white);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }
}
