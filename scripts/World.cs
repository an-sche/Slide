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
        CreateTestLevel();
    }

    private void CreateTestLevel()
    {
        // 3x2 grid of surface tiles, each 400x400, centered at origin
        (SurfaceType type, int col, int row)[] grid =
        {
            (SurfaceType.Ground,        0, 0),
            (SurfaceType.Slidy,         1, 0),
            (SurfaceType.Fast,          2, 0),
            (SurfaceType.Confusing,     0, 1),
            (SurfaceType.FastConfusing, 1, 1),
            (SurfaceType.Straight,      2, 1),
        };

        var tileSize = new Vector2(1200, 1200);

        foreach (var (type, col, row) in grid)
        {
            var center = new Vector2(
                (col - 1) * tileSize.X,       // cols 0,1,2 → centers -400, 0, 400
                (row - 0.5f) * tileSize.Y      // rows 0,1   → centers -200, 200
            );
            AddChild(new SurfaceZone { Type = type, Size = tileSize, Position = center });
        }

        // Kill border, 400px thick, surrounding the 3600x2400 grid
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(4400, 400), Position = new Vector2(0, -1600) }); // top
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(4400, 400), Position = new Vector2(0,  1600) }); // bottom
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(400,  2400), Position = new Vector2(-2200, 0) }); // left
        AddChild(new SurfaceZone { Type = SurfaceType.Kill, Size = new Vector2(400,  2400), Position = new Vector2( 2200, 0) }); // right

        _unit!.Position = new Vector2(-400, -200); // center of Ground tile
    }

    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Right))
            _unit?.SetTarget(GetGlobalMousePosition());
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-10000, -10000, 20000, 20000), new Color(0.18f, 0.32f, 0.14f));
    }
}
