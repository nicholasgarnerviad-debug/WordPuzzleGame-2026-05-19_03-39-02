namespace WordPuzzle.Puzzle
{
    [System.Serializable]
    public class TierData
    {
        public int tierId;
        public PuzzleDefinition[] puzzles;
        public bool isUnlocked;
        public long unlockedTimestamp;
    }
}
