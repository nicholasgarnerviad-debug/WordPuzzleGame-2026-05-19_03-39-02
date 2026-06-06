using System.Collections.Generic;
using NUnit.Framework;
using WordPuzzle.Persistence;

/// <summary>
/// Library Path View — pure logic tests for <see cref="PuzzlePathProgress"/> (best-solve +
/// progressively-revealed optimal path). No Unity, no scene. Verifies the LOCKED rules:
///   • best-solve only improves (better replaces, worse/equal does not),
///   • revealed-optimal set unions and never shrinks,
///   • matching is word + position vs the stored canonical solution,
///   • a perfect/optimal-length solve auto-reveals the entire canonical path,
///   • old-save records (null/empty fields) fold in clean.
/// The in-editor runner is unreliable; these assertions are the source of truth. A deliberate
/// CANARY test guards that the harness actually evaluates assertions.
/// </summary>
[TestFixture]
public class PuzzlePathProgressTests
{
    // Canonical optimal path for the running example (cat -> cot -> cog -> dog): par 3.
    private static readonly string[] Solution = { "cat", "cot", "cog", "dog" };
    private const int OptimalSteps = 3;

    private static List<string> Chain(params string[] words) => new List<string>(words);

    [Test]
    public void Canary_HarnessEvaluatesAssertions()
    {
        Assert.AreEqual(4, 2 + 2, "arithmetic canary — if this fails the runner is broken");
        Assert.IsTrue(true);
    }

    // ---------------- (A) Best solve only improves ----------------

