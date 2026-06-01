using NUnit.Framework;
using UnityEngine;
using WordPuzzle.Persistence;

// Task 9A — DataManager constructor must remove the stale "Coins" PlayerPrefs key
// (written by the now-deleted CoinSystem orphan) WITHOUT touching any live save keys.
[TestFixture]
public class CleanupLegacyKeysTests
{
    // The live keys DataManager owns (see DataManager.cs constants). These must survive.
    private static readonly string[] LiveKeys =
    {
        "wordpuzzle_save",
        "wordpuzzle_progress",
        "puzzle_progress_v1",
        "settings_v1",
        "daily_v1",
        "onboarding_v1"
    };

    [SetUp]
    public void Setup()
    {
        PlayerPrefs.DeleteKey("Coins");
        foreach (var k in LiveKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void Teardown()
    {
        PlayerPrefs.DeleteKey("Coins");
        foreach (var k in LiveKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }

    [Test]
    public void Constructor_RemovesLegacyCoinsKey()
    {
        PlayerPrefs.SetString("Coins", "100");
        PlayerPrefs.Save();
        Assert.IsTrue(PlayerPrefs.HasKey("Coins"), "Precondition: Coins key was written.");

        var _ = new DataManager();

        Assert.IsFalse(PlayerPrefs.HasKey("Coins"),
            "DataManager constructor must delete the legacy 'Coins' key.");
    }

    [Test]
    public void Constructor_DoesNotTouchLiveKeys()
    {
        // Seed every live key with a sentinel value.
        foreach (var k in LiveKeys) PlayerPrefs.SetString(k, "sentinel-" + k);
        PlayerPrefs.SetString("Coins", "100");
        PlayerPrefs.Save();

        var _ = new DataManager();

        // Legacy key gone…
        Assert.IsFalse(PlayerPrefs.HasKey("Coins"), "Legacy 'Coins' key must be deleted.");

        // …but every live key is still present AND its value is untouched.
        foreach (var k in LiveKeys)
        {
            Assert.IsTrue(PlayerPrefs.HasKey(k), $"Live key '{k}' must NOT be deleted by cleanup.");
            Assert.AreEqual("sentinel-" + k, PlayerPrefs.GetString(k),
                $"Live key '{k}' value must be untouched by cleanup.");
        }
    }

    [Test]
    public void Constructor_WhenNoCoinsKey_DoesNotThrow_AndLeavesLiveKeys()
    {
        foreach (var k in LiveKeys) PlayerPrefs.SetString(k, "sentinel-" + k);
        PlayerPrefs.Save();
        Assert.IsFalse(PlayerPrefs.HasKey("Coins"), "Precondition: no Coins key.");

        Assert.DoesNotThrow(() => { var _ = new DataManager(); });

        foreach (var k in LiveKeys)
            Assert.IsTrue(PlayerPrefs.HasKey(k), $"Live key '{k}' must survive when no legacy key exists.");
    }
}
