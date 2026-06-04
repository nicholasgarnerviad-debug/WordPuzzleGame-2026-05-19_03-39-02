using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using WordPuzzle.Puzzle;
using WordPuzzle.Persistence;

namespace WordPuzzle.Persistence
{
    public class DataManager : IDataManager
{
    private const string SAVE_FILE_KEY = "wordpuzzle_save";
    private const string PROGRESS_FILE_KEY = "wordpuzzle_progress";
    private const string PUZZLE_PROGRESS_KEY = "puzzle_progress_v1";  // Spec §3.2
    private const string SETTINGS_KEY = "settings_v1";                 // Spec §3 Settings
    private const string DAILY_KEY = "daily_v1";                       // Task 1B
    private const string ONBOARDING_KEY = "onboarding_v1";             // Task 3A

    private GameStateSnapshot currentGameState;
    private PlayerProgress playerProgress;
    private PuzzleProgressData puzzleProgress;
    private SettingsData settings;
    private DailyProgress dailyProgress;
    private OnboardingData onboarding;
    private Dictionary<int, TierData> tierCache;
    private TierDataLoader tierLoader;

    public DataManager()
    {
        tierCache = new Dictionary<int, TierData>();
        tierLoader = new TierDataLoader();
        CleanupLegacyKeys();
    }

    // 9A: one-time removal of stale key written by the now-deleted CoinSystem orphan.
    private void CleanupLegacyKeys()
    {
        if (PlayerPrefs.HasKey("Coins"))
        {
            PlayerPrefs.DeleteKey("Coins");
            PlayerPrefs.Save();
        }
    }

    public async Task SaveGameStateAsync(GameStateSnapshot snapshot)
    {
        currentGameState = snapshot;

        var saveData = new SaveData
        {
            gameState = new GameStateData
            {
                currentMode = snapshot.currentMode,
                currentPuzzleId = snapshot.currentPuzzleId,
                wordChain = snapshot.wordChain,
                currentInput = snapshot.currentInput,
                lives = snapshot.lives,
                hintsUsed = snapshot.hintsUsed,
                revealsUsed = snapshot.revealsUsed,
                undosUsed = snapshot.undosUsed,
                timestamp = snapshot.timestamp,
                sessionId = snapshot.sessionId
            },
            playerProgress = ConvertProgressToData(playerProgress),
            savedTimestamp = System.DateTime.UtcNow.Ticks
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_FILE_KEY, json);

        // Async on next frame
        await Task.Delay(0);
    }

    public async Task<GameStateSnapshot> LoadGameStateAsync()
    {
        if (!PlayerPrefs.HasKey(SAVE_FILE_KEY))
        {
            return CreateEmptySnapshot();
        }

        string json = PlayerPrefs.GetString(SAVE_FILE_KEY);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        var snapshot = new GameStateSnapshot
        {
            currentMode = saveData.gameState.currentMode,
            currentPuzzleId = saveData.gameState.currentPuzzleId,
            wordChain = saveData.gameState.wordChain,
            currentInput = saveData.gameState.currentInput,
            lives = saveData.gameState.lives,
            hintsUsed = saveData.gameState.hintsUsed,
            revealsUsed = saveData.gameState.revealsUsed,
            undosUsed = saveData.gameState.undosUsed,
            timestamp = saveData.gameState.timestamp,
            sessionId = saveData.gameState.sessionId
        };

        await Task.Delay(0);
        return snapshot;
    }

    public async Task UpdatePlayerProgressAsync(PlayerProgress progress)
    {
        playerProgress = progress;

        string json = JsonUtility.ToJson(ConvertProgressToData(progress));
        PlayerPrefs.SetString(PROGRESS_FILE_KEY, json);

        await Task.Delay(0);
    }

    public async Task<PlayerProgress> GetPlayerProgressAsync()
    {
        if (playerProgress != null)
            return playerProgress;

        if (!PlayerPrefs.HasKey(PROGRESS_FILE_KEY))
        {
            playerProgress = new PlayerProgress();
            await UpdatePlayerProgressAsync(playerProgress);
            return playerProgress;
        }

        string json = PlayerPrefs.GetString(PROGRESS_FILE_KEY);
        PlayerProgressData data = JsonUtility.FromJson<PlayerProgressData>(json);

        playerProgress = ConvertDataToProgress(data);
        await Task.Delay(0);
        return playerProgress;
    }

    public async Task<TierData> GetTierDataAsync(int tierId)
    {
        if (tierCache.ContainsKey(tierId))
            return tierCache[tierId];

        TierData tierData = await tierLoader.LoadTierAsync(tierId);
        tierCache[tierId] = tierData;

        return tierData;
    }

    public async Task LoadAllTierDataAsync()
    {
        // Load all tiers from Resources/Data/tier_definitions.json
        for (int i = 1; i <= 10; i++)
        {
            await GetTierDataAsync(i);
        }
    }

