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

    public static SurfaceType? FromColor(Color c)
    {
        int r = c.R8, g = c.G8, b = c.B8;
        if (r == Ground.R8        && g == Ground.G8        && b == Ground.B8)        return SurfaceType.Ground;
        if (r == Slidy.R8         && g == Slidy.G8         && b == Slidy.B8)         return SurfaceType.Slidy;
        if (r == Fast.R8          && g == Fast.G8          && b == Fast.B8)          return SurfaceType.Fast;
        if (r == Confusing.R8     && g == Confusing.G8     && b == Confusing.B8)     return SurfaceType.Confusing;
        if (r == FastConfusing.R8 && g == FastConfusing.G8 && b == FastConfusing.B8) return SurfaceType.FastConfusing;
        if (r == Straight.R8      && g == Straight.G8      && b == Straight.B8)      return SurfaceType.Straight;
        if (r == Kill.R8          && g == Kill.G8          && b == Kill.B8)          return SurfaceType.Kill;
        return null; // void / unknown → no zone
    }
}
