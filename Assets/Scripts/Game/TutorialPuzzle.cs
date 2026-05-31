using WordPuzzle.Puzzle;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Fixed tutorial puzzle: cat -> bat -> bag (2 steps, all words verified in word_library.json).
    /// puzzleId = -1001 is a reserved sentinel that skips the tier/library system.
    /// </summary>
    public static class TutorialPuzzle
    {
        public static PuzzleDefinition Create() => new PuzzleDefinition
        {
            puzzleId     = -1001,
            startWord    = "cat",
            endWord      = "bag",
            optimalSteps = 2,
            solution     = new[] { "cat", "bat", "bag" },
            seedValue    = 0
        };
    }
}
