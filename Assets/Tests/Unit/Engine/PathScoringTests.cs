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

    [Test]
    public void UsedPowerUp_DoesNotChangeGrade_ButIsRecorded()
    {
        var withoutPU = PathScoring.Score(Par, Par, 0, 0, false, usedPowerUp: false);
        var withPU    = PathScoring.Score(Par, Par, 0, 0, false, usedPowerUp: true);
        Assert.AreEqual(withoutPU.grade, withPU.grade);
        Assert.AreEqual(withoutPU.stars, withPU.stars);
        Assert.IsTrue(withPU.usedPowerUp);
        Assert.IsFalse(withoutPU.usedPowerUp);
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
