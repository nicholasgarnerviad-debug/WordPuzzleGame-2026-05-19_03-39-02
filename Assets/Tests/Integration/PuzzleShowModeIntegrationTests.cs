using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Modes;

[TestFixture]
public class PuzzleShowModeIntegrationTests
{
    private PuzzleShowMode mode;
    private GameStateManager stateManager;
    private WordPuzzle testPuzzle;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mode = new PuzzleShowMode();
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        stateManager = new GameStateManager(mockValidator, mockDataManager);
        testPuzzle = new WordPuzzle(
            id: 2,
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
    public void StartGame_LoadsSolutionPath()
    {
        mode.StartGame(testPuzzle);

        var state = stateManager.GetCurrentState();
        Assert.IsNotNull(state);
        Assert.AreEqual("cat", state.puzzle.startWord);
    }

    [Test]
    public void HandleWordSubmission_CorrectSolutionWord_Accepted()
    {
        mode.StartGame(testPuzzle);
        mockValidator.SetValidResult(true, true);
        var stateBefore = stateManager.GetCurrentState();

        // PuzzleShowMode expects solution[0] first ("cat"), which matches start word.
        // Skip past it by submitting the start word, then test "bat" (solution[1]).
        mode.HandleWordSubmission("cat");
        mode.HandleWordSubmission("bat");

        var stateAfter = stateManager.GetCurrentState();
        Assert.Greater(stateAfter.wordChain.Count, stateBefore.wordChain.Count);
    }

    [Test]
    public void HandleWordSubmission_WrongWord_Rejected()
    {
        mode.StartGame(testPuzzle);
        var stateBefore = stateManager.GetCurrentState();

        mode.HandleWordSubmission("wrong");

        var stateAfter = stateManager.GetCurrentState();
        Assert.AreEqual(stateBefore.wordChain.Count, stateAfter.wordChain.Count);
    }

    [Test]
    public void GetStats_ReturnsValidGameModeStats()
    {
        mode.StartGame(testPuzzle);

        var stats = mode.GetStats();

        Assert.AreEqual("Puzzle Show", stats.modeName);
        Assert.GreaterOrEqual(stats.accuracy, 0f);
    }
}
