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
    // ----------------------------------------------------------------------
    [Test]
    public void ValidateWord_DistanceAndProgress_MatchExpectedSemantics()
    {
        // From "cat" with target "map": valid step "bat" should report
        // distStart == 1, distEnd == 2 (bat→mat→map), and progress true
        // (cat→map distance is 3; bat→map distance is 2).
        var result = validator.ValidateWord("bat");

        Assert.IsTrue(result.isValid);
        Assert.AreEqual(1, result.distanceToStart);
        Assert.AreEqual(2, result.distanceToEnd);
        Assert.IsTrue(result.isProgress);
    }
}
