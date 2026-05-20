using NUnit.Framework;

public class WordGraphTests
{
    private WordGraph graph;

    [SetUp]
    public void Setup()
    {
        graph = new WordGraph();

        // Build small test graph
        graph.AddWord("cat");
        graph.AddWord("bat");
        graph.AddWord("hat");
        graph.AddWord("mat");
        graph.AddWord("map");

        graph.BuildAdjacencies();
    }

    [Test]
    public void GetShortestPath_ValidPath_ReturnsCorrectSequence()
    {
        // Act
        var path = graph.GetShortestPath("cat", "map");

        // Assert
        Assert.Greater(path.Count, 0);
        Assert.AreEqual("cat", path[0]);
        Assert.AreEqual("map", path[path.Count - 1]);
    }

    [Test]
    public void GetDistance_ValidWords_ReturnsCorrectDistance()
    {
        // Act
        int distance = graph.GetDistance("cat", "bat");

        // Assert
        Assert.AreEqual(1, distance);
    }

    [Test]
    public void CanSolve_ValidPath_ReturnsTrue()
    {
        // Act
        bool canSolve = graph.CanSolve("cat", "map");

        // Assert
        Assert.IsTrue(canSolve);
    }
}
