using NUnit.Framework;
using UnityEngine;
using WordPuzzle.Persistence;

/// <summary>
/// Task 36 Phase 2 — daily_v1 persistence. Confirms the new fields survive a JsonUtility round-trip
/// and that the Q6 forward migration (Normalize) keeps an old save's streak intact while seeding the
/// played-date from the completion-date. JsonUtility is used directly (no DataManager/PlayerPrefs).
/// </summary>
[TestFixture]
public class DailyProgressMigrationTests
{
    [Test]
    public void RoundTrip_PreservesStreakRecordAndRepair()
    {
        var p = new DailyProgress
        {
            currentStreak = 4,
            longestStreak = 9,
            lastPlayedDateIso = "2026-06-04",
            lastRepairDateIso = "2026-05-28",
            todayPlayed = true,
        };
        p.outcomes.Add(new DayOutcome { dateIso = "2026-06-03", won = false });
        p.outcomes.Add(new DayOutcome { dateIso = "2026-06-04", won = true });

        string json = JsonUtility.ToJson(p);
        var loaded = JsonUtility.FromJson<DailyProgress>(json);
        loaded.Normalize();

        Assert.AreEqual(4, loaded.currentStreak);
        Assert.AreEqual(9, loaded.longestStreak);
        Assert.AreEqual("2026-06-04", loaded.lastPlayedDateIso);
        Assert.AreEqual("2026-05-28", loaded.lastRepairDateIso);
        Assert.AreEqual(2, loaded.outcomes.Count, "the W/L ledger round-trips");
        Assert.AreEqual("2026-06-03", loaded.outcomes[0].dateIso);
        Assert.IsFalse(loaded.outcomes[0].won);
        Assert.IsTrue(loaded.outcomes[1].won);
    }

    [Test]
    public void OldSave_LoadsStreakIntact_AndSeedsPlayedDateFromCompletion()
    {
        // A pre-2.0 save: only the old completion fields exist in the JSON.
        string oldJson = "{\"lastCompletedDateIso\":\"2026-06-01\",\"currentStreak\":5," +
                         "\"longestStreak\":10,\"completedDates\":[\"2026-06-01\"]," +
                         "\"todayCompleted\":false,\"todayPuzzleIndex\":-1}";

        var loaded = JsonUtility.FromJson<DailyProgress>(oldJson);
        loaded.Normalize();

        Assert.AreEqual(5, loaded.currentStreak, "existing streak preserved (Q6: no recompute/wipe)");
        Assert.AreEqual(10, loaded.longestStreak);
        Assert.AreEqual("2026-06-01", loaded.lastPlayedDateIso, "played-date seeded from completion-date");
        Assert.IsNotNull(loaded.outcomes);
        Assert.AreEqual(0, loaded.outcomes.Count, "the new W/L ledger starts empty");
    }

    [Test]
    public void Normalize_DoesNotOverwriteAnExistingPlayedDate()
    {
        var p = new DailyProgress { lastCompletedDateIso = "2026-06-01", lastPlayedDateIso = "2026-06-03" };
        p.Normalize();
        Assert.AreEqual("2026-06-03", p.lastPlayedDateIso, "seed only when the played-date is empty");
    }
}
