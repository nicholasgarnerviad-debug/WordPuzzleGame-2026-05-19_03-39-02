using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Shared mock implementations for testing
public class MockWordValidator : IWordValidator
{
    private bool isValid = true;
    private bool isNextStep = true;

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


public class MockGameStateManager : IGameStateManager
{
    private GameState currentState = new GameState();
    private bool wonState = false;
    private List<string> foundWords = new List<string>();
    private int longestStreak = 0;
    private int totalScore = 0;

    public void SetupPuzzle(WordPuzzle puzzle)
    {
        currentState.wordChain = new[] { puzzle.startWord };
        currentState.lives = 3;
    }

    public void Dispatch(GameAction action) { }

    public GameState GetCurrentState()
    {
        currentState.isWon = wonState;
        return currentState;
    }

    public void StartNewPuzzle(WordPuzzle puzzle)
    {
        SetupPuzzle(puzzle);
    }

    public IDisposable Subscribe(Action<GameState> observer)
    {
        return null;
    }

    public void SetWonState(bool won)
    {
        wonState = won;
    }

    public char[] GetAvailableLetters()
    {
        var usedLetters = new HashSet<char>();
        foreach (var word in currentState.wordChain)
        {
            foreach (var c in word)
            {
                usedLetters.Add(c);
            }
        }
        var available = new List<char>();
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
        if (foundWords.Contains(word))
            return 0;
        foundWords.Add(word);
        int points = word.Length;
        totalScore += points;
        currentState.score = totalScore;
        currentState.currentStreak++;
        longestStreak = System.Math.Max(longestStreak, currentState.currentStreak);
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
        if (foundWords.Count == 0)
            return "--";
        return foundWords.OrderByDescending(w => w.Length).FirstOrDefault() ?? "--";
    }

    public int GetLongestStreak()
    {
        return longestStreak;
    }

    public ResultsScreen.GameStats GetFinalStats()
    {
        return new ResultsScreen.GameStats
        {
            finalScore = totalScore,
            gameDuration = 0f,
            wordsFound = foundWords.Count,
            validAttempts = foundWords.Count,
            totalAttempts = foundWords.Count,
            bestWord = GetBestWord(),
            currentStreak = currentState.currentStreak,
            longestStreak = longestStreak
        };
    }

    public void ResetTracking()
    {
        foundWords.Clear();
        longestStreak = 0;
        totalScore = 0;
    }
}

public class MockPuzzleGenerator : IPuzzleGenerator
{
    public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        return CreateDefaultPuzzle();
    }

    public PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
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

public class MockDataManager : IDataManager
{
    public Task SaveGameStateAsync(GameStateSnapshot snapshot)
        => Task.CompletedTask;

    public Task<GameStateSnapshot> LoadGameStateAsync()
        => Task.FromResult(new GameStateSnapshot());

    public Task UpdatePlayerProgressAsync(PlayerProgress progress)
        => Task.CompletedTask;

    public Task<PlayerProgress> GetPlayerProgressAsync()
        => Task.FromResult(new PlayerProgress());

    public Task<TierData> GetTierDataAsync(int tierId)
        => Task.FromResult(new TierData { tierId = tierId, isUnlocked = true });

    public Task LoadAllTierDataAsync()
        => Task.CompletedTask;
}

public class MockEconomyManager : IEconomyManager
{
    public int coinsAdded = 0;

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task<int> GetCoinsAsync()
        => Task.FromResult(0);

    public Task AddCoinsAsync(int amount, string source)
    {
        coinsAdded += amount;
        return Task.CompletedTask;
    }

    public Task<int> GetHintsAsync()
        => Task.FromResult(0);

    public Task UseHintAsync()
        => Task.CompletedTask;

    public Task AddHintsAsync(int amount, string source)
        => Task.CompletedTask;

    public Task<int> GetRevealsAsync()
        => Task.FromResult(0);

    public Task UseRevealAsync()
        => Task.CompletedTask;

    public Task AddRevealsAsync(int amount, string source)
        => Task.CompletedTask;

    public Task<int> GetUndosAsync()
        => Task.FromResult(0);

    public Task UseUndoAsync()
        => Task.CompletedTask;

    public Task AddUndosAsync(int amount, string source)
        => Task.CompletedTask;

    public PlayerProgress GetCurrentProgress()
        => new PlayerProgress();

    public void LogEconomyEvent(string eventName, string data) { }
}

public class MockGameModeContext : GameModeContext
{
    public WordPuzzle lastLoadedPuzzle;
    public int coinsAdded = 0;
    public new MockPuzzleGenerator puzzleGenerator;
    public new MockGameStateManager stateManager;
    public MockEconomyManager economyManager;
    public new MockDataManager dataManager;
    public new MockWordValidator wordValidator;

    public MockGameModeContext()
    {
        puzzleGenerator = new MockPuzzleGenerator();
        wordValidator = new MockWordValidator();
        stateManager = new MockGameStateManager();
        economyManager = new MockEconomyManager();
        dataManager = new MockDataManager();

        base.puzzleGenerator = puzzleGenerator;
        base.wordValidator = wordValidator;
        base.stateManager = stateManager;
        base.economy = economyManager;
        base.dataManager = dataManager;
    }

    public void SetupPuzzleGenerator()
    {
        base.puzzleGenerator = puzzleGenerator;
    }

    public void SetupStateManager()
    {
        base.stateManager = stateManager;
    }

