using System.Threading.Tasks;
using NUnit.Framework;
using WordPuzzle.State;

/// <summary>
/// Task 36 Phase 5 (36K) — faucet/sink CLAIM LOGIC: the escalating 7-day login cycle, the
/// watch-for-coins daily cap, and one-time streak-milestone pops. All clock-free (dates injected),
/// real EconomyManager + MockDataManager. The reward-doubler and streak-repair sinks reuse the
/// existing AddCoinsAsync / SpendCoinsAsync and are covered by Task33EconomyTests.
/// </summary>
[TestFixture]
public class DailyFaucetTests
{
    private MockDataManager data;
    private EconomyManager economy;

    [SetUp]
    public void Setup()
    {
        data = new MockDataManager();
        economy = new EconomyManager(data);
    }

    // ── Login reward — escalating 7-day cycle ────────────────────────────────
    [Test]
    public async Task LoginReward_GrantsCycleValuesAndAdvances()
    {
        await economy.InitializeAsync();
        var cycle = BalanceConfig.LoginRewardCycle;

        Assert.IsTrue(economy.IsLoginRewardAvailable("2026-06-01"));
        Assert.AreEqual(cycle[0], economy.PeekLoginRewardCoins());

        Assert.AreEqual(cycle[0], await economy.ClaimLoginRewardAsync("2026-06-01"));
        Assert.AreEqual(cycle[1], await economy.ClaimLoginRewardAsync("2026-06-02"));
        Assert.AreEqual(cycle[2], await economy.ClaimLoginRewardAsync("2026-06-03"));

        Assert.AreEqual(cycle[0] + cycle[1] + cycle[2], economy.GetCurrentProgress().totalCoins);
    }

    [Test]
    public async Task LoginReward_IsIdempotentWithinADay()
    {
        await economy.InitializeAsync();
        int first = await economy.ClaimLoginRewardAsync("2026-06-01");
        Assert.Greater(first, 0);

        Assert.IsFalse(economy.IsLoginRewardAvailable("2026-06-01"));
        Assert.AreEqual(0, await economy.ClaimLoginRewardAsync("2026-06-01"), "no second claim on the same day");
        Assert.AreEqual(first, economy.GetCurrentProgress().totalCoins);
    }

    [Test]
    public async Task LoginReward_WrapsAfterSevenDays()
    {
        await economy.InitializeAsync();
        var cycle = BalanceConfig.LoginRewardCycle;   // 7 entries
        int expectedSum = 0;
        for (int day = 0; day < cycle.Length; day++)
        {
            int coins = await economy.ClaimLoginRewardAsync($"2026-07-{day + 1:00}");
            Assert.AreEqual(cycle[day], coins, $"day {day + 1} should grant cycle[{day}]");
            expectedSum += coins;
        }

        // 8th distinct day wraps back to cycle[0].
        int wrapCoins = await economy.ClaimLoginRewardAsync("2026-07-08");
        Assert.AreEqual(cycle[0], wrapCoins, "cycle wraps after day 7");
        Assert.AreEqual(expectedSum + cycle[0], economy.GetCurrentProgress().totalCoins);
    }

    // ── Watch-for-coins — capped per local day ───────────────────────────────
    [Test]
    public async Task WatchCoins_CapsPerDay_ThenResetsNextDay()
    {
        await economy.InitializeAsync();
        int cap = BalanceConfig.WatchCoinsDailyCap;
        int reward = BalanceConfig.WatchCoinsReward;

        Assert.AreEqual(cap, economy.WatchCoinsRemainingToday("2026-06-01"));
        for (int i = 0; i < cap; i++)
            Assert.AreEqual(reward, await economy.GrantWatchCoinsAsync("2026-06-01"), $"watch {i + 1} within cap");

        Assert.AreEqual(0, economy.WatchCoinsRemainingToday("2026-06-01"));
        Assert.AreEqual(0, await economy.GrantWatchCoinsAsync("2026-06-01"), "over the daily cap grants nothing");

        // New local day resets the counter.
        Assert.AreEqual(cap, economy.WatchCoinsRemainingToday("2026-06-02"));
        Assert.AreEqual(reward, await economy.GrantWatchCoinsAsync("2026-06-02"));

        Assert.AreEqual(reward * (cap + 1), economy.GetCurrentProgress().totalCoins);
    }

    // ── Streak milestones — one-time pops at 7 / 30 / 100 ────────────────────
    [Test]
    public async Task StreakMilestone_PaysEachOnce_AsStreakGrows()
    {
        await economy.InitializeAsync();
        int reward = BalanceConfig.StreakMilestoneReward;

        Assert.AreEqual(0, await economy.AwardStreakMilestonesAsync(6), "no milestone below the first");
        Assert.AreEqual(reward, await economy.AwardStreakMilestonesAsync(7), "reaching 7 pays once");
        Assert.AreEqual(0, await economy.AwardStreakMilestonesAsync(7), "7 does not pay twice");
        Assert.AreEqual(0, await economy.AwardStreakMilestonesAsync(29), "no pay between milestones");
        Assert.AreEqual(reward, await economy.AwardStreakMilestonesAsync(30), "reaching 30 pays once");
        Assert.AreEqual(reward, await economy.AwardStreakMilestonesAsync(100), "reaching 100 pays once");

        Assert.AreEqual(reward * 3, economy.GetCurrentProgress().totalCoins);
    }

    [Test]
    public async Task StreakMilestone_JumpPaysAllNewlyReached()
    {
        await economy.InitializeAsync();
        // A streak that vaults straight past two milestones pays both.
        int paid = await economy.AwardStreakMilestonesAsync(31);
        Assert.AreEqual(BalanceConfig.StreakMilestoneReward * 2, paid, "7 and 30 both newly reached");
    }
}
