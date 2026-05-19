using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class GameControllerTests
{
    private GameController gameController;
    private GameObject gameObject;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        gameController = gameObject.AddComponent<GameController>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void GenerateNewPuzzle_CreatesValidPuzzle()
    {
        gameController.GenerateNewPuzzle(1);
        PuzzleData puzzle = gameController.GetCurrentPuzzle();

        Assert.IsNotNull(puzzle);
        Assert.Greater(puzzle.words.Count, 0);
    }

    [Test]
    public void SubmitWord_ValidWord_ReturnsTrue()
    {
        gameController.GenerateNewPuzzle(1);
        bool result = gameController.SubmitWord("apple");

        Assert.IsTrue(result);
    }

    [Test]
    public void SubmitWord_InvalidWord_ReturnsFalse()
    {
        gameController.GenerateNewPuzzle(1);
        bool result = gameController.SubmitWord("notaword");

        Assert.IsFalse(result);
    }

    [Test]
    public void SubmitWord_DuplicateWord_ReturnsFalse()
    {
        gameController.GenerateNewPuzzle(1);
        gameController.SubmitWord("apple");
        bool result = gameController.SubmitWord("apple");

        Assert.IsFalse(result);
    }

    [Test]
    public void IsCurrentPuzzleComplete_WithAllWordsFound_ReturnsTrue()
    {
        gameController.GenerateNewPuzzle(1);
        PuzzleData puzzle = gameController.GetCurrentPuzzle();

        foreach (var word in puzzle.words)
        {
            gameController.SubmitWord(word);
        }

        Assert.IsTrue(gameController.IsCurrentPuzzleComplete());
    }

    [Test]
    public void GetCurrentScore_CalculatesCorrectly()
    {
        gameController.GenerateNewPuzzle(1);
        gameController.SubmitWord("apple"); // 5 letters
        gameController.SubmitWord("brave"); // 5 letters

        Assert.AreEqual(10, gameController.GetCurrentScore());
    }
}
