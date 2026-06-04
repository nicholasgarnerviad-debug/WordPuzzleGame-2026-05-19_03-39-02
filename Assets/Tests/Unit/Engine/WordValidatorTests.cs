using NUnit.Framework;
using System.Collections.Generic;
using WordPuzzle.Puzzle;

[TestFixture]
public class WordValidatorTests
{
    private WordValidator validator;
    private WordGraph wordGraph;

    [SetUp]
    public void Setup()
    {
        wordGraph = new WordGraph();

        // Add test words
        wordGraph.AddWord("cat");
        wordGraph.AddWord("bat");
        wordGraph.AddWord("hat");
        wordGraph.AddWord("mat");
        wordGraph.AddWord("map");

        wordGraph.BuildAdjacencies();

        validator = new WordValidator(wordGraph);
        validator.Initialize("cat", "map", new[] { "cat" });
    }

    [Test]
    public void ValidateWord_ValidNextStep_ReturnsValid()
    {
        var result = validator.ValidateWord("bat");

        Assert.IsTrue(result.isValid);
        Assert.IsTrue(result.isNextStep);
    }

    [Test]
    public void ValidateWord_NotInDictionary_ReturnsInvalid()
    {
        var result = validator.ValidateWord("xyz");

        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Word not in dictionary", result.message);
    }

    [Test]
    public void ValidateWord_TwoLetterDifference_ReturnsInvalid()
    {
        var result = validator.ValidateWord("map");

        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Must change exactly one letter", result.message);
    }

    [Test]
    public void ValidateWord_AlreadyUsed_ReturnsInvalid()
    {
        var result = validator.ValidateWord("cat");

        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Word already used", result.message);
    }

    // ----------------------------------------------------------------------
    // TASK 2 — ValidateWord must perform ZERO graph traversals after
    //   Initialize has cached the distance map.
    // ----------------------------------------------------------------------
    [Test]
    public void ValidateWord_AfterInitialize_PerformsNoBfsTraversals()
    {
        int baseline = wordGraph.BfsTraversalCount;

        var accepted = validator.ValidateWord("bat");
        var rejectedDict = validator.ValidateWord("xyz");
        var rejectedReuse = validator.ValidateWord("cat");

        Assert.IsTrue(accepted.isValid);
        Assert.IsFalse(rejectedDict.isValid);
        Assert.IsFalse(rejectedReuse.isValid);

        Assert.AreEqual(baseline, wordGraph.BfsTraversalCount,
            "ValidateWord should never trigger a BFS; the distance map is precomputed in Initialize.");
    }

    // ----------------------------------------------------------------------
    // TASK 2 — distStart / distEnd / progress must match the old behavior.
    // Daily's detour detection (Task 36) keys off isProgress, so this pins
    // BOTH a genuine-progress step and a lateral (same-distance) step.
    //
    // Graph distances to target "map": map=0, mat=1, cat=2, bat=2, hat=2.
    // ("cat" and "mat" share the pattern "_at", so cat↔mat is a direct edge
    //  and cat→map = cat→mat→map = 2 — NOT 3.)
    // ----------------------------------------------------------------------
    [Test]
    public void ValidateWord_DistanceAndProgress_MatchExpectedSemantics()
    {
        // From "cat" (distance 2 to "map"):
        //   "mat" is strictly closer (distance 1) -> PROGRESS.
        var progressStep = validator.ValidateWord("mat");
        Assert.IsTrue(progressStep.isValid);
        Assert.AreEqual(1, progressStep.distanceToStart);
        Assert.AreEqual(1, progressStep.distanceToEnd);
        Assert.IsTrue(progressStep.isProgress, "mat (dist 1) is closer than cat (dist 2)");

        //   "bat" is the SAME distance (2) -> a valid step but NOT progress.
        //   This is exactly what Daily counts as a detour.
        var lateralStep = validator.ValidateWord("bat");
        Assert.IsTrue(lateralStep.isValid);
        Assert.AreEqual(1, lateralStep.distanceToStart);
        Assert.AreEqual(2, lateralStep.distanceToEnd);
        Assert.IsFalse(lateralStep.isProgress, "bat (dist 2) is not closer than cat (dist 2)");
    }
}
