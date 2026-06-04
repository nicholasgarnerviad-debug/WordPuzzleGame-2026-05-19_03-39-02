using NUnit.Framework;

/// <summary>
/// Task 36 Phase 5 — economy numbers (Q5-confirmed): the par-scaled daily reward helper, the
/// coin-pack affordability invariant, and the login-reward cycle. Pure BalanceConfig checks.
/// </summary>
[TestFixture]
public class DailyEconomyTests
{
    [Test]
    public void DailyCoinReward_ScalesByGrade()
    {
        Assert.AreEqual(BalanceConfig.DailyRewardPerfect, BalanceConfig.DailyCoinReward(3, false));
        Assert.AreEqual(BalanceConfig.DailyRewardGood,    BalanceConfig.DailyCoinReward(2, false));
        Assert.AreEqual(BalanceConfig.DailyRewardSolved,  BalanceConfig.DailyCoinReward(1, false));
    }

    [Test]
    public void DailyCoinReward_Failed_IsConsolation_RegardlessOfStars()
    {
        Assert.AreEqual(BalanceConfig.DailyRewardFailed, BalanceConfig.DailyCoinReward(0, true));
        Assert.AreEqual(BalanceConfig.DailyRewardFailed, BalanceConfig.DailyCoinReward(3, true));
    }

    [Test]
    public void DailyCoinReward_IsMonotonic_BetterGradePaysAtLeastAsMuch()
    {
        Assert.GreaterOrEqual(BalanceConfig.DailyCoinReward(3, false), BalanceConfig.DailyCoinReward(2, false));
        Assert.GreaterOrEqual(BalanceConfig.DailyCoinReward(2, false), BalanceConfig.DailyCoinReward(1, false));
    }

    [Test]
    public void CoinPackInvariant_CheapestPackAffordsMidHintBundle()
    {
        // 36I core-bug fix: the cheapest coin pack (150 coins) must afford a mid Hint bundle (x15 = 135).
        Assert.GreaterOrEqual(150, BalanceConfig.HintBundlePrices[1]);
    }

    [Test]
    public void LoginRewardCycle_IsSevenDays_AndPeaksOnDaySeven()
    {
        Assert.AreEqual(7, BalanceConfig.LoginRewardCycle.Length);
        Assert.AreEqual(150, BalanceConfig.LoginRewardCycle[6]);
    }
}
