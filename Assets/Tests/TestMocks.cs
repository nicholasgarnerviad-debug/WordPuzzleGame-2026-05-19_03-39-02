using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Persistence;
using WordPuzzle.UI;
using WordPuzzle.Modes;

// Task 41B — recording analytics mock: captures every (name, params) emission in order.
public class MockAnalytics : IAnalytics
{
    public readonly List<(string name, (string key, object value)[] p)> Events
        = new List<(string, (string, object)[])>();

    public void Log(string eventName) => Events.Add((eventName, new (string, object)[0]));

    public void Log(string eventName, params (string key, object value)[] p)
        => Events.Add((eventName, p ?? new (string, object)[0]));

    public int CountOf(string eventName)
    {
        int n = 0;
        foreach (var e in Events) if (e.name == eventName) n++;
        return n;
    }
}

// Shared mock implementations for testing
public class MockWordValidator : IWordValidator
{
    private bool isValid = true;
    private bool isNextStep = true;
    private bool isProgress = true;   // Daily 2.0 — controllable so tests can drive a DETOUR (accepted but NOT progress).

    // Task 9C — controllable typed rejection reason + arbitrary Message.
    // Lets QA drive the enum-driven reject path and prove that the user-facing
    // text is derived from RejectReason, NOT from Message.
    private WordPuzzle.Puzzle.WordRejectReason rejectReason = WordPuzzle.Puzzle.WordRejectReason.None;
    private string message = "";

    public void SetValidResult(bool valid, bool nextStep, bool progress = true)
    {
        isValid = valid;
        isNextStep = nextStep;
        isProgress = progress;
    }

    /// <summary>
    /// Task 9C — configure a rejected validation result: isValid=false, isNextStep=false,
    /// the given typed RejectReason, and an arbitrary (possibly nonsense) Message so tests
    /// can confirm the user-facing string ignores Message and is derived from the enum.
    /// </summary>
    public void SetRejection(WordPuzzle.Puzzle.WordRejectReason reason, string msg)
    {
        isValid = false;
        isNextStep = false;
        rejectReason = reason;
        message = msg;
    }

    public void Initialize(string startWord, string endWord, string[] currentWordChain) { }

    public WordPuzzle.Puzzle.ValidationResult ValidateWord(string word)
    {
        return new WordPuzzle.Puzzle.ValidationResult(isValid, message, isNextStep, isProgress, -1, -1, rejectReason);
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

    public WordPuzzle.Puzzle.PuzzleDefinition GenerateRandomPuzzleOfLength(int wordLength, int targetDistance = -1)
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

    // Spec §3.2: puzzle-progress round-trip support
    private PuzzleProgressData lastPuzzleProgress;

    public Task SavePuzzleProgressAsync(PuzzleProgressData progress)
    {
        // Deep copy so callers can mutate after save without affecting stored state.
        lastPuzzleProgress = progress == null ? new PuzzleProgressData() : new PuzzleProgressData
        {
            currentTier = progress.currentTier,
            completedPuzzleIds = new List<int>(progress.completedPuzzleIds ?? new List<int>()),
            inProgressPuzzleIds = new List<int>(progress.inProgressPuzzleIds ?? new List<int>()),
            lastUpdated = progress.lastUpdated
        };
        persistedData["puzzleProgress"] = lastPuzzleProgress;
        return Task.CompletedTask;
    }

    public Task<PuzzleProgressData> LoadPuzzleProgressAsync()
    {
        if (lastPuzzleProgress == null) lastPuzzleProgress = new PuzzleProgressData();
        // Return a defensive copy.
        var copy = new PuzzleProgressData
        {
            currentTier = lastPuzzleProgress.currentTier,
            completedPuzzleIds = new List<int>(lastPuzzleProgress.completedPuzzleIds),
            inProgressPuzzleIds = new List<int>(lastPuzzleProgress.inProgressPuzzleIds),
            lastUpdated = lastPuzzleProgress.lastUpdated
        };
        return Task.FromResult(copy);
    }

    // Spec §3.2: round-trip support for user settings
    private SettingsData lastSettings;

    public Task SaveSettingsAsync(SettingsData settings)
    {
        lastSettings = settings == null ? new SettingsData() : settings.Clone();
        persistedData["settings"] = lastSettings;
        return Task.CompletedTask;
    }

    public Task<SettingsData> LoadSettingsAsync()
    {
        if (lastSettings == null) lastSettings = new SettingsData();
        return Task.FromResult(lastSettings.Clone());
    }

    private DailyProgress lastDailyProgress;

    public Task SaveDailyProgressAsync(DailyProgress progress)
    {
        lastDailyProgress = progress ?? new DailyProgress();
        persistedData["dailyProgress"] = lastDailyProgress;
        return Task.CompletedTask;
    }

    public Task<DailyProgress> LoadDailyProgressAsync()
    {
        if (lastDailyProgress == null) lastDailyProgress = new DailyProgress();
        return Task.FromResult(lastDailyProgress);
    }

    // Task 3A: onboarding persistence (intentionally NOT cleared by ResetAllAsync).
    private OnboardingData lastOnboarding;

    public Task SaveOnboardingAsync(OnboardingData onboarding)
    {
        lastOnboarding = onboarding ?? new OnboardingData();
        persistedData["onboarding"] = lastOnboarding;
        return Task.CompletedTask;
    }

    public Task<OnboardingData> LoadOnboardingAsync()
    {
        if (lastOnboarding == null) lastOnboarding = new OnboardingData();
        return Task.FromResult(lastOnboarding);
    }

    // Spec §3.2: destructive reset wipes puzzle/player/daily progress, retains settings.
    public Task ResetAllAsync()
    {
        lastPuzzleProgress = null;
        lastPlayerProgress = null;
        lastGameState = null;
        lastDailyProgress = null;
        persistedData.Remove("puzzleProgress");
        persistedData.Remove("playerProgress");
        persistedData.Remove("gameState");
        persistedData.Remove("dailyProgress");
        return Task.CompletedTask;
    }
}

public class MockEconomyManager : IEconomyManager
{
    public int coinsAdded = 0;
    public int coinsSpent = 0;
    private int balance = 0;
    public int hintsAdded = 0;

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task<int> GetCoinsAsync()
        => Task.FromResult(balance);

