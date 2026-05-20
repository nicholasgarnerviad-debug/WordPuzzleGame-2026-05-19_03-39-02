using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CrossModeEconomyTests
{
    private IEconomyManager economyManager;
    private MockDataManager mockDataManager;

    [SetUp]
    public async Task Setup()
    {
        mockDataManager = new MockDataManager();
        economyManager = new EconomyManager(mockDataManager);
        await economyManager.InitializeAsync();
    }

    [Test]
    public async Task AddCoins_InClassicMode_PersistsToPuzzleShowMode()
    {
        // Arrange
        int coinsToAdd = 50;
        string source = "classic_mode";

        // Act
        await economyManager.AddCoinsAsync(coinsToAdd, source);
        int coinsAfterAdd = await economyManager.GetCoinsAsync();

        // Assert
        Assert.AreEqual(coinsToAdd, coinsAfterAdd, "Coins should persist after AddCoinsAsync");
    }

    [Test]
    public async Task UseHint_InMultipleModes_TracksAcrossModes()
    {
        // Arrange
        int hintsToAdd = 5;
        string source = "puzzle_show";
        await economyManager.AddHintsAsync(hintsToAdd, source);

        // Act
        await economyManager.UseHintAsync();
        await economyManager.UseHintAsync();
        int hintsRemaining = await economyManager.GetHintsAsync();

        // Assert
        Assert.AreEqual(3, hintsRemaining, "Hints remaining should be 3 after using 2 out of 5");
    }

    [Test]
    public async Task EconomyPersists_AcrossManagerInstances()
    {
        // Arrange
        int coinsToAdd = 100;
        string source = "time_attack";
        await economyManager.AddCoinsAsync(coinsToAdd, source);

        // Act
        // Create a new EconomyManager with the same MockDataManager
        IEconomyManager newEconomyManager = new EconomyManager(mockDataManager);
        await newEconomyManager.InitializeAsync();
        int coinsFromNewManager = await newEconomyManager.GetCoinsAsync();

        // Assert
        Assert.AreEqual(coinsToAdd, coinsFromNewManager, "Coins should persist across EconomyManager instances");
    }

    [Test]
    public async Task AddCoins_WithNegativeAmount_IsIgnoredSilently()
    {
        // Arrange
        await economyManager.AddCoinsAsync(100, "test");

        // Act
        await economyManager.AddCoinsAsync(-50, "test");  // Negative add
        var coins = await economyManager.GetCoinsAsync();

        // Assert
        Assert.AreEqual(100, coins);  // Should stay at 100, not go to 50
    }

    [Test]
    public async Task UseHint_WithInsufficientHints_FailsSilently()
    {
        // Arrange - no hints added

        // Act
        await economyManager.UseHintAsync();  // Try to use when hints = 0
        var remaining = await economyManager.GetHintsAsync();

        // Assert
        Assert.AreEqual(0, remaining);  // Should stay at 0
    }

    [Test]
    public async Task UseReveal_WithInsufficientReveals_FailsSilently()
    {
        // Arrange - no reveals added

        // Act
        await economyManager.UseRevealAsync();  // Try to use when reveals = 0
        var remaining = await economyManager.GetRevealsAsync();

        // Assert
        Assert.AreEqual(0, remaining);  // Should stay at 0
    }
}

/// <summary>
/// Mock implementation of IDataManager for testing in isolation.
/// Uses in-memory storage to avoid side effects from PlayerPrefs or other persistence systems.
/// </summary>
public class MockDataManager : IDataManager
{
    private PlayerProgress savedProgress;

    public MockDataManager()
    {
        // Initialize with default PlayerProgress
        savedProgress = new PlayerProgress();
    }

    public Task SaveGameStateAsync(GameStateSnapshot snapshot)
    {
        // Mock implementation - no-op for this test context
        return Task.CompletedTask;
    }

    public Task<GameStateSnapshot> LoadGameStateAsync()
    {
        // Mock implementation - return empty snapshot
        return Task.FromResult(new GameStateSnapshot());
    }

    public Task UpdatePlayerProgressAsync(PlayerProgress progress)
    {
        // Clone to simulate JSON deserialization creating new object instances
        savedProgress = ClonePlayerProgress(progress);
        return Task.CompletedTask;
    }

    public Task<PlayerProgress> GetPlayerProgressAsync()
    {
        // Return a clone to simulate JSON deserialization
        if (savedProgress == null)
            return Task.FromResult<PlayerProgress>(null);

        return Task.FromResult(ClonePlayerProgress(savedProgress));
    }

    private PlayerProgress ClonePlayerProgress(PlayerProgress original)
    {
        if (original == null)
            return null;

        return new PlayerProgress
        {
            totalCoins = original.totalCoins,
            totalPuzzlesCompleted = original.totalPuzzlesCompleted,
            highestTierUnlocked = original.highestTierUnlocked,
            tierProgress = new Dictionary<int, TierProgress>(original.tierProgress),
            totalHintsEarned = original.totalHintsEarned,
            totalRevealsEarned = original.totalRevealsEarned,
            totalUndosEarned = original.totalUndosEarned,
            classicStats = original.classicStats != null
                ? new ClassicModeStats
                {
                    gamesPlayed = original.classicStats.gamesPlayed,
                    gamesWon = original.classicStats.gamesWon,
                    totalCoinsEarned = original.classicStats.totalCoinsEarned,
                    totalPuzzlesCompleted = original.classicStats.totalPuzzlesCompleted
                }
                : null,
            timeAttackStats = original.timeAttackStats != null
                ? new TimeAttackModeStats
                {
                    gamesPlayed = original.timeAttackStats.gamesPlayed,
                    bestRoundReached = original.timeAttackStats.bestRoundReached,
                    totalCoinsEarned = original.timeAttackStats.totalCoinsEarned
                }
                : null
        };
    }

    public Task<TierData> GetTierDataAsync(int tierId)
    {
        // Mock implementation - return empty TierData
        return Task.FromResult(new TierData());
    }

    public Task LoadAllTierDataAsync()
    {
        // Mock implementation - no-op for this test context
        return Task.CompletedTask;
    }
}
