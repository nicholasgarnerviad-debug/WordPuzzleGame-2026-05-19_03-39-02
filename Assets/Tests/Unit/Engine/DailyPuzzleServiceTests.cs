using NUnit.Framework;
using System;
using WordPuzzle.Game;
using WordPuzzle.Persistence;
using WordPuzzle.Puzzle;

[TestFixture]
public class DailyPuzzleServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime Today { get; set; }
        public string TodayIso => Today.ToString("yyyy-MM-dd");
    }

    private static PuzzleDefinition[] Pool(int n)
    {
        var arr = new PuzzleDefinition[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = new PuzzleDefinition
            {
                puzzleId = 1000 + i,
                startWord = "cat",
                endWord = "bag",
                optimalSteps = 2,
                solution = new[] { "cat", "bat", "bag" },
                seedValue = i
            };
        }
        return arr;
    }

    [Test]
    public void SameDate_AcrossInstances_ProducesSamePuzzle()
    {
        var pool = Pool(450);
        var clock = new FixedClock { Today = new DateTime(2025, 6, 15) };

        var a = new DailyPuzzleService(clock, pool);
        var b = new DailyPuzzleService(clock, pool);

        Assert.AreEqual(a.TodayIndex(), b.TodayIndex());
        Assert.AreEqual(a.GetTodayPuzzle().puzzleId, b.GetTodayPuzzle().puzzleId);
    }

    [Test]
    public void Index_IsDeterministicFromEpoch()
    {
        var pool = Pool(450);
        // 100 days after the 2025-01-01 epoch → index 100.
        var clock = new FixedClock { Today = DailyPuzzleService.Epoch.AddDays(100) };
        var svc = new DailyPuzzleService(clock, pool);
        Assert.AreEqual(100, svc.TodayIndex());
        Assert.AreEqual(pool[100].puzzleId, svc.GetTodayPuzzle().puzzleId);
    }

    [Test]
    public void Index_WrapsModPoolCount()
    {
        var pool = Pool(450);
        var clock = new FixedClock { Today = DailyPuzzleService.Epoch.AddDays(450 + 7) };
        var svc = new DailyPuzzleService(clock, pool);
        Assert.AreEqual(7, svc.TodayIndex());
    }

    [Test]
    public void NextDay_AdvancesIndexByOne()
    {
        var pool = Pool(450);
        var clock = new FixedClock { Today = new DateTime(2025, 4, 10) };
        var svc = new DailyPuzzleService(clock, pool);
        int today = svc.TodayIndex();

        clock.Today = clock.Today.AddDays(1);
        int tomorrow = svc.TodayIndex();
        Assert.AreEqual((today + 1) % pool.Length, tomorrow);
    }

    [Test]
    public void PreEpochDate_ClampsToZero()
    {
        var pool = Pool(450);
        var clock = new FixedClock { Today = DailyPuzzleService.Epoch.AddDays(-30) };
        var svc = new DailyPuzzleService(clock, pool);
        Assert.AreEqual(0, svc.TodayIndex());
    }

    [Test]
    public void EmptyPool_TodayIndexNegative()
    {
        var svc = new DailyPuzzleService(
            new FixedClock { Today = DateTime.Today },
            Array.Empty<PuzzleDefinition>());
        Assert.AreEqual(-1, svc.TodayIndex());
        Assert.IsNull(svc.GetTodayPuzzle());
    }
}
