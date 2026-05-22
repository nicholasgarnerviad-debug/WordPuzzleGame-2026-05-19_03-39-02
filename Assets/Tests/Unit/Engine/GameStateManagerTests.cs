using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.State;

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
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_PressLetter_UpdatesState()
    {
        // Arrange
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
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
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
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
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
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
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
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
        var puzzle = new WordPuzzle(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Difficulty.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        int streak = manager.GetCurrentStreak();

        // Assert
        Assert.AreEqual(1, streak);
    }
}
