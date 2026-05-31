using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Persistence;
using WordPuzzle.UI;
using WordPuzzle.Modes;

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

    public WordPuzzle.Puzzle.ValidationResult ValidateWord(string word)
    {
        return new WordPuzzle.Puzzle.ValidationResult(isValid, "", isNextStep, true, -1, -1);
    }

    public bool IsValidNextWord(string word, string previousWord)
    {
        return isValid;
    }
}


public class MockGameStateManager : IGameStateManager
{
    private GameState currentState;
    private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;
    private bool wonState = false;
    private List<string> foundWords = new List<string>();
    private int longestStreak = 0;
    private int totalScore = 0;

    public void SetupPuzzle(WordPuzzle.Puzzle.WordPuzzle puzzle)
    {
        currentPuzzle = puzzle;
        currentState = new GameState(puzzle);
    }

    public void Dispatch(GameAction action) { }

    public GameState GetCurrentState()
    {
        return currentState ?? throw new System.InvalidOperationException("No puzzle started");
    }

    public void StartNewPuzzle(WordPuzzle.Puzzle.WordPuzzle puzzle)
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
        longestStreak = System.Math.Max(longestStreak, foundWords.Count);
        currentState = currentState.WithScore(totalScore).WithWordsFound(foundWords.Count);
        return points;
    }

    public int GetCurrentStreak()
    {
        return foundWords.Count;
    }

    public int GetWordsRemaining()
    {
        return 0;
    }

    public void SetWordsRemaining(int count)
    {
        // Mock implementation - no-op
    }

    public float GetTimeRemaining()
    {
        return 0f;
    }

    public void SetTimeRemaining(float time)
    {
        // Mock implementation - no-op
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

    public WordPuzzle.State.GameStats GetFinalStats()
    {
        return new WordPuzzle.State.GameStats
        {
            wordsFound = foundWords.Count,
            totalTime = 0f,
            score = totalScore,
            accuracy = 100f,
            currentStreak = foundWords.Count,
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
    public WordPuzzle.Puzzle.PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        return CreateDefaultPuzzle();
    }

    public WordPuzzle.Puzzle.PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
    {
        return CreateDefaultPuzzle();
    }

    private WordPuzzle.Puzzle.PuzzleDefinition CreateDefaultPuzzle()
    {
        return new WordPuzzle.Puzzle.PuzzleDefinition
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
    private Dictionary<string, object> persistedData = new Dictionary<string, object>();
    private GameStateSnapshot lastGameState;
    private PlayerProgress lastPlayerProgress;

    public Task SaveGameStateAsync(GameStateSnapshot snapshot)
    {
        lastGameState = snapshot;
        persistedData["gameState"] = snapshot;
        return Task.CompletedTask;
    }

    public Task<GameStateSnapshot> LoadGameStateAsync()
    {
        if (persistedData.TryGetValue("gameState", out var state) && state is GameStateSnapshot snapshot)
        {
            return Task.FromResult(snapshot);
        }
        return Task.FromResult(lastGameState ?? new GameStateSnapshot());
    }

    public Task UpdatePlayerProgressAsync(PlayerProgress progress)
    {
        lastPlayerProgress = progress;
        persistedData["playerProgress"] = progress;
        return Task.CompletedTask;
    }

    public Task<PlayerProgress> GetPlayerProgressAsync()
    {
        if (persistedData.TryGetValue("playerProgress", out var progress) && progress is PlayerProgress playerProgress)
        {
            return Task.FromResult(playerProgress);
        }
        return Task.FromResult(lastPlayerProgress ?? new PlayerProgress());
    }

    public Task<WordPuzzle.Puzzle.TierData> GetTierDataAsync(int tierId)
        => Task.FromResult(new WordPuzzle.Puzzle.TierData { tierId = tierId, isUnlocked = true });

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
