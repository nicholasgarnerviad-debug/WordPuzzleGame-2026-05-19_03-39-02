using NUnit.Framework;
using WordPuzzle.State;
using WordPuzzle.Puzzle;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

// Task 9C — failed submissions surface a user-facing string derived from the
// validator's TYPED WordRejectReason enum, NOT from its free-text Message.
[TestFixture]
public class SubmissionRejectReasonTests
{
    private GameStateManager manager;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;
    private SubmissionResult? lastResult;

    [SetUp]
    public void Setup()
    {
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        manager = new GameStateManager(mockValidator, mockDataManager);
        lastResult = null;
        manager.OnWordSubmissionResult += r => lastResult = r;
    }

    private void StartPuzzle()
    {
        // Start word "cat" is 3 letters; rejected words below are ALSO 3 letters so the
        // length short-circuit in HandleSubmitWord does not fire before the validator runs.
        manager.StartNewPuzzle(new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy));
    }

    [Test]
    public void Reject_NotInDictionary_YieldsNotARealWord()
    {
        StartPuzzle();
        mockValidator.SetRejection(WordRejectReason.NotInDictionary, "validator-internal-noise");

        manager.Dispatch(new SubmitWordAction("zzz"));

        Assert.IsTrue(lastResult.HasValue, "OnWordSubmissionResult must fire on a rejected submit.");
        Assert.IsFalse(lastResult.Value.accepted);
        Assert.AreEqual("Not a real word", lastResult.Value.reason);
        Assert.AreEqual(SubmissionRejectReason.NotInDictionary, lastResult.Value.rejectReason);
    }

    [Test]
    public void Reject_AlreadyUsed_YieldsAlreadyUsed()
    {
        StartPuzzle();
        mockValidator.SetRejection(WordRejectReason.AlreadyUsed, "garbage message");

        manager.Dispatch(new SubmitWordAction("bat"));

        Assert.IsTrue(lastResult.HasValue);
        Assert.IsFalse(lastResult.Value.accepted);
        Assert.AreEqual("Already used", lastResult.Value.reason);
        Assert.AreEqual(SubmissionRejectReason.AlreadyUsed, lastResult.Value.rejectReason);
    }

    [Test]
    public void Reject_NotOneLetterDifferent_YieldsChangeExactlyOneLetter()
    {
        StartPuzzle();
        mockValidator.SetRejection(WordRejectReason.NotOneLetterDifferent, "nonsense");

        manager.Dispatch(new SubmitWordAction("pig"));

        Assert.IsTrue(lastResult.HasValue);
        Assert.IsFalse(lastResult.Value.accepted);
        Assert.AreEqual("Change exactly one letter", lastResult.Value.reason);
        Assert.AreEqual(SubmissionRejectReason.NotOneLetterDifferent, lastResult.Value.rejectReason);
    }

    [Test]
    public void Reject_UserText_DerivesFromEnum_NotFromMessage()
    {
        // KEY ASSERTION: change ONLY the validator's free-text Message to a nonsense string
        // while keeping the correct enum. The user-facing text must STILL be the enum-derived
        // string — proving the path does not echo Message.
        StartPuzzle();
        mockValidator.SetRejection(WordRejectReason.NotInDictionary,
            "THIS-MESSAGE-MUST-NOT-APPEAR-IN-USER-TEXT");

        manager.Dispatch(new SubmitWordAction("zzz"));

        Assert.IsTrue(lastResult.HasValue);
        Assert.AreEqual("Not a real word", lastResult.Value.reason,
            "User text must come from RejectReason, not the validator Message.");
        Assert.AreNotEqual("THIS-MESSAGE-MUST-NOT-APPEAR-IN-USER-TEXT", lastResult.Value.reason);
    }
}
