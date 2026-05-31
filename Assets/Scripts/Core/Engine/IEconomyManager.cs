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

        // Progress tracking
        PlayerProgress GetCurrentProgress();

        // Telemetry
        void LogEconomyEvent(string eventName, string data);
    }
}
