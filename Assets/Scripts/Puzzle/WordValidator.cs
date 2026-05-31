using System.Collections.Generic;

namespace WordPuzzle.Puzzle
{
    public class WordValidator : IWordValidator
    {
        private WordGraph wordGraph;
        private string previousWord;
        private string targetWord;
        private List<string> currentChain;

        // Cached distance map from every reachable word to the puzzle target.
        // Recomputed only when the target changes (puzzle-stable).
        private Dictionary<string, int> distanceToTarget;
        private string cachedTargetWord;
        // Distance from previousWord to target (looked up from the cached map).
        // int.MaxValue means previousWord is unreachable from target.
        private int prevDistToTarget;

        public WordValidator(WordGraph wordGraph)
        {
            this.wordGraph = wordGraph;
        }

        public void Initialize(string startWord, string endWord, string[] currentWordChain)
        {
            previousWord = currentWordChain.Length > 0
                ? currentWordChain[currentWordChain.Length - 1]
                : startWord;
            targetWord = endWord;
            currentChain = new List<string>(currentWordChain);

            // Distance map is keyed by target word; reuse across submissions in the
            // same puzzle. A BFS only runs on the first call or when the target changes.
            if (distanceToTarget == null || cachedTargetWord != targetWord)
            {
                distanceToTarget = wordGraph.ComputeDistancesFrom(targetWord);
                cachedTargetWord = targetWord;
            }
            prevDistToTarget = distanceToTarget != null
                && distanceToTarget.TryGetValue(previousWord, out var pd)
                ? pd
                : int.MaxValue;
        }

        public ValidationResult ValidateWord(string word)
        {
            word = word.ToLower();

            // Check if word exists
            if (!wordGraph.IsValidWord(word))
            {
                return new ValidationResult(
                    valid: false,
                    msg: "Word not in dictionary",
                    nextStep: false,
                    progress: false,
                    distStart: -1,
                    distEnd: -1
                );
            }

            // Check if already in chain
            if (currentChain.Contains(word))
            {
                return new ValidationResult(
                    valid: false,
                    msg: "Word already used",
                    nextStep: false,
                    progress: false,
                    distStart: -1,
                    distEnd: -1
                );
            }

            // Check one letter difference from previous word
            if (!WordOps.HaveOneLetterDifference(previousWord, word))
            {
                return new ValidationResult(
                    valid: false,
                    msg: "Must change exactly one letter",
                    nextStep: false,
                    progress: false,
                    distStart: -1,
                    distEnd: -1
                );
            }

            // We just established Hamming-1 + dictionary membership, so the graph
            // distance from `word` to `previousWord` is exactly 1.
            int distStart = 1;
            int distEnd = distanceToTarget != null
                && distanceToTarget.TryGetValue(word, out var d)
                ? d
                : -1;
            // Progress means: this word is strictly closer to the target than the
            // previous word was. Unreachable end (-1) is never "closer".
            bool isProgress = distEnd >= 0 && distEnd < prevDistToTarget;

            return new ValidationResult(
                valid: true,
                msg: "Valid word",
                nextStep: true,
                progress: isProgress,
                distStart: distStart,
                distEnd: distEnd
            );
        }

        public bool IsValidNextWord(string word, string previousWord)
        {
            return WordOps.HaveOneLetterDifference(previousWord, word.ToLower());
        }
    }

    public interface IWordValidator
    {
        void Initialize(string startWord, string endWord, string[] currentWordChain);
        ValidationResult ValidateWord(string word);
        bool IsValidNextWord(string word, string previousWord);
    }
}
