using WordPuzzle.State;

namespace WordPuzzle.Modes
{
    public interface IGameMode
    {
        void Initialize(GameStateManager stateManager);
        void StartGame(WordPuzzle puzzle);
        void HandleWordSubmission(string word);
        void Tick(float deltaTime);
        GameModeStats GetStats();
        void Reset();
    }

    public struct GameModeStats
    {
        public string modeName;
        public int wordsFound;
        public float totalTime;
        public int score;
        public float accuracy;
    }
}
