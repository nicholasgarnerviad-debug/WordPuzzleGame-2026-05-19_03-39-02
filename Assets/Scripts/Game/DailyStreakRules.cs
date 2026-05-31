using System;
using System.Collections.Generic;
using System.Globalization;
using WordPuzzle.Persistence;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Pure, dependency-free streak math for the daily puzzle.
    /// All inputs are explicit (no clocks, no persistence) so the rules are
    /// trivially unit-testable.
    /// </summary>
    public static class DailyStreakRules
    {
        public const int CompletedDatesCap = 60;

        /// <summary>
        /// Apply a "today's daily puzzle was completed" event to the progress record.
        /// Mutates and returns the same instance (allocating one if null was passed).
        /// Same-day re-completion never double-counts the streak; the function is idempotent
        /// for any given todayIso.
        /// </summary>
        public static DailyProgress ApplyCompletion(DailyProgress p, string todayIso, int puzzleIndex)
        {
            if (p == null) p = new DailyProgress();
            if (string.IsNullOrEmpty(todayIso)) return p;

            // Idempotent: completing the same day twice never re-increments the streak.
            if (p.lastCompletedDateIso == todayIso)
            {
                p.todayCompleted = true;
                p.todayPuzzleIndex = puzzleIndex;
                return p;
            }

            string yesterdayIso = AddDays(todayIso, -1);

            if (string.IsNullOrEmpty(p.lastCompletedDateIso))
            {
                p.currentStreak = 1;                // first-ever completion
            }
            else if (p.lastCompletedDateIso == yesterdayIso)
            {
                p.currentStreak = p.currentStreak <= 0 ? 1 : p.currentStreak + 1;
            }
            else
            {
                p.currentStreak = 1;                // a day was skipped — streak resets
            }

            if (p.currentStreak > p.longestStreak) p.longestStreak = p.currentStreak;
            p.lastCompletedDateIso = todayIso;
            p.todayCompleted = true;
            p.todayPuzzleIndex = puzzleIndex;

            if (p.completedDates == null) p.completedDates = new List<string>();
            if (p.completedDates.Count == 0 || p.completedDates[p.completedDates.Count - 1] != todayIso)
                p.completedDates.Add(todayIso);
            while (p.completedDates.Count > CompletedDatesCap)
                p.completedDates.RemoveAt(0);

            return p;
        }

        /// <summary>
        /// Refresh the convenience flag <see cref="DailyProgress.todayCompleted"/> against
        /// the current day. Call after loading from persistence.
        /// </summary>
        public static void RefreshTodayFlag(DailyProgress p, string todayIso)
        {
            if (p == null) return;
            p.todayCompleted = !string.IsNullOrEmpty(p.lastCompletedDateIso)
                            && p.lastCompletedDateIso == todayIso;
        }

        // ---- ISO date arithmetic ------------------------------------------------------
        internal static string AddDays(string iso, int days)
        {
            if (DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
            {
                return dt.AddDays(days).ToString("yyyy-MM-dd");
            }
            return iso;
        }
    }
}