    // Spec §3.2: Puzzle Show tier-completion progress persistence
    public async Task SavePuzzleProgressAsync(PuzzleProgressData progress)
    {
        if (progress == null) progress = new PuzzleProgressData();
        progress.lastUpdated = System.DateTime.UtcNow.Ticks;
        puzzleProgress = progress;

        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString(PUZZLE_PROGRESS_KEY, json);
        PlayerPrefs.Save();

        await Task.Delay(0);
    }

    public async Task<PuzzleProgressData> LoadPuzzleProgressAsync()
    {
        if (puzzleProgress != null)
            return puzzleProgress;

        if (!PlayerPrefs.HasKey(PUZZLE_PROGRESS_KEY))
        {
            puzzleProgress = new PuzzleProgressData();
            await Task.Delay(0);
            return puzzleProgress;
        }

        string json = PlayerPrefs.GetString(PUZZLE_PROGRESS_KEY);
        PuzzleProgressData data = JsonUtility.FromJson<PuzzleProgressData>(json);

        // Defensive: JsonUtility leaves null lists if JSON was malformed
        if (data == null) data = new PuzzleProgressData();
        if (data.completedPuzzleIds == null) data.completedPuzzleIds = new System.Collections.Generic.List<int>();
        if (data.inProgressPuzzleIds == null) data.inProgressPuzzleIds = new System.Collections.Generic.List<int>();
        if (data.currentTier < 1) data.currentTier = 1;

        puzzleProgress = data;
        await Task.Delay(0);
        return puzzleProgress;
    }

    // Spec §3.2 Settings: persist user settings via PlayerPrefs key "settings_v1"
    public async Task SaveSettingsAsync(SettingsData settingsToSave)
    {
        if (settingsToSave == null) settingsToSave = new SettingsData();

        // Defensive clamps before serialization.
        settingsToSave.masterVolume = Mathf.Clamp01(settingsToSave.masterVolume);
        settingsToSave.sfxVolume = Mathf.Clamp01(settingsToSave.sfxVolume);
        settingsToSave.musicVolume = Mathf.Clamp01(settingsToSave.musicVolume);

        settings = settingsToSave.Clone();

        string json = JsonUtility.ToJson(settingsToSave);
        PlayerPrefs.SetString(SETTINGS_KEY, json);
        PlayerPrefs.Save();

        await Task.Delay(0);
    }

    public async Task<SettingsData> LoadSettingsAsync()
    {
        if (settings != null)
            return settings.Clone();

        if (!PlayerPrefs.HasKey(SETTINGS_KEY))
        {
            settings = new SettingsData();
            await Task.Delay(0);
            return settings.Clone();
        }

        string json = PlayerPrefs.GetString(SETTINGS_KEY);
        SettingsData data = null;
        try
        {
            data = JsonUtility.FromJson<SettingsData>(json);
        }
        catch
        {
            data = null;
        }

        if (data == null) data = new SettingsData();

        // Defensive: clamp ranges
        data.masterVolume = Mathf.Clamp01(data.masterVolume);
        data.musicVolume = Mathf.Clamp01(data.musicVolume);
        data.sfxVolume = Mathf.Clamp01(data.sfxVolume);

        settings = data;
        await Task.Delay(0);
        return settings.Clone();
    }

    // Task 1B — Daily puzzle / streak persistence.
    public async Task SaveDailyProgressAsync(DailyProgress progress)
    {
        if (progress == null) progress = new DailyProgress();
        dailyProgress = progress;
        string json = JsonUtility.ToJson(progress);
        PlayerPrefs.SetString(DAILY_KEY, json);
        PlayerPrefs.Save();
        await Task.Delay(0);
    }

    public async Task<DailyProgress> LoadDailyProgressAsync()
    {
        if (dailyProgress != null)
        {
            await Task.Delay(0);
            return dailyProgress;
        }
        if (!PlayerPrefs.HasKey(DAILY_KEY))
        {
            dailyProgress = new DailyProgress();
            await Task.Delay(0);
            return dailyProgress;
        }
        string json = PlayerPrefs.GetString(DAILY_KEY);
        DailyProgress data = null;
        try { data = JsonUtility.FromJson<DailyProgress>(json); } catch { data = null; }
        if (data == null) data = new DailyProgress();
        data.Normalize();   // Task 36 — default new collections + Q6 forward migration (seed played-date)
        dailyProgress = data;
        await Task.Delay(0);
        return dailyProgress;
    }

    // Task 3A — Tutorial onboarding persistence. Intentionally NOT cleared by ResetAllAsync
    // (onboarding survives a progress reset; only the Replay Tutorial item clears it).
    public async Task SaveOnboardingAsync(OnboardingData onboardingData)
    {
        if (onboardingData == null) onboardingData = new OnboardingData();
        onboarding = onboardingData;
        string json = JsonUtility.ToJson(onboardingData);
        PlayerPrefs.SetString(ONBOARDING_KEY, json);
        PlayerPrefs.Save();
        await Task.Delay(0);
    }

