using Lanternmere.Core;
using Lanternmere.Core.Puzzles;
using Xunit;

namespace Lanternmere.Tests;

public class RainGardenPuzzleTests
{
    [Fact]
    public void WaterPlot_CorrectOrder_SolvesPuzzle()
    {
        var world = new WorldState();
        var puzzle = new RainGardenPuzzle(world);

        foreach (var plot in RainGardenPuzzle.CorrectOrder)
        {
            var feedback = puzzle.WaterPlot(plot);
            if (plot == RainGardenPuzzle.CorrectOrder[^1])
                Assert.Equal(PuzzleFeedback.Solved, feedback);
            else
                Assert.Equal(PuzzleFeedback.CorrectStep, feedback);
        }

        Assert.True(puzzle.IsSolved);
    }

    [Fact]
    public void WaterPlot_WrongOrder_ResetsWithoutSoftlock()
    {
        var world = new WorldState();
        var puzzle = new RainGardenPuzzle(world);

        puzzle.WaterPlot(RainGardenPuzzle.CorrectOrder[0]);
        var feedback = puzzle.WaterPlot(RainGardenPuzzle.CorrectOrder[3]); // out of order

        Assert.Equal(PuzzleFeedback.ResetWrongOrder, feedback);
        Assert.Equal(0, puzzle.WateredCount);
        Assert.False(puzzle.IsSolved);
    }

    [Fact]
    public void WaterPlot_RepeatedFailuresStillAllowEventualSuccess()
    {
        var world = new WorldState();
        var puzzle = new RainGardenPuzzle(world);

        for (var i = 0; i < 5; i++)
        {
            puzzle.WaterPlot(999); // always wrong
        }
        foreach (var plot in RainGardenPuzzle.CorrectOrder)
        {
            puzzle.WaterPlot(plot);
        }

        Assert.True(puzzle.IsSolved);
    }

    [Fact]
    public void HintAvailable_AfterTwoFailedAttempts()
    {
        var world = new WorldState();
        var puzzle = new RainGardenPuzzle(world);

        Assert.False(puzzle.HintAvailable);
        puzzle.WaterPlot(999);
        puzzle.WaterPlot(999);

        Assert.True(puzzle.HintAvailable);
    }

    [Fact]
    public void PuzzleState_PersistsAcrossSaveReloadMidAttempt()
    {
        var world = new WorldState();
        var puzzle = new RainGardenPuzzle(world);
        puzzle.WaterPlot(RainGardenPuzzle.CorrectOrder[0]);
        puzzle.WaterPlot(RainGardenPuzzle.CorrectOrder[1]);

        // Simulate reload: a fresh WorldState populated from the same PuzzleStates dict.
        var reloaded = new WorldState { PuzzleStates = new(world.PuzzleStates) };
        var resumed = new RainGardenPuzzle(reloaded);

        Assert.Equal(2, resumed.WateredCount);
        Assert.False(resumed.IsSolved);
    }
}

public class WindCliffsPuzzleTests
{
    [Fact]
    public void LightBeacon_WithoutRainGardenVisit_ReturnsLocked()
    {
        var world = new WorldState();
        var puzzle = new WindCliffsPuzzle(world);

        var feedback = puzzle.LightBeacon(WindCliffsPuzzle.RequiredOrder[0]);

        Assert.Equal(PuzzleFeedback.Locked, feedback);
        Assert.False(puzzle.IsSolved);
    }

    [Fact]
    public void LightBeacon_CorrectTwoInOrder_Solves()
    {
        var world = new WorldState();
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);
        var puzzle = new WindCliffsPuzzle(world);

        puzzle.LightBeacon(WindCliffsPuzzle.RequiredOrder[0]);
        var feedback = puzzle.LightBeacon(WindCliffsPuzzle.RequiredOrder[1]);

