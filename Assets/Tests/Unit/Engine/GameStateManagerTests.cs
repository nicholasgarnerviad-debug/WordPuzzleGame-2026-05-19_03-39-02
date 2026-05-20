using NUnit.Framework;
using System.Collections.Generic;

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

// Mock implementations for testing
public class MockWordValidator : IWordValidator
{
    private bool isValid;
    private bool isNextStep;

    public void SetValidResult(bool valid, bool nextStep)
    {
        isValid = valid;
        isNextStep = nextStep;
    }

    public void Initialize(string startWord, string endWord, string[] currentWordChain) { }

    public ValidationResult ValidateWord(string word)
    {
        return new ValidationResult(isValid, "", isNextStep, true, -1, -1);
    }

    public bool IsValidNextWord(string word, string previousWord)
    {
        return isValid;
    }
}

public class MockDataManager : IDataManager
{
    public System.Threading.Tasks.Task SaveGameStateAsync(GameStateSnapshot snapshot)
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<GameStateSnapshot> LoadGameStateAsync()
        => System.Threading.Tasks.Task.FromResult(new GameStateSnapshot());

    public System.Threading.Tasks.Task UpdatePlayerProgressAsync(PlayerProgress progress)
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<PlayerProgress> GetPlayerProgressAsync()
        => System.Threading.Tasks.Task.FromResult(new PlayerProgress());

    public System.Threading.Tasks.Task<TierData> GetTierDataAsync(int tierId)
        => System.Threading.Tasks.Task.FromResult(new TierData());

    public System.Threading.Tasks.Task LoadAllTierDataAsync()
        => System.Threading.Tasks.Task.CompletedTask;
}
