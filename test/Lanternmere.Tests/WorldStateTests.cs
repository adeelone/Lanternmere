using Lanternmere.Core;
using Xunit;

namespace Lanternmere.Tests;

public class WorldStateTests
{
    [Fact]
    public void GetFlag_UnsetFlag_ReturnsFalse()
    {
        var world = new WorldState();
        Assert.False(world.GetFlag("never_set"));
    }

    [Fact]
    public void SetFlag_DefaultsToTrue()
    {
        var world = new WorldState();
        world.SetFlag("door_unlocked");
        Assert.True(world.GetFlag("door_unlocked"));
    }

    [Fact]
    public void SetFlag_CanBeSetFalseExplicitly()
    {
        var world = new WorldState();
        world.SetFlag("torch_lit", true);
        world.SetFlag("torch_lit", false);
        Assert.False(world.GetFlag("torch_lit"));
    }
}
