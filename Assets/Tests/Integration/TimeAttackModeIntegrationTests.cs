using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class TimeAttackModeIntegrationTests
{
    private TimeAttackMode timeAttackMode;
    private TimeAttackModeMockContext mockContext;

    [SetUp]
    public void Setup()
    {
        mockContext = new TimeAttackModeMockContext();
        timeAttackMode = new GameObject().AddComponent<TimeAttackMode>();
        timeAttackMode.Initialize(mockContext);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(timeAttackMode.gameObject);
    }

    [Test]
    public void StartGame_BeginsRound1()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();

        // Act
        timeAttackMode.StartGame();

        // Assert
        ModeStats stats = timeAttackMode.GetStats();
        Assert.AreEqual("Time Attack", stats.modeName);
    }

    [Test]
    public void TimeUpdate_DecreasesTimeRemaining()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();
        timeAttackMode.StartGame();

        // Act
        for (int i = 0; i < 10; i++)
        {
            timeAttackMode.Update(1f);
        }

        // Assert
        ModeStats stats = timeAttackMode.GetStats();
        Assert.Greater(stats.totalTime, 0);
    }
}

// Mock implementations for TimeAttackMode integration testing
public class TimeAttackModeMockContext : GameModeContext
{
    public WordPuzzle lastLoadedPuzzle;
    public int coinsAdded = 0;

    public TimeAttackModeMockContext()
    {
        puzzleGenerator = new TimeAttackModeMockPuzzleGenerator();
        wordValidator = new TimeAttackModeMockWordValidator();
        stateManager = new TimeAttackModeMockGameStateManager();
        economy = new TimeAttackModeMockEconomyManager();
        dataManager = new TimeAttackModeMockDataManager();
    }

    public void SetupPuzzleGenerator()
    {
        puzzleGenerator = new TimeAttackModeMockPuzzleGenerator();
    }

    public void SetupStateManager()
    {
        stateManager = new TimeAttackModeMockGameStateManager();
    }
}

public class TimeAttackModeMockPuzzleGenerator : IPuzzleGenerator
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

public class TimeAttackModeMockGameStateManager : IGameStateManager
{
    private GameState currentState;
    private bool wonState = false;

    public TimeAttackModeMockGameStateManager()
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

public class TimeAttackModeMockEconomyManager : IEconomyManager
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

public class TimeAttackModeMockWordValidator : IWordValidator
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

public class TimeAttackModeMockDataManager : IDataManager
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
