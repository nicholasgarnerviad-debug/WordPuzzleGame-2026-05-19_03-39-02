namespace WordPuzzle.Puzzle
{
    /// <summary>
    /// Pure exponential-backoff curve for ad load retries (Task 39B).
    /// Standard AdMob pattern: delay = min(base * 2^attempt, max), so transient
    /// network failures retry quickly while persistent ones back off to a ceiling.
    ///
    /// Deliberately free of Unity/SDK types — lives in the lowest assembly so it
    /// is unit-testable; AdService owns the coroutine that consumes these delays.
    /// Tunables live in <see cref="BalanceConfig"/> (AdRetryBaseDelaySeconds /
    /// AdRetryMaxDelaySeconds), never as literals here.
    /// </summary>
    public static class AdRetryPolicy
    {
        /// <summary>
        /// Seconds to wait before retry number <paramref name="attempt"/> (0-based).
        /// attempt 0 → base; each subsequent attempt doubles; clamped at the max.
        /// Negative attempts are treated as 0 (defensive — callers count up from 0).
        /// </summary>
        public static float NextDelaySeconds(int attempt)
        {
            if (attempt < 0) attempt = 0;

            // 2^attempt overflows past 30 shifts; anything that large is beyond
            // the cap anyway, so short-circuit to the ceiling.
            if (attempt >= 30) return BalanceConfig.AdRetryMaxDelaySeconds;

            long delay = (long)BalanceConfig.AdRetryBaseDelaySeconds << attempt;
            if (delay > BalanceConfig.AdRetryMaxDelaySeconds)
                return BalanceConfig.AdRetryMaxDelaySeconds;
            return delay;
        }
    }
}
