using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lanternmere.Systems;

public enum IconShape { Key, Leaf, Glass, Book, Diamond, Circle, Flame, Fern }

/// <summary>
/// Generates Lanternmere's placeholder art procedurally at runtime — no
/// external image files. This is the "cohesive, legally usable placeholder
/// set" the brief allows when final custom assets can't all be produced;
/// see ASSET_MANIFEST.md for the honest label. Every texture is built from
/// simple deterministic shape math so re-running the game reproduces
/// identical art, and every region shares the same warm-lamplight /
/// cool-natural palette described in docs/ART_DIRECTION.md.
/// </summary>
public static class TextureFactory
{
    /// <summary>A cheap deterministic hash used as a per-pixel pseudo-random dither seed, so tiles look textured rather than flat without needing real noise.</summary>
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = x * 374761393 + y * 668265263 + seed * 2246822519;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    public static Texture2D CreateSolid(GraphicsDevice device, Color color)
    {
        var tex = new Texture2D(device, 1, 1);
        tex.SetData(new[] { color });
        return tex;
    }

    /// <summary>A ground tile: base color with a subtle dither for texture, and a slightly darker border so tile edges read visually.</summary>
    public static Texture2D CreateGroundTile(GraphicsDevice device, int size, Color baseColor, int variantSeed)
    {
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var n = Hash(x, y, variantSeed);
                var shade = 0.9f + n * 0.16f; // +/- ~8% brightness dither
                var color = new Color(
                    (byte)Math.Clamp(baseColor.R * shade, 0, 255),
                    (byte)Math.Clamp(baseColor.G * shade, 0, 255),
                    (byte)Math.Clamp(baseColor.B * shade, 0, 255));
                pixels[y * size + x] = color;
            }
        }
        var tex = new Texture2D(device, size, size);
        tex.SetData(pixels);
        return tex;
    }

    /// <summary>A solid/wall tile: darker base with a beveled-looking top+left highlight edge so collision geometry reads clearly against ground tiles.</summary>
    public static Texture2D CreateWallTile(GraphicsDevice device, int size, Color baseColor)
    {
        var pixels = new Color[size * size];
        var highlight = Lighten(baseColor, 0.25f);
        var shadow = Darken(baseColor, 0.25f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                Color c = baseColor;
                if (y == 0 || x == 0) c = highlight;
                if (y == size - 1 || x == size - 1) c = shadow;
                pixels[y * size + x] = c;
            }
        }
        var tex = new Texture2D(device, size, size);
        tex.SetData(pixels);
        return tex;
    }

    /// <summary>A simple robed-figure silhouette used for the player and NPCs: circular head + trapezoid body, two-tone (body/accent).</summary>
    public static Texture2D CreateFigure(GraphicsDevice device, int width, int height, Color bodyColor, Color accentColor)
    {
        var pixels = new Color[width * height];
        Array.Fill(pixels, Color.Transparent);

        var headRadius = width * 0.28f;
        var headCenter = new Vector2(width / 2f, headRadius + 1);
        var bodyTop = headRadius * 2f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                var pos = new Vector2(x + 0.5f, y + 0.5f);

                if (Vector2.Distance(pos, headCenter) <= headRadius)
                {
                    pixels[idx] = accentColor;
                    continue;
                }

                if (y >= bodyTop)
                {
                    var t = (y - bodyTop) / Math.Max(1f, height - bodyTop);
                    var halfWidth = MathHelper.Lerp(width * 0.22f, width * 0.42f, t); // robe widens toward the feet
                    var center = width / 2f;
                    if (Math.Abs(pos.X - center) <= halfWidth)
                    {
                        pixels[idx] = t > 0.85f ? Darken(bodyColor, 0.3f) : bodyColor; // a shaded hem near the feet
                    }
                }
            }
        }

        var tex = new Texture2D(device, width, height);
        tex.SetData(pixels);
        return tex;
    }

    /// <summary>A small glyph-style icon for items, landmarks, and UI — simple enough to read at 16-24px, distinct enough to tell shapes apart without color alone (accessibility: shape carries meaning, not just hue).</summary>
    public static Texture2D CreateIcon(GraphicsDevice device, int size, Color color, IconShape shape)
    {
        var pixels = new Color[size * size];
        Array.Fill(pixels, Color.Transparent);
        var c = new Vector2(size / 2f, size / 2f);
        var r = size * 0.38f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var inside = shape switch
                {
                    IconShape.Circle => Vector2.Distance(p, c) <= r,
                    IconShape.Diamond => Math.Abs(p.X - c.X) / r + Math.Abs(p.Y - c.Y) / r <= 1f,
                    IconShape.Key => IsKeyShape(p, c, r),
                    IconShape.Leaf or IconShape.Fern => IsLeafShape(p, c, r),
                    IconShape.Glass => IsGlassShape(p, c, r),
                    IconShape.Book => IsBookShape(p, c, r),
                    IconShape.Flame => IsFlameShape(p, c, r),
                    _ => Vector2.Distance(p, c) <= r,
                };
                if (inside) pixels[y * size + x] = color;
            }
        }

        var tex = new Texture2D(device, size, size);
        tex.SetData(pixels);
        return tex;
    }

    private static bool IsKeyShape(Vector2 p, Vector2 c, float r)
    {
        var ringCenter = c - new Vector2(0, r * 0.35f);
        var ringOuter = Vector2.Distance(p, ringCenter) <= r * 0.4f;
        var ringInner = Vector2.Distance(p, ringCenter) <= r * 0.18f;
        var shaft = p.X >= c.X - r * 0.12f && p.X <= c.X + r * 0.12f && p.Y >= c.Y && p.Y <= c.Y + r * 0.9f;
        var tooth = p.Y >= c.Y + r * 0.55f && p.Y <= c.Y + r * 0.75f && p.X >= c.X && p.X <= c.X + r * 0.4f;
        return (ringOuter && !ringInner) || shaft || tooth;
    }

    private static bool IsLeafShape(Vector2 p, Vector2 c, float r)
    {
        var d = p - c;
        // a rotated ellipse reads as a leaf silhouette
        var rotated = new Vector2(d.X * 0.7f - d.Y * 0.7f, d.X * 0.7f + d.Y * 0.7f);
        return (rotated.X * rotated.X) / (r * r) + (rotated.Y * rotated.Y) / ((r * 0.5f) * (r * 0.5f)) <= 1f;
    }

    private static bool IsGlassShape(Vector2 p, Vector2 c, float r)
    {
        var top = c - new Vector2(0, r * 0.6f);
        var bottom = c + new Vector2(0, r * 0.6f);
        if (p.Y < top.Y || p.Y > bottom.Y) return false;
        var t = (p.Y - top.Y) / (bottom.Y - top.Y);
        var halfWidth = MathHelper.Lerp(r * 0.55f, r * 0.15f, MathF.Abs(t - 0.5f) * 2f);
        return Math.Abs(p.X - c.X) <= halfWidth;
    }

    private static bool IsBookShape(Vector2 p, Vector2 c, float r)
    {
        var rect = Math.Abs(p.X - c.X) <= r * 0.55f && Math.Abs(p.Y - c.Y) <= r * 0.42f;
        var spine = Math.Abs(p.X - c.X) <= r * 0.05f;
        return rect && !spine || (rect && spine);
    }

    private static bool IsFlameShape(Vector2 p, Vector2 c, float r)
    {
        var d = p - (c + new Vector2(0, r * 0.15f));
        var teardrop = (d.X * d.X) / (r * 0.4f * r * 0.4f) + (d.Y * d.Y) / (r * r) <= 1f && d.Y > -r * 0.9f;
        return teardrop;
    }

    private static Color Lighten(Color c, float amount) => new(
        (byte)Math.Clamp(c.R + 255 * amount, 0, 255),
        (byte)Math.Clamp(c.G + 255 * amount, 0, 255),
        (byte)Math.Clamp(c.B + 255 * amount, 0, 255));

    private static Color Darken(Color c, float amount) => new(
        (byte)Math.Clamp(c.R * (1 - amount), 0, 255),
        (byte)Math.Clamp(c.G * (1 - amount), 0, 255),
        (byte)Math.Clamp(c.B * (1 - amount), 0, 255));

    /// <summary>Maps a pickup's item id to a distinguishing icon shape, so a brass key, a fern, and a shard of glass don't all render as the same generic dot. Shared by every scene that draws item icons (region pickups, inventory).</summary>
    public static IconShape IconShapeForItem(string itemId) => itemId switch
    {
        "brass_key" => IconShape.Key,
        "pressed_fern" => IconShape.Leaf,
        _ when itemId.StartsWith("tide_glass_shard_") => IconShape.Glass,
        _ => IconShape.Circle,
    };

    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        return new Color(r, g, b);
    }
}
