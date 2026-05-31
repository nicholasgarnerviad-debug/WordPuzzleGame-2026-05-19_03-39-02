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
        public readonly HashSet<int> revealedLetterIndices;

        public GameState(
            WordPuzzleModel puzzle,
            List<string> wordChain = null,
            int score = 0,
            int wordsFound = 0,
            float elapsedTime = 0f,
            string currentInput = "",
            int hintsRemaining = 0,
            int revealsRemaining = 0,
            HashSet<int> revealedLetterIndices = null
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
        }

        public GameState WithScore(int newScore) =>
            new GameState(puzzle, wordChain, newScore, wordsFound, elapsedTime, currentInput, hintsRemaining, revealsRemaining, revealedLetterIndices);

        public GameState WithWordsFound(int newCount) =>
            new GameState(puzzle, wordChain, score, newCount, elapsedTime, currentInput, hintsRemaining, revealsRemaining, revealedLetterIndices);

        public bool IsPuzzleComplete => wordChain.Count > 0 && wordChain[wordChain.Count - 1] == puzzle.endWord;

        public override string ToString() =>
            $"GameState(puzzle={puzzle.puzzleId}, chain_length={wordChain.Count}, score={score}, elapsed={elapsedTime:F1}s)";
    }
}
