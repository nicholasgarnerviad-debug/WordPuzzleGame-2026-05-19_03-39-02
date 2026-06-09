using NUnit.Framework;
using WordPuzzle.Puzzle;

/// <summary>
/// Task 36 Phase 1 — par-grade acceptance for the single PathScoring entry point.
/// Pure value tests (no Unity, no scene). Cutoffs come from BalanceConfig; the grade is
/// decided on DETOURS, and running out of mistakes is a hard Fail.
/// </summary>
[TestFixture]
public class PathScoringTests
{
    // par/playerSteps do not drive the grade (detours + mistakes do); a representative par
    // keeps the cases readable.
    private const int Par = 5;

    [Test]
    public void ZeroDetours_IsPerfect_ThreeStars()
    {
        var r = PathScoring.Score(Par, Par, 0, 0, ranOutOfMistakes: false, usedPowerUp: false);
        Assert.AreEqual(PathGrade.Perfect, r.grade);
        Assert.AreEqual(3, r.stars);
        Assert.IsFalse(r.failed);
    }

    [Test]
    public void OneDetour_IsGood_TwoStars()
    {
        var r = PathScoring.Score(Par, Par + 1, 1, 0, false, false);
        Assert.AreEqual(PathGrade.Good, r.grade);
        Assert.AreEqual(2, r.stars);
    }

    [Test]
    public void TwoDetours_IsGood_TwoStars()
    {
        var r = PathScoring.Score(Par, Par + 2, 2, 0, false, false);
        Assert.AreEqual(PathGrade.Good, r.grade);
        Assert.AreEqual(2, r.stars);
    }

    [Test]
    public void ThreeDetours_IsSolved_OneStar_NeverZeroOnASolve()
    {
        var r = PathScoring.Score(Par, Par + 3, 3, 0, false, false);
        Assert.AreEqual(PathGrade.Solved, r.grade);
        Assert.AreEqual(1, r.stars);
        Assert.IsFalse(r.failed);
        Assert.Greater(r.stars, 0, "a completed solve must never score 0 stars");
    }

    [Test]
    public void RanOutOfMistakes_IsFailed_ZeroStars_EvenWithZeroDetours()
    {
        var r = PathScoring.Score(Par, 2, 0, 3, ranOutOfMistakes: true, usedPowerUp: false);
        Assert.AreEqual(PathGrade.Failed, r.grade);
        Assert.AreEqual(0, r.stars);
        Assert.IsTrue(r.failed);
    }

    [Test]
    public void AlternateSameLengthPath_ZeroDetours_IsPerfect()
    {
        // Reached the target in exactly par steps via a different route: 0 detours => Perfect.
        var r = PathScoring.Score(Par, Par, 0, 0, false, false);
        Assert.AreEqual(PathGrade.Perfect, r.grade);
    }

    [Test]
    public void Grade_IsMonotonic_MoreDetoursNeverImproves()
    {
        int prevStars = int.MaxValue;
        for (int detours = 0; detours <= 8; detours++)
        {
            var r = PathScoring.Score(Par, Par + detours, detours, 0, false, false);
            Assert.LessOrEqual(r.stars, prevStars,
                $"stars must not increase as detours rise (detours={detours})");
            Assert.AreEqual((int)r.grade, r.stars, "stars must equal (int)grade by construction");
            prevStars = r.stars;
        }
    }

    // ── Task 40A — power-up assistance caps the grade at BalanceConfig.PowerUpMaxGrade ──

    [Test]
    public void UsedPowerUp_FlawlessRun_CappedAtPowerUpMaxGrade()
    {
        // (a) 0 detours + power-up: would be Perfect, capped to Good (★★) — and still recorded.
        var r = PathScoring.Score(Par, Par, 0, 0, false, usedPowerUp: true);
        Assert.AreEqual(BalanceConfig.PowerUpMaxGrade, r.grade,
            "an assisted flawless run grades at the cap, never Perfect");
        Assert.AreEqual((int)BalanceConfig.PowerUpMaxGrade, r.stars);
        Assert.IsTrue(r.usedPowerUp);
    }

    [Test]
    public void NoPowerUp_FlawlessRun_PerfectUnchanged()
    {
        // (b) the unassisted Perfect is untouched by the cap.
        var r = PathScoring.Score(Par, Par, 0, 0, false, usedPowerUp: false);
        Assert.AreEqual(PathGrade.Perfect, r.grade);
        Assert.AreEqual(3, r.stars);
        Assert.IsFalse(r.usedPowerUp);
    }

    [Test]
    public void UsedPowerUp_AlreadyAtOrBelowCap_GradeUnchanged_CapNeverRaises()
    {
        // (c) detoured runs already at Good / Solved keep their grade — the cap only lowers.
        var good   = PathScoring.Score(Par, Par + 1, 1, 0, false, usedPowerUp: true);
        Assert.AreEqual(PathGrade.Good, good.grade, "Good stays Good under the cap");

        var solved = PathScoring.Score(Par, Par + 3, 3, 0, false, usedPowerUp: true);
        Assert.AreEqual(PathGrade.Solved, solved.grade, "the cap must never RAISE Solved to Good");
        Assert.AreEqual(1, solved.stars);
    }

    [Test]
    public void UsedPowerUp_RanOutOfMistakes_StillFailed()
    {
        // (d) a hard fail overrides everything; the cap does not resurrect it.
        var r = PathScoring.Score(Par, 2, 0, 3, ranOutOfMistakes: true, usedPowerUp: true);
        Assert.AreEqual(PathGrade.Failed, r.grade);
        Assert.AreEqual(0, r.stars);
        Assert.IsTrue(r.failed);
    }

    [Test]
    public void UsedPowerUp_CappedResult_PaysTheGoodCoinRate()
    {
        // (e) coins follow the capped stars naturally — an assisted flawless run pays
        // the Good rate, not the Perfect rate.
        var capped = PathScoring.Score(Par, Par, 0, 0, false, usedPowerUp: true);
        Assert.AreEqual(BalanceConfig.DailyRewardGood,
            BalanceConfig.DailyCoinReward(capped.stars, capped.failed),
            "the capped result must pay the Good coin rate");
        Assert.AreNotEqual(BalanceConfig.DailyRewardPerfect,
            BalanceConfig.DailyCoinReward(capped.stars, capped.failed),
            "an assisted run must never pay the Perfect rate");
    }

    [Test]
    public void Result_CarriesInputsThrough()
    {
        var r = PathScoring.Score(4, 6, 2, 1, false, false);
        Assert.AreEqual(4, r.par);
        Assert.AreEqual(6, r.playerSteps);
        Assert.AreEqual(2, r.detours);
        Assert.AreEqual(1, r.mistakesUsed);
    }
}
