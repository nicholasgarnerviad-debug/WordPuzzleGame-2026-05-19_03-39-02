using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using WordPuzzle.Puzzle;
using WordPuzzle.Persistence;

/// <summary>
/// TASK 18 — finish/harden regression locks: the "next unplayed puzzle in tier" selection
/// (18E.1) and the Settings round-trip + forward-migration of older settings_v1 blobs (18D).
/// (Tier structure, the min-move floor, post-win routing, and completion-state mapping are
/// locked by PuzzleShowTierTests / MinMovesFloorTests / PostWinRouterTests.)
/// </summary>
[TestFixture]
public class HardenPassTests
{
    // A tiny 1-tier / 3-puzzle definition for the selection logic (no WordGraph needed).
    private const string TierJson =
        "{\"tiers\":[{\"tierId\":1,\"isUnlocked\":true,\"puzzles\":[" +
        "{\"puzzleId\":1,\"startWord\":\"cat\",\"endWord\":\"cog\",\"optimalSteps\":2,\"solution\":[\"cat\",\"cot\",\"cog\"]}," +
        "{\"puzzleId\":2,\"startWord\":\"dog\",\"endWord\":\"dig\",\"optimalSteps\":2,\"solution\":[\"dog\",\"dog\"]}," +
        "{\"puzzleId\":3,\"startWord\":\"bat\",\"endWord\":\"bag\",\"optimalSteps\":2,\"solution\":[\"bat\",\"bag\"]}" +
        "]}]}";

    private static PuzzleGenerator MakeGen()
    {
        var g = new PuzzleGenerator(new WordGraph());
        g.Initialize(TierJson);
        return g;
    }

    // ── 18E.1 — Next Puzzle advances through the tier (prefers unplayed) ──────────

    [Test]
    public void GetUnplayedTierPuzzle_ReturnsOnlyUnplayed()
    {
        var g = MakeGen();
        var completed = new HashSet<int> { 1, 2 };
        for (int i = 0; i < 30; i++)
        {
            var p = g.GetUnplayedTierPuzzle(1, completed);
            Assert.IsNotNull(p);
            Assert.AreEqual(3, p.puzzleId, "puzzle 3 is the only unplayed one");
        }
    }

    [Test]
    public void GetUnplayedTierPuzzle_AllCompleted_ReturnsNull()
    {
        var g = MakeGen();
        // null is the signal GameBootstrap uses to route to the tier grid instead of replaying.
        Assert.IsNull(g.GetUnplayedTierPuzzle(1, new HashSet<int> { 1, 2, 3 }));
    }

    [Test]
    public void GetUnplayedTierPuzzle_NoneCompleted_ReturnsAValidTierPuzzle()
    {
        var g = MakeGen();
        var p = g.GetUnplayedTierPuzzle(1, new HashSet<int>());
        Assert.IsNotNull(p);
        Assert.Contains(p.puzzleId, new[] { 1, 2, 3 });
    }

    // ── 18D — Settings persist + migrate cleanly through DataManager ─────────────

    private const string SETTINGS_KEY = "settings_v1";
    private string savedBlob;

    [SetUp]
    public void SetUp()
    {
        savedBlob = PlayerPrefs.GetString(SETTINGS_KEY, null);
        PlayerPrefs.DeleteKey(SETTINGS_KEY);
    }

    [TearDown]
    public void TearDown()
    {
        if (string.IsNullOrEmpty(savedBlob)) PlayerPrefs.DeleteKey(SETTINGS_KEY);
        else PlayerPrefs.SetString(SETTINGS_KEY, savedBlob);
        PlayerPrefs.Save();
    }

    [Test]
    public void Settings_RoundTrip_PreservesEveryField()
    {
        var dm = new DataManager();
        var s = new SettingsData
        {
            masterVolume = 0.5f, sfxVolume = 0.3f, musicVolume = 0.2f, muted = true,
            reduceMotion = true, hapticsEnabled = false,
            colorBlindMode = ColorBlindMode.Deuteranopia, highContrast = true, textScale = 1.3f
        };
        dm.SaveSettingsAsync(s).GetAwaiter().GetResult();
        var loaded = dm.LoadSettingsAsync().GetAwaiter().GetResult();

        Assert.AreEqual(0.5f, loaded.masterVolume, 0.001f);
        Assert.AreEqual(0.3f, loaded.sfxVolume, 0.001f);
        Assert.IsTrue(loaded.muted);
        Assert.IsTrue(loaded.reduceMotion);
        Assert.IsFalse(loaded.hapticsEnabled);
        Assert.AreEqual(ColorBlindMode.Deuteranopia, loaded.colorBlindMode);
        Assert.IsTrue(loaded.highContrast);
        Assert.AreEqual(1.3f, loaded.textScale, 0.001f);
    }

    [Test]
    public void Settings_MigratesOldBlob_MissingNewerFields_DefaultsSafely()
    {
        // A settings_v1 blob authored before Task 9E (no colorBlindMode/highContrast/textScale).
        PlayerPrefs.SetString(SETTINGS_KEY, "{\"masterVolume\":0.4,\"sfxVolume\":0.6,\"muted\":true}");
        PlayerPrefs.Save();

        var loaded = new DataManager().LoadSettingsAsync().GetAwaiter().GetResult();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(0.4f, loaded.masterVolume, 0.001f, "existing field preserved");
        Assert.IsTrue(loaded.muted);
        Assert.AreEqual(ColorBlindMode.Off, loaded.colorBlindMode, "missing field → safe default Off");
        Assert.IsFalse(loaded.highContrast, "missing field → safe default false");
        Assert.AreEqual(1.0f, loaded.textScale, 0.001f, "missing field → safe default 1.0");
    }
}