    [Test]
    public void FirstSolve_RecordsBestPath()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "bat", "bot", "bog", "dog"), Solution, OptimalSteps);
        Assert.AreEqual(4, rec.bestSolveSteps); // 5 words = 4 steps
        CollectionAssert.AreEqual(new[] { "cat", "bat", "bot", "bog", "dog" }, rec.bestSolvePath);
    }

    [Test]
    public void BetterReplay_ReplacesBest()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "bat", "bot", "bog", "dog"), Solution, OptimalSteps);   // 4 steps
        rec = PuzzlePathProgress.ApplySolve(rec, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps);          // 3 steps — better
        Assert.AreEqual(3, rec.bestSolveSteps);
        CollectionAssert.AreEqual(Solution, rec.bestSolvePath);
    }

    [Test]
    public void WorseReplay_DoesNotChangeBest()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps);          // 3 steps (best)
        rec = PuzzlePathProgress.ApplySolve(rec, 1,
            Chain("cat", "bat", "bot", "bog", "dog"), Solution, OptimalSteps);   // 4 steps — worse
        Assert.AreEqual(3, rec.bestSolveSteps, "a worse replay must not change the stored best");
        CollectionAssert.AreEqual(Solution, rec.bestSolvePath);
    }

    [Test]
    public void EqualReplay_DoesNotChangeBest()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps);          // 3 steps
        var firstPath = rec.bestSolvePath;
        rec = PuzzlePathProgress.ApplySolve(rec, 1,
            Chain("cat", "xxt", "xxg", "dog"), Solution, OptimalSteps);          // also 3 steps
        Assert.AreEqual(3, rec.bestSolveSteps);
        CollectionAssert.AreEqual(firstPath, rec.bestSolvePath,
            "an equal-length replay must not overwrite the stored best (only strictly-better replaces)");
    }

    // ---------------- (B) Revealed set unions / never shrinks / word+position ----------------

    [Test]
    public void Match_IsByWordAndPosition()
    {
        // Detour route shares only start (0) and end (3) with the canonical path; the wrong-position
        // word "cot" at index 1 vs canonical "cot" at index 1 DOES match; "xog" at 2 does not.
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "xog", "dog"), Solution, optimalSteps: 99 /* force non-perfect */);
        CollectionAssert.AreEqual(new[] { 0, 1, 3 }, rec.revealedOptimalIndices);
    }

    [Test]
    public void SameWord_WrongPosition_DoesNotReveal()
    {
        // "cog" appears, but at index 1 (canonical has "cog" at index 2) — position mismatch, no reveal
        // beyond the trivially-matching start.
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cog", "bog", "dog"), Solution, optimalSteps: 99);
        Assert.IsFalse(rec.revealedOptimalIndices.Contains(2),
            "a canonical word used at the wrong position must NOT reveal that slot");
        Assert.IsTrue(rec.revealedOptimalIndices.Contains(0));  // start always matches
        Assert.IsTrue(rec.revealedOptimalIndices.Contains(3));  // end matches
    }

    [Test]
    public void Reveals_UnionAcrossReplays_AndNeverShrink()
    {
        // First solve reveals {0,1,3}; a second, different non-perfect solve reveals {0,2,3}.
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "xog", "dog"), Solution, optimalSteps: 99);
        CollectionAssert.AreEqual(new[] { 0, 1, 3 }, rec.revealedOptimalIndices);

        rec = PuzzlePathProgress.ApplySolve(rec, 1,
            Chain("cat", "xxt", "cog", "dog"), Solution, optimalSteps: 99);
        // Union of {0,1,3} and {0,2,3} = {0,1,2,3}; nothing previously revealed is lost.
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices);
    }

    [Test]
    public void WorseReplay_NeverHidesAnAlreadyRevealedWord()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps); // perfect → all revealed
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices);

        // A subsequent sloppy replay sharing only endpoints must not REMOVE any revealed slot.
        rec = PuzzlePathProgress.ApplySolve(rec, 1,
            Chain("cat", "bat", "bot", "bog", "dog"), Solution, OptimalSteps);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices,
            "an already-revealed slot must never be hidden by a worse replay");
    }

    // ---------------- Perfect-solve auto-reveal (confirmed decision) ----------------

    [Test]
    public void PerfectSolve_SamePath_RevealsEverything()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices);
        Assert.IsTrue(PuzzlePathProgress.IsFullyRevealed(rec, Solution.Length));
    }

    [Test]
    public void PerfectSolve_DifferentOptimalRoute_StillRevealsEntireCanonicalPath()
    {
        // Optimal-length (3 steps) but via a route whose middles don't position-match the stored
        // solution. The confirmed decision: a verified perfect solve auto-reveals the WHOLE path.
        var rec = PuzzlePathProgress.ApplySolve(null, 1,
            Chain("cat", "dat", "dot", "dog"), Solution, OptimalSteps);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices,
            "a perfect different-route solve must not leave the optimal path showing blanks");
    }

    // ---------------- Old-save graceful default ----------------

    [Test]
    public void OldSaveRecord_NullFields_FoldsInClean()
    {
        // Simulates a record materialized from an old/partial JSON: null arrays/lists, unset best.
        var legacy = new PuzzlePathRecord { puzzleId = 1, bestSolvePath = null, revealedOptimalIndices = null };
        var rec = PuzzlePathProgress.ApplySolve(legacy, 1,
            Chain("cat", "cot", "cog", "dog"), Solution, OptimalSteps);
        Assert.AreEqual(3, rec.bestSolveSteps);
        Assert.IsNotNull(rec.revealedOptimalIndices);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, rec.revealedOptimalIndices);
    }

    [Test]
    public void NullOrEmptyChain_IsNoOp()
    {
        var rec = PuzzlePathProgress.ApplySolve(null, 1, null, Solution, OptimalSteps);
        Assert.AreEqual(int.MaxValue, rec.bestSolveSteps, "no chain → best stays unset");
        Assert.IsEmpty(rec.revealedOptimalIndices);

        rec = PuzzlePathProgress.ApplySolve(rec, 1, new List<string>(), Solution, OptimalSteps);
        Assert.AreEqual(int.MaxValue, rec.bestSolveSteps);
    }
}