    public void SetupEconomyManager()
    {
        base.economy = economyManager;
    }

    public void SetupDataManager()
    {
        base.dataManager = dataManager;
    }
}


public class PuzzleShowModeMockGameStateManager : IGameStateManager
{
    private GameState currentState;
    private bool wonState = false;
    private List<string> foundWords = new List<string>();
    private int longestStreak = 0;
    private int totalScore = 0;

    public PuzzleShowModeMockGameStateManager()
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

    public IDisposable Subscribe(Action<GameState> observer)
    {
        return null;
    }

    public void SetWonState(bool won)
    {
        wonState = won;
    }

    public char[] GetAvailableLetters()
    {
        var usedLetters = new HashSet<char>();
        foreach (var word in currentState.wordChain)
        {
            foreach (var c in word)
            {
                usedLetters.Add(c);
            }
        }
        var available = new List<char>();
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
        if (foundWords.Contains(word))
            return 0;
        foundWords.Add(word);
        int points = word.Length;
        totalScore += points;
        currentState.score = totalScore;
        currentState.currentStreak++;
        longestStreak = System.Math.Max(longestStreak, currentState.currentStreak);
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
        if (foundWords.Count == 0)
            return "--";
        return foundWords.OrderByDescending(w => w.Length).FirstOrDefault() ?? "--";
    }

    public int GetLongestStreak()
    {
        return longestStreak;
    }

    public ResultsScreen.GameStats GetFinalStats()
    {
        return new ResultsScreen.GameStats
        {
            finalScore = totalScore,
            gameDuration = 0f,
            wordsFound = foundWords.Count,
            validAttempts = foundWords.Count,
            totalAttempts = foundWords.Count,
            bestWord = GetBestWord(),
            currentStreak = currentState.currentStreak,
            longestStreak = longestStreak
        };
    }

    public void ResetTracking()
    {
        foundWords.Clear();
        longestStreak = 0;
        totalScore = 0;
    }
}

public class PuzzleShowModeMockContext : GameModeContext
{
    public WordPuzzle lastLoadedPuzzle;
    public int coinsAdded = 0;
    public new PuzzleShowModeMockPuzzleGenerator puzzleGenerator;
    public new PuzzleShowModeMockGameStateManager stateManager;
    public new PuzzleShowModeMockEconomyManager economy;
    public new PuzzleShowModeMockDataManager dataManager;

    public PuzzleShowModeMockContext()
    {
        puzzleGenerator = new PuzzleShowModeMockPuzzleGenerator();
        stateManager = new PuzzleShowModeMockGameStateManager();
        economy = new PuzzleShowModeMockEconomyManager();
        dataManager = new PuzzleShowModeMockDataManager();

        base.puzzleGenerator = puzzleGenerator;
        base.stateManager = stateManager;
        base.economy = economy;
        base.dataManager = dataManager;
    }

    public void SetupTierData()
    {
        dataManager = new PuzzleShowModeMockDataManager();
        base.dataManager = dataManager;
    }

    public void SetupStateManager()
    {
        stateManager = new PuzzleShowModeMockGameStateManager();
        base.stateManager = stateManager;
    }
}

public class PuzzleShowModeMockPuzzleGenerator : IPuzzleGenerator
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

public class PuzzleShowModeMockEconomyManager : IEconomyManager
{
    public int coinsAdded = 0;

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task<int> GetCoinsAsync()
        => Task.FromResult(0);

    public Task AddCoinsAsync(int amount, string source)
    {
        coinsAdded += amount;
        return Task.CompletedTask;
    }

    public Task<int> GetHintsAsync()
        => Task.FromResult(0);

    public Task UseHintAsync()
        => Task.CompletedTask;

    public Task AddHintsAsync(int amount, string source)
        => Task.CompletedTask;

    public Task<int> GetRevealsAsync()
        => Task.FromResult(0);

    public Task UseRevealAsync()
        => Task.CompletedTask;

    public Task AddRevealsAsync(int amount, string source)
        => Task.CompletedTask;

    public Task<int> GetUndosAsync()
        => Task.FromResult(0);

    public Task UseUndoAsync()
        => Task.CompletedTask;

    public Task AddUndosAsync(int amount, string source)
        => Task.CompletedTask;

    public PlayerProgress GetCurrentProgress()
        => new PlayerProgress();

    public void LogEconomyEvent(string eventName, string data) { }
}

public class PuzzleShowModeMockDataManager : IDataManager
{
    public Task SaveGameStateAsync(GameStateSnapshot snapshot)
        => Task.CompletedTask;

    public Task<GameStateSnapshot> LoadGameStateAsync()
        => Task.FromResult(new GameStateSnapshot());

    public Task UpdatePlayerProgressAsync(PlayerProgress progress)
        => Task.CompletedTask;

    public Task<PlayerProgress> GetPlayerProgressAsync()
        => Task.FromResult(new PlayerProgress());

    public Task<TierData> GetTierDataAsync(int tierId)
    {
        return Task.FromResult(new TierData
        {
            tierId = tierId,
            isUnlocked = true,
            puzzles = new PuzzleDefinition[]
            {
                new PuzzleDefinition
                {
                    puzzleId = 1,
                    startWord = "cat",
                    endWord = "dog",
                    optimalSteps = 3,
                    solution = new[] { "cat", "bat", "bag", "dog" },
                    seedValue = 0
                }
            }
        });
    }

    public Task LoadAllTierDataAsync()
        => Task.CompletedTask;
}
