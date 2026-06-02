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
}
