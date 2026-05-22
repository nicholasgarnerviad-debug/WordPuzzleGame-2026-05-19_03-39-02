using WordPuzzle.State;
using WordPuzzle.Puzzle;
using WordPuzzleModel = WordPuzzle.Puzzle.WordPuzzle;

namespace WordPuzzle.Modes
{
    public enum ModeType
    {
        Classic,
        PuzzleShow,
        TimeAttack
    }

    public interface IGameMode
    {
        void Initialize(GameStateManager stateManager);
        void StartGame(WordPuzzleModel puzzle);
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
