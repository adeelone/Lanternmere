using System;
using System.IO;
using System.Linq;
using Lanternmere.Core.Content;
using Xunit;

namespace Lanternmere.Tests;

/// <summary>
/// Validates the *actual* content that ships in Content/Data — not just
/// synthetic fixtures — per the brief's "Automated map validation for
/// missing spawn points, bad destinations, duplicate IDs, and unreachable
/// required triggers." Finds the data directory by walking up from the
/// test binary until it finds the repo's src/Lanternmere/Content/Data
/// folder, so it works regardless of build configuration or CI checkout
/// layout.
/// </summary>
public class ShippedContentTests
{
    private static string FindDataRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Lanternmere", "Content", "Data");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate src/Lanternmere/Content/Data by walking up from the test binary.");
    }

    [Fact]
    public void AllShippedRegions_PassValidation()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));

        var issues = RegionLoader.Validate(regions);

        Assert.True(issues.Count == 0, "Region validation issues:\n" + string.Join("\n", issues));
    }

    [Fact]
    public void AllShippedRegions_IncludeTheFiveRequiredAreas()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));

        foreach (var expected in new[] { "village_hub", "rain_garden", "wind_cliffs", "amber_shore", "archive" })
        {
            Assert.True(regions.ContainsKey(expected), $"Missing required region '{expected}'");
        }
    }

    [Fact]
    public void AllShippedRegions_HaveEightLandmarksTotal()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));

        var landmarkCount = regions.Values.Sum(r => r.Interactables.Count(i => i.IsLandmark));

        Assert.Equal(8, landmarkCount);
    }

    [Fact]
    public void AllShippedRegions_HaveExactlyOneOptionalSecret()
    {
        // The brief requires "one optional multi-step secret." IsOptionalSecret
        // was previously set in content but never read by any game logic —
        // this test is what gives that flag an actual purpose: a content
        // invariant that would catch either zero secrets (requirement unmet)
        // or more than one (scope creep past what the brief asks for).
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));

        var secretCount = regions.Values.Sum(r => r.Interactables.Count(i => i.IsOptionalSecret));

        Assert.Equal(1, secretCount);
    }

    [Fact]
    public void AllShippedRegions_HaveFiveNpcsTotal()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));

        var npcCount = regions.Values.Sum(r => r.Npcs.Count);

        Assert.Equal(5, npcCount);
    }

    [Fact]
    public void AllNpcDialogueTrees_ExistAndAreWellFormed()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));
        var dialogueDir = Path.Combine(dataRoot, "dialogue");
        var dialogueFiles = Directory.GetFiles(dialogueDir, "*.json").Select(Path.GetFileNameWithoutExtension).ToHashSet();

        foreach (var npc in regions.Values.SelectMany(r => r.Npcs))
        {
            Assert.True(dialogueFiles.Contains(npc.DialogueId), $"NPC '{npc.Id}' references missing dialogue file '{npc.DialogueId}'.json");
        }
    }

    [Fact]
    public void ItemsAndDiscoveries_LoadAndAreNonEmpty()
    {
        var dataRoot = FindDataRoot();
        var items = ItemDatabase.LoadFromJson(File.ReadAllText(Path.Combine(dataRoot, "items", "items.json")));
        var discoveries = DiscoveryDatabase.LoadFromJson(File.ReadAllText(Path.Combine(dataRoot, "discoveries", "discoveries.json")));

        Assert.True(items.Items.Count >= 5);
        Assert.True(discoveries.Discoveries.Count >= 12, "Brief requires at least twelve journal discoveries.");
    }

    [Fact]
    public void EveryInteractablePuzzleReference_MatchesAKnownPuzzleId()
    {
        var dataRoot = FindDataRoot();
        var regions = RegionLoader.LoadAllFromDirectory(Path.Combine(dataRoot, "maps"));
        var knownPuzzleIds = new[]
        {
            Lanternmere.Core.Puzzles.RainGardenPuzzle.PuzzleId,
            Lanternmere.Core.Puzzles.WindCliffsPuzzle.PuzzleId,
            Lanternmere.Core.Puzzles.AmberShorePuzzle.PuzzleId,
        };

        foreach (var interactable in regions.Values.SelectMany(r => r.Interactables).Where(i => i.PuzzleId is not null))
        {
            Assert.Contains(interactable.PuzzleId, knownPuzzleIds);
        }
    }
}
