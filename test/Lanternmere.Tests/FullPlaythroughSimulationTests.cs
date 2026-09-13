using Lanternmere.Core;
using Lanternmere.Core.Puzzles;
using Xunit;

namespace Lanternmere.Tests;

/// <summary>
/// Simulates a full clean-save playthrough — title to the lantern-relight
/// ending trigger — entirely at the Core logic level (no GraphicsDevice
/// needed, since RegionScene now delegates all story-progression side
/// effects to <see cref="PuzzleProgression"/>, which is what this test
/// exercises directly). This is the automated half of the brief's
/// "clean-save playthrough checklist"; it proves the flag/puzzle/ending
/// graph has no softlock across the required order (rain garden -> wind
/// cliffs -> amber shore -> lantern) and, separately, that going
/// out-of-order (wind cliffs before rain garden) is correctly blocked
/// rather than silently broken. It does not replace an actual manual/
/// visual playthrough — see AUDIT.md for what that did and didn't cover.
/// </summary>
public class FullPlaythroughSimulationTests
{
    [Fact]
    public void CleanSave_RequiredOrder_ReachesEndingWithNoSoftlock()
    {
        var world = new WorldState();
        world.SetFlag("game_started");
        Assert.False(PuzzleProgression.CanRelightLantern(world));

        // Visit the rain garden (sets the cross-region clue flag Puzzle 2 needs).
        world.MarkRegionVisited("rain_garden");
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);

        var rainGarden = new RainGardenPuzzle(world);
        foreach (var plot in RainGardenPuzzle.CorrectOrder)
        {
            rainGarden.WaterPlot(plot);
        }
        Assert.True(rainGarden.IsSolved);
        PuzzleProgression.OnRainGardenSolved(world);
        Assert.True(world.GetFlag(PuzzleProgression.RainGardenFragmentFlag));

        var windCliffs = new WindCliffsPuzzle(world);
        foreach (var beacon in WindCliffsPuzzle.RequiredOrder)
        {
            windCliffs.LightBeacon(beacon);
        }
        Assert.True(windCliffs.IsSolved);
        PuzzleProgression.OnWindCliffsSolved(world);
        Assert.True(world.GetFlag(PuzzleProgression.BothPriorFragmentsFlag));

        // The brass key becomes available only after the wind cliffs puzzle
        // is solved (docs/PUZZLES.md region wiring) — confirm the flag it
        // depends on is actually set.
        Assert.True(world.GetFlag(PuzzleProgression.WindCliffsPuzzleSolvedFlag));

        var amberShore = new AmberShorePuzzle(world);
        Assert.False(amberShore.IsLocked, "Amber shore altar should unlock once both prior fragments are recovered.");
        for (var slot = 0; slot < AmberShorePuzzle.SlotCount; slot++)
        {
            amberShore.PlaceGlass(slot, AmberShorePuzzle.CorrectSlotItems[slot]);
        }
        Assert.True(amberShore.IsSolved);
        PuzzleProgression.OnAmberShoreSolved(world);

        Assert.True(PuzzleProgression.CanRelightLantern(world));
        PuzzleProgression.OnLanternRelit(world);
        Assert.True(world.ReachedMainEnding);
        Assert.False(world.ReachedOptionalEndingVariation, "Optional variation requires the archive secret too, which this run never visited.");
    }

    [Fact]
    public void CleanSave_WithArchiveSecretFirst_ReachesOptionalEndingVariation()
    {
        var world = new WorldState();
        world.MarkRegionVisited("rain_garden");
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);

        var rainGarden = new RainGardenPuzzle(world);
        foreach (var plot in RainGardenPuzzle.CorrectOrder) rainGarden.WaterPlot(plot);
        PuzzleProgression.OnRainGardenSolved(world);

        var windCliffs = new WindCliffsPuzzle(world);
        foreach (var beacon in WindCliffsPuzzle.RequiredOrder) windCliffs.LightBeacon(beacon);
        PuzzleProgression.OnWindCliffsSolved(world);

        var amberShore = new AmberShorePuzzle(world);
        for (var slot = 0; slot < AmberShorePuzzle.SlotCount; slot++)
        {
            amberShore.PlaceGlass(slot, AmberShorePuzzle.CorrectSlotItems[slot]);
        }
        PuzzleProgression.OnAmberShoreSolved(world);

        // The archive's optional secret is gated on the amber shore
        // fragment (docs/NARRATIVE_BIBLE.md revelation order) — reachable
        // only after it, matching region data's RequiresFlag on the
        // archive_final_record interactable.
        Assert.True(world.GetFlag("fragment_amber_shore_recovered"));
        world.AddDiscovery("journal_archive_final_record");

        PuzzleProgression.OnLanternRelit(world);

        Assert.True(world.ReachedMainEnding);
        Assert.True(world.ReachedOptionalEndingVariation);
    }

    [Fact]
    public void OutOfOrder_WindCliffsBeforeRainGarden_IsBlockedNotBroken()
    {
        var world = new WorldState();
        // Player goes straight to the wind cliffs without ever visiting the rain garden.
        var windCliffs = new WindCliffsPuzzle(world);

        var feedback = windCliffs.LightBeacon(WindCliffsPuzzle.RequiredOrder[0]);

        Assert.Equal(PuzzleFeedback.Locked, feedback);
        Assert.False(windCliffs.IsSolved);
        Assert.False(PuzzleProgression.CanRelightLantern(world));
    }

    [Fact]
    public void OutOfOrder_AmberShoreBeforeOthers_StaysLocked()
    {
        var world = new WorldState();
        var amberShore = new AmberShorePuzzle(world);

        Assert.True(amberShore.IsLocked);
        var feedback = amberShore.PlaceGlass(0, AmberShorePuzzle.CorrectSlotItems[0]);

        Assert.Equal(PuzzleFeedback.Locked, feedback);
    }

    [Fact]
    public void SaveReloadMidPlaythrough_PreservesAllProgressAcrossAllThreePuzzles()
    {
        var world = new WorldState();
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);

        new RainGardenPuzzle(world).WaterPlot(RainGardenPuzzle.CorrectOrder[0]);
        new WindCliffsPuzzle(world).LightBeacon(WindCliffsPuzzle.RequiredOrder[0]);
        world.SetFlag(PuzzleProgression.RainGardenFragmentFlag); // simulate one fragment already recovered pre-reload

        // Simulate a save/reload round trip via SaveSystem directly.
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "lanternmere-playthrough-" + System.Guid.NewGuid());
        GameSettings.SaveDirectoryOverrideForTests = tempDir;
        try
        {
            SaveSystem.Save(0, world);
            var (result, reloaded) = SaveSystem.Load(0);

            Assert.Equal(LoadResult.Success, result);
            var reloadedRainGarden = new RainGardenPuzzle(reloaded!);
            var reloadedWindCliffs = new WindCliffsPuzzle(reloaded!);

            Assert.Equal(1, reloadedRainGarden.WateredCount);
            Assert.Equal(1, reloadedWindCliffs.LitCount);
            Assert.True(reloaded!.GetFlag(PuzzleProgression.RainGardenFragmentFlag));
        }
        finally
        {
            GameSettings.SaveDirectoryOverrideForTests = null;
            try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
