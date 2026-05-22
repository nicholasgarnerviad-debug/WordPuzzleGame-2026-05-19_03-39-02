using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.Persistence;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

[TestFixture]
public class ModeControllerTest
{
    private ModeController controller;
    private GameStateManager stateManager;
    private PuzzleType testPuzzle;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        stateManager = new GameStateManager(mockValidator, mockDataManager);
        controller = new ModeController(stateManager);
        testPuzzle = new PuzzleType(
            id: 4,
            start: "cat",
            end: "dog",
            optimal: 3,
            solutionPath: new[] { "cat", "bat", "bad", "dog" },
            seed: 12345,
            diff: Diff.Easy
        );
    }

    [Test]
    public void SetMode_SetsActiveMode()
    {
        var classicMode = new ClassicMode();
        controller.SetMode(classicMode);

        Assert.AreEqual(classicMode, controller.GetActiveMode());
    }

    [Test]
    public void StartGame_DelegateToActiveMode()
    {
        var classicMode = new ClassicMode();
        controller.SetMode(classicMode);

        controller.StartGame(testPuzzle);

        var state = stateManager.GetCurrentState();
        Assert.IsNotNull(state);
    }

    [Test]
    public void SwitchMode_CallsResetOnPreviousMode()
    {
        var classicMode = new ClassicMode();
        controller.SetMode(classicMode);

        var timeAttackMode = new TimeAttackMode();
        controller.SetMode(timeAttackMode);

        Assert.AreEqual(timeAttackMode, controller.GetActiveMode());
    }
}
