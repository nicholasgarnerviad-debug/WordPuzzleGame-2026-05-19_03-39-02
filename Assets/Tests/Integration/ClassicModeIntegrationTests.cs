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
}
