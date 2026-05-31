using System.Threading.Tasks;
using UnityEngine;
using WordPuzzle.Persistence;

namespace WordPuzzle.State
{
    public class EconomyManager : IEconomyManager
    {
        private IDataManager dataManager;
        private PlayerProgress currentProgress;

    public EconomyManager(IDataManager dataManager)
    {
        this.dataManager = dataManager;
    }

    public async Task InitializeAsync()
    {
        currentProgress = await dataManager.GetPlayerProgressAsync();
    }

    public Task<int> GetCoinsAsync()
    {
        return Task.FromResult(currentProgress.totalCoins);
    }

    public async Task AddCoinsAsync(int amount, string source)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to add negative coins: {amount} from source: {source}");
            return;
        }

        currentProgress.totalCoins += amount;
        LogEconomyEvent("CoinsAdded", $"amount:{amount},source:{source},newBalance:{currentProgress.totalCoins}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task<bool> SpendCoinsAsync(int amount, string sink)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"SpendCoinsAsync: negative amount {amount} rejected (sink:{sink})");
            return false;
        }

        if (currentProgress.totalCoins < amount)
        {
            LogEconomyEvent("SpendFailed", $"sink:{sink},need:{amount},have:{currentProgress.totalCoins}");
            return false;
        }

        currentProgress.totalCoins -= amount;
        LogEconomyEvent("CoinsSpent", $"amount:{amount},sink:{sink},newBalance:{currentProgress.totalCoins}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
        return true;
    }

    public Task<int> GetHintsAsync()
    {
        return Task.FromResult(currentProgress.totalHintsEarned);
    }

    public async Task UseHintAsync()
    {
        if (currentProgress.totalHintsEarned <= 0)
        {
            Debug.LogWarning("Attempted to use hint but none available");
            return;
        }

        currentProgress.totalHintsEarned--;
        LogEconomyEvent("HintUsed", $"hintsRemaining:{currentProgress.totalHintsEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task AddHintsAsync(int amount, string source)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to add negative hints: {amount} from source: {source}");
            return;
        }

        currentProgress.totalHintsEarned += amount;
        LogEconomyEvent("HintsAdded", $"amount:{amount},source:{source},newBalance:{currentProgress.totalHintsEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public Task<int> GetRevealsAsync()
    {
        return Task.FromResult(currentProgress.totalRevealsEarned);
    }

    public async Task UseRevealAsync()
    {
        if (currentProgress.totalRevealsEarned <= 0)
        {
            Debug.LogWarning("Attempted to use reveal but none available");
            return;
        }

        currentProgress.totalRevealsEarned--;
        LogEconomyEvent("RevealUsed", $"revealsRemaining:{currentProgress.totalRevealsEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task AddRevealsAsync(int amount, string source)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to add negative reveals: {amount} from source: {source}");
            return;
        }

        currentProgress.totalRevealsEarned += amount;
        LogEconomyEvent("RevealsAdded", $"amount:{amount},source:{source},newBalance:{currentProgress.totalRevealsEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public Task<int> GetUndosAsync()
    {
        return Task.FromResult(currentProgress.totalUndosEarned);
    }

    public async Task UseUndoAsync()
    {
        if (currentProgress.totalUndosEarned <= 0)
        {
            Debug.LogWarning("Attempted to use undo but none available");
            return;
        }

        currentProgress.totalUndosEarned--;
        LogEconomyEvent("UndoUsed", $"undosRemaining:{currentProgress.totalUndosEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task AddUndosAsync(int amount, string source)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to add negative undos: {amount} from source: {source}");
            return;
        }

        currentProgress.totalUndosEarned += amount;
        LogEconomyEvent("UndosAdded", $"amount:{amount},source:{source},newBalance:{currentProgress.totalUndosEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public PlayerProgress GetCurrentProgress()
    {
        return currentProgress;
    }

    public void LogEconomyEvent(string eventName, string data)
    {
        Debug.Log($"[EconomyEvent] {eventName}: {data}");
    }
    }
}
