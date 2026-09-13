using System;

namespace Lanternmere.Core.Puzzles;

/// <summary>
/// Puzzle 1 (teaches observation + interaction) — docs/PUZZLES.md "Rain
/// garden watering sequence". Four plots must be watered driest-first; any
/// wrong-order attempt resets to zero watered (no failure state, per the
/// brief's "no punishment" requirement). State is just a watered-count
/// string in WorldState.PuzzleStates, since which specific plots are
/// watered is always the first N entries of CorrectOrder by construction.
/// </summary>
public sealed class RainGardenPuzzle
{
    public const string PuzzleId = "rain_garden_watering";
    private const string FailedAttemptsCounter = "rain_garden_watering_failed_attempts";

    /// <summary>Plot indices in required watering order (driest to least-dry). Plot 2 is the visually driest, then 0, then 3, then 1.</summary>
    public static readonly int[] CorrectOrder = { 2, 0, 3, 1 };

    private readonly WorldState _world;

    public RainGardenPuzzle(WorldState world) => _world = world;

    public string State
    {
        get => _world.PuzzleStates.TryGetValue(PuzzleId, out var s) ? s : "0_watered";
        private set => _world.PuzzleStates[PuzzleId] = value;
    }

    public bool IsSolved => State == "solved";

    public int WateredCount => State == "solved" ? CorrectOrder.Length
        : int.TryParse(State.AsSpan(0, State.IndexOf('_')), out var n) ? n : 0;

    public bool IsPlotWatered(int plotIndex) => Array.IndexOf(CorrectOrder, plotIndex) is var idx && idx >= 0 && idx < WateredCount;

    /// <summary>After 2 failed (wrong-order) attempts, the journal clue becomes available per the brief's "clues after repeated attempts" requirement.</summary>
    public bool HintAvailable => _world.GetCounter(FailedAttemptsCounter) >= 2;

    public PuzzleFeedback WaterPlot(int plotIndex)
    {
        if (IsSolved) return PuzzleFeedback.AlreadySolved;

        var expected = CorrectOrder[WateredCount];
        if (plotIndex == expected)
        {
            var newCount = WateredCount + 1;
            State = newCount == CorrectOrder.Length ? "solved" : $"{newCount}_watered";
            return newCount == CorrectOrder.Length ? PuzzleFeedback.Solved : PuzzleFeedback.CorrectStep;
        }

        State = "0_watered";
        _world.IncrementCounter(FailedAttemptsCounter);
        return PuzzleFeedback.ResetWrongOrder;
    }

    /// <summary>Manual reset via the puzzle's central marker, per the brief's "permit safe reset of each puzzle."</summary>
    public void ManualReset() => State = "0_watered";
}
