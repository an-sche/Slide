using Godot;

namespace Slide;

public partial class World : Node2D
{
    private Unit? _unit;
    private GameCamera? _camera;
    private Hud? _hud;
    private LevelTransition? _transition;

    public override void _Ready()
    {
        _unit = GetNode<Unit>("Unit");
        _camera = GetNode<GameCamera>("Camera");
        _camera.Initialize(_unit);

        _hud = new Hud();
        AddChild(_hud);
        _unit.Died += _hud.OnUnitDied;
        _unit.Respawned += _hud.OnUnitRespawned;
        _hud.SetUnit(_unit);

        _transition = new LevelTransition();
        AddChild(_transition);

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

        // Ground tile center is (-1200, -600); place blocks within it
        var startPos = new Vector2(-1400, -800);
        AddChild(new StartBlock { Position = startPos });

        var endBlock = new EndBlock { Position = new Vector2(-1000, -400) };
        AddChild(endBlock);
        endBlock.LevelCompleted += OnLevelCompleted;

        _unit!.SetStartPosition(startPos);

        // Patrol enemies on the slidy tile (center 0, -600)
        AddChild(new Enemy
        {
            Position = new Vector2(-500, -600),
            Radius   = 32f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(-500, -600), 250f),
                new Waypoint(new Vector2( 500, -600), 250f),
            ], PatrolEndBehavior.Loop),
        });
        AddChild(new Enemy
        {
            Position = new Vector2(200, -800),
            Radius   = 24f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(200, -800), 350f),
                new Waypoint(new Vector2(200, -400), 350f),
            ], PatrolEndBehavior.Loop),
        });
        AddChild(new Enemy
        {
            Position = new Vector2(-200, -400),
            Radius   = 20f,
            Behavior = new PatrolBehavior(
            [
                new Waypoint(new Vector2(-200, -400), 180f),
                new Waypoint(new Vector2( 400, -400), 180f),
                new Waypoint(new Vector2( 400, -800), 180f),
                new Waypoint(new Vector2(-200, -800), 180f),
            ], PatrolEndBehavior.Loop),
        });
    }

    private void OnLevelCompleted()
    {
        RunState.LevelUpAll();
        _transition!.ShowTransition("Slider 1", RunState.ElapsedSeconds, RunState.TotalDeaths);
    }

    public override void _Process(double delta)
    {
        if (Input.IsMouseButtonPressed(MouseButton.Right))
            _unit?.SetTarget(GetGlobalMousePosition());
    }

#if DEBUG
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Quoteleft }) return;
        if (_unit == null) return;

        var ps = _unit.PlayerState;
        ps.PlayerLevel      = 20;
        ps.AbilityLevels[0] = 4; // Q Boost
        ps.AbilityLevels[1] = 4; // W Warp
        ps.AbilityLevels[2] = 4; // E Donut
        ps.AbilityLevels[3] = 4; // R Ethereal
        ps.AbilityLevels[4] = 1; // F Gack
        GetViewport().SetInputAsHandled();
    }
#endif

    public override void _Draw()
    {
        DrawRect(new Rect2(-10000, -10000, 20000, 20000), new Color(0.18f, 0.32f, 0.14f));
    }
}
