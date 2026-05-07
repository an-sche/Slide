using Godot;

namespace Slide;

public static class SurfaceConstants
{
    public static readonly Color Ground        = new(0.25f, 0.65f, 0.25f);
    public static readonly Color Slidy         = new(0.55f, 0.82f, 1.00f);
    public static readonly Color Fast          = new(0.10f, 0.25f, 0.75f);
    public static readonly Color Confusing     = new(0.75f, 0.55f, 0.95f);
    public static readonly Color FastConfusing = new(0.38f, 0.10f, 0.58f);
    public static readonly Color Straight      = new(0.72f, 0.72f, 0.72f);
    public static readonly Color Kill          = new(0.75f, 0.08f, 0.08f);
    public static readonly Color Void          = new(0.12f, 0.12f, 0.15f);

    public static Color ForType(SurfaceType type) => type switch
    {
        SurfaceType.Slidy         => Slidy,
        SurfaceType.Fast          => Fast,
        SurfaceType.Confusing     => Confusing,
        SurfaceType.FastConfusing => FastConfusing,
        SurfaceType.Straight      => Straight,
        SurfaceType.Kill          => Kill,
        _                         => Ground,
    };
}
