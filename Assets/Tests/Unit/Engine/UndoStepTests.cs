using NUnit.Framework;
using WordPuzzle.State;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

// Task 9B — UndoStepAction: single authoritative chain-rewind path.
// Mirrors GameStateManagerTests setup (real GameStateManager + Mock validator/data).
[TestFixture]
public class UndoStepTests
{
    private GameStateManager manager;
    private MockWordValidator mockValidator;
    private MockDataManager mockDataManager;

    [SetUp]
    public void Setup()
    {
        mockValidator = new MockWordValidator();
        mockDataManager = new MockDataManager();
        manager = new GameStateManager(mockValidator, mockDataManager);
    }

    private PuzzleType MakePuzzle() =>
        new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);

    [Test]
    public void Undo_AfterTwoValidWords_RewindsChainToTwoWords()
    {
        manager.StartNewPuzzle(MakePuzzle());
        mockValidator.SetValidResult(true, true);

        // cat -> bat -> bag (3-word chain)
        manager.Dispatch(new SubmitWordAction("bat"));
        manager.Dispatch(new SubmitWordAction("bag"));
        Assert.AreEqual(3, manager.GetCurrentState().wordChain.Count, "Precondition: 3-word chain.");

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(2, state.wordChain.Count, "Undo must drop the chain back to 2 words.");
        Assert.AreEqual("cat", state.wordChain[0]);
        Assert.AreEqual("bat", state.wordChain[1]);
    }

    [Test]
    public void Undo_DecreasesScoreByUndoneWordLength()
    {
        manager.StartNewPuzzle(MakePuzzle());
        mockValidator.SetValidResult(true, true);

        manager.Dispatch(new SubmitWordAction("bat")); // +3
        manager.Dispatch(new SubmitWordAction("bag")); // +3  => score 6
        Assert.AreEqual(6, manager.GetCurrentScore(), "Precondition: score is 6 (bat+bag).");

        manager.Dispatch(new UndoStepAction()); // remove "bag" (len 3)

        Assert.AreEqual(3, manager.GetCurrentScore(),
            "Undo must subtract the undone word's length (3) from the score.");
    }

    [Test]
    public void Undo_DecrementsCurrentStreak()
    {
        manager.StartNewPuzzle(MakePuzzle());
        mockValidator.SetValidResult(true, true);

        manager.Dispatch(new SubmitWordAction("bat"));
        manager.Dispatch(new SubmitWordAction("bag"));
        Assert.AreEqual(2, manager.GetCurrentStreak(), "Precondition: streak 2.");

        manager.Dispatch(new UndoStepAction());

        Assert.AreEqual(1, manager.GetCurrentStreak(),
            "Undo decrements currentStreak by one.");
    }

    [Test]
    public void Undo_ClearsHintAndRevealPreview()
    {
        manager.StartNewPuzzle(MakePuzzle());
        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("bat"));

        // Task 31 — Hint and Reveal are now independent: spend a HINT to populate hintLetterIndex and
        // a REVEAL to populate revealedNextWord (Reveal no longer sets the hint index itself).
        manager.Dispatch(new UseHintAction(0));
        manager.Dispatch(new UseRevealAction());
        Assert.AreNotEqual(-1, manager.GetCurrentState().hintLetterIndex,
            "Precondition: hint populated a hint index.");
        Assert.AreNotEqual(string.Empty, manager.GetCurrentState().revealedNextWord,
            "Precondition: reveal populated the next word.");

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(-1, state.hintLetterIndex, "Undo must reset hintLetterIndex to -1.");
        Assert.AreEqual(string.Empty, state.revealedNextWord, "Undo must clear revealedNextWord.");
    }

    [Test]
    public void RepeatedUndo_ToStartWord_NeverYieldsNegativeScore()
    {
        manager.StartNewPuzzle(MakePuzzle());
        mockValidator.SetValidResult(true, true);

        manager.Dispatch(new SubmitWordAction("bat"));
        manager.Dispatch(new SubmitWordAction("bag"));

        // Undo more times than there are words to undo; surplus undos must be no-ops
        // (chain count <= 1 guard) and score must never go negative.
        for (int i = 0; i < 5; i++)
        {
            manager.Dispatch(new UndoStepAction());
            Assert.GreaterOrEqual(manager.GetCurrentScore(), 0,
                "Score must never go negative across repeated undos.");
            Assert.GreaterOrEqual(manager.GetCurrentStreak(), 0,
                "Streak must never go negative across repeated undos.");
        }

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.wordChain.Count, "Chain floors at the start word.");
        Assert.AreEqual("cat", state.wordChain[0]);
        Assert.AreEqual(0, state.score, "Back at the start word, score is 0.");
        Assert.AreEqual(0, manager.GetCurrentStreak(), "Back at the start word, streak is 0.");
    }
}
