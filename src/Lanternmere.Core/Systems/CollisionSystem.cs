using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Lanternmere.Systems;

/// <summary>
/// Minimal AABB tile/object collision. Deliberately excludes slopes per the
/// brief ("Tile and object collision with slopes avoided unless fully
/// supported") — every solid region is an axis-aligned rectangle.
/// </summary>
public sealed class CollisionSystem
{
    private readonly List<Rectangle> _solidRegions = new();

    public void SetSolidRegions(IEnumerable<Rectangle> regions)
    {
        _solidRegions.Clear();
        _solidRegions.AddRange(regions);
    }

    public void AddSolidRegion(Rectangle region) => _solidRegions.Add(region);

    /// <summary>
    /// Resolve a proposed move for an axis-aligned actor bounds, sliding
    /// along whichever axis isn't blocked rather than stopping outright —
    /// standard "move X then Y independently" tile collision resolution.
    /// </summary>
    public Vector2 ResolveMove(Rectangle actorBounds, Vector2 delta)
    {
        var resolved = delta;

        var movedX = actorBounds;
        movedX.Offset((int)delta.X, 0);
        if (CollidesWithAny(movedX))
        {
            resolved.X = 0;
        }

        var movedY = actorBounds;
        movedY.Offset((int)resolved.X, (int)delta.Y);
        if (CollidesWithAny(movedY))
        {
            resolved.Y = 0;
        }

        return resolved;
    }

    public bool CollidesWithAny(Rectangle bounds)
    {
        foreach (var region in _solidRegions)
        {
            if (region.Intersects(bounds)) return true;
        }
        return false;
    }

    public IReadOnlyList<Rectangle> SolidRegions => _solidRegions;
}
