using System;
using UnityEngine;
using WordPuzzle.Persistence;
using WordPuzzle.Puzzle;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Returns the puzzle of the day, deterministically derived from the player's
    /// LOCAL date. No network. Every client on the same calendar day picks the same
    /// index into a curated pool loaded from Resources/Data/daily_puzzles.json.
    /// </summary>
    public sealed class DailyPuzzleService
    {
        /// <summary>Fixed epoch — first day mapped to index 0.</summary>
        public static readonly DateTime Epoch = new DateTime(2025, 1, 1);

        private readonly IClock clock;
        private readonly PuzzleDefinition[] pool;

        public int PoolCount => pool?.Length ?? 0;

        public DailyPuzzleService(IClock clock, PuzzleDefinition[] pool)
        {
            this.clock = clock ?? new SystemClock();
            this.pool = pool ?? Array.Empty<PuzzleDefinition>();
        }

        /// <summary>
        /// Loads the pool from Resources/Data/daily_puzzles.json. Returns null if
        /// the asset is missing or empty (callers should fall back to disabling daily).
        /// </summary>
        public static DailyPuzzleService LoadFromResources(IClock clock = null,
            string resourcePath = "Data/daily_puzzles")
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[DailyPuzzleService] Resource '{resourcePath}' not found.");
                return new DailyPuzzleService(clock, Array.Empty<PuzzleDefinition>());
            }
            var wrapper = JsonUtility.FromJson<PoolWrapper>(asset.text);
            if (wrapper == null || wrapper.puzzles == null || wrapper.puzzles.Length == 0)
            {
                Debug.LogError("[DailyPuzzleService] Pool JSON parsed empty.");
                return new DailyPuzzleService(clock, Array.Empty<PuzzleDefinition>());
            }
            return new DailyPuzzleService(clock, wrapper.puzzles);
        }

        /// <summary>
        /// Number of whole days since <see cref="Epoch"/> in the clock's local date.
        /// Negative values (player local clock before epoch) clamp to 0.
        /// </summary>
        public int DaysSinceEpoch()
        {
            int delta = (clock.Today.Date - Epoch.Date).Days;
            return delta < 0 ? 0 : delta;
        }

        /// <summary>Today's index into the pool.</summary>
        public int TodayIndex()
        {
            int n = PoolCount;
            if (n <= 0) return -1;
            int d = DaysSinceEpoch();
            // Safe positive modulo.
            int idx = d % n;
            if (idx < 0) idx += n;
            return idx;
        }

        /// <summary>The PuzzleDefinition for today. Null if the pool is empty.</summary>
        public PuzzleDefinition GetTodayPuzzle()
        {
            int idx = TodayIndex();
            return idx < 0 ? null : pool[idx];
        }

        public string TodayIso => clock.TodayIso;

        // ---- JSON wrapper -------------------------------------------------------------
        [Serializable]
        private class PoolWrapper
        {
            public PuzzleDefinition[] puzzles;
        }
    }
}
