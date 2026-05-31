namespace WordPuzzle.Puzzle
{
    /// <summary>
    /// Shared word-comparison helpers. Single source of truth for Hamming-1 check
    /// used by WordGraph (adjacency) and WordValidator (submission check).
    /// </summary>
    public static class WordOps
    {
        /// <summary>
        /// True iff word1 and word2 are the same length and differ at exactly one position.
        /// </summary>
        public static bool HaveOneLetterDifference(string word1, string word2)
        {
            if (word1 == null || word2 == null) return false;
            if (word1.Length != word2.Length) return false;

            int differences = 0;
            for (int i = 0; i < word1.Length; i++)
            {
                if (word1[i] != word2[i])
                {
                    differences++;
                    if (differences > 1) return false;
                }
            }
            return differences == 1;
        }
    }
}