    public Task AddCoinsAsync(int amount, string source)
    {
        coinsAdded += amount;
        balance += amount;
        return Task.CompletedTask;
    }

    public Task<bool> SpendCoinsAsync(int amount, string sink)
    {
        if (balance < amount) return Task.FromResult(false);
        coinsSpent += amount;
        balance -= amount;
        return Task.FromResult(true);
    }

    public Task<int> GetHintsAsync()
        => Task.FromResult(hintsAdded);

    public Task UseHintAsync()
        => Task.CompletedTask;

    public Task AddHintsAsync(int amount, string source)
    {
        hintsAdded += amount;
        return Task.CompletedTask;
    }

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

    // Task 33 — time power-up, remove-ads, grants.
    public int timeAdded = 0;
    public bool removeAds = false;
    public int startingGrantCount = 0;
    public int dailyGrantCount = 0;

    public Task<int> GetTimePowerUpsAsync()
        => Task.FromResult(timeAdded);

    public Task UseTimePowerUpAsync()
        => Task.CompletedTask;

    public Task AddTimePowerUpsAsync(int amount, string source)
    {
        timeAdded += amount;
        return Task.CompletedTask;
    }

    public Task<bool> GetRemoveAdsAsync()
        => Task.FromResult(removeAds);

    public Task SetRemoveAdsAsync(bool value)
    {
        removeAds = value;
        return Task.CompletedTask;
    }

    public Task ApplyStartingInventoryIfNeeded()
    {
        startingGrantCount++;
        return Task.CompletedTask;
    }

    public Task GrantDailyIfDue(string todayIso)
    {
        dailyGrantCount++;
        return Task.CompletedTask;
    }

    // Task 36 36J — Starter Pack + ad-free window.
    public int starterPackGrantCount = 0;
    public long adFreeUntilUnix = 0;

    public Task<bool> GetStarterPackOwnedAsync()
        => Task.FromResult(starterPackGrantCount > 0);

    public Task GrantStarterPackAsync(int coins, int powerUpsEach, long adFreeUntilUnix)
    {
        if (starterPackGrantCount > 0) return Task.CompletedTask; // idempotent like the real one
        starterPackGrantCount++;
        coinsAdded += coins; balance += coins;
        hintsAdded += powerUpsEach; timeAdded += powerUpsEach;
        if (adFreeUntilUnix > this.adFreeUntilUnix) this.adFreeUntilUnix = adFreeUntilUnix;
        return Task.CompletedTask;
    }

    public bool IsAdFreeActive(long nowUnix) => adFreeUntilUnix > nowUnix;

    // Task 36 36K — faucet/sink stubs (mocks don't assert economy internals).
    public int loginClaims = 0;
    public int watchCoinsGrants = 0;
    public int milestoneAwards = 0;

    public bool IsLoginRewardAvailable(string todayIso) => true;
    public int PeekLoginRewardCoins() => BalanceConfig.LoginRewardCycle[0];
    public Task<int> ClaimLoginRewardAsync(string todayIso)
    {
        loginClaims++;
        return Task.FromResult(BalanceConfig.LoginRewardCycle[0]);
    }
    public int WatchCoinsRemainingToday(string todayIso) => BalanceConfig.WatchCoinsDailyCap;
    public Task<int> GrantWatchCoinsAsync(string todayIso)
    {
        watchCoinsGrants++;
        return Task.FromResult(BalanceConfig.WatchCoinsReward);
    }
    public Task<int> AwardStreakMilestonesAsync(int currentStreak)
    {
        milestoneAwards++;
        return Task.FromResult(0);
    }

    public PlayerProgress GetCurrentProgress()
        => new PlayerProgress();

    public void LogEconomyEvent(string eventName, string data) { }
}

// Task 6B — controllable IAdService mock for EditMode tests.
public class MockAdService : IAdService
{
    public bool IsRewardedReady     { get; set; } = true;
    public bool IsInterstitialReady { get; set; } = true;

    // Call counters
    public int RewardedShowCount     = 0;
    public int InterstitialShowCount = 0;

    // Control flags
    public bool ShouldGrantReward = true;   // set false to simulate dismiss/failure

    public void LoadRewarded()     { }
    public void LoadInterstitial() { }

    public void ShowRewarded(Action onRewarded, Action onClosed)
    {
        RewardedShowCount++;
        if (ShouldGrantReward) onRewarded?.Invoke();
        onClosed?.Invoke();
    }

    public void ShowInterstitial(Action onClosed)
    {
        InterstitialShowCount++;
        onClosed?.Invoke();
    }
}
