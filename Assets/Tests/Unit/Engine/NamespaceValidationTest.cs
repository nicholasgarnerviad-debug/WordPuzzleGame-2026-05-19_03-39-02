// Simple test to validate namespace resolution
using NUnit.Framework;
using WordPuzzle.Puzzle;
using WordPuzzle.State;

[TestFixture]
public class NamespaceValidationTest
{
    [Test]
    public void TestNamespaceResolution()
    {
        // This should pass if namespaces are properly resolved
        var wordGraph = new WordGraph();
        Assert.IsNotNull(wordGraph);
    }
}
