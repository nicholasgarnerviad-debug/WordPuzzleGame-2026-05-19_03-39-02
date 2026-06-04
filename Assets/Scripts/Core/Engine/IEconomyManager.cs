using System.Threading.Tasks;
using WordPuzzle.Persistence;

namespace WordPuzzle.State
{
    public interface IEconomyManager
    {
        // Initialization
        Task InitializeAsync();

        // Coin management
        Task<int> GetCoinsAsync();
        Task AddCoinsAsync(int amount, string source);
        /// <summary>
        /// Attempt to deduct <paramref name="amount"/> coins. Returns true when the balance
        /// was sufficient and the deduction was applied; false (no mutation) otherwise.
        /// </summary>
        Task<bool> SpendCoinsAsync(int amount, string sink);

        // Hint management
        Task<int> GetHintsAsync();
        Task UseHintAsync();
        Task AddHintsAsync(int amount, string source);

        // Reveal management
        Task<int> GetRevealsAsync();
        Task UseRevealAsync();
        Task AddRevealsAsync(int amount, string source);

        // Undo management
        Task<int> GetUndosAsync();
        Task UseUndoAsync();
        Task AddUndosAsync(int amount, string source);

        // Time power-up management (Task 33 — owned +TIME inventory)
        Task<int> GetTimePowerUpsAsync();
        Task UseTimePowerUpAsync();
        Task AddTimePowerUpsAsync(int amount, string source);

        // Remove-ads flag (Task 33)
        Task<bool> GetRemoveAdsAsync();
        Task SetRemoveAdsAsync(bool value);

        // Starting inventory + daily grant (Task 33)
        /// <summary>Grants the 5-each starting inventory once (idempotent via startingGrantApplied).</summary>
        Task ApplyStartingInventoryIfNeeded();
        /// <summary>Grants +2 each once for the given local day (idempotent; no stacking of missed days).</summary>
        Task GrantDailyIfDue(string todayIso);

        // Starter Pack + ad-free window (Task 36 36J)
        /// <summary>True once the one-time Starter Pack has been purchased.</summary>
        Task<bool> GetStarterPackOwnedAsync();
        /// <summary>
        /// Grant the one-time Starter Pack: <paramref name="coins"/> + <paramref name="powerUpsEach"/> of
        /// EACH power-up (hint/undo/reveal/time) + an ad-free window through <paramref name="adFreeUntilUnix"/>.
        /// IDEMPOTENT: a no-op (no coins, no power-ups, no window change) once owned, so a restore or a
        /// double-tap can never double-grant the consumable contents.
        /// </summary>
        Task GrantStarterPackAsync(int coins, int powerUpsEach, long adFreeUntilUnix);
        /// <summary>True when the temporary ad-free window is still active at <paramref name="nowUnix"/>.</summary>
        bool IsAdFreeActive(long nowUnix);

        // Faucets / sinks (Task 36 36K) — all clock-free: the caller supplies the local-day ISO string.
        /// <summary>True when today's login reward has not yet been claimed.</summary>
        bool IsLoginRewardAvailable(string todayIso);
        /// <summary>Coins the NEXT login claim would grant (UI preview; no mutation).</summary>
        int PeekLoginRewardCoins();
        /// <summary>Claim today's login reward (idempotent per day). Returns coins granted (0 if already claimed). Advances the 7-day cycle.</summary>
        Task<int> ClaimLoginRewardAsync(string todayIso);
        /// <summary>Watch-for-coins watches still available today (resets at local midnight).</summary>
        int WatchCoinsRemainingToday(string todayIso);
        /// <summary>Grant a watch-for-coins reward (call after the ad completes). Returns coins granted (0 if the daily cap is hit).</summary>
        Task<int> GrantWatchCoinsAsync(string todayIso);
        /// <summary>Pay any streak milestone newly reached by <paramref name="currentStreak"/> (each paid once ever). Returns total coins paid.</summary>
        Task<int> AwardStreakMilestonesAsync(int currentStreak);

        // Progress tracking
        PlayerProgress GetCurrentProgress();

        // Telemetry
        void LogEconomyEvent(string eventName, string data);
    }
}
