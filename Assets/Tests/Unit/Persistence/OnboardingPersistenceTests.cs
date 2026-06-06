using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using WordPuzzle.Persistence;

public class OnboardingPersistenceTests
{
    private const string OnboardingKey = "onboarding_v1";

    [SetUp]
    public void Setup()
    {
        PlayerPrefs.DeleteKey(OnboardingKey);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(OnboardingKey);
    }

    [Test]
    public async Task FreshLoad_ReturnsNonNullNotCompleted()
    {
        var dataManager = new DataManager();
        var loaded = await dataManager.LoadOnboardingAsync();

        Assert.IsNotNull(loaded);
        Assert.IsFalse(loaded.completed);
    }

    [Test]
    public async Task SaveThenLoadFreshInstance_RoundTripsBothFlags()
    {
        var saver = new DataManager();
        await saver.SaveOnboardingAsync(new OnboardingData { completed = true, skipped = true });

        // Fresh instance bypasses the in-memory cache, proving PlayerPrefs persistence.
        var loader = new DataManager();
        var loaded = await loader.LoadOnboardingAsync();

        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded.completed);
        Assert.IsTrue(loaded.skipped);
    }

    [Test]
    public async Task ResetProgress_DoesNotClearOnboarding()
    {
        // Reset Progress (ResetAllAsync) wipes gameplay/economy progress but must PRESERVE the
        // tutorial-seen flag — only Replay Tutorial (OnboardingRules.Reset) clears it.
        var saver = new DataManager();
        await saver.SaveOnboardingAsync(new OnboardingData { completed = true, skipped = true });

        await saver.ResetAllAsync();

        var loader = new DataManager();
        var loaded = await loader.LoadOnboardingAsync();

        Assert.IsNotNull(loaded);
        Assert.IsTrue(loaded.completed, "Reset Progress must NOT clear the tutorial-seen flag.");
        Assert.IsTrue(loaded.skipped);
    }
}
