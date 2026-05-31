using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WordPuzzle.Puzzle;

namespace WordPuzzle.Persistence
{
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

        // Puzzle Show tier-completion progress (Spec §3.2)
        Task SavePuzzleProgressAsync(PuzzleProgressData progress);
        Task<PuzzleProgressData> LoadPuzzleProgressAsync();

        // User settings (Spec §3 Settings)
        Task SaveSettingsAsync(SettingsData settings);
        Task<SettingsData> LoadSettingsAsync();

        // Daily puzzle + streak (Task 1B)
        Task SaveDailyProgressAsync(DailyProgress progress);
        Task<DailyProgress> LoadDailyProgressAsync();

        // Tutorial onboarding (Task 3A)
        Task SaveOnboardingAsync(OnboardingData onboarding);
        Task<OnboardingData> LoadOnboardingAsync();

        // Destructive: wipe all puzzle/player progress; keep settings (Spec §3.2)
        Task ResetAllAsync();
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

    /// <summary>
    /// Persistent record of PuzzleShowMode tier-completion progress (Spec §3.2).
    /// Uses List&lt;int&gt; for JsonUtility compatibility; PuzzleShowMode rebuilds
    /// HashSet&lt;int&gt; in-memory for O(1) Contains() lookups.
    /// </summary>
    [Serializable]
    public class PuzzleProgressData
    {
        public int currentTier = 1;
        public List<int> completedPuzzleIds = new List<int>();   // global, dedup'd
        public List<int> inProgressPuzzleIds = new List<int>();  // started but not finished
        public long lastUpdated;

        public PuzzleProgressData() { }
    }
}
