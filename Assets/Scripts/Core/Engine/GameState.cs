using System;
using System.Collections.Generic;
using WordPuzzleModel = WordPuzzle.Puzzle.WordPuzzle;

namespace WordPuzzle.State
{
    /// <summary>
    /// Immutable game state snapshot. All state is read-only; new states are created
    /// via WithX() methods rather than mutation. Enables undo/redo, time travel, and
    /// functional state transitions.
    /// </summary>
    public sealed class GameState
    {
        public readonly WordPuzzleModel puzzle;
        public readonly List<string> wordChain;
        public readonly int score;
        public readonly int wordsFound;
        public readonly float elapsedTime;
        public readonly string currentInput;
        public readonly int hintsRemaining;
        public readonly int revealsRemaining;
        // DEPRECATED — replaced by hintLetterIndex + revealedNextWord.
        public readonly HashSet<int> revealedLetterIndices;

        // Phase 2 power-up surface: position in the next solution word that differs from
        // the last chain word (-1 when no hint is active), and the full text of the next
        // solution word once the player spends a reveal (empty when not revealed).
        public readonly int hintLetterIndex;
        public readonly string revealedNextWord;

        public GameState(
            WordPuzzleModel puzzle,
            List<string> wordChain = null,
            int score = 0,
            int wordsFound = 0,
            float elapsedTime = 0f,
            string currentInput = "",
            int hintsRemaining = 0,
            int revealsRemaining = 0,
            HashSet<int> revealedLetterIndices = null,
            int hintLetterIndex = -1,
            string revealedNextWord = ""
        )
        {
            this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            this.wordChain = wordChain ?? new List<string> { puzzle.startWord };
            this.score = score;
            this.wordsFound = wordsFound;
            this.elapsedTime = elapsedTime;
            this.currentInput = currentInput ?? "";
            this.hintsRemaining = hintsRemaining;
            this.revealsRemaining = revealsRemaining;
            this.revealedLetterIndices = revealedLetterIndices ?? new HashSet<int>();
            this.hintLetterIndex = hintLetterIndex;
            this.revealedNextWord = revealedNextWord ?? "";
        }

        public GameState WithScore(int newScore) =>
            new GameState(puzzle, wordChain, newScore, wordsFound, elapsedTime, currentInput, hintsRemaining, revealsRemaining, revealedLetterIndices, hintLetterIndex, revealedNextWord);

        public GameState WithWordsFound(int newCount) =>
            new GameState(puzzle, wordChain, score, newCount, elapsedTime, currentInput, hintsRemaining, revealsRemaining, revealedLetterIndices, hintLetterIndex, revealedNextWord);

        public bool IsPuzzleComplete => wordChain.Count > 0 && wordChain[wordChain.Count - 1] == puzzle.endWord;

        public override string ToString() =>
            $"GameState(puzzle={puzzle.puzzleId}, chain_length={wordChain.Count}, score={score}, elapsed={elapsedTime:F1}s)";
    }
}