        Assert.Equal(PuzzleFeedback.Solved, feedback);
    }

    [Fact]
    public void LightBeacon_WrongBeacon_DoesNotResetOrSolve()
    {
        var world = new WorldState();
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);
        var puzzle = new WindCliffsPuzzle(world);

        puzzle.LightBeacon(WindCliffsPuzzle.RequiredOrder[0]);
        var feedback = puzzle.LightBeacon(1); // beacon 1 is not load-bearing

        Assert.Equal(PuzzleFeedback.NoOpWrongChoice, feedback);
        Assert.Equal(1, puzzle.LitCount); // progress preserved
        Assert.False(puzzle.IsSolved);
    }

    [Fact]
    public void LightBeacon_CorrectBeaconsWrongOrder_DoesNotSolveOrReset()
    {
        var world = new WorldState();
        world.SetFlag(WindCliffsPuzzle.RainGardenVisitedFlag);
        var puzzle = new WindCliffsPuzzle(world);

        // Light the second required beacon before the first.
        var feedback = puzzle.LightBeacon(WindCliffsPuzzle.RequiredOrder[1]);

        Assert.Equal(PuzzleFeedback.NoOpWrongChoice, feedback);
        Assert.Equal(0, puzzle.LitCount);
        Assert.False(puzzle.IsSolved);
    }
}

public class AmberShorePuzzleTests
{
    [Fact]
    public void PlaceGlass_WhileLocked_ReturnsLocked()
    {
        var world = new WorldState();
        var puzzle = new AmberShorePuzzle(world);

        var feedback = puzzle.PlaceGlass(0, AmberShorePuzzle.CorrectSlotItems[0]);

        Assert.Equal(PuzzleFeedback.Locked, feedback);
    }

    [Fact]
    public void PlaceGlass_UnlockedAfterBothFragments()
    {
        var world = new WorldState();
        world.SetFlag(AmberShorePuzzle.RainGardenFragmentFlag);
        world.SetFlag(AmberShorePuzzle.WindCliffsFragmentFlag);
        var puzzle = new AmberShorePuzzle(world);

        Assert.False(puzzle.IsLocked);
    }

    [Fact]
    public void PlaceGlass_AllCorrect_Solves()
    {
        var world = new WorldState();
        world.SetFlag(AmberShorePuzzle.RainGardenFragmentFlag);
        world.SetFlag(AmberShorePuzzle.WindCliffsFragmentFlag);
        var puzzle = new AmberShorePuzzle(world);

        puzzle.PlaceGlass(0, AmberShorePuzzle.CorrectSlotItems[0]);
        puzzle.PlaceGlass(1, AmberShorePuzzle.CorrectSlotItems[1]);
        var feedback = puzzle.PlaceGlass(2, AmberShorePuzzle.CorrectSlotItems[2]);

        Assert.Equal(PuzzleFeedback.Solved, feedback);
    }

    [Fact]
    public void PlaceGlass_IncorrectPattern_DoesNotResetPriorCorrectSlots()
    {
        var world = new WorldState();
        world.SetFlag(AmberShorePuzzle.RainGardenFragmentFlag);
        world.SetFlag(AmberShorePuzzle.WindCliffsFragmentFlag);
        var puzzle = new AmberShorePuzzle(world);

        puzzle.PlaceGlass(0, AmberShorePuzzle.CorrectSlotItems[0]); // correct
        var feedback = puzzle.PlaceGlass(1, AmberShorePuzzle.CorrectSlotItems[2]); // wrong shard for slot 1

        Assert.Equal(PuzzleFeedback.IncorrectPattern, feedback);
        Assert.True(puzzle.IsSlotCorrect(0)); // slot 0 untouched
        Assert.False(puzzle.IsSolved);
    }

    [Fact]
    public void RemoveGlass_AlwaysAllowedBeforeSolved()
    {
        var world = new WorldState();
        world.SetFlag(AmberShorePuzzle.RainGardenFragmentFlag);
        world.SetFlag(AmberShorePuzzle.WindCliffsFragmentFlag);
        var puzzle = new AmberShorePuzzle(world);

        puzzle.PlaceGlass(0, AmberShorePuzzle.CorrectSlotItems[0]);
        puzzle.RemoveGlass(0);

        Assert.Null(puzzle.ItemInSlot(0));
    }
}
