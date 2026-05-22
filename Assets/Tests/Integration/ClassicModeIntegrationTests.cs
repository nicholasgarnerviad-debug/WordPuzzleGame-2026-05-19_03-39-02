using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

public class ClassicModeIntegrationTests
{
    private ClassicMode classicMode;
    private MockGameModeContext mockContext;

    [SetUp]
    public void Setup()
    {
        mockContext = new MockGameModeContext();
        classicMode = new GameObject().AddComponent<ClassicMode>();
        classicMode.Initialize(mockContext);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(classicMode.gameObject);
    }

    [Test]
    public void StartGame_LoadsFirstPuzzle()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();

        // Act
        classicMode.StartGame();

        // Assert
        Assert.IsNotNull(mockContext.lastLoadedPuzzle);
        Assert.AreEqual("cat", mockContext.lastLoadedPuzzle.startWord);
    }

    [Test]
    public void HandleInput_WinPuzzle_LoadsNextPuzzle()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();
        classicMode.StartGame();

        // Act
        // Simulate winning the puzzle
        mockContext.stateManager.SetWonState(true);
        classicMode.HandleInput(new SubmitWordAction("dog"));

        // Assert - verify coins were earned
        Assert.Greater(mockContext.coinsAdded, 0);
    }

    [Test]
    public void HandleWordSubmission_InvalidWord_CountsAsFailure()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();
        classicMode.StartGame();
        var stateBefore = mockContext.stateManager.GetCurrentState();

        // Act
        classicMode.HandleInput(new SubmitWordAction("xyz")); // Invalid word

        // Assert
        var stateAfter = mockContext.stateManager.GetCurrentState();
        Assert.AreEqual(stateBefore.wordChain.Length, stateAfter.wordChain.Length, "Invalid word should not be added");
    }

    [Test]
    public void IsGameOver_WithFailures_ReturnsTrueAfterMaxFailures()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        mockContext.SetupStateManager();
        classicMode.StartGame();

        // Act
        // Submit invalid words to trigger failures
        for (int i = 0; i < 5; i++)
        {
            classicMode.HandleInput(new SubmitWordAction("invalid" + i));
        }

        // Assert
        ModeStats stats = classicMode.GetStats();
        Assert.IsTrue(stats.isGameOver, "Game should be over after max failures");
    }
}
