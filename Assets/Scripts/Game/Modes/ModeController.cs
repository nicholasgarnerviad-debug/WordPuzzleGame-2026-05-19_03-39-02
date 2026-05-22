using System;
using WordPuzzle.State;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Central controller for switching between game modes. Manages mode lifecycle
    /// and delegates state to the active mode.
    /// </summary>
    public class ModeController
    {
        private IGameMode activeMode;
        private GameStateManager stateManager;

        public ModeController(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public void SetMode(IGameMode mode)
        {
            if (mode == null)
                throw new ArgumentNullException(nameof(mode));

            // Cleanup old mode
            activeMode?.Reset();

            // Setup new mode
            activeMode = mode;
            activeMode.Initialize(stateManager);
        }

        public void StartGame(WordPuzzle puzzle)
        {
            if (activeMode == null)
                throw new InvalidOperationException("No mode set. Call SetMode first.");

            activeMode.StartGame(puzzle);
        }

        public void HandleWordSubmission(string word)
        {
            activeMode?.HandleWordSubmission(word);
        }

        public void Tick(float deltaTime)
        {
            activeMode?.Tick(deltaTime);
        }

        public GameModeStats GetCurrentStats()
        {
            return activeMode?.GetStats() ?? default;
        }

        public IGameMode GetActiveMode() => activeMode;
    }
}
