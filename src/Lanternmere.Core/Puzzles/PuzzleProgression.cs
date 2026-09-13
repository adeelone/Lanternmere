namespace Lanternmere.Core.Puzzles;

/// <summary>
/// The story-progression side effects of solving each required puzzle
/// (fragment flags, discoveries, aggregate multi-fragment gates). Kept as
/// pure Core logic — separate from the puzzle state machines themselves —
/// so RegionScene (rendering) and tests both call the exact same wiring
/// instead of RegionScene containing untested glue that only a manual
/// playthrough would ever exercise.
/// </summary>
public static class PuzzleProgression
{
    public const string RainGardenFragmentFlag = "fragment_rain_garden_recovered";
    public const string WindCliffsFragmentFlag = "fragment_wind_cliffs_recovered";
    public const string AmberShoreFragmentFlag = "fragment_amber_shore_recovered";
    public const string WindCliffsPuzzleSolvedFlag = "wind_cliffs_puzzle_solved";
    public const string BothPriorFragmentsFlag = "both_prior_fragments_recovered";
    public const string AllFragmentsFlag = "all_fragments_recovered";

    public static void OnRainGardenSolved(WorldState world)
    {
        world.SetFlag(RainGardenFragmentFlag);
        world.AddDiscovery("journal_rain_garden_fragment");
        RefreshAggregateFragmentFlags(world);
    }

    public static void OnWindCliffsSolved(WorldState world)
    {
        world.SetFlag(WindCliffsFragmentFlag);
        world.SetFlag(WindCliffsPuzzleSolvedFlag);
        world.AddDiscovery("journal_wind_cliffs_fragment");
        RefreshAggregateFragmentFlags(world);
    }

    public static void OnAmberShoreSolved(WorldState world)
    {
        world.SetFlag(AmberShoreFragmentFlag);
        world.AddDiscovery("journal_amber_shore_fragment");
        RefreshAggregateFragmentFlags(world);
    }

    /// <summary>DialogueCondition only tests one flag at a time, but the Ferryperson's full account and the Cartographer's "ready" line need two-of-three / three-of-three checks — synthesized here as their own flags.</summary>
    public static void RefreshAggregateFragmentFlags(WorldState world)
    {
        var rainGarden = world.GetFlag(RainGardenFragmentFlag);
        var windCliffs = world.GetFlag(WindCliffsFragmentFlag);
        var amberShore = world.GetFlag(AmberShoreFragmentFlag);

        if (rainGarden && windCliffs) world.SetFlag(BothPriorFragmentsFlag);
        if (rainGarden && windCliffs && amberShore) world.SetFlag(AllFragmentsFlag);
    }

    public static bool AllFragmentsRecovered(WorldState world) => world.GetFlag(AllFragmentsFlag);

    /// <summary>The finale gate check RegionScene runs when the player interacts with the central lantern.</summary>
    public static bool CanRelightLantern(WorldState world) => AllFragmentsRecovered(world);

    /// <summary>Call once the lantern is relit: sets the main ending flag and, if the archive's optional secret was already found, the optional ending variation too.</summary>
    public static void OnLanternRelit(WorldState world)
    {
        world.ReachedMainEnding = true;
        if (world.HasDiscovery("journal_archive_final_record"))
        {
            world.ReachedOptionalEndingVariation = true;
        }
    }
}
