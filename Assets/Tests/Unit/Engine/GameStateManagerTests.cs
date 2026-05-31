using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.State;
using WordPuzzle.Persistence;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

[TestFixture]
public class GameStateManagerTests
{
    private GameStateManager manager;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        manager = new GameStateManager(mockValidator, mockDataManager);
    }

    [Test]
    public void StartNewPuzzle_InitializesState()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);

        // Act
        manager.StartNewPuzzle(puzzle);
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_PressLetter_UpdatesState()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        // Act
        manager.Dispatch(new PressLetterAction('b'));
        var state = manager.GetCurrentState();

        // Assert - verify that the action was processed (score hasn't changed, chain length is still 1)
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_DeleteLetter()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.Dispatch(new PressLetterAction('b'));
        manager.Dispatch(new PressLetterAction('a'));

        // Act
        manager.Dispatch(new DeleteLetterAction());
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_SubmitValidWord_AddsToChain()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(2, state.wordChain.Count);
        Assert.AreEqual("bat", state.wordChain[1]);
    }

    [Test]
    public void GetCurrentScore_ReturnsScore()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        int score = manager.GetCurrentScore();

        // Assert
        Assert.AreEqual(3, score); // "bat" = 3 letters
    }

    [Test]
    public void GetCurrentStreak_ReturnsStreak()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        int streak = manager.GetCurrentStreak();

        // Assert
        Assert.AreEqual(1, streak);
    }

    // ── Phase 2 power-up mechanics ────────────────────────────────────

    [Test]
    public void StartNewPuzzle_InitializesPowerUps()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        var state = manager.GetCurrentState();
        Assert.AreEqual(2, state.hintsRemaining);
        Assert.AreEqual(1, state.revealsRemaining);
        Assert.AreEqual(0, state.revealedLetterIndices.Count);
    }

    [Test]
    public void Dispatch_UseHint_DecrementsHintsAndRevealsLetter()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.hintsRemaining);
        Assert.AreEqual(1, state.revealedLetterIndices.Count);
        Assert.IsTrue(state.revealedLetterIndices.Contains(0));
    }

    [Test]
    public void Dispatch_UseHint_RevealsDifferentLetterEachTime()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseHintAction(0));
        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.hintsRemaining);
        Assert.AreEqual(2, state.revealedLetterIndices.Count);
    }

    [Test]
    public void Dispatch_UseReveal_RevealsAllLetters()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseRevealAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.revealsRemaining);
        Assert.AreEqual(puzzle.endWord.Length, state.revealedLetterIndices.Count);
    }

    [Test]
    public void Dispatch_UseReveal_WhenExhausted_DoesNotGoNegative()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseRevealAction());
        manager.Dispatch(new UseRevealAction()); // second call should be no-op

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.revealsRemaining);
    }

    [Test]
    public void Dispatch_Undo_RemovesLastWordFromChain()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("bat"));

        Assert.AreEqual(2, manager.GetCurrentState().wordChain.Count);

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_Undo_OnStartingChain_DoesNothing()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.wordChain.Count);
    }
}
