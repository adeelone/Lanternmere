using Lanternmere.Core;
using Lanternmere.Core.Content;
using Xunit;

namespace Lanternmere.Tests;

public class ItemDatabaseTests
{
    private const string SampleJson = """
    { "items": [ { "id": "brass_key", "displayName": "Brass Key", "description": "Opens the archive.", "iconPath": "Textures/items/brass_key" } ] }
    """;

    [Fact]
    public void LoadFromJson_ParsesItems()
    {
        var db = ItemDatabase.LoadFromJson(SampleJson);

        Assert.True(db.TryGet("brass_key", out var item));
        Assert.Equal("Brass Key", item.DisplayName);
    }
}

public class DiscoveryDatabaseTests
{
    private const string SampleJson = """
    { "discoveries": [
        { "id": "journal_01_arrival", "category": "journal", "title": "Arrival", "text": "...", "unlockedByFlag": "game_started" },
        { "id": "journal_02", "category": "landmark", "title": "Fragment", "text": "...", "unlockedByFlag": "fragment_recovered" }
    ] }
    """;

    [Fact]
    public void UnlockedFor_ReturnsOnlyDiscoveriesInWorldState()
    {
        var db = DiscoveryDatabase.LoadFromJson(SampleJson);
        var world = new WorldState();
        world.AddDiscovery("journal_01_arrival");

        var unlocked = db.UnlockedFor(world);

        Assert.Single(unlocked);
        Assert.Equal("journal_01_arrival", System.Linq.Enumerable.First(unlocked).Id);
    }

    [Fact]
    public void AddDiscovery_ReturnsTrueOnlyWhenNewlyAdded()
    {
        var world = new WorldState();

        Assert.True(world.AddDiscovery("journal_01_arrival"));
        Assert.False(world.AddDiscovery("journal_01_arrival"));
    }
}
