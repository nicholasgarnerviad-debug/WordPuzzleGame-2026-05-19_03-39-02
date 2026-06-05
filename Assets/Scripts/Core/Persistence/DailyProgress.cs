using System;
using System.Collections.Generic;

namespace WordPuzzle.Persistence
{
    /// <summary>
    /// Persistent record of daily-puzzle streak state. PlayerPrefs key "daily_v1"
    /// (see DataManager). All dates are local-time ISO strings ("yyyy-MM-dd").
    /// </summary>
    [Serializable]
    public class DailyProgress
    {
        public string lastCompletedDateIso = "";
        public int currentStreak = 0;
        public int longestStreak = 0;
        public List<string> completedDates = new List<string>();   // capped to last 60 (oldest first)
        public bool todayCompleted = false;
        public int todayPuzzleIndex = -1;

        // Daily 2.0 (Task 36) — PLAYED-streak + rolling Win/Loss record + streak repair. Old saves
        // auto-default these; DataManager seeds lastPlayedDateIso from lastCompletedDateIso on load
        // (Q6 forward migration) so the new "played" streak continues from the value already earned.
        public string lastPlayedDateIso = "";   // last day ATTEMPTED (solve or fail) — the played-streak key
        public string lastRepairDateIso = "";   // last streak-repair date (cooldown anchor)
        public bool todayPlayed = false;         // one-and-done lock: today already attempted
        public List<DayOutcome> outcomes = new List<DayOutcome>();   // trailing-365 Win/Loss ledger

        // Daily 2.0 (Task 38) — today's stored par-scored result, so re-tapping an already-played daily
        // RE-SHOWS it instead of starting a fresh run. Only meaningful while todayPlayed (the read is gated
        // on it); overwritten on each new play. Legacy saves default todayResultValid=false (no re-show).
        public bool todayResultValid = false;
        public int todayResultStars = 0;          // 0–3
        public int todayResultPar = 0;
        public int todayResultPlayerSteps = 0;
        public bool todayResultFailed = false;

        public DailyProgress() { }

        /// <summary>
        /// Normalize a freshly-deserialized record: default any null collections and apply the Q6
        /// forward migration — seed the played-date from the completion-date for pre-2.0 saves so the
        /// new "played" streak continues from the value the player already earned (never wiped/recomputed).
        /// Idempotent; only seeds when lastPlayedDateIso is empty.
        /// </summary>
        public void Normalize()
        {
            if (completedDates == null) completedDates = new List<string>();
            if (outcomes == null) outcomes = new List<DayOutcome>();
            if (string.IsNullOrEmpty(lastPlayedDateIso) && !string.IsNullOrEmpty(lastCompletedDateIso))
                lastPlayedDateIso = lastCompletedDateIso;
        }
    }

    /// <summary>One day's daily-puzzle outcome in the rolling record. Win = solved; loss = failed.</summary>
    [Serializable]
    public struct DayOutcome
    {
        public string dateIso;
        public bool won;
    }

    /// <summary>
    /// Test-injectable clock abstraction. Production uses SystemClock; tests inject TestClock.
    /// </summary>
    public interface IClock
    {
        DateTime Today { get; }
        string TodayIso { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime Today => DateTime.Today;
        public string TodayIso => DateTime.Today.ToString("yyyy-MM-dd");
    }
}
