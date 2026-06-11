using NUnit.Framework;

/// <summary>
/// Task 41A — the pure consent-ordering contract: ad init never precedes consent completion,
/// and never happens when ads are not requestable.
/// </summary>
[TestFixture]
public class ConsentGateTests
{
    [Test]
    public void NotGathered_NeverInits_EvenIfAdsWouldBeAllowed()
    {
        Assert.IsFalse(ConsentGate.ShouldInitAds(gathered: false, canRequestAds: true),
            "init must never precede consent completion");
        Assert.IsFalse(ConsentGate.ShouldInitAds(gathered: false, canRequestAds: false));
    }

    [Test]
    public void Gathered_ButAdsNotRequestable_DoesNotInit()
    {
        Assert.IsFalse(ConsentGate.ShouldInitAds(gathered: true, canRequestAds: false));
    }

    [Test]
    public void Gathered_AndRequestable_Inits()
    {
        Assert.IsTrue(ConsentGate.ShouldInitAds(gathered: true, canRequestAds: true));
    }

    [Test]
    public void NullConsentService_CompletesSynchronously_WithAdsAllowed()
    {
        var consent = new WordPuzzle.NullConsentService();
        bool completed = false;
        consent.Gather(() => completed = true);

        Assert.IsTrue(completed, "the Null impl must complete immediately (never blocks Editor boot)");
        Assert.IsTrue(consent.CanRequestAds);
        Assert.IsFalse(consent.PrivacyOptionsRequired, "Settings hides the privacy row under Null");
    }
}
