using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DataManager : IDataManager
{
    private const string SAVE_FILE_KEY = "wordpuzzle_save";
    private const string PROGRESS_FILE_KEY = "wordpuzzle_progress";

    private GameStateSnapshot currentGameState;
    private PlayerProgress playerProgress;
    private Dictionary<int, TierData> tierCache;
    private TierDataLoader tierLoader;

    public DataManager()
    {
        tierCache = new Dictionary<int, TierData>();
        tierLoader = new TierDataLoader();
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
