using NUnit.Framework;
using WordPuzzle.Modes;

/// <summary>
/// TASK 16A — post-win surface routing. Pins which surface each mode/context shows so a
/// regression (e.g. Classic dumping to the full page, or Time Attack ending mid-run) fails loudly.
/// </summary>
[TestFixture]
public class PostWinRouterTests
{
    [Test]
    public void Classic_Solve_ShowsCompactPanel()
    {
        Assert.AreEqual(PostWinSurface.CompactWinPanel,
            PostWinRouter.Decide(ModeKind.Classic, isDaily: false, puzzleComplete: true, timeUp: false));
    }

    [Test]
    public void Classic_NotComplete_ShowsNothing()
    {
        Assert.AreEqual(PostWinSurface.None,
            PostWinRouter.Decide(ModeKind.Classic, false, puzzleComplete: false, timeUp: false));
    }

    [Test]
    public void Daily_Solve_ShowsFullResults()
    {
        Assert.AreEqual(PostWinSurface.FullResults,
            PostWinRouter.Decide(ModeKind.Classic, isDaily: true, puzzleComplete: true, timeUp: false));
    }

    [Test]
    public void TimeAttack_SolveWithTimeLeft_AdvancesNextLadder()
    {
        Assert.AreEqual(PostWinSurface.AdvanceNextLadder,
            PostWinRouter.Decide(ModeKind.TimeAttack, false, puzzleComplete: true, timeUp: false));
    }

    [Test]
    public void TimeAttack_TimeUp_ShowsFullResults()
    {
        // Run-end results even if the in-progress ladder wasn't finished.
        Assert.AreEqual(PostWinSurface.FullResults,
            PostWinRouter.Decide(ModeKind.TimeAttack, false, puzzleComplete: false, timeUp: true));
        Assert.AreEqual(PostWinSurface.FullResults,
            PostWinRouter.Decide(ModeKind.TimeAttack, false, puzzleComplete: true, timeUp: true));
    }

    [Test]
    public void TimeAttack_MidRunNotComplete_ShowsNothing()
    {
        Assert.AreEqual(PostWinSurface.None,
            PostWinRouter.Decide(ModeKind.TimeAttack, false, puzzleComplete: false, timeUp: false));
    }

    [Test]
    public void PuzzleShow_Solve_ShowsFullResults()
    {
        Assert.AreEqual(PostWinSurface.FullResults,
            PostWinRouter.Decide(ModeKind.PuzzleShow, false, puzzleComplete: true, timeUp: false));
    }

    [Test]
    public void PuzzleShow_NotComplete_ShowsNothing()
    {
        Assert.AreEqual(PostWinSurface.None,
            PostWinRouter.Decide(ModeKind.PuzzleShow, false, puzzleComplete: false, timeUp: false));
    }
}
