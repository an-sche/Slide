using System;
using System.Collections.Generic;
using Godot;

namespace Slide;

public partial class CanvasView : Control
{
    public event Action<Vector2I>? PixelClicked;
    public event Action<Vector2I>? PixelLeftPressed;
    public event Action<Vector2I, Vector2>? PixelRightClicked;
    public event Action?           StrokeEnded;

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
    private EditorOverlay[]  _overlays              = [];
    private EditorLine[]     _lines                 = [];
    private Vector2?         _ghostLineWorldFrom;

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

    public void SetOverlays(EditorOverlay[] overlays, EditorLine[] lines, float cellSize)
    {
        _overlays = overlays;
        _lines    = lines;
        _cellSize = cellSize;
        QueueRedraw();
    }

    public void SetGhostLine(Vector2? worldFrom) => _ghostLineWorldFrom = worldFrom;

    public Color GetPixel(Vector2I px) => _image?.GetPixel(px.X, px.Y) ?? Colors.Black;

    public void SetPixel(Vector2I px, Color color)
    {
        if (_image == null) return;
        if ((uint)px.X >= (uint)_image.GetWidth() || (uint)px.Y >= (uint)_image.GetHeight()) return;
        _image.SetPixel(px.X, px.Y, color);
    }

    public void FlushTexture()
    {
        if (_image == null || _texture == null) return;
        _texture.Update(_image);
        QueueRedraw();
    }

    public IEnumerable<Vector2I> BrushPixels(Vector2I center)
    {
        if (_image == null || _brushRadius < 0) yield break;
        int r = _brushRadius;
        int w = _image.GetWidth(), h = _image.GetHeight();
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int px = center.X + dx, py = center.Y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;
            yield return new Vector2I(px, py);
        }
    }

    public int BrushRadius
    {
        get => _brushRadius;
        set { _brushRadius = value; QueueRedraw(); }
    }

    public void AdjustBrushRadius(int delta)
    {
        if (_brushRadius < 0) return;
        _brushRadius = Mathf.Max(0, _brushRadius + delta);
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

        // Path lines between waypoints
        foreach (var line in _lines)
            DrawLine(WorldToScreen(line.WorldFrom), WorldToScreen(line.WorldTo), line.Color, 1.5f);

        // Ghost line: last-placed waypoint → cursor during placement
        if (_ghostLineWorldFrom.HasValue)
            DrawLine(WorldToScreen(_ghostLineWorldFrom.Value), GetLocalMousePosition(),
                     new Color(1f, 1f, 1f, 0.35f), 1.5f);

        // Entity/enemy overlays
        foreach (var ov in _overlays)
            DrawOverlay(WorldToScreen(ov.WorldPos), ov);

        // Brush preview
        if (_brushRadius < 0) return;
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

            case InputEventMouseButton mb when !mb.Pressed && mb.ButtonIndex == MouseButton.Left:
                StrokeEnded?.Invoke();
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseButton mb when mb.Pressed && mb.ButtonIndex == MouseButton.Right:
                PixelRightClicked?.Invoke(ToPixel(mb.Position), mb.GlobalPosition);
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
        const float R      = 8f;
        var         white  = new Color(1f, 1f, 1f, 0.7f);
        var         sel    = new Color(1f, 0.9f, 0.2f);

        switch (ov.Shape)
        {
            case OverlayShape.Circle:
                DrawCircle(sp, R, ov.Color);
                DrawArc(sp, R, 0, Mathf.Tau, 32, white, 1.5f);
                if (ov.Selected)
                    DrawArc(sp, R + 4f, 0, Mathf.Tau, 32, sel, 2f);
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
                if (ov.Selected)
                {
                    float s = R + 4f;
                    Vector2[] sel_pts =
                    [
                        sp + new Vector2(0,  -s),
                        sp + new Vector2(s,   0),
                        sp + new Vector2(0,   s),
                        sp + new Vector2(-s,  0),
                    ];
                    DrawPolyline([..sel_pts, sel_pts[0]], sel, 2f);
                }
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
