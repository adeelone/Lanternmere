namespace Lanternmere.Core.Puzzles;

/// <summary>
/// Puzzle 3 (uses knowledge from all regions, gates the finale) —
/// docs/PUZZLES.md "Amber shore tide-glass array". Locked until both other
/// region fragments are recovered; then the player places three distinct
/// tide-glass shards into three altar slots in the pattern the Ferryperson
/// describes. Correct placements are never lost by an incorrect placement
/// elsewhere ("no reset of already-placed correct pieces"); removal is
/// always the player's explicit choice.
/// </summary>
public sealed class AmberShorePuzzle
{
    public const string PuzzleId = "amber_shore_tide_glass";
    public const string RainGardenFragmentFlag = "fragment_rain_garden_recovered";
    public const string WindCliffsFragmentFlag = "fragment_wind_cliffs_recovered";

    public const int SlotCount = 3;

    /// <summary>The correct shard item id for each altar slot, per the Ferryperson's account.</summary>
    public static readonly string[] CorrectSlotItems = { "tide_glass_shard_dawn", "tide_glass_shard_dusk", "tide_glass_shard_deep" };

    private readonly WorldState _world;

    public AmberShorePuzzle(WorldState world) => _world = world;

    public string State
    {
        get => _world.PuzzleStates.TryGetValue(PuzzleId, out var s) ? s : (IsUnlockable ? "unlocked" : "locked");
        private set => _world.PuzzleStates[PuzzleId] = value;
    }

    private bool IsUnlockable => _world.GetFlag(RainGardenFragmentFlag) && _world.GetFlag(WindCliffsFragmentFlag);

    /// <summary>Call once per frame/interaction check: promotes locked -> unlocked the moment both fragments are recovered.</summary>
    public void RefreshLockState()
    {
        if (State == "locked" && IsUnlockable) State = "unlocked";
    }

    public bool IsLocked => State == "locked";
    public bool IsSolved => State == "solved";

    private string SlotKey(int slot) => $"{PuzzleId}:{slot}";

    public string? ItemInSlot(int slot) =>
        _world.PuzzleSlotItems.TryGetValue(SlotKey(slot), out var item) ? item : null;

    public bool IsSlotCorrect(int slot) => ItemInSlot(slot) == CorrectSlotItems[slot];

    public PuzzleFeedback PlaceGlass(int slot, string itemId)
    {
        RefreshLockState();
        if (IsSolved) return PuzzleFeedback.AlreadySolved;
        if (IsLocked) return PuzzleFeedback.Locked;

        if (itemId != CorrectSlotItems[slot])
        {
            return PuzzleFeedback.IncorrectPattern; // no state change: wrong guesses never undo correct ones
        }

        _world.PuzzleSlotItems[SlotKey(slot)] = itemId;

        var allCorrect = true;
        for (var i = 0; i < SlotCount; i++)
        {
            if (!IsSlotCorrect(i)) { allCorrect = false; break; }
        }

        if (allCorrect)
        {
            State = "solved";
            return PuzzleFeedback.Solved;
        }
        return PuzzleFeedback.CorrectStep;
    }

    /// <summary>Always allowed before solved, per the brief's "no punishment for experimentation."</summary>
    public PuzzleFeedback RemoveGlass(int slot)
    {
        if (IsSolved) return PuzzleFeedback.AlreadySolved;
        _world.PuzzleSlotItems.Remove(SlotKey(slot));
        return PuzzleFeedback.Removed;
    }
}
