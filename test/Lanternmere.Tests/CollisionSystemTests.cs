using Lanternmere.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Lanternmere.Tests;

public class CollisionSystemTests
{
    [Fact]
    public void ResolveMove_NoObstacles_MovesFully()
    {
        var collision = new CollisionSystem();
        var bounds = new Rectangle(0, 0, 16, 16);

        var resolved = collision.ResolveMove(bounds, new Vector2(5, 3));

        Assert.Equal(new Vector2(5, 3), resolved);
    }

    [Fact]
    public void ResolveMove_BlockedOnXAxis_StillAllowsYAxis()
    {
        var collision = new CollisionSystem();
        collision.AddSolidRegion(new Rectangle(20, -100, 8, 300)); // vertical wall to the right
        var bounds = new Rectangle(0, 0, 16, 16);

        var resolved = collision.ResolveMove(bounds, new Vector2(10, 5));

        Assert.Equal(0, resolved.X);
        Assert.Equal(5, resolved.Y);
    }

    [Fact]
    public void ResolveMove_BlockedOnBothAxes_StopsCompletely()
    {
        var collision = new CollisionSystem();
        collision.AddSolidRegion(new Rectangle(10, 10, 100, 100));
        var bounds = new Rectangle(0, 0, 16, 16);

        var resolved = collision.ResolveMove(bounds, new Vector2(8, 8));

        Assert.Equal(Vector2.Zero, resolved);
    }
}
