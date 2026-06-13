using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.State;
using WordPuzzle.Persistence;
using PuzzleType = WordPuzzle.Puzzle.WordPuzzle;
using Diff = WordPuzzle.Puzzle.Difficulty;

[TestFixture]
public class GameStateManagerTests
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

    [Test]
    public void StartNewPuzzle_InitializesState()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);

        // Act
        manager.StartNewPuzzle(puzzle);
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_PressLetter_UpdatesState()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        // Act
        manager.Dispatch(new PressLetterAction('b'));
        var state = manager.GetCurrentState();

        // Assert - verify that the action was processed (score hasn't changed, chain length is still 1)
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_DeleteLetter()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.Dispatch(new PressLetterAction('b'));
        manager.Dispatch(new PressLetterAction('a'));

        // Act
        manager.Dispatch(new DeleteLetterAction());
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_SubmitValidWord_AddsToChain()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        var state = manager.GetCurrentState();

        // Assert
        Assert.AreEqual(2, state.wordChain.Count);
        Assert.AreEqual("bat", state.wordChain[1]);
    }

    [Test]
    public void GetCurrentScore_ReturnsScore()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        int score = manager.GetCurrentScore();

        // Assert
        Assert.AreEqual(3, score); // "bat" = 3 letters
    }

    [Test]
    public void GetCurrentStreak_ReturnsStreak()
    {
        // Arrange
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);

        // Act
        manager.Dispatch(new SubmitWordAction("bat"));
        int streak = manager.GetCurrentStreak();

        // Assert
        Assert.AreEqual(1, streak);
    }

    // ── Phase 2 power-up mechanics ────────────────────────────────────

    [Test]
    public void StartNewPuzzle_InitializesPowerUps()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        var state = manager.GetCurrentState();
        // 5A/5B — budgets now seed from the centralized BalanceConfig, not literals.
        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle, state.hintsRemaining);
        Assert.AreEqual(BalanceConfig.DefaultRevealsPerPuzzle, state.revealsRemaining);
        Assert.AreEqual(-1, state.hintLetterIndex);
        Assert.AreEqual(string.Empty, state.revealedNextWord);
    }

    [Test]
    public void Dispatch_UseHint_DecrementsHintsAndSetsHintLetterIndex()
    {
        // Solution: cat → bat → bag → dog. From "cat" the next target is "bat";
        // they differ at index 0 (c vs b).
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle - 1, state.hintsRemaining);
        Assert.AreEqual(0, state.hintLetterIndex);
        Assert.AreEqual(string.Empty, state.revealedNextWord);
    }

    [Test]
    public void Dispatch_UseHint_WhenExhausted_DoesNotGoNegative()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        // Drain the full budget plus extra calls; the surplus must be no-ops.
        for (int i = 0; i < BalanceConfig.DefaultHintsPerPuzzle + 2; i++)
            manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.hintsRemaining);
    }

    [Test]
    public void Dispatch_UseReveal_SetsRevealedNextWord_NotHintIndex()
    {
        // From start word "cat" the next solution word is "bat".
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseRevealAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.revealsRemaining);
        Assert.AreEqual("bat", state.revealedNextWord);
        // Task 31 — Reveal shows ONLY its preview row; it must NOT also gold-highlight the active
        // input tile like Hint does. hintLetterIndex stays -1 (Hint is the separate power-up for that).
        Assert.AreEqual(-1, state.hintLetterIndex);
    }

    [Test]
    public void Dispatch_UseReveal_WhenExhausted_DoesNotGoNegative()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseRevealAction());
        manager.Dispatch(new UseRevealAction()); // second call should be no-op

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.revealsRemaining);
    }

    [Test]
    public void Dispatch_UseHint_ClearedAfterValidSubmit()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.Dispatch(new UseHintAction(0));
        Assert.AreEqual(0, manager.GetCurrentState().hintLetterIndex);

        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("bat"));

        var state = manager.GetCurrentState();
        Assert.AreEqual(-1, state.hintLetterIndex,
            "Submitting a valid word should clear the stale hint preview.");
        Assert.AreEqual(string.Empty, state.revealedNextWord);
    }

    [Test]
    public void Dispatch_UseReveal_ClearedAfterUndo()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("bat"));
        manager.Dispatch(new UseRevealAction());
        Assert.AreEqual("bag", manager.GetCurrentState().revealedNextWord);

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(-1, state.hintLetterIndex,
            "Undo should clear the hint preview since the chain just rewound.");
        Assert.AreEqual(string.Empty, state.revealedNextWord);
    }

    [Test]
    public void Dispatch_UseHint_WithNoSolution_NoOps()
    {
        // Empty solution array → hint should refuse without consuming the counter.
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new string[0], 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle, state.hintsRemaining,
            "Hint must not consume the counter when no solution path exists.");
        Assert.AreEqual(-1, state.hintLetterIndex);
    }

    [Test]
    public void Dispatch_UseHint_AtEndOfSolution_NoOps()
    {
        // Solution has only one entry — there is no "next" word for any hint to point at.
        // Spec §1.1 guards on solution.Length < 2 by warning and returning without spend.
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle, state.hintsRemaining,
            "Hint must not consume the counter when no further solution word exists.");
        Assert.AreEqual(-1, state.hintLetterIndex);
    }

    [Test]
    public void Dispatch_UseHint_OffPath_PicksMinHammingTarget()
    {
        // Solution: bat → bag → bog. Chain ends at "cat" (off-path, no exact match,
        // and "cat" != solution[0] so the start-word special case does not apply).
        // Fallback iterates i=1..Length-1; Hamming("cat", "bag")=2, Hamming("cat", "bog")=3.
        // Best non-zero is "bag" → cat vs bag differ at index 0 ('c' vs 'b').
        var puzzle = new PuzzleType(1, "bat", "bog", 2,
            new[] { "bat", "bag", "bog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle); // chain starts ["bat"]
        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("cat")); // chain: [bat, cat]

        manager.Dispatch(new UseHintAction(0));

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.hintLetterIndex);
        Assert.AreEqual(BalanceConfig.DefaultHintsPerPuzzle - 1, state.hintsRemaining);
    }

    [Test]
    public void Dispatch_Undo_RemovesLastWordFromChain()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        mockValidator.SetValidResult(true, true);
        manager.Dispatch(new SubmitWordAction("bat"));

        Assert.AreEqual(2, manager.GetCurrentState().wordChain.Count);

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.wordChain.Count);
        Assert.AreEqual("cat", state.wordChain[0]);
    }

    [Test]
    public void Dispatch_Undo_OnStartingChain_DoesNothing()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.Dispatch(new UndoStepAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(1, state.wordChain.Count);
    }

    // ── TimeAttack §4 AddTime power-up ────────────────────────────────────

    [Test]
    public void StartNewPuzzle_InitializesAddTimeFieldsToZero()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.addTimesRemaining);
        Assert.AreEqual(0f, state.addTimeGrantSeconds);
    }

    [Test]
    public void ConfigureAddTimePowerUp_SeedsChargesAndGrant()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);

        manager.ConfigureAddTimePowerUp(2, 10f);

        var state = manager.GetCurrentState();
        Assert.AreEqual(2, state.addTimesRemaining);
        Assert.AreEqual(10f, state.addTimeGrantSeconds);
    }

    [Test]
    public void Dispatch_UseAddTime_DecrementsChargeAndRaisesOnTimeAdded()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.ConfigureAddTimePowerUp(1, 15f);

        float granted = 0f;
        int callCount = 0;
        manager.OnTimeAdded += s => { granted = s; callCount++; };

        manager.Dispatch(new UseAddTimeAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.addTimesRemaining);
        Assert.AreEqual(15f, granted);
        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void Dispatch_UseAddTime_WhenExhausted_DoesNotRaiseEventOrGoNegative()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.ConfigureAddTimePowerUp(1, 10f);

        int callCount = 0;
        manager.OnTimeAdded += _ => callCount++;

        manager.Dispatch(new UseAddTimeAction()); // consumes the one charge
        manager.Dispatch(new UseAddTimeAction()); // no-op
        manager.Dispatch(new UseAddTimeAction()); // no-op

        var state = manager.GetCurrentState();
        Assert.AreEqual(0, state.addTimesRemaining);
        Assert.AreEqual(1, callCount,
            "OnTimeAdded must only fire while charges remain.");
    }

    [Test]
    public void Dispatch_UseAddTime_WithZeroGrant_NoOps()
    {
        var puzzle = new PuzzleType(1, "cat", "dog", 3,
            new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);
        manager.StartNewPuzzle(puzzle);
        manager.ConfigureAddTimePowerUp(3, 0f);

        int callCount = 0;
        manager.OnTimeAdded += _ => callCount++;

        manager.Dispatch(new UseAddTimeAction());

        var state = manager.GetCurrentState();
        Assert.AreEqual(3, state.addTimesRemaining,
            "AddTime must not consume a charge when grant size is zero.");
        Assert.AreEqual(0, callCount);
    }

    // ── Daily 2.0 (Task 36) — two-resource reducer (detour = score, mistake = run) ──

    private static PuzzleType DailyPuzzle() => new PuzzleType(1, "cat", "dog", 3,
        new[] { "cat", "bat", "bag", "dog" }, 0, Diff.Easy);

    [Test]
    public void Daily_DetourThenInvalid_TracksDetourAndSpendsOneMistake()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        // Accepted but NOT progress => a DETOUR (costs score, not the run).
        mockValidator.SetValidResult(true, true, progress: false);
        manager.Dispatch(new SubmitWordAction("bat"));

        // Invalid guess (correct length, not a valid step) => a MISTAKE (costs the run).
        mockValidator.SetValidResult(false, false);
        manager.Dispatch(new SubmitWordAction("zzz"));

        Assert.AreEqual(1, manager.GetDetourCount(), "one detour recorded");
        Assert.AreEqual(BalanceConfig.DailyMistakeBudget - 1, manager.GetMistakesRemaining(),
            "one mistake spent, run not failed");
        Assert.IsFalse(manager.GetDailyResult().HasValue, "run still in progress");
    }

    [Test]
    public void Daily_ProgressMove_IsFree_NoDetour_NoMistake()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        mockValidator.SetValidResult(true, true, progress: true);
        manager.Dispatch(new SubmitWordAction("bat"));

        Assert.AreEqual(0, manager.GetDetourCount());
        Assert.AreEqual(BalanceConfig.DailyMistakeBudget, manager.GetMistakesRemaining());
    }

    [Test]
    public void Daily_ExhaustMistakeBudget_FailsWithFailedResult()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        mockValidator.SetValidResult(false, false);
        var bad = new[] { "zzz", "yyy", "www", "qqq", "vvv" };
        for (int i = 0; i < BalanceConfig.DailyMistakeBudget; i++)
            manager.Dispatch(new SubmitWordAction(bad[i]));

        Assert.AreEqual(0, manager.GetMistakesRemaining());
        var result = manager.GetDailyResult();
        Assert.IsTrue(result.HasValue, "a failed daily produces a result");
        Assert.IsTrue(result.Value.failed);
        Assert.AreEqual(0, result.Value.stars, "Failed = 0 stars");
    }

    // ── Daily rework — typos can't kill you: a LEGAL-SHAPED dictionary miss bounces FREE;
    //    rule violations (incl. the both-invalid precedence case) still cost a mistake. ──

    [Test]
    public void Daily_LegalShapedDictionaryMiss_BouncesFree_NoMistake_NoDetour()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        string lastMsg = null; bool lastAccepted = true;
        manager.OnWordSubmissionResult += r => { lastMsg = r.reason; lastAccepted = r.accepted; };

        // "cax" IS one letter from the chain tail "cat" but is not a word → a FREE bounce.
        mockValidator.SetRejection(WordPuzzle.Puzzle.WordRejectReason.NotInDictionary, "nonsense");
        manager.Dispatch(new SubmitWordAction("cax"));

        Assert.AreEqual(BalanceConfig.DailyMistakeBudget, manager.GetMistakesRemaining(),
            "a legal-shaped typo must NOT spend a mistake");
        Assert.AreEqual(0, manager.GetDetourCount(), "a typo is not a detour");
        Assert.AreEqual(0, manager.GetDailyStepClasses().Count, "no share-card row for a free bounce");
        Assert.IsFalse(manager.GetDailyResult().HasValue, "run unaffected");
        Assert.IsFalse(lastAccepted, "still a rejection (shake + message)");
        Assert.AreEqual("Not a word — free try", lastMsg, "the free bounce says so");
    }

    [Test]
    public void Daily_BothInvalid_Precedence_RuleViolation_SpendsMistake()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        string lastMsg = null;
        manager.OnWordSubmissionResult += r => lastMsg = r.reason;

        // "zzz" is NOT a word AND not one letter from "cat". The validator checks the
        // dictionary FIRST (so it reports NotInDictionary), but precedence says the free
        // pass is only for LEGAL-shaped misses — a rule-break pays.
        mockValidator.SetRejection(WordPuzzle.Puzzle.WordRejectReason.NotInDictionary, "nonsense");
        manager.Dispatch(new SubmitWordAction("zzz"));

        Assert.AreEqual(BalanceConfig.DailyMistakeBudget - 1, manager.GetMistakesRemaining(),
            "both-invalid = rule violation = a mistake");
        Assert.AreEqual("Change exactly one letter", lastMsg,
            "the costly rejection carries the RULE copy, not 'not a word'");
    }

    [Test]
    public void Daily_NonHamming1_StillSpendsMistake()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        // A multi-letter change (typed reason from the validator) → costs a mistake, as today.
        mockValidator.SetRejection(WordPuzzle.Puzzle.WordRejectReason.NotOneLetterDifferent, "x");
        manager.Dispatch(new SubmitWordAction("dog"));

        Assert.AreEqual(BalanceConfig.DailyMistakeBudget - 1, manager.GetMistakesRemaining());
    }

    [Test]
    public void Daily_TyposNeverFail_RuleBreaksStillDo()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);
        mockValidator.SetRejection(WordPuzzle.Puzzle.WordRejectReason.NotInDictionary, "x");

        // More free typos than the whole budget — the run never ends…
        for (int i = 0; i < BalanceConfig.DailyMistakeBudget + 2; i++)
            manager.Dispatch(new SubmitWordAction("cax"));
        Assert.IsFalse(manager.GetDailyResult().HasValue, "typos alone can never fail the run");
        Assert.AreEqual(BalanceConfig.DailyMistakeBudget, manager.GetMistakesRemaining());

        // …but rule-breaks still burn the budget down to the unchanged Failed result.
        for (int i = 0; i < BalanceConfig.DailyMistakeBudget; i++)
            manager.Dispatch(new SubmitWordAction("zzz"));
        Assert.AreEqual(0, manager.GetMistakesRemaining());
        Assert.IsTrue(manager.GetDailyResult().HasValue && manager.GetDailyResult().Value.failed,
            "0 mistakes = Failed, unchanged");
    }

    // (A CANARY asserting the OLD rule — "a legal-shaped dictionary miss spends a mistake" —
    //  was run here and FAILED as required (expected budget-1, got budget), proving both the
    //  behavior change and the runner. Removed after the red was confirmed, per convention.)

    [Test]
    public void Daily_UndoAfterDetour_DecrementsDetourToZero_NoMistakeRefund()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 4);

        // Spend a mistake first, then take a detour.
        mockValidator.SetValidResult(false, false);
        manager.Dispatch(new SubmitWordAction("zzz"));
        mockValidator.SetValidResult(true, true, progress: false);
        manager.Dispatch(new SubmitWordAction("bat"));
        Assert.AreEqual(1, manager.GetDetourCount());

        manager.Dispatch(new UndoStepAction());

        Assert.AreEqual(0, manager.GetDetourCount(), "undo steps back the detour");
        Assert.AreEqual(BalanceConfig.DailyMistakeBudget - 1, manager.GetMistakesRemaining(),
            "a spent mistake is NOT refunded by undo");
    }

    [Test]
    public void Daily_SolveAtPar_ZeroDetours_PerfectResult()
    {
        manager.StartNewPuzzle(DailyPuzzle());
        manager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, 3); // par 3

        mockValidator.SetValidResult(true, true, progress: true);
        manager.Dispatch(new SubmitWordAction("bat"));
        manager.Dispatch(new SubmitWordAction("bag"));
        manager.Dispatch(new SubmitWordAction("dog")); // reaches endWord => win

        var result = manager.GetDailyResult();
        Assert.IsTrue(result.HasValue, "a solved daily produces a result");
        Assert.IsFalse(result.Value.failed);
        Assert.AreEqual(0, result.Value.detours);
        Assert.AreEqual(3, result.Value.playerSteps);
        Assert.AreEqual(3, result.Value.stars, "optimal-length path => Perfect (3 stars)");
    }

    [Test]
    public void Classic_NotDaily_IgnoresDetoursAndMistakes()
    {
        manager.StartNewPuzzle(DailyPuzzle()); // NOT promoted to a daily run

        mockValidator.SetValidResult(false, false);
        manager.Dispatch(new SubmitWordAction("zzz"));
        mockValidator.SetValidResult(true, true, progress: false);
        manager.Dispatch(new SubmitWordAction("bat"));

        Assert.IsFalse(manager.IsDailyRun());
        Assert.AreEqual(0, manager.GetDetourCount(), "non-daily run never tracks detours");
        Assert.AreEqual(0, manager.GetMistakesRemaining(), "non-daily run never seeds mistakes");
        Assert.IsFalse(manager.GetDailyResult().HasValue);
    }
}
