using NUnit.Framework;
using System.Threading.Tasks;
using WordPuzzle.State;

[TestFixture]
public class EconomyManagerTests
{
    private EconomyManager manager;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockDataManager = new MockDataManager();
        manager = new EconomyManager(mockDataManager);
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod]
    public static void SetupUnity()
    {
        // Unity setup if needed
    }

    [Test]
    public async Task AddCoins_IncrementsBalance()
    {
        // Arrange
        await manager.InitializeAsync();
        int initialCoins = await manager.GetCoinsAsync();

        // Act
        await manager.AddCoinsAsync(50, "TestSource");

        // Assert
        int finalCoins = await manager.GetCoinsAsync();
        Assert.AreEqual(initialCoins + 50, finalCoins);
    }

    [Test]
    public async Task UseHint_DecrementsHints()
    {
        // Arrange
        await manager.InitializeAsync();
        await manager.AddHintsAsync(5, "TestSource");
        int initialHints = await manager.GetHintsAsync();

        // Act
        await manager.UseHintAsync();

        // Assert
        int finalHints = await manager.GetHintsAsync();
        Assert.AreEqual(initialHints - 1, finalHints);
    }
}
