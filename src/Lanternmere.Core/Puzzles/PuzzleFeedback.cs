namespace Lanternmere.Core.Puzzles;

/// <summary>
/// Every puzzle interaction returns one of these so the UI can give
/// non-punishing feedback per the brief: "Provide feedback for partial
/// progress and incorrect attempts without punishment."
/// </summary>
public enum PuzzleFeedback
{
    CorrectStep,
    Solved,
    ResetWrongOrder,
    NoOpWrongChoice,
    Locked,
    AlreadySolved,
    IncorrectPattern,
    Removed,
}
