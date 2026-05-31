using System;
using WordPuzzle.State;
using WordPuzzle.Puzzle;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Time Attack mode: Complete as many words as possible in 60 seconds.
    /// Any valid one-letter transition counts (not limited to solution path).
    /// </summary>
    public class TimeAttackMode : IGameMode
    {
        private GameStateManager stateManager;
        private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;
        private float timeRemaining;
        private const float TOTAL_TIME = 60f;

        /// <summary>
        /// Event fired when time remaining changes.
        /// Subscribers receive the updated time remaining in seconds.
        /// </summary>
        public event Action<float> TimeChanged;

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
            timeRemaining = TOTAL_TIME;
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null || timeRemaining <= 0) return;
            stateManager.SubmitWord(word);
        }

        public void Tick(float deltaTime)
        {
            if (stateManager == null) return;

            timeRemaining -= deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;

            stateManager.IncrementElapsedTime(deltaTime);

            // Notify subscribers of time change
            TimeChanged?.Invoke(timeRemaining);
        }

        public GameModeStats GetStats()
        {
            var state = stateManager?.GetCurrentState();
            var timeUsed = TOTAL_TIME - timeRemaining;

            return new GameModeStats
            {
                modeName = "Time Attack",
                wordsFound = state?.wordsFound ?? 0,
                totalTime = timeUsed,
                score = state?.score ?? 0,
                accuracy = 100f // All submissions must be valid
            };
        }

        public void Reset()
        {
            timeRemaining = TOTAL_TIME;
            currentPuzzle = null;
        }

        public bool IsGameOver()
        {
            return timeRemaining <= 0;
        }

        public bool IsTimeUp()
        {
            return timeRemaining <= 0;
        }

        public float GetTimeRemaining()
        {
            return timeRemaining;
        }
    }
}
