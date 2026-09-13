#if DEBUG
using System.Collections.Generic;

namespace Lanternmere.Core;

/// <summary>
/// Command-line dev flags for jumping straight into a region or UI scene,
/// instead of navigating there by hand — useful for visual QA and
/// screenshot-based verification in environments where scripting real
/// keyboard/gamepad input into the game window isn't reliable. Compiled
/// out of Release builds entirely (this whole file is behind #if DEBUG),
/// so it can never be a "debug shortcut in release" per the brief.
///
///   --region &lt;id&gt; [--spawn &lt;id&gt;]   jump straight into a region
///   --scene &lt;name&gt;                   jump straight into a UI overlay scene
///                                     (journal|map|inventory|pause|settings|
///                                     credits|creditsroll|ending) over a
///                                     synthetic mid-playthrough world state
///   --perflog [--perfseconds N]      log min/avg/max FPS to the console
///                                     after N seconds (default 5), then exit
/// </summary>
public sealed class DevLaunchOptions
{
    public string? RegionId;
    public string SpawnId = "default";
    public string? SceneName;
    public bool PerfLog;
    public int PerfLogSeconds = 5;

    public static DevLaunchOptions Parse(string[] args)
    {
        var opts = new DevLaunchOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--region" when i + 1 < args.Length: opts.RegionId = args[++i]; break;
                case "--spawn" when i + 1 < args.Length: opts.SpawnId = args[++i]; break;
                case "--scene" when i + 1 < args.Length: opts.SceneName = args[++i]; break;
                case "--perflog": opts.PerfLog = true; break;
                case "--perfseconds" when i + 1 < args.Length && int.TryParse(args[i + 1], out var s): opts.PerfLogSeconds = s; i++; break;
            }
        }
        return opts;
    }

    public bool HasAnything => RegionId is not null || SceneName is not null || PerfLog;

    /// <summary>A synthetic mid-playthrough WorldState so --scene screenshots (journal with entries, inventory with items, a partly-revealed map) show something meaningful rather than an empty new game.</summary>
    public static WorldState BuildPreviewWorldState()
    {
        var world = new WorldState();
        world.SetFlag("game_started");
        world.MarkRegionVisited("village_hub");
        world.MarkRegionVisited("rain_garden");
        world.MarkRegionVisited("wind_cliffs");
        world.SetFlag(Puzzles.WindCliffsPuzzle.RainGardenVisitedFlag);
        Puzzles.PuzzleProgression.OnRainGardenSolved(world);
        Puzzles.PuzzleProgression.OnWindCliffsSolved(world);
        world.AddItem("brass_key");
        world.AddItem("pressed_fern");
        world.AddItem("tide_glass_shard_dawn");
        world.RevealLandmark("village_hub_lantern");
        world.RevealLandmark("rain_garden_dry_fountain");
        return world;
    }
}
#endif
