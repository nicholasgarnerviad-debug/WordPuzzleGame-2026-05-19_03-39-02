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

    // ── Daily 2.0 (Task 36) — PLAYED streak (fail keeps it) + 365 W/L record + repair ──

    [Test]
    public void Played_PlayPlayFailPlay_KeepsStreakOfFour()
    {
        var p = new DailyProgress();
        DailyStreakRules.ApplyPlayed(p, "2026-06-01", true);
        DailyStreakRules.ApplyPlayed(p, "2026-06-02", true);
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", false); // a FAIL still counts as played
        DailyStreakRules.ApplyPlayed(p, "2026-06-04", true);

        Assert.AreEqual(4, p.currentStreak, "a failed daily counts as played and keeps the streak");
        Assert.AreEqual(4, DailyStreakRules.GamesPlayed(p));
        Assert.AreEqual(3, DailyStreakRules.Wins(p));
        Assert.AreEqual(1, DailyStreakRules.Losses(p));
        Assert.AreEqual(75, DailyStreakRules.WinRatePct(p)); // 3 of 4
    }

    [Test]
    public void Played_MissedDay_ResetsStreak_PreservesLongest()
    {
        var p = new DailyProgress { lastPlayedDateIso = "2026-06-01", currentStreak = 3, longestStreak = 5 };
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", true); // 06-02 was missed

        Assert.AreEqual(1, p.currentStreak);
        Assert.AreEqual(5, p.longestStreak, "longest preserved across a reset");
    }

    [Test]
    public void Played_FailOnConsecutiveDay_CountsAsLoss_StreakAdvances()
    {
        var p = new DailyProgress { lastPlayedDateIso = "2026-06-02", currentStreak = 2, longestStreak = 2 };
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", false);

        Assert.AreEqual(3, p.currentStreak, "fail on a consecutive day still advances the played streak");
        Assert.AreEqual(1, DailyStreakRules.Losses(p));
        Assert.AreEqual(0, DailyStreakRules.Wins(p));
        Assert.IsFalse(p.todayCompleted, "a fail is played but NOT completed");
    }

    [Test]
    public void Played_SameDayTwice_IsIdempotent()
    {
        var p = new DailyProgress();
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", true);
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", true);

        Assert.AreEqual(1, p.currentStreak);
        Assert.AreEqual(1, DailyStreakRules.GamesPlayed(p), "same day must not double-record");
    }

    [Test]
    public void Record_PrunesEntriesOlderThanWindow()
    {
        var p = new DailyProgress();
        p.outcomes.Add(new DayOutcome { dateIso = "2025-01-01", won = true }); // well over a year before
        DailyStreakRules.ApplyPlayed(p, "2026-06-04", true);

        Assert.AreEqual(1, DailyStreakRules.GamesPlayed(p), "the >365-day entry is pruned");
        Assert.AreEqual("2026-06-04", p.outcomes[0].dateIso);
    }

    [Test]
    public void Repair_OnlyForYesterdayMiss()
    {
        int cd = BalanceConfig.StreakRepairCooldownDays;
        // Last play day-before-yesterday => only yesterday missed => repairable.
        Assert.IsTrue(DailyStreakRules.CanRepair("2026-06-03", "2026-06-01", "", cd));
        // Last play 3 days ago => more than one missed day => NOT repairable.
        Assert.IsFalse(DailyStreakRules.CanRepair("2026-06-03", "2026-05-31", "", cd));
        // Already played yesterday => nothing to repair.
        Assert.IsFalse(DailyStreakRules.CanRepair("2026-06-03", "2026-06-02", "", cd));
    }

    [Test]
    public void Repair_RespectsCooldown()
    {
        // cooldown 7: a repair 3 days ago is still on cooldown; one exactly 7 days ago is allowed.
        Assert.IsFalse(DailyStreakRules.CanRepair("2026-06-03", "2026-06-01", "2026-05-31", 7));
        Assert.IsTrue(DailyStreakRules.CanRepair("2026-06-03", "2026-06-01", "2026-05-27", 7));
    }

    [Test]
    public void Repair_BridgesYesterdayWithoutPlayingToday()
    {
        var p = new DailyProgress { lastPlayedDateIso = "2026-06-01", currentStreak = 3, longestStreak = 5 };
        DailyStreakRules.ApplyRepair(p, "2026-06-03", 7);

        Assert.AreEqual(4, p.currentStreak, "repair bridges the one missed day (+1)");
        Assert.AreEqual("2026-06-02", p.lastPlayedDateIso, "bridged to yesterday, not today");
        Assert.AreEqual("2026-06-03", p.lastRepairDateIso);
        Assert.IsFalse(p.todayPlayed, "repair does NOT auto-play today (Q3)");

        // Playing today then continues the bridged streak.
        DailyStreakRules.ApplyPlayed(p, "2026-06-03", true);
        Assert.AreEqual(5, p.currentStreak);
    }

    [Test]
    public void Repair_NoOpWhenNotRepairable()
    {
        var p = new DailyProgress { lastPlayedDateIso = "2026-05-31", currentStreak = 3, longestStreak = 3 };
        DailyStreakRules.ApplyRepair(p, "2026-06-03", 7); // 2-day gap → not repairable

        Assert.AreEqual(3, p.currentStreak, "no bridge applied");
        Assert.AreEqual("2026-05-31", p.lastPlayedDateIso, "unchanged");
        Assert.AreEqual("", p.lastRepairDateIso);
    }

    [Test]
    public void RefreshPlayedFlag_TogglesOnDateChange()
    {
        var p = new DailyProgress { lastPlayedDateIso = "2026-06-03" };
        DailyStreakRules.RefreshPlayedFlag(p, "2026-06-03");
        Assert.IsTrue(p.todayPlayed);
        DailyStreakRules.RefreshPlayedFlag(p, "2026-06-04");
        Assert.IsFalse(p.todayPlayed);
    }
}
