using NUnit.Framework;
using WordPuzzle.Game;
using WordPuzzle.Persistence;

[TestFixture]
public class DailyStreakRulesTests
{
    [Test]
    public void FirstEverCompletion_StartsStreakAtOne()
    {
        var p = new DailyProgress();
        DailyStreakRules.ApplyCompletion(p, "2026-05-31", 12);

        Assert.AreEqual(1, p.currentStreak);
        Assert.AreEqual(1, p.longestStreak);
        Assert.AreEqual("2026-05-31", p.lastCompletedDateIso);
        Assert.IsTrue(p.todayCompleted);
        Assert.AreEqual(12, p.todayPuzzleIndex);
        CollectionAssert.AreEquivalent(new[] { "2026-05-31" }, p.completedDates);
    }

    [Test]
    public void ConsecutiveDay_IncrementsStreak()
    {
        var p = new DailyProgress { lastCompletedDateIso = "2026-05-30", currentStreak = 4, longestStreak = 4 };
        DailyStreakRules.ApplyCompletion(p, "2026-05-31", 0);

        Assert.AreEqual(5, p.currentStreak);
        Assert.AreEqual(5, p.longestStreak);
    }

    [Test]
    public void GapDay_ResetsStreakAndPreservesLongest()
    {
        var p = new DailyProgress { lastCompletedDateIso = "2026-05-28", currentStreak = 7, longestStreak = 10 };
        DailyStreakRules.ApplyCompletion(p, "2026-05-31", 0);

        Assert.AreEqual(1, p.currentStreak, "skipped day should reset");
        Assert.AreEqual(10, p.longestStreak, "longest preserved across reset");
    }

    [Test]
    public void SameDayTwice_DoesNotDoubleCount()
    {
        var p = new DailyProgress();
        DailyStreakRules.ApplyCompletion(p, "2026-05-31", 12);
        int streakAfterFirst = p.currentStreak;

        DailyStreakRules.ApplyCompletion(p, "2026-05-31", 12);

        Assert.AreEqual(streakAfterFirst, p.currentStreak);
        Assert.AreEqual(1, p.completedDates.Count, "completedDates should not duplicate the same day");
    }

    [Test]
    public void LongestStreak_TracksMaxAcrossResets()
    {
        var p = new DailyProgress();
        DailyStreakRules.ApplyCompletion(p, "2026-01-01", 0);
        DailyStreakRules.ApplyCompletion(p, "2026-01-02", 0);
        DailyStreakRules.ApplyCompletion(p, "2026-01-03", 0);
        Assert.AreEqual(3, p.currentStreak);
        Assert.AreEqual(3, p.longestStreak);

        // Skip a day → reset; longest preserved.
        DailyStreakRules.ApplyCompletion(p, "2026-01-05", 0);
        Assert.AreEqual(1, p.currentStreak);
        Assert.AreEqual(3, p.longestStreak);

        // Build a longer streak.
        DailyStreakRules.ApplyCompletion(p, "2026-01-06", 0);
        DailyStreakRules.ApplyCompletion(p, "2026-01-07", 0);
        DailyStreakRules.ApplyCompletion(p, "2026-01-08", 0);
        DailyStreakRules.ApplyCompletion(p, "2026-01-09", 0);
        Assert.AreEqual(5, p.currentStreak);
        Assert.AreEqual(5, p.longestStreak);
    }

    [Test]
    public void CompletedDatesCapAt60()
    {
        var p = new DailyProgress();
        var date = new System.DateTime(2025, 1, 1);
        for (int i = 0; i < 80; i++)
        {
            DailyStreakRules.ApplyCompletion(p, date.AddDays(i).ToString("yyyy-MM-dd"), 0);
        }
        Assert.AreEqual(DailyStreakRules.CompletedDatesCap, p.completedDates.Count);
        // Oldest dates are dropped first; newest remains the last entry.
        Assert.AreEqual(date.AddDays(79).ToString("yyyy-MM-dd"),
            p.completedDates[p.completedDates.Count - 1]);
    }

    [Test]
    public void RefreshTodayFlag_TogglesOnDateChange()
    {
        var p = new DailyProgress { lastCompletedDateIso = "2026-05-31" };

        DailyStreakRules.RefreshTodayFlag(p, "2026-05-31");
        Assert.IsTrue(p.todayCompleted);

        DailyStreakRules.RefreshTodayFlag(p, "2026-06-01");
        Assert.IsFalse(p.todayCompleted);
    }
}
