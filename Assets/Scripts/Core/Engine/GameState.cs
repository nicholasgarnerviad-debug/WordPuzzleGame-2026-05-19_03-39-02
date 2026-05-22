using System;
using System.Collections.Generic;
using WordPuzzle.Puzzle;

namespace WordPuzzle.State
{
    /// <summary>
    /// Immutable game state snapshot. All state is read-only; new states are created
    /// via WithX() methods rather than mutation. Enables undo/redo, time travel, and
    /// functional state transitions.
    /// </summary>
    public sealed class GameState
{
    public readonly WordPuzzle puzzle;
    public readonly List<string> wordChain;
    public readonly int score;
    public readonly int wordsFound;
    public readonly float elapsedTime;

    public GameState(
        WordPuzzle puzzle,
        List<string> wordChain = null,
        int score = 0,
        int wordsFound = 0,
        float elapsedTime = 0f
    )
    {
        this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
        this.wordChain = wordChain ?? new List<string> { puzzle.startWord };
        this.score = score;
        this.wordsFound = wordsFound;
        this.elapsedTime = elapsedTime;
    }

    // Functional builders - return new GameState instead of mutating
    public GameState WithWordChain(List<string> newChain) =>
        new GameState(puzzle, newChain, score, wordsFound, elapsedTime);

    public GameState WithScore(int newScore) =>
        new GameState(puzzle, wordChain, newScore, wordsFound, elapsedTime);

    public GameState WithWordsFound(int newCount) =>
        new GameState(puzzle, wordChain, score, newCount, elapsedTime);

    public GameState WithElapsedTime(float newTime) =>
        new GameState(puzzle, wordChain, score, wordsFound, newTime);

    public bool IsPuzzleComplete => wordChain.Count > 0 && wordChain[wordChain.Count - 1] == puzzle.endWord;

    public override string ToString() =>
        $"GameState(puzzle={puzzle.puzzleId}, chain_length={wordChain.Count}, score={score}, elapsed={elapsedTime:F1}s)";
    }
}
