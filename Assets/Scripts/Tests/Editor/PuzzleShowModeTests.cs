using NUnit.Framework;
using UnityEngine;

public class PuzzleShowModeTests
{
    private GameObject gameObject;
    private GameController gameController;
    private PuzzleShowMode puzzleShowMode;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        gameController = gameObject.AddComponent<GameController>();
        puzzleShowMode = gameObject.AddComponent<PuzzleShowMode>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void Initialize_SetupCompletes()
    {
        puzzleShowMode.Initialize();
        Assert.AreEqual(0, puzzleShowMode.GetCoinsEarned());
    }

    [Test]
    public void StartGame_LoadsFirstTier()
    {
        puzzleShowMode.Initialize();
        puzzleShowMode.StartGame();

        PuzzleData puzzle = gameController.GetCurrentPuzzle();
        Assert.IsNotNull(puzzle);
    }

    [Test]
    public void GetModeName_ReturnsCorrectName()
    {
        Assert.AreEqual("Puzzle Show", puzzleShowMode.GetModeName());
    }

    [Test]
    public void CoinsEarned_MatchesRewardPerPuzzle()
    {
        puzzleShowMode.Initialize();
        Assert.AreEqual(Constants.PUZZLE_SHOW_COIN_REWARD, puzzleShowMode.GetCoinsEarned());
    }
}
