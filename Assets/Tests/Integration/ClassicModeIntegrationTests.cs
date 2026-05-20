using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class ClassicModeIntegrationTests
{
    private ClassicMode classicMode;
    private MockGameModeContext mockContext;

    [SetUp]
    public void Setup()
    {
        mockContext = new MockGameModeContext();
        classicMode = new GameObject().AddComponent<ClassicMode>();
        classicMode.Initialize(mockContext);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(classicMode.gameObject);
    }

    [Test]
    public void StartGame_LoadsFirstPuzzle()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();

        // Act
        classicMode.StartGame();

        // Assert
        Assert.IsNotNull(mockContext.lastLoadedPuzzle);
        Assert.AreEqual("cat", mockContext.lastLoadedPuzzle.startWord);
    }

    [UnityTest]
    public IEnumerator HandleInput_WinPuzzle_LoadsNextPuzzle()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();
        classicMode.StartGame();

        // Act
        // Simulate winning the puzzle
        mockContext.stateManager.SetWonState(true);
        classicMode.HandleInput(new SubmitWordAction("dog"));
        yield return null;

        // Assert - verify coins were earned
        Assert.Greater(mockContext.coinsAdded, 0);
    }
}

// Mock implementations for integration testing
public class MockGameModeContext : GameModeContext
{
    public WordPuzzle lastLoadedPuzzle;
    public int coinsAdded = 0;

    public MockGameModeContext()
    {
        puzzleGenerator = new MockPuzzleGenerator();
        wordValidator = new MockWordValidator();
        stateManager = new MockGameStateManager();
        economy = new MockEconomyManager();
        dataManager = new MockDataManager();
    }

    public void SetupPuzzleGenerator()
    {
        puzzleGenerator = new MockPuzzleGenerator();
    }

    public void SetupStateManager()
    {
        stateManager = new MockGameStateManager();
    }
}

public class MockPuzzleGenerator : IPuzzleGenerator
{
    public PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
    {
        return CreateDefaultPuzzle();
    }

    public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        return CreateDefaultPuzzle();
    }

    private PuzzleDefinition CreateDefaultPuzzle()
    {
        return new PuzzleDefinition
        {
            puzzleId = 1,
            startWord = "cat",
            endWord = "dog",
            optimalSteps = 3,
            solution = new[] { "cat", "bat", "bag", "dog" },
            seedValue = 0
        };
    }
}

public class MockGameStateManager : IGameStateManager
{
    private GameState currentState;
    private bool wonState = false;

    public MockGameStateManager()
    {
        currentState = new GameState
        {
            wordChain = new[] { "cat" },
            currentInput = "",
            lives = 3,
            isWon = false,
            isLost = false
        };
    }

    public GameState GetCurrentState()
    {
        return currentState.Clone();
    }

    public void StartNewPuzzle(WordPuzzle puzzle)
    {
        currentState = new GameState
        {
            wordChain = new[] { puzzle.startWord },
            currentInput = "",
            lives = 3,
            isWon = false,
            isLost = false
        };
    }

    public void Dispatch(GameAction action)
    {
        // Mock dispatch - set won state for testing
        if (wonState)
        {
            currentState.isWon = true;
        }
    }

    public IDisposable Subscribe(System.Action<GameState> observer)
    {
        return null;
    }

    public void SetWonState(bool won)
    {
        wonState = won;
    }
}

public class MockEconomyManager : IEconomyManager
{
    public int coinsAdded = 0;

    public System.Threading.Tasks.Task InitializeAsync()
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<int> GetCoinsAsync()
        => System.Threading.Tasks.Task.FromResult(0);

    public System.Threading.Tasks.Task AddCoinsAsync(int amount, string source)
    {
        coinsAdded += amount;
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task<int> GetHintsAsync()
        => System.Threading.Tasks.Task.FromResult(0);

    public System.Threading.Tasks.Task UseHintAsync()
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task AddHintsAsync(int amount, string source)
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<int> GetRevealsAsync()
        => System.Threading.Tasks.Task.FromResult(0);

    public System.Threading.Tasks.Task UseRevealAsync()
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task AddRevealsAsync(int amount, string source)
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task<int> GetUndosAsync()
        => System.Threading.Tasks.Task.FromResult(0);

    public System.Threading.Tasks.Task UseUndoAsync()
        => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task AddUndosAsync(int amount, string source)
        => System.Threading.Tasks.Task.CompletedTask;

    public PlayerProgress GetCurrentProgress()
        => new PlayerProgress();

    public void LogEconomyEvent(string eventName, string data) { }
}

public class MockWordValidator : IWordValidator
{
    public void Initialize(string startWord, string endWord, string[] currentWordChain) { }

    public ValidationResult ValidateWord(string word)
    {
        return new ValidationResult(true, "", true, true, -1, -1);
    }

    public bool IsValidNextWord(string word, string previousWord)
    {
        return true;
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
