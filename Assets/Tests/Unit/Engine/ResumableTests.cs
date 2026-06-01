using NUnit.Framework;
using WordPuzzle;
using WordPuzzle.Persistence;
using WordPuzzle.Puzzle;

// Task 9G — GameBootstrap.TryGetResumable: pure, never-throwing resumability check.
[TestFixture]
public class ResumableTests
{
    private static PuzzleDefinition CatDogDef() => new PuzzleDefinition
    {
        puzzleId = 42,
        startWord = "cat",
        endWord = "dog",
        optimalSteps = 3,
        solution = new[] { "cat", "bat", "bag", "dog" },
        seedValue = 0
    };

    // resolve returns a valid def for the matching id, null otherwise.
    private static System.Func<int, PuzzleDefinition> ResolveCatDog =>
        id => id == 42 ? CatDogDef() : null;

    [Test]
    public void InProgress_Resolvable_ReturnsTrueAndSetsDef()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 42,
            wordChain = new[] { "cat", "bat" } // tail "bat" != endWord "dog"
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsTrue(result, "An in-progress, resolvable puzzle is resumable.");
        Assert.IsNotNull(def);
        Assert.AreEqual(42, def.puzzleId);
    }

    [Test]
    public void Won_TailEqualsEndWord_ReturnsFalse()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 42,
            wordChain = new[] { "cat", "bat", "bag", "dog" } // tail == endWord
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsFalse(result, "A won puzzle (tail == endWord) is not resumable.");
        Assert.IsNull(def, "def must be cleared for a won puzzle.");
    }

    [Test]
    public void Won_TailEqualsEndWord_CaseInsensitive_ReturnsFalse()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 42,
            wordChain = new[] { "cat", "DOG" } // tail matches endWord ignoring case
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsFalse(result, "Win detection must be case-insensitive.");
        Assert.IsNull(def);
    }

    [Test]
    public void EmptyChain_ReturnsFalse()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 42,
            wordChain = new string[0]
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsFalse(result, "An empty chain is not resumable.");
        Assert.IsNull(def);
    }

    [Test]
    public void NullChain_ReturnsFalse()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 42,
            wordChain = null
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsFalse(result, "A null chain is not resumable.");
        Assert.IsNull(def);
    }

    [Test]
    public void NullSnapshot_ReturnsFalse_NoThrow()
    {
        bool result = true;
        Assert.DoesNotThrow(() =>
        {
            result = GameBootstrap.TryGetResumable(null, ResolveCatDog, out _);
        });
        Assert.IsFalse(result, "A null snapshot is not resumable.");
    }

    [Test]
    public void MenuMode_ReturnsFalse()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Menu",
            currentPuzzleId = 42,
            wordChain = new[] { "cat", "bat" }
        };

        bool result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out var def);

        Assert.IsFalse(result, "Menu mode is not a resumable gameplay state.");
        Assert.IsNull(def);
    }

    [Test]
    public void UnresolvableId_ReturnsFalse_NoThrow()
    {
        var snap = new GameStateSnapshot
        {
            currentMode = "Classic",
            currentPuzzleId = 999, // ResolveCatDog returns null for non-42
            wordChain = new[] { "cat", "bat" }
        };

        bool result = true;
        Assert.DoesNotThrow(() =>
        {
            result = GameBootstrap.TryGetResumable(snap, ResolveCatDog, out _);
        });
        Assert.IsFalse(result, "An unresolvable puzzle id is not resumable.");
    }
}
