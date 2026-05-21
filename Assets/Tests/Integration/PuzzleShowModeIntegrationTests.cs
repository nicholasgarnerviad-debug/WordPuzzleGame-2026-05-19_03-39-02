using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class PuzzleShowModeIntegrationTests
{
    private PuzzleShowMode puzzleShowMode;
    private PuzzleShowModeMockContext mockContext;

    [SetUp]
    public void Setup()
    {
        mockContext = new PuzzleShowModeMockContext();
        puzzleShowMode = new GameObject().AddComponent<PuzzleShowMode>();
        puzzleShowMode.Initialize(mockContext);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(puzzleShowMode.gameObject);
    }

    [Test]
    public void StartGame_LoadsFirstTier()
    {
        // Arrange
        mockContext.SetupTierData();

        // Act
        puzzleShowMode.StartGame();

        // Assert
        ModeStats stats = puzzleShowMode.GetStats();
        Assert.AreEqual("Puzzle Show", stats.modeName);
    }

    [Test]
    public void CompletePuzzle_ProgressesThroughTier()
    {
        // Arrange
        mockContext.SetupTierData();
        mockContext.SetupStateManager();
        puzzleShowMode.StartGame();

        // Act
        // Simulate winning the puzzle
        mockContext.stateManager.SetWonState(true);
        puzzleShowMode.HandleInput(new SubmitWordAction("word"));

        // Assert
        ModeStats stats = puzzleShowMode.GetStats();
        Assert.AreEqual(1, stats.puzzlesCompleted);
    }
}
