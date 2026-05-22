using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Modes;

[TestFixture]
public class TimeAttackModeIntegrationTests
{
    private TimeAttackMode mode;
    private GameStateManager stateManager;
    private WordPuzzle testPuzzle;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mode = new TimeAttackMode();
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        stateManager = new GameStateManager(mockValidator, mockDataManager);
        testPuzzle = new WordPuzzle(
            id: 3,
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
    public void StartGame_InitializesTimer()
    {
        mode.StartGame(testPuzzle);

        var state = stateManager.GetCurrentState();
        Assert.IsNotNull(state);
        Assert.AreEqual(0f, state.elapsedTime);
    }

    [Test]
    public void Tick_IncrementsElapsedTime()
    {
        mode.StartGame(testPuzzle);

        mode.Tick(5.0f);

        var state = stateManager.GetCurrentState();
        Assert.AreEqual(5.0f, state.elapsedTime, 0.01f);
    }

    [Test]
    public void HandleWordSubmission_ValidWord_BeforeTimeout()
    {
        mode.StartGame(testPuzzle);
        mockValidator.SetValidResult(true, true);

        mode.HandleWordSubmission("bat");

        var state = stateManager.GetCurrentState();
        Assert.Greater(state.wordChain.Count, 1);
    }

    [Test]
    public void Tick_ExceedingTimeLimit_StopsAcceptingInput()
    {
        mode.StartGame(testPuzzle);
        mockValidator.SetValidResult(true, true);

        // Advance time past 60 seconds
        for (int i = 0; i < 61; i++)
        {
            mode.Tick(1.0f);
        }

        var stateBefore = stateManager.GetCurrentState();
        mode.HandleWordSubmission("bat");
        var stateAfter = stateManager.GetCurrentState();

        Assert.AreEqual(stateBefore.wordChain.Count, stateAfter.wordChain.Count);
    }

    [Test]
    public void GetStats_ReturnsValidGameModeStats()
    {
        mode.StartGame(testPuzzle);
        mode.Tick(10.0f);

        var stats = mode.GetStats();

        Assert.AreEqual("Time Attack", stats.modeName);
        Assert.AreEqual(10.0f, stats.totalTime, 0.01f);
    }
}
