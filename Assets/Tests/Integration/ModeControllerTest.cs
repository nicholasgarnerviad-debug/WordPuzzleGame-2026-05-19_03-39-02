using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class ModeControllerTest
{
    private ModeController modeController;
    private MockGameModeContext mockContext;

    [SetUp]
    public void Setup()
    {
        var gameObject = new GameObject();
        modeController = gameObject.AddComponent<ModeController>();

        mockContext = new MockGameModeContext();

        // Initialize the controller with mock context
        modeController.Initialize(
            mockContext.dataManager,
            mockContext.economy,
            mockContext.puzzleGenerator,
            mockContext.stateManager,
            mockContext.wordValidator
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (modeController != null && modeController.gameObject != null)
        {
            UnityEngine.Object.Destroy(modeController.gameObject);
        }
    }

    [Test]
    public void SwitchMode_InitializesNewMode()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();

        // Act
        modeController.SwitchMode(ModeType.Classic);

        // Assert
        Assert.AreEqual(ModeType.Classic, modeController.GetCurrentMode());
    }

    [Test]
    public void SwitchMode_FromClassicToPuzzleShow_SwitchesSuccessfully()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        modeController.SwitchMode(ModeType.Classic);

        // Act
        modeController.SwitchMode(ModeType.PuzzleShow);

        // Assert
        Assert.AreEqual(ModeType.PuzzleShow, modeController.GetCurrentMode());
    }

    [Test]
    public void SwitchMode_TracksLastModeBeforeSwitching()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        modeController.SwitchMode(ModeType.Classic);

        // Act
        modeController.SwitchMode(ModeType.TimeAttack);

        // Assert
        Assert.AreEqual(ModeType.TimeAttack, modeController.LastMode);
    }
}

// Mock implementation for ModeControllerTest
public class MockGameModeContext : GameModeContext
{
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
    private int totalScore = 0;

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

    public char[] GetAvailableLetters()
    {
        var usedLetters = new System.Collections.Generic.HashSet<char>();
        foreach (var word in currentState.wordChain)
        {
            foreach (var c in word)
            {
                usedLetters.Add(c);
            }
        }
        var available = new System.Collections.Generic.List<char>();
        for (char c = 'a'; c <= 'z'; c++)
        {
            if (!usedLetters.Contains(c))
            {
                available.Add(c);
            }
        }
        return available.ToArray();
    }

    public int GetCurrentScore()
    {
        return totalScore;
    }

    public bool IsValidWord(string word)
    {
        return !string.IsNullOrEmpty(word);
    }

    public int SubmitWord(string word)
    {
        if (!IsValidWord(word))
            return 0;
        word = word.ToLower();
        int points = word.Length;
        totalScore += points;
        currentState.score = totalScore;
        return points;
    }

    public int GetCurrentStreak()
    {
        return currentState.currentStreak;
    }

    public int GetWordsRemaining()
    {
        return currentState.wordsRemaining;
    }

    public void SetWordsRemaining(int count)
    {
        currentState.wordsRemaining = count;
    }

    public float GetTimeRemaining()
    {
        return currentState.timeRemaining;
    }

    public void SetTimeRemaining(float time)
    {
        currentState.timeRemaining = time;
    }

    public string GetBestWord()
    {
        return "cat";
    }

    public int GetLongestStreak()
    {
        return 0;
    }

    public ResultsScreen.GameStats GetFinalStats()
    {
        return new ResultsScreen.GameStats
        {
            finalScore = totalScore,
            gameDuration = 0f,
            wordsFound = 1,
            validAttempts = 1,
            totalAttempts = 1,
            bestWord = "cat",
            currentStreak = 0,
            longestStreak = 0
        };
    }

    public void ResetTracking()
    {
        totalScore = 0;
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
