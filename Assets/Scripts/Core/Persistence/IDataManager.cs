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

        // Task: Library Path View — per-puzzle best-solve + progressively-revealed optimal path.
        // JsonUtility auto-defaults this to an empty list for OLD saves (no migration code needed):
        // a previously-beaten puzzle with no record simply shows nothing extra until next played.
        public List<PuzzlePathRecord> puzzlePaths = new List<PuzzlePathRecord>();

        // Task 45 — tiers whose "Tier N Unlocked" celebration modal has been shown (once per tier
        // EVER). Additive field: JsonUtility defaults it to an empty list for old saves, so every
        // already-unlocked tier celebrates at most once more, then never again. No migration.
        public List<int> celebratedTiers = new List<int>();

        public PuzzleProgressData() { }
    }

    /// <summary>
    /// Library Path View per-puzzle state (one per beaten Puzzle Show puzzle).
    ///   (A) bestSolvePath — the full word sequence of the player's BEST attempt
    ///       ("best" = fewest steps == fewest detours, since par == optimalSteps). Only
    ///       ever IMPROVES: a strictly-shorter replay replaces it; a worse replay changes nothing.
    ///   (B) revealedOptimalIndices — the accumulating set of canonical-solution slots the
    ///       player has matched (word + position). Unions on each solve; NEVER shrinks. A
    ///       perfect/optimal-length solve auto-reveals every slot (confirmed design decision).
    /// All fields are JsonUtility-friendly (no nullable structs); the pure update logic lives
    /// in <see cref="PuzzlePathProgress"/>.
    /// </summary>
    [Serializable]
    public class PuzzlePathRecord
    {
        public int puzzleId;
        public string[] bestSolvePath = System.Array.Empty<string>();
        public int bestSolveSteps = int.MaxValue;          // chain.Count-1; LOWER is better. MaxValue == unset.
        public List<int> revealedOptimalIndices = new List<int>();

        public PuzzlePathRecord() { }
    }
}
