using WordPuzzle.State;
using WordPuzzle.Puzzle;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Classic puzzle word chain game. Complete the word chain by finding
    /// valid one-letter transitions from start to end word.
    /// </summary>
    public class ClassicMode : IGameMode
    {
        private GameStateManager stateManager;
        private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;
        private const int MAX_FAILURES = 5;
        private int failureCount = 0;

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
            failureCount = 0;
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null) return;

            var stateBefore = stateManager.GetCurrentState();
            stateManager.SubmitWord(word);
            var stateAfter = stateManager.GetCurrentState();

            // If word wasn't added, count as failure
            if (stateAfter.wordChain.Count == stateBefore.wordChain.Count)
            {
                failureCount++;
            }
        }

        public void Tick(float deltaTime)
        {
            // Classic mode doesn't have a timer, but we still track elapsed time
            // Time tracking is handled internally by the mode
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
                accuracy = state.wordsFound > 0 ?
                    (1f - (failureCount / (float)(state.wordsFound + failureCount))) : 0f
            };
        }

        public void Reset()
        {
            stateManager = null;
            currentPuzzle = null;
            failureCount = 0;
        }
    }
}

