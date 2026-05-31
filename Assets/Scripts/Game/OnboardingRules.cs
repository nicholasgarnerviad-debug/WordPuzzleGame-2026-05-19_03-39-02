using WordPuzzle.Persistence;

namespace WordPuzzle.Game
{
    /// <summary>
    /// Pure, dependency-free onboarding routing rules.
    /// All inputs are explicit — no persistence, no MonoBehaviour.
    /// </summary>
    public static class OnboardingRules
    {
        public static bool ShouldRouteToTutorial(OnboardingData d)
            => d == null || !d.completed;

        public static OnboardingData MarkCompleted(OnboardingData d, bool skipped)
        {
            if (d == null) d = new OnboardingData();
            d.completed = true;
            if (skipped) d.skipped = true;
            return d;
        }

        public static OnboardingData Reset(OnboardingData d)
        {
            if (d == null) d = new OnboardingData();
            d.completed = false;
            d.skipped = false;
            return d;
        }
    }
}
