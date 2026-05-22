using NUnit.Framework;
using System.Threading.Tasks;
using WordPuzzle.Persistence;

public class DataManagerTests
{
    private DataManager dataManager;

    [SetUp]
    public void Setup()
    {
        dataManager = new DataManager();
    }

    [Test]
    public async Task SaveAndLoadGameState_PreservesAllData()
    {
        // Arrange
        var snapshot = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 1,
            wordChain = new[] { "cat", "bat", "hat" },
            currentInput = "mat",
            lives = 2,
            hintsUsed = 1,
            revealsUsed = 0,
            undosUsed = 0,
            timestamp = System.DateTime.UtcNow.Ticks,
            sessionId = "test-session"
        };

        // Act
        await dataManager.SaveGameStateAsync(snapshot);
        var loaded = await dataManager.LoadGameStateAsync();

        // Assert
        Assert.AreEqual(snapshot.currentMode, loaded.currentMode);
        Assert.AreEqual(snapshot.currentPuzzleId, loaded.currentPuzzleId);
        Assert.AreEqual(snapshot.wordChain.Length, loaded.wordChain.Length);
        Assert.AreEqual(snapshot.currentInput, loaded.currentInput);
        Assert.AreEqual(snapshot.lives, loaded.lives);
    }

    [Test]
    public async Task UpdateAndGetPlayerProgress_PreservesData()
    {
        // Arrange
        var progress = new PlayerProgress
        {
            totalCoins = 100,
            totalPuzzlesCompleted = 5,
            highestTierUnlocked = 2,
            totalHintsEarned = 10
        };

        // Act
        await dataManager.UpdatePlayerProgressAsync(progress);
        var loaded = await dataManager.GetPlayerProgressAsync();

        // Assert
        Assert.AreEqual(100, loaded.totalCoins);
        Assert.AreEqual(5, loaded.totalPuzzlesCompleted);
        Assert.AreEqual(2, loaded.highestTierUnlocked);
    }
}
