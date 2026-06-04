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

        // ──────────────────────────────────────────────────────────────────────────────
        // Daily 2.0 (Task 36) — PLAYED streak + 365-day W/L record + streak repair.
        // Still pure: all inputs explicit (ISO 'yyyy-MM-dd' strings, no clock, no persistence).
        // ──────────────────────────────────────────────────────────────────────────────

        /// <summary>Trailing window (days, inclusive) for the rolling Win/Loss record.</summary>
        public const int RecordWindowDays = 365;

        /// <summary>
        /// Apply a "today's daily was PLAYED" event (solved OR failed). A FAILED daily KEEPS the
        /// streak; a MISSED calendar day breaks it. Idempotent for any given todayIso. On a solve it
        /// also stamps the completion markers (for the bonus + 'solved today' signal); the W/L outcome
        /// is recorded either way. This is the Daily 2.0 streak authority — the flow calls THIS, not
        /// ApplyCompletion (which is retained unchanged for back-compat; do not call both).
        /// </summary>
        public static DailyProgress ApplyPlayed(DailyProgress p, string todayIso, bool solved)
        {
            if (p == null) p = new DailyProgress();
            if (string.IsNullOrEmpty(todayIso)) return p;

            // Idempotent: playing/finishing the same day twice never double-counts.
            if (p.lastPlayedDateIso == todayIso)
            {
                p.todayPlayed = true;
                return p;
            }

            string yesterdayIso = AddDays(todayIso, -1);
            if (string.IsNullOrEmpty(p.lastPlayedDateIso))
                p.currentStreak = 1;                                  // first-ever play
            else if (p.lastPlayedDateIso == yesterdayIso)
                p.currentStreak = p.currentStreak <= 0 ? 1 : p.currentStreak + 1;
            else
                p.currentStreak = 1;                                  // a calendar day was missed → reset

            if (p.currentStreak > p.longestStreak) p.longestStreak = p.currentStreak;
            p.lastPlayedDateIso = todayIso;
            p.todayPlayed = true;

            // Completion markers (solve only). A fail is played (streak kept above) but not completed.
            p.todayCompleted = solved;
            if (solved)
            {
                p.lastCompletedDateIso = todayIso;
                if (p.completedDates == null) p.completedDates = new List<string>();
                if (p.completedDates.Count == 0 || p.completedDates[p.completedDates.Count - 1] != todayIso)
                    p.completedDates.Add(todayIso);
                while (p.completedDates.Count > CompletedDatesCap) p.completedDates.RemoveAt(0);
            }

            RecordOutcome(p, todayIso, solved);
            return p;
        }

        /// <summary>
        /// Append today's Win/Loss to the rolling ledger (idempotent same-day) and prune entries older
        /// than the trailing window. A solve is a win; a failed daily is a loss. Neither touches the streak.
        /// </summary>
        public static void RecordOutcome(DailyProgress p, string todayIso, bool won)
        {
            if (p == null || string.IsNullOrEmpty(todayIso)) return;
            if (p.outcomes == null) p.outcomes = new List<DayOutcome>();
            if (p.outcomes.Count == 0 || p.outcomes[p.outcomes.Count - 1].dateIso != todayIso)
                p.outcomes.Add(new DayOutcome { dateIso = todayIso, won = won });
            PruneHistory(p, todayIso);
        }

        /// <summary>Drop ledger entries older than the trailing <see cref="RecordWindowDays"/> window.</summary>
        public static void PruneHistory(DailyProgress p, string todayIso)
        {
            if (p == null || p.outcomes == null || string.IsNullOrEmpty(todayIso)) return;
            string cutoffIso = AddDays(todayIso, -(RecordWindowDays - 1));   // inclusive window
            p.outcomes.RemoveAll(o => string.Compare(o.dateIso, cutoffIso, StringComparison.Ordinal) < 0);
        }

        public static int GamesPlayed(DailyProgress p) => (p == null || p.outcomes == null) ? 0 : p.outcomes.Count;

        public static int Wins(DailyProgress p)
        {
            if (p == null || p.outcomes == null) return 0;
            int n = 0;
            foreach (var o in p.outcomes) if (o.won) n++;
            return n;
        }

        public static int Losses(DailyProgress p) => GamesPlayed(p) - Wins(p);

        public static int WinRatePct(DailyProgress p)
        {
            int games = GamesPlayed(p);
            if (games <= 0) return 0;
            return (int)Math.Round(100.0 * Wins(p) / games);
        }

        /// <summary>
        /// True when the broken streak can be repaired: the miss is exactly YESTERDAY (the last play was
        /// the day BEFORE yesterday) AND at least <paramref name="cooldownDays"/> have passed since the
        /// last repair (or none yet). Pure — caller passes BalanceConfig.StreakRepairCooldownDays.
        /// </summary>
        public static bool CanRepair(string todayIso, string lastPlayedDateIso, string lastRepairDateIso, int cooldownDays)
        {
            if (string.IsNullOrEmpty(todayIso) || string.IsNullOrEmpty(lastPlayedDateIso)) return false;
            // Yesterday-only: the last play must be exactly two days ago (so ONLY yesterday was missed).
            if (lastPlayedDateIso != AddDays(todayIso, -2)) return false;
            if (string.IsNullOrEmpty(lastRepairDateIso)) return true;
            // Cooldown: the next allowed repair date is lastRepair + cooldownDays.
            return string.Compare(AddDays(lastRepairDateIso, cooldownDays), todayIso, StringComparison.Ordinal) <= 0;
        }

        /// <summary>
        /// Repair yesterday's miss: bridge the gap (+1 streak, as if yesterday was played) and stamp the
        /// repair date. Does NOT mark today played — the player still plays today for its own credit
        /// (Task 36 Q3). No-op when <see cref="CanRepair"/> is false. Pure.
        /// </summary>
        public static DailyProgress ApplyRepair(DailyProgress p, string todayIso, int cooldownDays)
        {
            if (p == null) return p;
            if (!CanRepair(todayIso, p.lastPlayedDateIso, p.lastRepairDateIso, cooldownDays)) return p;
            p.currentStreak = (p.currentStreak <= 0 ? 0 : p.currentStreak) + 1;
            if (p.currentStreak > p.longestStreak) p.longestStreak = p.currentStreak;
            p.lastPlayedDateIso = AddDays(todayIso, -1);   // bridge: as if yesterday was played
            p.lastRepairDateIso = todayIso;
            return p;
        }

        /// <summary>
        /// Refresh the "played today / one-and-done locked" flag against the current day. Call after
        /// loading from persistence (generalizes RefreshTodayFlag to the played lock).
        /// </summary>
        public static void RefreshPlayedFlag(DailyProgress p, string todayIso)
        {
            if (p == null) return;
            p.todayPlayed = !string.IsNullOrEmpty(p.lastPlayedDateIso) && p.lastPlayedDateIso == todayIso;
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
