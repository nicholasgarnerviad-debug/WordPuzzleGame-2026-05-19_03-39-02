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

    // ─── Time power-up (Task 33) — mirrors the hint/reveal/undo pattern ───
    public Task<int> GetTimePowerUpsAsync()
    {
        return Task.FromResult(currentProgress.totalTimeEarned);
    }

    public async Task UseTimePowerUpAsync()
    {
        if (currentProgress.totalTimeEarned <= 0)
        {
            Debug.LogWarning("Attempted to use time power-up but none available");
            return;
        }

        currentProgress.totalTimeEarned--;
        LogEconomyEvent("TimeUsed", $"timeRemaining:{currentProgress.totalTimeEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task AddTimePowerUpsAsync(int amount, string source)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Attempted to add negative time power-ups: {amount} from source: {source}");
            return;
        }

        currentProgress.totalTimeEarned += amount;
        LogEconomyEvent("TimeAdded", $"amount:{amount},source:{source},newBalance:{currentProgress.totalTimeEarned}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    // ─── Remove-ads (Task 33) ───
    public Task<bool> GetRemoveAdsAsync()
    {
        return Task.FromResult(currentProgress.removeAds);
    }

    public async Task SetRemoveAdsAsync(bool value)
    {
        currentProgress.removeAds = value;
        LogEconomyEvent("RemoveAdsSet", $"value:{value}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    // ─── Starting inventory + daily grant (Task 33) ───
    public async Task ApplyStartingInventoryIfNeeded()
    {
        if (currentProgress.startingGrantApplied) return;

        int g = BalanceConfig.StartingPowerUpGrant;
        // Top up to AT LEAST the starting amount — never reduce a save that already has more.
        currentProgress.totalHintsEarned   = Mathf.Max(currentProgress.totalHintsEarned, g);
        currentProgress.totalRevealsEarned = Mathf.Max(currentProgress.totalRevealsEarned, g);
        currentProgress.totalUndosEarned   = Mathf.Max(currentProgress.totalUndosEarned, g);
        currentProgress.totalTimeEarned    = Mathf.Max(currentProgress.totalTimeEarned, g);
        currentProgress.startingGrantApplied = true;

        LogEconomyEvent("StartingInventoryGranted", $"each:{g}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public async Task GrantDailyIfDue(string todayIso)
    {
        if (string.IsNullOrEmpty(todayIso)) return;
        if (currentProgress.lastDailyGrantDate == todayIso) return; // already granted today — idempotent

        int g = BalanceConfig.DailyPowerUpGrant;
        currentProgress.totalHintsEarned   += g;
        currentProgress.totalRevealsEarned += g;
        currentProgress.totalUndosEarned   += g;
        currentProgress.totalTimeEarned    += g;
        currentProgress.lastDailyGrantDate = todayIso; // missed days do NOT stack — one grant per visited day

        LogEconomyEvent("DailyGrant", $"each:{g},date:{todayIso}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    // ─── Starter Pack + ad-free window (Task 36 36J) ───
    public Task<bool> GetStarterPackOwnedAsync()
    {
        return Task.FromResult(currentProgress.starterPackOwned);
    }

    public async Task GrantStarterPackAsync(int coins, int powerUpsEach, long adFreeUntilUnix)
    {
        // One-time: once owned, NEVER re-grant the consumable coins/power-ups (covers restore + double-tap).
        if (currentProgress.starterPackOwned)
        {
            LogEconomyEvent("StarterPackAlreadyOwned", "noop");
            return;
        }

        if (coins > 0) currentProgress.totalCoins += coins;
        if (powerUpsEach > 0)
        {
            currentProgress.totalHintsEarned   += powerUpsEach;
            currentProgress.totalRevealsEarned += powerUpsEach;
            currentProgress.totalUndosEarned   += powerUpsEach;
            currentProgress.totalTimeEarned    += powerUpsEach;
        }
        // Extend (never shorten) the ad-free window.
        if (adFreeUntilUnix > currentProgress.adFreeUntilUnix)
            currentProgress.adFreeUntilUnix = adFreeUntilUnix;
        currentProgress.starterPackOwned = true;

        LogEconomyEvent("StarterPackGranted", $"coins:{coins},each:{powerUpsEach},adFreeUntil:{adFreeUntilUnix}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
    }

    public bool IsAdFreeActive(long nowUnix)
    {
        return currentProgress != null && currentProgress.adFreeUntilUnix > nowUnix;
    }

    // ─── Login reward (Task 36 36K) — escalating 7-day cycle, one claim per local day ───
    public bool IsLoginRewardAvailable(string todayIso)
    {
        return !string.IsNullOrEmpty(todayIso) && currentProgress.lastLoginRewardDate != todayIso;
    }

    public int PeekLoginRewardCoins()
    {
        var cycle = BalanceConfig.LoginRewardCycle;
        if (cycle == null || cycle.Length == 0) return 0;
        int idx = ((currentProgress.loginRewardIndex % cycle.Length) + cycle.Length) % cycle.Length;
        return cycle[idx];
    }

    public async Task<int> ClaimLoginRewardAsync(string todayIso)
    {
        if (!IsLoginRewardAvailable(todayIso)) return 0;  // idempotent per local day
        var cycle = BalanceConfig.LoginRewardCycle;
        if (cycle == null || cycle.Length == 0) return 0;

        int idx = ((currentProgress.loginRewardIndex % cycle.Length) + cycle.Length) % cycle.Length;
        int coins = cycle[idx];

        currentProgress.totalCoins += coins;
        currentProgress.loginRewardIndex = (idx + 1) % cycle.Length;   // advance; wraps after day 7
        currentProgress.lastLoginRewardDate = todayIso;

        LogEconomyEvent("LoginReward", $"coins:{coins},nextIndex:{currentProgress.loginRewardIndex},date:{todayIso}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
        return coins;
    }

    // ─── Watch-for-coins (Task 36 36K) — rewarded video, capped per local day ───
    public int WatchCoinsRemainingToday(string todayIso)
    {
        int used = (currentProgress.lastWatchCoinsDate == todayIso) ? currentProgress.watchCoinsCountToday : 0;
        int rem = BalanceConfig.WatchCoinsDailyCap - used;
        return rem < 0 ? 0 : rem;
    }

    public async Task<int> GrantWatchCoinsAsync(string todayIso)
    {
        if (string.IsNullOrEmpty(todayIso)) return 0;
        if (currentProgress.lastWatchCoinsDate != todayIso)   // a new local day rolls the counter over
        {
            currentProgress.lastWatchCoinsDate = todayIso;
            currentProgress.watchCoinsCountToday = 0;
        }
        if (currentProgress.watchCoinsCountToday >= BalanceConfig.WatchCoinsDailyCap) return 0;

        int coins = BalanceConfig.WatchCoinsReward;
        currentProgress.totalCoins += coins;
        currentProgress.watchCoinsCountToday++;

        LogEconomyEvent("WatchCoins", $"coins:{coins},usedToday:{currentProgress.watchCoinsCountToday},date:{todayIso}");
        await dataManager.UpdatePlayerProgressAsync(currentProgress);
        return coins;
    }

    // ─── Streak milestones (Task 36 36K) — one-time coin pop at each milestone length ───
    public async Task<int> AwardStreakMilestonesAsync(int currentStreak)
    {
        var milestones = BalanceConfig.StreakMilestones;
        if (milestones == null) return 0;

        int paid = 0;
        foreach (int m in milestones)   // ascending; "highest awarded" gate makes this once-ever per milestone
        {
            if (m <= currentStreak && m > currentProgress.highestStreakMilestoneAwarded)
            {
                currentProgress.totalCoins += BalanceConfig.StreakMilestoneReward;
                currentProgress.highestStreakMilestoneAwarded = m;
                paid += BalanceConfig.StreakMilestoneReward;
            }
        }
        if (paid > 0)
        {
            LogEconomyEvent("StreakMilestone", $"streak:{currentStreak},paid:{paid},highest:{currentProgress.highestStreakMilestoneAwarded}");
            await dataManager.UpdatePlayerProgressAsync(currentProgress);
        }
        return paid;
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
