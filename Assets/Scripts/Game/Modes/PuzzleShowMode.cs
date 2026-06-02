using System;
using System.Collections.Generic;
using WordPuzzle.State;
using WordPuzzle.Puzzle;
using WordPuzzle.Persistence;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Per-puzzle UI/lifecycle state (Spec §3.6).
    /// </summary>
    public enum PuzzleState
    {
        Locked,
        UnlockedUnplayed,
        InProgress,
        Completed
    }

    /// <summary>
    /// Puzzle Show mode: The solution path is fully revealed. Player must follow
    /// the exact solution path shown to complete the puzzle.
    ///
    /// Spec §3: Tier-completion gate. Player must complete N puzzles in current
    /// tier before advancing. PuzzleShowMode is persistence-agnostic: it accepts
    /// PuzzleProgressData via LoadProgress and emits via ExportProgress; the
    /// orchestrator (GameBootstrap) handles save/load via IDataManager.
    /// </summary>
    public class PuzzleShowMode : IGameMode
    {
        // Forwarded from BalanceConfig — static readonly so they read across assemblies
        // without a compile-time const embedding. Value: 6.
        public static readonly int MaxTier = BalanceConfig.MaxTier;

        /// <summary>Spec §3.1 — base (Tier 1) gate. Value: 10.</summary>
        public static readonly int PuzzlesRequiredToAdvanceTier = BalanceConfig.PuzzlesRequiredToAdvanceTier;

        /// <summary>
        /// Task 15 passthrough so UI (which references Modes, not Puzzle) can read the
        /// progressive per-tier unlock requirement without a direct BalanceConfig dependency.
        /// </summary>
        public static int PuzzlesRequiredToAdvance(int tier) => BalanceConfig.PuzzlesRequiredToAdvance(tier);

        private GameStateManager stateManager;
        private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;
        private int currentTier = 1;

        // Spec §3.2: in-memory progress state (HashSet for O(1) Contains).
        private readonly HashSet<int> completedPuzzleIds = new HashSet<int>();
        private readonly HashSet<int> inProgressPuzzleIds = new HashSet<int>();

        // Lookup of tier -> set of puzzleIds belonging to that tier. Populated by
        // SetTierPuzzleLookup (called by orchestrator after tier_definitions load).
        // Used to recompute PuzzlesCompletedInCurrentTier on tier advance / load.
        private Dictionary<int, HashSet<int>> tierPuzzleIdLookup;

        // -------- Public API (Spec §3.2) --------

        public int CurrentTier => currentTier;
        public bool AllTiersComplete => currentTier > MaxTier;

        public int PuzzlesCompletedInCurrentTier { get; private set; }
        // Task 15 — progressive gate: deeper tiers require more completions to advance.
        public int PuzzlesRequiredThisTier => BalanceConfig.PuzzlesRequiredToAdvance(currentTier);
        public bool IsTierComplete() => PuzzlesCompletedInCurrentTier >= PuzzlesRequiredThisTier;

        public IReadOnlyCollection<int> CompletedPuzzleIds => completedPuzzleIds;
        public IReadOnlyCollection<int> InProgressPuzzleIds => inProgressPuzzleIds;

        public bool IsPuzzleCompleted(int puzzleId) => completedPuzzleIds.Contains(puzzleId);

        /// <summary>Spec §3.6 puzzle-state resolution.</summary>
        public PuzzleState GetPuzzleState(int puzzleId, bool tierUnlocked)
            => ResolveState(puzzleId, tierUnlocked, completedPuzzleIds, inProgressPuzzleIds);

        /// <summary>
        /// Pure §3.6 state resolver — shared by the live mode and the Puzzle Library UI
        /// (Task 15C) so card colouring matches gameplay state exactly. No instance state.
        /// </summary>
        public static PuzzleState ResolveState(int puzzleId, bool tierUnlocked,
            ICollection<int> completed, ICollection<int> inProgress)
        {
            if (!tierUnlocked) return PuzzleState.Locked;
            if (completed != null && completed.Contains(puzzleId)) return PuzzleState.Completed;
            if (inProgress != null && inProgress.Contains(puzzleId)) return PuzzleState.InProgress;
            return PuzzleState.UnlockedUnplayed;
        }

        // -------- Events (Spec §3.2) --------

        /// <summary>Fires when a puzzle is newly completed. Arg: puzzleId.</summary>
        public event Action<int> OnPuzzleCompleted;

        /// <summary>Fires when the tier gate is met. Args: (oldTier, newTier).</summary>
        public event Action<int, int> OnTierAdvanced;

        /// <summary>Fires when player completes the final tier (currentTier > MaxTier).</summary>
        public event Action OnAllTiersComplete;

        // -------- Tier-lookup wiring (orchestrator-supplied) --------

        /// <summary>
        /// Supply the tier→puzzleIds mapping (authoritative source: tier_definitions.json).
        /// Spec §3.3: "DO NOT hardcode the math … use authoritative lookup from tier_definitions."
        /// </summary>
        public void SetTierPuzzleLookup(Dictionary<int, HashSet<int>> lookup)
        {
            tierPuzzleIdLookup = lookup;
            RecomputeCompletedInCurrentTier();
        }

        // -------- Progress I/O (Spec §3.2) --------

        public void LoadProgress(PuzzleProgressData data)
        {
            completedPuzzleIds.Clear();
            inProgressPuzzleIds.Clear();

            if (data == null)
            {
                currentTier = 1;
                PuzzlesCompletedInCurrentTier = 0;
                return;
            }

            currentTier = data.currentTier < 1 ? 1 : data.currentTier;

            if (data.completedPuzzleIds != null)
                foreach (var id in data.completedPuzzleIds) completedPuzzleIds.Add(id);

            if (data.inProgressPuzzleIds != null)
                foreach (var id in data.inProgressPuzzleIds) inProgressPuzzleIds.Add(id);

            RecomputeCompletedInCurrentTier();
        }

        public PuzzleProgressData ExportProgress()
        {
            return new PuzzleProgressData
            {
                currentTier = currentTier,
                completedPuzzleIds = new List<int>(completedPuzzleIds),
                inProgressPuzzleIds = new List<int>(inProgressPuzzleIds),
                lastUpdated = DateTime.UtcNow.Ticks
            };
        }

        // -------- IGameMode --------

        public void Initialize(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public void StartGame(WordPuzzle.Puzzle.WordPuzzle puzzle)
        {
            if (stateManager == null)
                throw new InvalidOperationException("Must call Initialize first");

            currentPuzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            stateManager.StartNewPuzzle(puzzle);

            // Spec §3.4: track in-progress (only if not already completed).
            if (!completedPuzzleIds.Contains(puzzle.puzzleId))
                inProgressPuzzleIds.Add(puzzle.puzzleId);
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null) return;

            // Spec §3 — drop strict solutionIndex gate. Defer validation to the
            // GameStateManager / WordValidator and let the player reach endWord by
            // ANY valid one-letter-edit path (canonical solution is shown only as a hint).
            var before = stateManager.GetCurrentState();
            int countBefore = before.wordChain.Count;
            stateManager.SubmitWord(word);
            var after = stateManager.GetCurrentState();

            bool wordAdded = after.wordChain.Count > countBefore;
            if (wordAdded && after.IsPuzzleComplete)
            {
                OnPuzzleSolutionReached(currentPuzzle.puzzleId);
            }
        }

        /// <summary>Spec §3.3 completion handling.</summary>
        private void OnPuzzleSolutionReached(int puzzleId)
        {
            // §3.3.2 — dedup; §3.5 — replays don't increment.
            bool isNewCompletion = !completedPuzzleIds.Contains(puzzleId);
            if (isNewCompletion)
            {
                completedPuzzleIds.Add(puzzleId);

                // §3.3.2b — increment only if this puzzle is in the *current* tier.
                if (PuzzleBelongsToTier(puzzleId, currentTier))
                {
                    PuzzlesCompletedInCurrentTier++;
                }
            }

            // §3.3.3 — clear in-progress marker.
            inProgressPuzzleIds.Remove(puzzleId);

            // §3.3.4 — fire completion event (even on replay, so orchestrator persists).
            OnPuzzleCompleted?.Invoke(puzzleId);

            // §3.3.5 — tier-advance check (only on a real new completion).
            if (isNewCompletion && IsTierComplete())
            {
                if (currentTier < MaxTier)
                {
                    int oldTier = currentTier;
                    currentTier++;
                    // §3.3.5 — recompute for the new tier from already-completed set.
                    RecomputeCompletedInCurrentTier();
                    OnTierAdvanced?.Invoke(oldTier, currentTier);
                }
                else if (currentTier == MaxTier)
                {
                    // Final tier just got its gate met.
                    int oldTier = currentTier;
                    currentTier++;  // -> MaxTier+1, AllTiersComplete now true.
                    OnTierAdvanced?.Invoke(oldTier, currentTier);
                    OnAllTiersComplete?.Invoke();
                }
                // currentTier > MaxTier: silently no-op (already complete).
            }
        }

        private bool PuzzleBelongsToTier(int puzzleId, int tier)
        {
            // If orchestrator never supplied a lookup, assume "yes" so the gate still
            // functions for tests/dev without tier_definitions. (Safest: do count it.)
            if (tierPuzzleIdLookup == null) return true;
            return tierPuzzleIdLookup.TryGetValue(tier, out var ids) && ids.Contains(puzzleId);
        }

        private void RecomputeCompletedInCurrentTier()
        {
            if (tierPuzzleIdLookup == null || !tierPuzzleIdLookup.TryGetValue(currentTier, out var tierIds))
            {
                // No authoritative tier mapping yet — preserve current counter rather than
                // zeroing (caller will recompute once SetTierPuzzleLookup is invoked).
                return;
            }

            int count = 0;
            foreach (var id in completedPuzzleIds)
            {
                if (tierIds.Contains(id)) count++;
            }
            PuzzlesCompletedInCurrentTier = count;
        }

        /// <summary>
        /// Legacy/external tier advance. Kept for backward compatibility with
        /// GameBootstrap.CheckGameOver; the auto-advance path inside HandleWordSubmission
        /// is the primary flow now.
        /// </summary>
        public void AdvanceTier()
        {
            if (currentTier <= MaxTier)
            {
                int oldTier = currentTier;
                currentTier++;
                RecomputeCompletedInCurrentTier();
                OnTierAdvanced?.Invoke(oldTier, currentTier);
                if (currentTier > MaxTier) OnAllTiersComplete?.Invoke();
            }
        }

        public void Tick(float deltaTime)
        {
            stateManager?.IncrementElapsedTime(deltaTime);
        }

        public GameModeStats GetStats()
        {
            var state = stateManager?.GetCurrentState();
            return new GameModeStats
            {
                modeName = "Puzzle Show",
                wordsFound = state?.wordsFound ?? 0,
                totalTime = state?.elapsedTime ?? 0f,
                score = state?.score ?? 0,
                accuracy = 100f // Always perfect in show mode
            };
        }

        public void Reset()
        {
            currentPuzzle = null;
            // NOTE: progress (completedPuzzleIds, currentTier, etc.) is intentionally
            // NOT cleared here — those persist across puzzles. Use LoadProgress(null)
            // for a hard reset.
        }

        public bool IsGameOver()
        {
            // Spec §3 — gameplay terminates only when the player has actually built
            // a chain ending in the puzzle's endWord (state.IsPuzzleComplete).
            if (currentPuzzle == null || stateManager == null) return false;
            return stateManager.GetCurrentState().IsPuzzleComplete;
        }

        public string[] GetFullSolution()
        {
            return currentPuzzle?.solution ?? new string[0];
        }
    }
}
