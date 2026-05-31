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

        public DailyProgress() { }
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
