using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class ModeControllerTest
{
    private ModeController modeController;
    private MockGameModeContext mockContext;

    [SetUp]
    public void Setup()
    {
        var gameObject = new GameObject();
        modeController = gameObject.AddComponent<ModeController>();

        mockContext = new MockGameModeContext();

        // Initialize the controller with mock context
        modeController.Initialize(
            mockContext.dataManager,
            mockContext.economy,
            mockContext.puzzleGenerator,
            mockContext.stateManager,
            mockContext.wordValidator
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (modeController != null && modeController.gameObject != null)
        {
            UnityEngine.Object.Destroy(modeController.gameObject);
        }
    }

    [Test]
    public void SwitchMode_InitializesNewMode()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();

        // Act
        modeController.SwitchMode(ModeType.Classic);

        // Assert
        Assert.AreEqual(ModeType.Classic, modeController.GetCurrentMode());
    }

    [Test]
    public void SwitchMode_FromClassicToPuzzleShow_SwitchesSuccessfully()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        modeController.SwitchMode(ModeType.Classic);

        // Act
        modeController.SwitchMode(ModeType.PuzzleShow);

        // Assert
        Assert.AreEqual(ModeType.PuzzleShow, modeController.GetCurrentMode());
    }

    [Test]
    public void SwitchMode_TracksLastModeBeforeSwitching()
    {
        // Arrange
        mockContext.SetupPuzzleGenerator();
        modeController.SwitchMode(ModeType.Classic);

        // Act
        modeController.SwitchMode(ModeType.TimeAttack);

        // Assert
        Assert.AreEqual(ModeType.TimeAttack, modeController.LastMode);
    }
}
