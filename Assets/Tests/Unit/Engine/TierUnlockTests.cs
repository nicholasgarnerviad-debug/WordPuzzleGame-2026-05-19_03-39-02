using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.Modes;
using WordPuzzle.Persistence;
using WordPuzzle.Puzzle;

/// <summary>
/// TASK 19D — tier-unlock correctness. Locks the gate arithmetic that drives whether the
/// next tier unlocks, and the Tier-7 (final) edge so completion never errors past MaxTier.
/// (The library shows tier T as unlocked when T &lt;= currentTier; currentTier advances when
/// the gate is met, so these guard the "next tier unlocks" behavior.)
/// </summary>
[TestFixture]
public class TierUnlockTests
{
    private static List<int> Range(int start, int count)
    {
        var l = new List<int>();
        for (int i = 0; i < count; i++) l.Add(start + i);
        return l;
    }

    [Test]
    public void TierGate_MetAtThreshold_IsComplete_BelowIsNot()
    {
        int need = PuzzleShowMode.PuzzlesRequiredToAdvance(1); // base gate = 10
        var lookup = new Dictionary<int, HashSet<int>> { { 1, new HashSet<int>(Range(1, need + 5)) } };

        var mode = new PuzzleShowMode();
        mode.SetTierPuzzleLookup(lookup);

        mode.LoadProgress(new PuzzleProgressData { currentTier = 1, completedPuzzleIds = Range(1, need - 1) });
        Assert.IsFalse(mode.IsTierComplete(), $"{need - 1} completed (< {need}) must NOT complete the tier.");

        mode.LoadProgress(new PuzzleProgressData { currentTier = 1, completedPuzzleIds = Range(1, need) });
        Assert.IsTrue(mode.IsTierComplete(), $"{need} completed (>= {need}) must complete the tier.");
    }

    [Test]
    public void AdvanceTier_FromTier_UnlocksTheNext()
    {
        var mode = new PuzzleShowMode();
        mode.LoadProgress(new PuzzleProgressData { currentTier = 1 });
        mode.AdvanceTier();
        // The library marks tier T unlocked when T <= currentTier, so currentTier == 2 means
        // tier 2 is now playable.
        Assert.AreEqual(2, mode.CurrentTier);
    }

    [Test]
    public void Tier7_Completion_NoErrorAndMarksAllComplete()
    {
        var mode = new PuzzleShowMode();
        mode.LoadProgress(new PuzzleProgressData { currentTier = BalanceConfig.MaxTier }); // 7
        Assert.AreEqual(7, mode.CurrentTier);

        Assert.DoesNotThrow(() => mode.AdvanceTier());
        Assert.IsTrue(mode.AllTiersComplete, "advancing past the final tier flags AllTiersComplete.");
        // Calling again must not throw or run away past MaxTier+1.
        Assert.DoesNotThrow(() => mode.AdvanceTier());
        Assert.IsTrue(mode.AllTiersComplete);
    }

    [Test]
    public void ProgressiveGate_RisesPerTier()
    {
        int prev = 0;
        for (int t = 1; t <= BalanceConfig.MaxTier; t++)
        {
            int need = PuzzleShowMode.PuzzlesRequiredToAdvance(t);
            Assert.GreaterOrEqual(need, prev);
            Assert.LessOrEqual(need, BalanceConfig.PuzzlesPerTier);
            prev = need;
        }
        Assert.AreEqual(10, PuzzleShowMode.PuzzlesRequiredToAdvance(1));
    }

    // ── Task 20 — reconciliation from completion data (persist-on-unlock + backfill source) ──

    // tier t owns ids (t-1)*PuzzlesPerTier+1 .. t*PuzzlesPerTier.
    private static Dictionary<int, HashSet<int>> BuildFullLookup()
    {
        var d = new Dictionary<int, HashSet<int>>();
        int id = 1;
        for (int t = 1; t <= BalanceConfig.MaxTier; t++)
        {
            var set = new HashSet<int>();
            for (int i = 0; i < BalanceConfig.PuzzlesPerTier; i++) set.Add(id++);
            d[t] = set;
        }
        return d;
    }

    private static int TierFirstId(int tier) => (tier - 1) * BalanceConfig.PuzzlesPerTier + 1;

    [Test]
    public void Reconcile_BelowFirstThreshold_StaysTier1()
    {
        var done = new HashSet<int>(Range(TierFirstId(1), PuzzleShowMode.PuzzlesRequiredToAdvance(1) - 1));
        Assert.AreEqual(1, PuzzleShowMode.ReconcileHighestUnlockedTier(done, BuildFullLookup()));
    }

    [Test]
    public void Reconcile_Tier1ThresholdMet_UnlocksTier2()
    {
        var done = new HashSet<int>(Range(TierFirstId(1), PuzzleShowMode.PuzzlesRequiredToAdvance(1)));
        Assert.AreEqual(2, PuzzleShowMode.ReconcileHighestUnlockedTier(done, BuildFullLookup()));
    }

    [Test]
    public void Reconcile_Tier1And2Met_UnlocksTier3()
    {
        var done = new HashSet<int>(Range(TierFirstId(1), PuzzleShowMode.PuzzlesRequiredToAdvance(1)));
        done.UnionWith(Range(TierFirstId(2), PuzzleShowMode.PuzzlesRequiredToAdvance(2)));
        Assert.AreEqual(3, PuzzleShowMode.ReconcileHighestUnlockedTier(done, BuildFullLookup()));
    }

    [Test]
    public void Reconcile_NonContiguous_StopsAtFirstUnmetGate()
    {
        // Tier 1 met, Tier 2 NOT met, Tier 3 fully done — but Tier 3 is gated behind Tier 2.
        var done = new HashSet<int>(Range(TierFirstId(1), PuzzleShowMode.PuzzlesRequiredToAdvance(1)));
        done.UnionWith(Range(TierFirstId(2), PuzzleShowMode.PuzzlesRequiredToAdvance(2) - 5));
        done.UnionWith(Range(TierFirstId(3), BalanceConfig.PuzzlesPerTier));
        Assert.AreEqual(2, PuzzleShowMode.ReconcileHighestUnlockedTier(done, BuildFullLookup()));
    }

    [Test]
    public void Reconcile_AllTiersComplete_CapsAtMaxTier_NoTier8()
    {
        var lookup = BuildFullLookup();
        var done = new HashSet<int>();
        foreach (var kv in lookup) done.UnionWith(kv.Value);
        Assert.AreEqual(BalanceConfig.MaxTier, PuzzleShowMode.ReconcileHighestUnlockedTier(done, lookup));
    }

    [Test]
    public void Reconcile_NullSafe_ReturnsOne()
    {
        Assert.AreEqual(1, PuzzleShowMode.ReconcileHighestUnlockedTier(null, BuildFullLookup()));
        Assert.AreEqual(1, PuzzleShowMode.ReconcileHighestUnlockedTier(new HashSet<int>(), null));
    }
}
