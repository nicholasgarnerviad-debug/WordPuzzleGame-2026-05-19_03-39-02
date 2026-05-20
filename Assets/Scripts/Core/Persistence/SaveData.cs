// Serializable wrapper for saving to JSON
public class SaveData
{
    public GameStateData gameState;
    public PlayerProgressData playerProgress;
    public long savedTimestamp;
}

[System.Serializable]
public class GameStateData
{
    public string currentMode;
    public int currentPuzzleId;
    public string[] wordChain;
    public string currentInput;
    public int lives;
    public int hintsUsed;
    public int revealsUsed;
    public int undosUsed;
    public long timestamp;
    public string sessionId;
}

[System.Serializable]
public class PlayerProgressData
{
    public int totalCoins;
    public int totalPuzzlesCompleted;
    public int highestTierUnlocked;
    public int totalHintsEarned;
    public int totalRevealsEarned;
    public int totalUndosEarned;
    public ClassicModeStatsData classicStats;
    public TimeAttackModeStatsData timeAttackStats;
}

[System.Serializable]
public class ClassicModeStatsData
{
    public int gamesPlayed;
    public int gamesWon;
    public int totalCoinsEarned;
    public int totalPuzzlesCompleted;
}

[System.Serializable]
public class TimeAttackModeStatsData
{
    public int gamesPlayed;
    public int bestRoundReached;
    public int totalCoinsEarned;
}
