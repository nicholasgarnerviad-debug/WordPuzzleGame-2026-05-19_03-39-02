namespace WordPuzzle.Modes
{
    /// <summary>Which post-win surface to present (Task 16).</summary>
    public enum PostWinSurface
    {
        None,               // nothing yet (puzzle not finished / run continues silently)
        AdvanceNextLadder,  // load the next ladder immediately (Time Attack within a run)
        CompactWinPanel,    // small inline win panel on the board (endless Classic)
        FullResults         // full results page (Daily achievement, Puzzle Show, Time Attack run-end)
    }

    /// <summary>The active mode family, decoupled from concrete mode types for testability.</summary>
    public enum ModeKind { Classic, TimeAttack, PuzzleShow }

    /// <summary>
    /// Pure post-win surface routing (Task 16A). No Unity/state dependencies so it can be
    /// unit-tested directly and is the single source of truth GameBootstrap.CheckGameOver calls.
    /// </summary>
    public static class PostWinRouter
    {
        public static PostWinSurface Decide(ModeKind kind, bool isDaily, bool puzzleComplete, bool timeUp)
        {
            switch (kind)
            {
                case ModeKind.TimeAttack:
                    // Run ends (full results) only when the clock hits 0; otherwise a solved
                    // ladder auto-advances to keep the run going.
                    if (timeUp) return PostWinSurface.FullResults;
                    return puzzleComplete ? PostWinSurface.AdvanceNextLadder : PostWinSurface.None;

                case ModeKind.PuzzleShow:
                    // Each solved puzzle is an achievement moment → full stat screen.
                    return puzzleComplete ? PostWinSurface.FullResults : PostWinSurface.None;

                default: // Classic
                    if (!puzzleComplete) return PostWinSurface.None;
                    // Daily is a one-a-day achievement (full results); endless Classic gets the
                    // compact panel so the player stays in the loop.
                    return isDaily ? PostWinSurface.FullResults : PostWinSurface.CompactWinPanel;
            }
        }
    }
}
