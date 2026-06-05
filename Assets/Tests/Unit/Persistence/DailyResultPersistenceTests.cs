using NUnit.Framework;
using UnityEngine;
using WordPuzzle.Persistence;

// Task 38 (#2) — the stored daily result (used to RE-SHOW the result when a played daily is re-tapped)
// must persist through JsonUtility (daily_v1 is direct-serialized) AND default safely on a pre-Task-38
// save, so an old save never falsely claims a stored result.
[TestFixture]
public class DailyResultPersistenceTests
{
    [Test]
    public void ResultFields_RoundTripThroughJson()
    {
        var p = new DailyProgress
        {
            todayResultValid = true, todayResultStars = 2, todayResultPar = 4,
            todayResultPlayerSteps = 6, todayResultFailed = false
        };
        var loaded = JsonUtility.FromJson<DailyProgress>(JsonUtility.ToJson(p));

        Assert.IsTrue(loaded.todayResultValid);
        Assert.AreEqual(2, loaded.todayResultStars);
        Assert.AreEqual(4, loaded.todayResultPar);
        Assert.AreEqual(6, loaded.todayResultPlayerSteps);
        Assert.IsFalse(loaded.todayResultFailed);
    }

    [Test]
    public void LegacySave_HasNoStoredResult()
    {
        // A pre-Task-38 save lacks the result fields -> they default to "no result", so the re-show is
        // safely skipped (never a bogus 0-star/par-0 result for a player who already played).
        var legacy = JsonUtility.FromJson<DailyProgress>("{\"currentStreak\":3,\"todayPlayed\":true}");
        Assert.IsFalse(legacy.todayResultValid);
        Assert.IsTrue(legacy.todayPlayed, "sanity: the legacy fields that ARE present still load");
    }
}
