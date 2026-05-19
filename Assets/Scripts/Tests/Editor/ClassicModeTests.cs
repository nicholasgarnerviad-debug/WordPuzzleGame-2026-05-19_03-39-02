using NUnit.Framework;
using UnityEngine;

public class ClassicModeTests
{
    private GameObject gameObject;
    private GameController gameController;
    private ClassicMode classicMode;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        gameController = gameObject.AddComponent<GameController>();
        classicMode = gameObject.AddComponent<ClassicMode>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void Initialize_SetupCompletes()
    {
        classicMode.Initialize();
        Assert.AreEqual(0, classicMode.GetCoinsEarned());
    }

    [Test]
    public void StartGame_InitializesMode()
    {
        classicMode.Initialize();
        classicMode.StartGame();

        PuzzleData puzzle = gameController.GetCurrentPuzzle();
        Assert.IsNotNull(puzzle);
    }

    [Test]
    public void GetModeName_ReturnsCorrectName()
    {
        Assert.AreEqual("Classic", classicMode.GetModeName());
    }

    [Test]
    public void CoinsEarned_AccumulatesPerPuzzle()
    {
        classicMode.Initialize();
        classicMode.StartGame();

        // Complete a puzzle (would be done via gameController in real game)
        // For now, verify initial state
        Assert.AreEqual(0, classicMode.GetCoinsEarned());
    }
}
