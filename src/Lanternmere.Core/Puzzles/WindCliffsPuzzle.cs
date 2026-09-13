using System;

namespace Lanternmere.Core.Puzzles;

/// <summary>
/// Puzzle 2 (combines clues from two locations) — docs/PUZZLES.md "Wind
/// cliffs beacon triangulation". Three beacons; only two are load-bearing
/// (indicated by a local rope-fray clue), and they must be lit in the
/// order indicated by a rain-garden weathervane clue (a cross-region
/// requirement — this puzzle cannot be solved, or even attempted on the
/// beacon-lighting step, until the player has visited the rain garden).
/// Lighting the non-load-bearing beacon, or a load-bearing one out of
/// order, is a no-op with feedback — never a reset, per the brief.
/// </summary>
public sealed class WindCliffsPuzzle
{
    public const string PuzzleId = "wind_cliffs_beacon";
    public const string RainGardenVisitedFlag = "visited_rain_garden";

    /// <summary>Beacon indices that are load-bearing (per the rope-fray clue), in the required lighting order (upwind first, per the weathervane clue).</summary>
    public static readonly int[] RequiredOrder = { 0, 2 };
    public const int BeaconCount = 3;

    private readonly WorldState _world;

    public WindCliffsPuzzle(WorldState world) => _world = world;

    public string State
    {
        get => _world.PuzzleStates.TryGetValue(PuzzleId, out var s) ? s : "0_of_3_lit";
        private set => _world.PuzzleStates[PuzzleId] = value;
    }

    public bool IsSolved => State == "solved";

    public int LitCount => State == "solved" ? RequiredOrder.Length
        : int.TryParse(State.AsSpan(0, State.IndexOf('_')), out var n) ? n : 0;

    public bool IsBeaconLit(int beaconIndex) => Array.IndexOf(RequiredOrder, beaconIndex) is var idx && idx >= 0 && idx < LitCount;

    public bool IsLoadBearing(int beaconIndex) => Array.IndexOf(RequiredOrder, beaconIndex) >= 0;

    public PuzzleFeedback LightBeacon(int beaconIndex)
    {
        if (IsSolved) return PuzzleFeedback.AlreadySolved;
        if (!_world.GetFlag(RainGardenVisitedFlag)) return PuzzleFeedback.Locked;

        var expected = RequiredOrder[LitCount];
        if (beaconIndex == expected)
        {
            var newCount = LitCount + 1;
            State = newCount == RequiredOrder.Length ? "solved" : $"{newCount}_of_3_lit";
            return newCount == RequiredOrder.Length ? PuzzleFeedback.Solved : PuzzleFeedback.CorrectStep;
        }

        // Wrong beacon (non-load-bearing) or a load-bearing one lit out of
        // order: feedback only, progress is never lost.
        return PuzzleFeedback.NoOpWrongChoice;
    }

    /// <summary>Manual reset via the lever at the beacon base — the only way progress can go backwards, since wrong choices never reset it.</summary>
    public void ManualReset() => State = "0_of_3_lit";
}
