using NUnit.Framework;
using System.Collections.Generic;

public class PuzzleGeneratorTests
{
    private PuzzleGenerator generator;
    private List<string> testWords;

    [SetUp]
    public void Setup()
    {
        testWords = new List<string>
        {
            "apple", "apply", "about",
            "brave", "break", "broad",
            "crane", "craft", "crate"
        };
        generator = new PuzzleGenerator(testWords);
    }

    [Test]
    public void ValidateWord_WithValidWord_ReturnsTrue()
    {
        Assert.IsTrue(generator.ValidateWord("apple"));
        Assert.IsTrue(generator.ValidateWord("brave"));
    }

    [Test]
    public void ValidateWord_WithInvalidWord_ReturnsFalse()
    {
        Assert.IsFalse(generator.ValidateWord("xyz"));
        Assert.IsFalse(generator.ValidateWord("notaword"));
    }

    [Test]
    public void GeneratePuzzle_ReturnsValidPuzzleData()
    {
        PuzzleData puzzle = generator.GeneratePuzzle(1);

        Assert.IsNotNull(puzzle);
        Assert.Greater(puzzle.words.Count, 0);
        Assert.IsNotEmpty(puzzle.centerLetter);
        Assert.AreEqual(1, puzzle.difficulty);
    }

    [Test]
    public void GeneratePuzzle_DifficultyClamped()
    {
        PuzzleData easyPuzzle = generator.GeneratePuzzle(-1);
        PuzzleData hardPuzzle = generator.GeneratePuzzle(10);

        Assert.AreEqual(1, easyPuzzle.difficulty);
        Assert.AreEqual(5, hardPuzzle.difficulty);
    }

    [Test]
    public void GetHint_ReturnsFormattedHint()
    {
        var words = new List<string> { "apple" };
        string hint = generator.GetHint(words);

        Assert.IsNotEmpty(hint);
        Assert.AreEqual("a----", hint);
    }
}
