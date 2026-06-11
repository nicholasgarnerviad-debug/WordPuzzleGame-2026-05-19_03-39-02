using NUnit.Framework;
using WordPuzzle;
using WordPuzzle.Puzzle;

/// <summary>
/// Task 41B — the emission contracts the taxonomy hangs on:
/// • daily_result fires exactly once per completed run; the one-and-done re-tap
///   (ReportDailyReShow — the seam ShowStoredDailyResult calls) emits NOTHING.
/// • puzzle_complete fires once per Classic win-panel cycle.
/// </summary>
[TestFixture]
public class AnalyticsReporterTests
{
    [Test]
    public void DailyResult_FiresExactlyOnce_ReTapFiresNothing()
    {
        var mock = new MockAnalytics();
        var reporter = new AnalyticsReporter(mock);
        var ps = PathScoring.Score(4, 4, 0, 0, ranOutOfMistakes: false, usedPowerUp: false);

        reporter.DailyResult(ps, streak: 3);   // run end — the one emission
        reporter.DailyReShow();                // re-tap #1 (one-and-done lock)
        reporter.DailyReShow();                // re-tap #2

        Assert.AreEqual(1, mock.CountOf("daily_result"),
            "daily_result must fire exactly once per daily; re-shows are silent");
        Assert.AreEqual(1, mock.Events.Count, "the re-show path emits NO event at all");
    }

    [Test]
    public void DailyResult_CarriesTheFullParamSet()
    {
        var mock = new MockAnalytics();
        var reporter = new AnalyticsReporter(mock);
        var ps = PathScoring.Score(5, 6, 1, 2, ranOutOfMistakes: false, usedPowerUp: true);

        reporter.DailyResult(ps, streak: 9);

        var (name, p) = mock.Events[0];
        Assert.AreEqual("daily_result", name);
        var keys = System.Array.ConvertAll(p, x => x.key);
        CollectionAssert.AreEquivalent(
            new[] { "grade", "stars", "par", "steps", "detours", "mistakes", "used_powerup", "streak" },
            keys, "the daily_result param set is the whole taxonomy surface — no PII, nothing extra");
    }

    [Test]
    public void PuzzleComplete_OncePerWinPanelCycle()
    {
        var mock = new MockAnalytics();
        var reporter = new AnalyticsReporter(mock);

        // One Classic win-panel cycle = exactly one emission (EndGame is unreachable for Classic).
        reporter.PuzzleComplete("classic", steps: 4, win: true);

        Assert.AreEqual(1, mock.CountOf("puzzle_complete"));
        var (_, p) = mock.Events[0];
        Assert.AreEqual("classic", System.Array.Find(p, x => x.key == "mode").value);
    }

    [Test]
    public void NullAnalyticsFallback_NeverThrows()
    {
        var reporter = new AnalyticsReporter(null);   // defensive default
        Assert.DoesNotThrow(() => reporter.SessionStart());
        Assert.DoesNotThrow(() => reporter.DailyReShow());
    }
}
