using System.Collections.Generic;

namespace Lanternmere.Core;

/// <summary>
/// The mutable state a save slot captures: world flags, puzzle progress,
/// journal entries, inventory, shortcuts, and player location at the last
/// safe anchor. Kept as a plain data class (no MonoGame types) so it is
/// trivially serializable and unit-testable without a graphics device —
/// see test/Lanternmere.Tests/SaveSystemTests.cs.
/// </summary>
public sealed class WorldState
{
    /// <summary>Arbitrary world/story flags, e.g. "met_the_cartographer" -> true.</summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>Puzzle id -> current state id, e.g. "wind_cliffs_beacon" -> "two_of_three_lit".</summary>
    public Dictionary<string, string> PuzzleStates { get; set; } = new();

    public List<string> JournalDiscoveryIds { get; set; } = new();
    public List<string> InventoryItemIds { get; set; } = new();
    public List<string> UnlockedShortcutIds { get; set; } = new();
    public List<string> RecoveredNameFragmentIds { get; set; } = new();

    public string CurrentRegionId { get; set; } = "village_hub";
    public string LastSafeAnchorId { get; set; } = "village_hub_lantern";
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }

    public double PlaytimeSeconds { get; set; }
    public bool ReachedMainEnding { get; set; }
    public bool ReachedOptionalEndingVariation { get; set; }

    public bool GetFlag(string name) => Flags.TryGetValue(name, out var v) && v;
    public void SetFlag(string name, bool value = true) => Flags[name] = value;
}
