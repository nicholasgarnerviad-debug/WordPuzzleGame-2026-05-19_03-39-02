using NUnit.Framework;
using WordPuzzle.UI;

// Task 38 — regression for the daily-HUD leak. GameplayScreen.SetDailyPar is the ONLY writer of the
// "Par · Mistakes left" steps slot (SetStepsRemaining is never called at runtime), so when a daily ends
// the slot MUST be released to empty or its text lingers into Classic/tutorial. The release/show decision
// is a pure static helper (ComposeDailyHud) so it is unit-testable without a MonoBehaviour.
[TestFixture]
public class DailyHudTests
{
    [Test]
    public void ComposeDailyHud_Released_IsEmpty()
    {
        // par < 0 releases the slot — the fix that stops the stale daily HUD leaking into other modes.
        Assert.AreEqual(string.Empty, GameplayScreen.ComposeDailyHud(-1, -1));
        Assert.AreEqual(string.Empty, GameplayScreen.ComposeDailyHud(-1, 5));
    }

    [Test]
    public void ComposeDailyHud_Active_ShowsParAndMistakes()
    {
        string s = GameplayScreen.ComposeDailyHud(2, 3);
        StringAssert.Contains("Par 2", s);
        StringAssert.Contains("Mistakes left: 3", s);
    }

    [Test]
    public void ComposeDailyHud_ZeroParIsStillActive()
    {
        // par == 0 is a valid daily par (>= 0): the slot shows, it does NOT release.
        Assert.AreNotEqual(string.Empty, GameplayScreen.ComposeDailyHud(0, 0));
        StringAssert.Contains("Par 0", GameplayScreen.ComposeDailyHud(0, 0));
    }
}
