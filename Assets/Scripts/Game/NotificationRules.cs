using System;

namespace WordPuzzle
{
    /// <summary>
    /// Pure scheduling rules for the daily streak-reminder notification (Task 41C).
    /// No Unity/package types — LocalNotificationService consumes these; the hour tunable is
    /// BalanceConfig.ReminderHourLocal. The ONLY scheduling pattern is cancel-then-reschedule
    /// (idempotent, never stacks duplicates).
    /// </summary>
    public static class NotificationRules
    {
        /// <summary>
        /// Whether a reminder should be scheduled at all. The toggle is the only gate —
        /// todayPlayed shifts WHEN (see NextFireLocal), never WHETHER: a player who already
        /// played still wants tomorrow's reminder.
        /// </summary>
        public static bool ShouldSchedule(bool todayPlayed, bool notificationsEnabled)
            => notificationsEnabled;

        /// <summary>
        /// Next local fire time: tomorrow-at-hour if today is already played or the hour has
        /// passed; today-at-hour otherwise. Minutes/seconds are zeroed.
        /// </summary>
        public static DateTime NextFireLocal(DateTime now, int hour, bool todayPlayed)
        {
            var todayAt = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, now.Kind);
            return (todayPlayed || now >= todayAt) ? todayAt.AddDays(1) : todayAt;
        }

        /// <summary>
        /// Notification body copy. Streak 0 (new players) omits the streak suffix entirely.
        /// </summary>
        public static string Body(int streak)
            => streak > 0
                ? $"Today's ladder is ready — streak: {streak} \U0001F525"
                : "Today's ladder is ready";
    }
}
