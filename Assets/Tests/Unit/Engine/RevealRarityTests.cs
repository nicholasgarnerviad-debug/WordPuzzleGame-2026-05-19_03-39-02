using NUnit.Framework;

/// <summary>
/// Classic polish pass (W4) — Reveal must stay the RAREST / most-premium assist. Reveal shows a correct
/// optimal-path word (the strongest help), so it is granted for free LESS than hint/undo/time and stays
/// the most expensive to use/buy. These are pure BalanceConfig invariants — they fail loudly if a future
/// tweak accidentally makes Reveal as cheap/common as the generous power-ups.
/// </summary>
[TestFixture]
public class RevealRarityTests
{
    [Test]
    public void StartingRevealGrant_IsScarcerThan_OtherPowerUps()
    {
        Assert.Less(BalanceConfig.StartingRevealGrant, BalanceConfig.StartingPowerUpGrant,
            "Reveal must start scarcer than the generous hint/undo/time grant.");
        Assert.GreaterOrEqual(BalanceConfig.StartingRevealGrant, 0, "Grants cannot be negative.");
    }

    [Test]
    public void DailyRevealGrant_IsScarcerThan_OtherPowerUps()
    {
        Assert.Less(BalanceConfig.DailyRevealGrant, BalanceConfig.DailyPowerUpGrant,
            "Reveal must trickle in slower per day than the generous hint/undo/time grant.");
        Assert.GreaterOrEqual(BalanceConfig.DailyRevealGrant, 0, "Grants cannot be negative.");
    }

    [Test]
    public void RevealInRunCost_IsPremium_AtLeastHintAndUndo()
    {
        Assert.GreaterOrEqual(BalanceConfig.RevealCost, BalanceConfig.HintCost,
            "Reveal's in-run cost must stay at least as premium as Hint.");
        Assert.GreaterOrEqual(BalanceConfig.RevealCost, BalanceConfig.UndoCost,
            "Reveal's in-run cost must stay at least as premium as Undo.");
    }

    [Test]
    public void RevealBundlePrices_AreThePremiumTier_VsHintAndUndo()
    {
        var reveal = BalanceConfig.RevealBundlePrices;
        var hint   = BalanceConfig.HintBundlePrices;
        var undo   = BalanceConfig.UndoBundlePrices;

        Assert.AreEqual(hint.Length, reveal.Length, "Bundle tiers must line up index-for-index with Hint.");
        Assert.AreEqual(undo.Length, reveal.Length, "Bundle tiers must line up index-for-index with Undo.");

        for (int i = 0; i < reveal.Length; i++)
        {
            Assert.GreaterOrEqual(reveal[i], hint[i],
                $"Reveal bundle tier {i} must cost at least as much as the Hint tier (premium).");
            Assert.GreaterOrEqual(reveal[i], undo[i],
                $"Reveal bundle tier {i} must cost at least as much as the Undo tier (premium).");
        }
    }
}
