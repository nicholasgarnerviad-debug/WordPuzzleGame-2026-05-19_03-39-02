using WordPuzzle.State;
using WordPuzzle.Puzzle;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Classic puzzle word-chain game. Spec §2:
    ///  - Puzzle is a random 3-7 letter ladder generated at run start by
    ///    PuzzleGenerator.GenerateRandomPuzzleOfLength (driven by GameBootstrap).
    ///  - NO timer, NO life/failure cap. The player keeps trying (the validator
    ///    rejects bad words but only fires OnWordSubmissionResult — see §1).
    ///  - IsGameOver fires only on puzzle completion. CheckGameOver auto-advances
    ///    to the next puzzle (handled by GameBootstrap §2).
    /// </summary>
    public class ClassicMode : IGameMode
    {
        private GameStateManager stateManager;
        private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;

        public void Initialize(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new System.ArgumentNullException(nameof(stateManager));
        }

        public void StartGame(WordPuzzle.Puzzle.WordPuzzle puzzle)
        {
            if (stateManager == null)
                throw new System.InvalidOperationException("Must call Initialize first");

            currentPuzzle = puzzle ?? throw new System.ArgumentNullException(nameof(puzzle));
            stateManager.StartNewPuzzle(puzzle);
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null) return;
            stateManager.SubmitWord(word);
        }

        // Spec §2 — no timer logic; just track elapsed time for stats display.
        public void Tick(float deltaTime)
        {
            stateManager?.IncrementElapsedTime(deltaTime);
        }

        public GameModeStats GetStats()
        {
            var state = stateManager?.GetCurrentState();
            if (state == null) return default;

            return new GameModeStats
            {
                modeName = "Classic",
                wordsFound = state.wordsFound,
                totalTime = state.elapsedTime,
                score = state.score,
                // Accuracy is no longer derived from a failure counter (lives sentinel),
                // so just report 100% when the player completed the puzzle.
                accuracy = state.IsPuzzleComplete ? 100f : 0f
            };
        }

        public void Reset()
        {
            stateManager = null;
            currentPuzzle = null;
        }

        public bool IsGameOver()
        {
            if (stateManager == null || currentPuzzle == null) return false;
            return stateManager.GetCurrentState().IsPuzzleComplete;
        }
    }
}
