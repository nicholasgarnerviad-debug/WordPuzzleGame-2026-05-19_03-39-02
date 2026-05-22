using System.Collections.Generic;
using WordPuzzle.State;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Puzzle Show mode: The solution path is fully revealed. Player must follow
    /// the exact solution path shown to complete the puzzle.
    /// </summary>
    public class PuzzleShowMode : IGameMode
    {
        private GameStateManager stateManager;
        private WordPuzzle currentPuzzle;
        private int solutionIndex = 0;

        public void Initialize(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new System.ArgumentNullException(nameof(stateManager));
        }

        public void StartGame(WordPuzzle puzzle)
        {
            if (stateManager == null)
                throw new System.InvalidOperationException("Must call Initialize first");

            currentPuzzle = puzzle ?? throw new System.ArgumentNullException(nameof(puzzle));
            stateManager.StartNewPuzzle(puzzle);
            solutionIndex = 0;
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null) return;

            // In show mode, we accept the solution words in order
            if (solutionIndex < currentPuzzle.solution.Length)
            {
                string expectedWord = currentPuzzle.solution[solutionIndex];
                if (word.ToLower() == expectedWord.ToLower())
                {
                    stateManager.SubmitWord(word.ToLower());
                    solutionIndex++;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (stateManager != null)
                stateManager.UpdateElapsedTime(deltaTime);
        }

        public GameModeStats GetStats()
        {
            var state = stateManager?.GetCurrentState();
            return new GameModeStats
            {
                modeName = "Puzzle Show",
                wordsFound = state?.wordsFound ?? 0,
                totalTime = state?.elapsedTime ?? 0f,
                score = state?.score ?? 0,
                accuracy = 100f // Always perfect in show mode
            };
        }

        public void Reset()
        {
            solutionIndex = 0;
            currentPuzzle = null;
        }

        public bool IsGameOver()
        {
            if (currentPuzzle == null) return false;
            return solutionIndex >= currentPuzzle.solution.Length;
        }

        public string[] GetFullSolution()
        {
            return currentPuzzle?.solution ?? new string[0];
        }
    }
}