    public async Task<OnboardingData> LoadOnboardingAsync()
    {
        if (onboarding != null)
        {
            await Task.Delay(0);
            return onboarding;
        }
        if (!PlayerPrefs.HasKey(ONBOARDING_KEY))
        {
            onboarding = new OnboardingData();
            await Task.Delay(0);
            return onboarding;
        }
        string json = PlayerPrefs.GetString(ONBOARDING_KEY);
        OnboardingData data = null;
        try { data = JsonUtility.FromJson<OnboardingData>(json); } catch { data = null; }
        if (data == null) data = new OnboardingData();
        onboarding = data;
        await Task.Delay(0);
        return onboarding;
    }

    // Spec §3.2: destructive reset — wipes puzzle progress + player progress + daily streak,
    // keeps settings_v1 intact.
    public async Task ResetAllAsync()
    {
        // Clear in-memory caches.
        currentGameState = null;
        playerProgress = null;
        puzzleProgress = null;
        dailyProgress = null;

        // Wipe persisted keys (keep SETTINGS_KEY).
        if (PlayerPrefs.HasKey(PUZZLE_PROGRESS_KEY))
            PlayerPrefs.DeleteKey(PUZZLE_PROGRESS_KEY);
        if (PlayerPrefs.HasKey(PROGRESS_FILE_KEY))
            PlayerPrefs.DeleteKey(PROGRESS_FILE_KEY);
        if (PlayerPrefs.HasKey(SAVE_FILE_KEY))
            PlayerPrefs.DeleteKey(SAVE_FILE_KEY);
        if (PlayerPrefs.HasKey(DAILY_KEY))
            PlayerPrefs.DeleteKey(DAILY_KEY);

        PlayerPrefs.Save();

        await Task.Delay(0);
    }

    private GameStateSnapshot CreateEmptySnapshot()
    {
        return new GameStateSnapshot
        {
            currentMode = "Menu",
            currentPuzzleId = 0,
            wordChain = new string[] { },
            currentInput = "",
            lives = 3,
            hintsUsed = 0,
            revealsUsed = 0,
            undosUsed = 0,
            timestamp = System.DateTime.UtcNow.Ticks,
            sessionId = System.Guid.NewGuid().ToString()
        };
    }

    private PlayerProgressData ConvertProgressToData(PlayerProgress progress)
    {
        if (progress == null)
            return new PlayerProgressData();

        return new PlayerProgressData
        {
            totalCoins = progress.totalCoins,
            totalPuzzlesCompleted = progress.totalPuzzlesCompleted,
            highestTierUnlocked = progress.highestTierUnlocked,
            totalHintsEarned = progress.totalHintsEarned,
            totalRevealsEarned = progress.totalRevealsEarned,
            totalUndosEarned = progress.totalUndosEarned,
            totalTimeEarned = progress.totalTimeEarned,
            removeAds = progress.removeAds,
            startingGrantApplied = progress.startingGrantApplied,
            lastDailyGrantDate = progress.lastDailyGrantDate,
            starterPackOwned = progress.starterPackOwned,
            adFreeUntilUnix = progress.adFreeUntilUnix,
            classicStats = new ClassicModeStatsData
            {
                gamesPlayed = progress.classicStats.gamesPlayed,
                gamesWon = progress.classicStats.gamesWon,
                totalCoinsEarned = progress.classicStats.totalCoinsEarned,
                totalPuzzlesCompleted = progress.classicStats.totalPuzzlesCompleted
            },
            timeAttackStats = new TimeAttackModeStatsData
            {
                gamesPlayed = progress.timeAttackStats.gamesPlayed,
                bestRoundReached = progress.timeAttackStats.bestRoundReached,
                totalCoinsEarned = progress.timeAttackStats.totalCoinsEarned
            }
        };
    }

    private PlayerProgress ConvertDataToProgress(PlayerProgressData data)
    {
        return new PlayerProgress
        {
            totalCoins = data.totalCoins,
            totalPuzzlesCompleted = data.totalPuzzlesCompleted,
            highestTierUnlocked = data.highestTierUnlocked,
            totalHintsEarned = data.totalHintsEarned,
            totalRevealsEarned = data.totalRevealsEarned,
            totalUndosEarned = data.totalUndosEarned,
            totalTimeEarned = data.totalTimeEarned,
            removeAds = data.removeAds,
            startingGrantApplied = data.startingGrantApplied,
            lastDailyGrantDate = data.lastDailyGrantDate ?? "",
            starterPackOwned = data.starterPackOwned,
            adFreeUntilUnix = data.adFreeUntilUnix,
            classicStats = new ClassicModeStats
            {
                gamesPlayed = data.classicStats.gamesPlayed,
                gamesWon = data.classicStats.gamesWon,
                totalCoinsEarned = data.classicStats.totalCoinsEarned,
                totalPuzzlesCompleted = data.classicStats.totalPuzzlesCompleted
            },
            timeAttackStats = new TimeAttackModeStats
            {
                gamesPlayed = data.timeAttackStats.gamesPlayed,
                bestRoundReached = data.timeAttackStats.bestRoundReached,
                totalCoinsEarned = data.timeAttackStats.totalCoinsEarned
            }
        };
    }
}
}
