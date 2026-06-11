using System;
using NUnit.Framework;
using WordPuzzle;

/// <summary>
/// Task 41C — pure scheduling rules: played/unplayed × before/after hour × toggle.
/// Hour tunable = BalanceConfig.ReminderHourLocal (19:00, user-confirmed).
/// </summary>
[TestFixture]
public class NotificationRulesTests
{
    private static readonly int Hour = BalanceConfig.ReminderHourLocal;

    private static DateTime At(int hour, int minute = 0)
        => new DateTime(2026, 6, 9, hour, minute, 0, DateTimeKind.Local);

    // ── ShouldSchedule: the toggle is the only gate ──

    [Test]
    public void Disabled_NeverSchedules_RegardlessOfPlayState()
    {
        Assert.IsFalse(NotificationRules.ShouldSchedule(todayPlayed: false, notificationsEnabled: false));
        Assert.IsFalse(NotificationRules.ShouldSchedule(todayPlayed: true, notificationsEnabled: false));
    }

    [Test]
    public void Enabled_Schedules_PlayedOrNot()
    {
        // todayPlayed shifts WHEN (NextFireLocal), never WHETHER.
        Assert.IsTrue(NotificationRules.ShouldSchedule(todayPlayed: false, notificationsEnabled: true));
        Assert.IsTrue(NotificationRules.ShouldSchedule(todayPlayed: true, notificationsEnabled: true));
    }

    // ── NextFireLocal: played/unplayed × before/after hour ──

    [Test]
    public void Unplayed_BeforeHour_FiresTodayAtHour()
    {
        var fire = NotificationRules.NextFireLocal(At(Hour - 5), Hour, todayPlayed: false);
        Assert.AreEqual(At(Hour), fire);
    }

    [Test]
    public void Unplayed_AfterHour_FiresTomorrowAtHour()
    {
        var fire = NotificationRules.NextFireLocal(At(Hour + 1), Hour, todayPlayed: false);
        Assert.AreEqual(At(Hour).AddDays(1), fire);
    }

    [Test]
    public void Played_BeforeHour_FiresTomorrowAtHour()
    {
        // Already played today: do NOT nag tonight — tomorrow's daily is the next target.
        var fire = NotificationRules.NextFireLocal(At(Hour - 5), Hour, todayPlayed: true);
        Assert.AreEqual(At(Hour).AddDays(1), fire);
    }

    [Test]
    public void Played_AfterHour_FiresTomorrowAtHour()
    {
        var fire = NotificationRules.NextFireLocal(At(Hour + 2), Hour, todayPlayed: true);
        Assert.AreEqual(At(Hour).AddDays(1), fire);
    }

    [Test]
    public void ExactlyAtHour_CountsAsPassed_FiresTomorrow()
    {
        var fire = NotificationRules.NextFireLocal(At(Hour), Hour, todayPlayed: false);
        Assert.AreEqual(At(Hour).AddDays(1), fire);
    }

    [Test]
    public void FireTime_HasZeroMinutesAndSeconds()
    {
        var fire = NotificationRules.NextFireLocal(At(Hour - 3, minute: 47), Hour, todayPlayed: false);
        Assert.AreEqual(0, fire.Minute);
        Assert.AreEqual(0, fire.Second);
        Assert.AreEqual(Hour, fire.Hour);
    }

    // ── Body copy ──

    [Test]
    public void Body_WithStreak_IncludesStreakCount()
    {
        StringAssert.Contains("streak: 6", NotificationRules.Body(6));
    }

    [Test]
    public void Body_NewPlayer_OmitsStreakEntirely()
    {
        Assert.AreEqual("Today's ladder is ready", NotificationRules.Body(0));
    }
}
