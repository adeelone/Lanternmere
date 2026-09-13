using System.Collections.Generic;
using Lanternmere.Core.Content;
using Xunit;

namespace Lanternmere.Tests;

public class RegionLoaderTests
{
    private static RegionDescriptor MakeRegion(string id, int w = 3, int h = 3) => new()
    {
        Id = id,
        WidthTiles = w,
        HeightTiles = h,
        SolidTiles = new int[w * h],
        SpawnPoints = new Dictionary<string, SpawnPointDef> { ["default"] = new() { X = 1, Y = 1 } },
    };

    [Fact]
    public void LoadFromJson_RoundTripsBasicFields()
    {
        var region = MakeRegion("village_hub");
        var json = System.Text.Json.JsonSerializer.Serialize(region);

        var loaded = RegionLoader.LoadFromJson(json);

        Assert.Equal("village_hub", loaded.Id);
        Assert.True(loaded.SpawnPoints.ContainsKey("default"));
    }

    [Fact]
    public void Validate_MissingDefaultSpawn_ReportsIssue()
    {
        var region = MakeRegion("village_hub");
        region.SpawnPoints.Clear();

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = region });

        Assert.Contains(issues, i => i.Message.Contains("default"));
    }

    [Fact]
    public void Validate_TransitionToUnknownRegion_ReportsIssue()
    {
        var region = MakeRegion("village_hub");
        region.Transitions.Add(new TransitionDef { Id = "to_nowhere", DestinationRegionId = "does_not_exist", DestinationSpawnId = "default" });

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = region });

        Assert.Contains(issues, i => i.Message.Contains("unknown region"));
    }

    [Fact]
    public void Validate_TransitionToUnknownSpawn_ReportsIssue()
    {
        var hub = MakeRegion("village_hub");
        var garden = MakeRegion("rain_garden");
        hub.Transitions.Add(new TransitionDef { Id = "to_garden", DestinationRegionId = "rain_garden", DestinationSpawnId = "does_not_exist" });

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = hub, ["rain_garden"] = garden });

        Assert.Contains(issues, i => i.Message.Contains("does_not_exist"));
    }

    [Fact]
    public void Validate_ValidTransition_ReportsNoIssue()
    {
        var hub = MakeRegion("village_hub");
        var garden = MakeRegion("rain_garden");
        hub.Transitions.Add(new TransitionDef { Id = "to_garden", DestinationRegionId = "rain_garden", DestinationSpawnId = "default" });

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = hub, ["rain_garden"] = garden });

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_DuplicateIds_ReportsIssue()
    {
        var region = MakeRegion("village_hub");
        region.Interactables.Add(new InteractableDef { Id = "lantern", X = 1, Y = 1 });
        region.Npcs.Add(new NpcPlacementDef { Id = "lantern", X = 1, Y = 1 });

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = region });

        Assert.Contains(issues, i => i.Message.Contains("Duplicate id"));
    }

    [Fact]
    public void Validate_SolidTilesLengthMismatch_ReportsIssue()
    {
        var region = MakeRegion("village_hub");
        region.SolidTiles = new int[2]; // wrong length for 3x3

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = region });

        Assert.Contains(issues, i => i.Message.Contains("SolidTiles length"));
    }

    [Fact]
    public void Validate_InteractableBuriedInSolidTiles_ReportsUnreachable()
    {
        var region = MakeRegion("village_hub");
        region.TileSize = 16;
        // Mark every tile solid so the interactable at tile (1,1) is fully boxed in.
        for (var i = 0; i < region.SolidTiles.Length; i++) region.SolidTiles[i] = 1;
        region.Interactables.Add(new InteractableDef { Id = "buried", X = 16, Y = 16 });

        var issues = RegionLoader.Validate(new Dictionary<string, RegionDescriptor> { ["village_hub"] = region });

        Assert.Contains(issues, i => i.Message.Contains("unreachable"));
    }
}
