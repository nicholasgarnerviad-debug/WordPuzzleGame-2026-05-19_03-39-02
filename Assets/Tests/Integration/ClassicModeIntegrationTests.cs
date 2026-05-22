using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Modes;

[TestFixture]
public class ClassicModeIntegrationTests
{
    private ClassicMode mode;
    private GameStateManager stateManager;
    private WordPuzzle testPuzzle;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mode = new ClassicMode();
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        stateManager = new GameStateManager(mockValidator, mockDataManager);
        testPuzzle = new WordPuzzle(
            id: 1,
            start: "cat",
            end: "dog",
            optimal: 3,
            solutionPath: new[] { "cat", "bat", "bad", "dog" },
            seed: 12345,
            diff: Difficulty.Easy
        );

        mode.Initialize(stateManager);
    }

    [Test]
    public void StartGame_LoadsFirstPuzzle()
    {
        mode.StartGame(testPuzzle);

        var state = stateManager.GetCurrentState();
        Assert.IsNotNull(state);
        Assert.AreEqual("cat", state.puzzle.startWord);
    }

    [Test]
    public void HandleWordSubmission_ValidWord_IncreaseChainLength()
    {
        mode.StartGame(testPuzzle);
        mockValidator.SetValidResult(true, true);
        var stateBefore = stateManager.GetCurrentState();

        mode.HandleWordSubmission("bat");

        var stateAfter = stateManager.GetCurrentState();
        Assert.Greater(stateAfter.wordChain.Count, stateBefore.wordChain.Count);
    }

    [Test]
    public void HandleWordSubmission_InvalidWord_CountsAsFailure()
    {
        mode.StartGame(testPuzzle);
        mockValidator.SetValidResult(false, false);
        var stateBefore = stateManager.GetCurrentState();

        mode.HandleWordSubmission("xyz");

        var stateAfter = stateManager.GetCurrentState();
        Assert.AreEqual(stateBefore.wordChain.Count, stateAfter.wordChain.Count);
    }

    [Test]
    public void GetStats_ReturnsValidGameModeStats()
    {
        mode.StartGame(testPuzzle);

        var stats = mode.GetStats();

        Assert.AreEqual("Classic", stats.modeName);
        Assert.GreaterOrEqual(stats.score, 0);
        Assert.GreaterOrEqual(stats.wordsFound, 0);
    }
}
