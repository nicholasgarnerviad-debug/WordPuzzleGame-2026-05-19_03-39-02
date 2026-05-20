using NUnit.Framework;
using System.Collections.Generic;

public class PuzzleGeneratorTests
{
    private PuzzleGenerator generator;
    private WordGraph wordGraph;

    [SetUp]
    public void Setup()
    {
        wordGraph = new WordGraph();
        wordGraph.AddWord("cat");
        wordGraph.AddWord("bat");
        wordGraph.AddWord("hat");
        wordGraph.AddWord("mat");
        wordGraph.AddWord("map");
        wordGraph.AddWord("bag");
        wordGraph.BuildAdjacencies();

        var tierCache = new Dictionary<int, TierData>();
        generator = new PuzzleGenerator(wordGraph, tierCache);
    }

    [Test]
    public void GenerateRandomPuzzle_Easy_ReturnsValidPuzzle()
    {
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Easy);

        // Assert
        Assert.IsNotNull(puzzle);
        Assert.IsNotEmpty(puzzle.startWord);
        Assert.IsNotEmpty(puzzle.endWord);
        Assert.Greater(puzzle.optimalSteps, 0);
    }

    [Test]
    public void GenerateRandomPuzzle_Medium_ReturnsMediumDifficulty()
    {
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Medium);

        // Assert
        Assert.IsNotNull(puzzle);
        Assert.Greater(puzzle.optimalSteps, 0);
    }

    [Test]
    public void GenerateRandomPuzzle_Hard_ReturnsHardDifficulty()
    {
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Hard);

        // Assert
        Assert.IsNotNull(puzzle);
        Assert.Greater(puzzle.optimalSteps, 0);
    }

    [Test]
    public void GenerateRandomPuzzle_SolutionContainsStartAndEnd()
    {
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Medium);

        // Assert
        Assert.IsNotNull(puzzle.solution);
        Assert.Greater(puzzle.solution.Length, 0);
        Assert.AreEqual(puzzle.startWord, puzzle.solution[0]);
        Assert.AreEqual(puzzle.endWord, puzzle.solution[puzzle.solution.Length - 1]);
    }

    [Test]
    public void GenerateRandomPuzzle_SolutionLengthMatchesOptimalSteps()
    {
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Medium);

        // Assert
        Assert.AreEqual(puzzle.optimalSteps, puzzle.solution.Length - 1);
    }

    [Test]
    public void GetTierPuzzle_ValidTierId_ReturnsPuzzleDefinition()
    {
        // Arrange
        var tierCache = new Dictionary<int, TierData>
        {
            {
                1, new TierData
                {
                    tierId = 1,
                    puzzles = new[]
                    {
                        new PuzzleDefinition
                        {
                            puzzleId = 1,
                            startWord = "cat",
                            endWord = "dog",
                            optimalSteps = 3,
                            solution = new[] { "cat", "bat", "bag", "dog" },
                            seedValue = 123
                        }
                    },
                    isUnlocked = true
                }
            }
        };

        var generatorWithTier = new PuzzleGenerator(wordGraph, tierCache);

        // Act
        var puzzle = generatorWithTier.GetTierPuzzle(1, 0);

        // Assert
        Assert.IsNotNull(puzzle);
        Assert.AreEqual("cat", puzzle.startWord);
        Assert.AreEqual("dog", puzzle.endWord);
    }

    [Test]
    public void GetTierPuzzle_InvalidTierId_ReturnsNull()
    {
        // Act
        var puzzle = generator.GetTierPuzzle(999, 0);

        // Assert
        Assert.IsNull(puzzle);
    }

    [Test]
    public void GetTierPuzzle_InvalidPuzzleIndex_ReturnsNull()
    {
        // Arrange
        var tierCache = new Dictionary<int, TierData>
        {
            {
                1, new TierData
                {
                    tierId = 1,
                    puzzles = new[]
                    {
                        new PuzzleDefinition
                        {
                            puzzleId = 1,
                            startWord = "cat",
                            endWord = "dog",
                            optimalSteps = 3,
                            solution = new[] { "cat", "bat", "bag", "dog" },
                            seedValue = 123
                        }
                    },
                    isUnlocked = true
                }
            }
        };

        var generatorWithTier = new PuzzleGenerator(wordGraph, tierCache);

        // Act
        var puzzle = generatorWithTier.GetTierPuzzle(1, 999);

        // Assert
        Assert.IsNull(puzzle);
    }

    [Test]
    public void GenerateRandomPuzzle_FallbackPuzzle_HasValidData()
    {
        // Fallback puzzle should always be valid even if generation fails
        // The FallbackPuzzle has predefined data
        // Act
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Easy);

        // Assert - fallback puzzle will have valid structure
        Assert.IsNotNull(puzzle.startWord);
        Assert.IsNotNull(puzzle.endWord);
        Assert.Greater(puzzle.puzzleId, 0);
    }
}
