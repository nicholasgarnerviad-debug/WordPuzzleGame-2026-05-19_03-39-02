using System.Threading.Tasks;

public interface IDataManager
{
    // Real-time save (called on every action)
    Task SaveGameStateAsync(GameStateSnapshot snapshot);

    // Load on app startup
    Task<GameStateSnapshot> LoadGameStateAsync();

    // Progress tracking
    Task UpdatePlayerProgressAsync(PlayerProgress progress);
    Task<PlayerProgress> GetPlayerProgressAsync();

    // Tier data for Puzzle Show mode
    Task<TierData> GetTierDataAsync(int tierId);
    Task LoadAllTierDataAsync();
}

public class GameStateSnapshot
{
    public string currentMode;          // "Classic", "PuzzleShow", "TimeAttack"
    public int currentPuzzleId;
    public string[] wordChain;
    public string currentInput;
    public int lives;
    public int hintsUsed;
    public int revealsUsed;
    public int undosUsed;
    public long timestamp;
    public string sessionId;

    public GameStateSnapshot() { }
}

public class TierData
{
    public int tierId;
    public PuzzleDefinition[] puzzles;
    public bool isUnlocked;
    public long unlockedTimestamp;
}

public class PuzzleDefinition
{
    public int puzzleId;
    public string startWord;
    public string endWord;
    public int optimalSteps;
    public string[] solution;
    public int seedValue;
}
