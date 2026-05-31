using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Modes;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

/// <summary>
/// ACCEPTANCE 5B — Budgets and costs come from the centralized BalanceConfig,
/// not from scattered literals.
///
/// 1. Starting a puzzle through GameStateManager seeds hint/reveal budgets from
///    BalanceConfig.DefaultHintsPerPuzzle / DefaultRevealsPerPuzzle (proves the seed
///    reads config, not the old literal 2/1).
/// 2. RevealCost > HintCost (Reveal is the premium power-up).
/// 3. PuzzleShowMode's tier constants forward from BalanceConfig.
/// </summary>
[TestFixture]
public class BalanceConfigWiringTests
{
    private GameStateManager manager;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        manager = new GameStateManager(mockValidator, mockDataManager);
    }

    [Test]
    public void StartNewPuzzle_SeedsPowerUpBudgetsFromBalanceConfig()
    {
        // Known all-common ladder: cat -> cot -> cog -> dog.
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "cot", "cog", "dog" }, 0, Diff.Easy);

        manager.StartNewPuzzle(puzzle);
        var state = manager.GetCurrentState();

        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle, state.hintsRemaining,
            "Seeded hint budget must come from BalanceConfig.DefaultHintsPerPuzzle, not a literal.");
        Assert.AreEqual(BalanceConfig.DefaultRevealsPerPuzzle, state.revealsRemaining,
            "Seeded reveal budget must come from BalanceConfig.DefaultRevealsPerPuzzle, not a literal.");
    }

    [Test]
    public void RevealCost_IsGreaterThan_HintCost()
    {
        Assert.Greater(BalanceConfig.RevealCost, BalanceConfig.HintCost,
            "Reveal is the premium power-up: RevealCost must stay greater than HintCost.");
    }

    [Test]
    public void PuzzleShowMode_TierConstants_ForwardFromBalanceConfig()
    {
        Assert.AreEqual(BalanceConfig.MaxTier, PuzzleShowMode.MaxTier,
            "PuzzleShowMode.MaxTier must forward from BalanceConfig.MaxTier.");

        Assert.AreEqual(BalanceConfig.PuzzlesRequiredToAdvanceTier,
            PuzzleShowMode.PuzzlesRequiredToAdvanceTier,
            "PuzzleShowMode.PuzzlesRequiredToAdvanceTier must forward from BalanceConfig.");

        // Instance-level alias must also resolve to the same config value.
        var mode = new PuzzleShowMode();
        Assert.AreEqual(BalanceConfig.PuzzlesRequiredToAdvanceTier, mode.PuzzlesRequiredThisTier,
            "PuzzleShowMode.PuzzlesRequiredThisTier must equal BalanceConfig.PuzzlesRequiredToAdvanceTier.");
    }
}
