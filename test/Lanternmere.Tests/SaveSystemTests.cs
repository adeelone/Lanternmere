using System;
using System.IO;
using Lanternmere.Core;
using Xunit;

namespace Lanternmere.Tests;

/// <summary>
/// Covers the brief's explicit requirement: "Include migration handling
/// and save/load round-trip tests." Each test uses an isolated
/// APPDATA/HOME so it never touches a real player's save directory.
/// </summary>
public class SaveSystemTests : IDisposable
{
    private readonly string _originalAppData;
    private readonly string _tempDir;

    public SaveSystemTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lanternmere-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        // GameSettings.GetSaveDirectory() reads ApplicationData; redirect it
        // per-test via the environment variable that SpecialFolder resolves
        // from on this platform, so tests never collide with each other or
        // a real save.
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? "";
        Environment.SetEnvironmentVariable("APPDATA", _tempDir);
        Environment.SetEnvironmentVariable("HOME", _tempDir); // covers Linux/macOS resolution path too
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData);
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var world = new WorldState
        {
            CurrentRegionId = "wind_cliffs",
            LastSafeAnchorId = "wind_cliffs_beacon",
            PlayerX = 42.5f,
            PlayerY = 17f,
            PlaytimeSeconds = 123.4,
        };
        world.SetFlag("met_the_cartographer");
        world.PuzzleStates["wind_cliffs_beacon"] = "two_of_three_lit";
        world.JournalDiscoveryIds.Add("journal_02_rain_garden_fragment");
        world.InventoryItemIds.Add("pressed_fern");

        SaveSystem.Save(0, world);
        var (result, loaded) = SaveSystem.Load(0);

        Assert.Equal(LoadResult.Success, result);
        Assert.NotNull(loaded);
        Assert.Equal("wind_cliffs", loaded!.CurrentRegionId);
        Assert.Equal(42.5f, loaded.PlayerX);
        Assert.True(loaded.GetFlag("met_the_cartographer"));
        Assert.Equal("two_of_three_lit", loaded.PuzzleStates["wind_cliffs_beacon"]);
        Assert.Contains("journal_02_rain_garden_fragment", loaded.JournalDiscoveryIds);
        Assert.Contains("pressed_fern", loaded.InventoryItemIds);
    }

    [Fact]
    public void Load_MissingSlot_ReturnsNotFound()
    {
        var (result, world) = SaveSystem.Load(1);
        Assert.Equal(LoadResult.NotFound, result);
        Assert.Null(world);
    }

    [Fact]
    public void Load_CorruptedPrimary_FallsBackToBackup()
    {
        var world = new WorldState { CurrentRegionId = "amber_shore" };
        SaveSystem.Save(2, world);

        // A second good save creates a valid backup of the first save...
        world.CurrentRegionId = "village_hub";
        SaveSystem.Save(2, world);

        // ...now corrupt the primary file directly to simulate a crash
        // mid-write or disk corruption.
        var primaryPath = Path.Combine(GameSettings.GetSaveDirectory(), "save2.json");
        File.WriteAllText(primaryPath, "{ not valid json ][");

        var (result, loaded) = SaveSystem.Load(2);

        Assert.Equal(LoadResult.RecoveredFromBackup, result);
        Assert.NotNull(loaded);
        // Backup holds the *previous* good save (region "amber_shore"),
        // since the corrupted write was the "village_hub" save.
        Assert.Equal("amber_shore", loaded!.CurrentRegionId);
    }

    [Fact]
    public void Load_CorruptedPrimaryAndNoBackup_ReturnsCorrupted()
    {
        var saveDir = GameSettings.GetSaveDirectory();
        var primaryPath = Path.Combine(saveDir, "save0.json");
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(primaryPath, "not json at all");

        var (result, loaded) = SaveSystem.Load(0);

        Assert.Equal(LoadResult.Corrupted, result);
        Assert.Null(loaded);
    }
}
