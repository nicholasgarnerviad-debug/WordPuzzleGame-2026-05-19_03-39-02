using NUnit.Framework;
using WordPuzzle.Game;
using WordPuzzle.Persistence;

[TestFixture]
public class OnboardingRulesTests
{
    [Test]
    public void NullData_RoutesToTutorial()
    {
        Assert.IsTrue(OnboardingRules.ShouldRouteToTutorial(null));
    }

    [Test]
    public void FreshData_NotCompleted_RoutesToTutorial()
    {
        var d = new OnboardingData();
        Assert.IsFalse(d.completed);
        Assert.IsTrue(OnboardingRules.ShouldRouteToTutorial(d));
    }

    [Test]
    public void MarkCompleted_NotSkipped_CompletesWithoutSkipFlag()
    {
        var d = new OnboardingData();
        var result = OnboardingRules.MarkCompleted(d, false);

        Assert.IsTrue(result.completed);
        Assert.IsFalse(result.skipped);
        Assert.IsFalse(OnboardingRules.ShouldRouteToTutorial(result));
    }

    [Test]
    public void MarkCompleted_Skipped_SetsCompletedAndSkipped()
    {
        var d = new OnboardingData();
        var result = OnboardingRules.MarkCompleted(d, true);

        Assert.IsTrue(result.completed);
        Assert.IsTrue(result.skipped);
        Assert.IsFalse(OnboardingRules.ShouldRouteToTutorial(result));
    }

    [Test]
    public void Reset_AfterCompletion_ReturnsToTutorialState()
    {
        var d = OnboardingRules.MarkCompleted(new OnboardingData(), true);
        Assert.IsTrue(d.completed);

        var reset = OnboardingRules.Reset(d);

        Assert.IsFalse(reset.completed);
        Assert.IsFalse(reset.skipped);
        Assert.IsTrue(OnboardingRules.ShouldRouteToTutorial(reset));
    }
}
