using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.State;
using WordPuzzle.Puzzle;

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
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);

        // Act
        manager.StartNewPuzzle(puzzle);
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(1, state.wordChain.Length);
        Assert.AreEqual("cat", state.wordChain[0]);
        Assert.AreEqual(3, state.lives);
        Assert.IsFalse(state.isWon);
    }

    [Test]
    public void Dispatch_PressLetter_AddsToInput()
    {
        // Arrange
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
        manager.StartNewPuzzle(puzzle);

        // Act
        manager.Dispatch(new PressLetterAction('b'));
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual("b", state.currentInput);
    }

    [Test]
    public void Dispatch_DeleteLetter_RemovesFromInput()
    {
        // Arrange
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.Dispatch(new PressLetterAction('b'));
        manager.Dispatch(new PressLetterAction('a'));

        // Act
        manager.Dispatch(new DeleteLetterAction());
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual("b", state.currentInput);
    }

    [Test]
    public void Dispatch_SubmitValidWord_AddsToChain()
    {
        // Arrange
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(2, state.wordChain.Length);
        Assert.AreEqual("bat", state.wordChain[1]);
    }

    [Test]
    public void Dispatch_SubmitInvalidWord_ReducesLives()
    {
        // Arrange
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(false, false);

        // Act
        manager.Dispatch(new SubmitWordAction("xyz"));
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(2, state.lives);
        Assert.AreEqual(1, state.wordChain.Length);
    }
}
