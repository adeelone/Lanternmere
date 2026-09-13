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

    /// <summary>Region ids the player has ever entered — drives the fog-of-discovery map (only visited regions render detail).</summary>
    public List<string> VisitedRegionIds { get; set; } = new();

    /// <summary>Landmark interactable ids the player has examined at least once — drives landmark sketches in the journal and map pins.</summary>
    public List<string> RevealedLandmarkIds { get; set; } = new();

    /// <summary>Named integer counters (e.g. puzzle failed-attempt counts) that don't fit the boolean Flags model.</summary>
    public Dictionary<string, int> Counters { get; set; } = new();

    /// <summary>Puzzle slot contents, keyed "{puzzleId}:{slotIndex}" -> item id placed there. Used by slot-based puzzles like the amber shore tide-glass array.</summary>
    public Dictionary<string, string> PuzzleSlotItems { get; set; } = new();

    public int GetCounter(string name) => Counters.TryGetValue(name, out var v) ? v : 0;
    public int IncrementCounter(string name) => Counters[name] = GetCounter(name) + 1;

    public string CurrentRegionId { get; set; } = "village_hub";
    public string LastSafeAnchorId { get; set; } = "village_hub_lantern";
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }

    public double PlaytimeSeconds { get; set; }
    public bool ReachedMainEnding { get; set; }
    public bool ReachedOptionalEndingVariation { get; set; }

    public bool HasItem(string itemId) => InventoryItemIds.Contains(itemId);

    public void AddItem(string itemId)
    {
        if (!InventoryItemIds.Contains(itemId)) InventoryItemIds.Add(itemId);
    }

    public void MarkRegionVisited(string regionId)
    {
        if (!VisitedRegionIds.Contains(regionId)) VisitedRegionIds.Add(regionId);
    }

    public void RevealLandmark(string interactableId)
    {
        if (!RevealedLandmarkIds.Contains(interactableId)) RevealedLandmarkIds.Add(interactableId);
    }

    public bool HasDiscovery(string discoveryId) => JournalDiscoveryIds.Contains(discoveryId);

    /// <summary>Adds a discovery to the journal if not already present. Returns true if this was newly added (so callers can show a "new discovery" notification).</summary>
    public bool AddDiscovery(string discoveryId)
    {
        if (JournalDiscoveryIds.Contains(discoveryId)) return false;
        JournalDiscoveryIds.Add(discoveryId);
        return true;
    }

    public bool GetFlag(string name) => Flags.TryGetValue(name, out var v) && v;
    public void SetFlag(string name, bool value = true) => Flags[name] = value;
}
