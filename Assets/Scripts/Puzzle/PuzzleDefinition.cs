namespace WordPuzzle.Puzzle
{
    public class PuzzleDefinition
    {
        private int _puzzleId;
        private string _startWord;
        private string _endWord;
        private int _optimalSteps;
        private string[] _solution;
        private int _seedValue;

        public int PuzzleId
        {
            get => _puzzleId;
            set => _puzzleId = value;
        }

        public string StartWord
        {
            get => _startWord;
            set => _startWord = value;
        }

        public string EndWord
        {
            get => _endWord;
            set => _endWord = value;
        }

        public int OptimalSteps
        {
            get => _optimalSteps;
            set => _optimalSteps = value;
        }

        public string[] Solution
        {
            get => _solution;
            set => _solution = value;
        }

        public int SeedValue
        {
            get => _seedValue;
            set => _seedValue = value;
        }

        // Legacy field names for backward compatibility with object initializers
        public int puzzleId
        {
            get => PuzzleId;
            set => PuzzleId = value;
        }

        public string startWord
        {
            get => StartWord;
            set => StartWord = value;
        }

        public string endWord
        {
            get => EndWord;
            set => EndWord = value;
        }

        public int optimalSteps
        {
            get => OptimalSteps;
            set => OptimalSteps = value;
        }

        public string[] solution
        {
            get => Solution;
            set => Solution = value;
        }

        public int seedValue
        {
            get => SeedValue;
            set => SeedValue = value;
        }
    }
}
