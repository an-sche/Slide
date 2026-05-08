using System;
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
        if (Near(r, g, b, Ground))        return SurfaceType.Ground;
        if (Near(r, g, b, Slidy))         return SurfaceType.Slidy;
        if (Near(r, g, b, Fast))          return SurfaceType.Fast;
        if (Near(r, g, b, Confusing))     return SurfaceType.Confusing;
        if (Near(r, g, b, FastConfusing)) return SurfaceType.FastConfusing;
        if (Near(r, g, b, Straight))      return SurfaceType.Straight;
        if (Near(r, g, b, Kill))          return SurfaceType.Kill;
        return null; // void / unknown → no zone
    }

    // Image.SetPixel truncates float*255 while Color.R8 rounds, producing a ±1 error per
    // channel for colors whose float*255 has a fractional part. All surface colors are far
    // enough apart that ±1 cannot produce a false positive.
    private static bool Near(int r, int g, int b, Color c) =>
        Math.Abs(r - c.R8) <= 1 && Math.Abs(g - c.G8) <= 1 && Math.Abs(b - c.B8) <= 1;
}
