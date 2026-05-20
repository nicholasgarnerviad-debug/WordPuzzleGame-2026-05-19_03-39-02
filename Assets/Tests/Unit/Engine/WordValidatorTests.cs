using NUnit.Framework;
using System.Collections.Generic;

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
        // Act
        var result = validator.ValidateWord("bat");

        // Assert
        Assert.IsTrue(result.isValid);
        Assert.IsTrue(result.isNextStep);
    }

    [Test]
    public void ValidateWord_NotInDictionary_ReturnsInvalid()
    {
        // Act
        var result = validator.ValidateWord("xyz");

        // Assert
        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Word not in dictionary", result.message);
    }

    [Test]
    public void ValidateWord_TwoLetterDifference_ReturnsInvalid()
    {
        // Act
        var result = validator.ValidateWord("map");

        // Assert
        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Must change exactly one letter", result.message);
    }

    [Test]
    public void ValidateWord_AlreadyUsed_ReturnsInvalid()
    {
        // Act
        var result = validator.ValidateWord("cat");

        // Assert
        Assert.IsFalse(result.isValid);
        Assert.AreEqual("Word already used", result.message);
    }
}
