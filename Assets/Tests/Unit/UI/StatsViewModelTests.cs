using NUnit.Framework;
using WordPuzzle.UI;
using WordPuzzle.Persistence;

// Task 9F — StatsScreen.BuildStatsViewModel is a pure, null-safe factory that
// derives display numbers from DailyProgress + PlayerProgress.
[TestFixture]
public class StatsViewModelTests
{
    [Test]
    public void Build_FromFullData_PassesThroughAndCountsDates()
    {
        var daily = new DailyProgress
        {
            currentStreak = 4,
            longestStreak = 9,
        };
        daily.completedDates.Add("2026-05-28");
        daily.completedDates.Add("2026-05-29");
        daily.completedDates.Add("2026-05-30");

        var player = new PlayerProgress
        {
            totalCoins = 250,
            totalPuzzlesCompleted = 12,
        };
        player.classicStats.gamesPlayed = 7;
        player.classicStats.gamesWon = 5;
        player.timeAttackStats.gamesPlayed = 3;
        player.timeAttackStats.bestRoundReached = 8;

        var vm = StatsScreen.BuildStatsViewModel(daily, player);

        Assert.AreEqual(3, vm.dailyCompleted, "dailyCompleted == completedDates.Count");
        Assert.AreEqual(4, vm.currentStreak, "currentStreak passthrough");
        Assert.AreEqual(9, vm.longestStreak, "longestStreak passthrough");

        Assert.AreEqual(250, vm.totalCoins, "totalCoins passthrough");
        Assert.AreEqual(12, vm.totalPuzzlesCompleted, "totalPuzzlesCompleted passthrough");

        Assert.AreEqual(7, vm.classicGamesPlayed, "classic gamesPlayed passthrough");
        Assert.AreEqual(5, vm.classicGamesWon, "classic gamesWon passthrough");

        Assert.AreEqual(3, vm.timeAttackGamesPlayed, "timeAttack gamesPlayed passthrough");
        Assert.AreEqual(8, vm.timeAttackBestRound, "timeAttack bestRound passthrough");
    }

    [Test]
    public void Build_WithBothNull_ReturnsZeroedViewModel_NoThrow()
    {
        StatsViewModel vm = default;
        Assert.DoesNotThrow(() => { vm = StatsScreen.BuildStatsViewModel(null, null); });

        Assert.AreEqual(0, vm.dailyCompleted);
        Assert.AreEqual(0, vm.currentStreak);
        Assert.AreEqual(0, vm.longestStreak);
        Assert.AreEqual(0, vm.totalCoins);
        Assert.AreEqual(0, vm.totalPuzzlesCompleted);
        Assert.AreEqual(0, vm.classicGamesPlayed);
        Assert.AreEqual(0, vm.classicGamesWon);
    }

    [Test]
    public void Build_WithNullDaily_OnlyPlayerFieldsPopulated()
    {
        var player = new PlayerProgress { totalCoins = 99 };
        player.classicStats.gamesWon = 2;

        var vm = StatsScreen.BuildStatsViewModel(null, player);

        Assert.AreEqual(0, vm.currentStreak);
        Assert.AreEqual(0, vm.dailyCompleted);
        Assert.AreEqual(99, vm.totalCoins);
        Assert.AreEqual(2, vm.classicGamesWon);
    }

    [Test]
    public void Build_WithEmptyCompletedDates_DailyCompletedIsZero()
    {
        var daily = new DailyProgress { currentStreak = 1 }; // completedDates is empty list
        var vm = StatsScreen.BuildStatsViewModel(daily, null);

        Assert.AreEqual(0, vm.dailyCompleted);
        Assert.AreEqual(1, vm.currentStreak);
    }
}
