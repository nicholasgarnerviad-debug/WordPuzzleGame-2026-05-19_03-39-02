namespace WordPuzzle.Puzzle
{
    public class TierData
    {
        private int _tierId;
        private PuzzleDefinition[] _puzzles;
        private bool _isUnlocked;
        private long _unlockedTimestamp;

        public int TierId
        {
            get => _tierId;
            set => _tierId = value;
        }

        public PuzzleDefinition[] Puzzles
        {
            get => _puzzles;
            set => _puzzles = value;
        }

        public bool IsUnlocked
        {
            get => _isUnlocked;
            set => _isUnlocked = value;
        }

        public long UnlockedTimestamp
        {
            get => _unlockedTimestamp;
            set => _unlockedTimestamp = value;
        }

        // Legacy field names for backward compatibility with object initializers
        public int tierId
        {
            get => TierId;
            set => TierId = value;
        }

        public PuzzleDefinition[] puzzles
        {
            get => Puzzles;
            set => Puzzles = value;
        }

        public bool isUnlocked
        {
            get => IsUnlocked;
            set => IsUnlocked = value;
        }

        public long unlockedTimestamp
        {
            get => UnlockedTimestamp;
            set => UnlockedTimestamp = value;
        }
    }
}
