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

        // Progress tracking
        PlayerProgress GetCurrentProgress();

        // Telemetry
        void LogEconomyEvent(string eventName, string data);
    }
}
