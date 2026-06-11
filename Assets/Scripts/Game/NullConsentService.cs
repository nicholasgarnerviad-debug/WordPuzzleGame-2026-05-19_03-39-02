using System;

namespace WordPuzzle
{
    /// <summary>
    /// Editor/test default consent impl (Task 41A) — completes immediately with ads allowed,
    /// exactly like NullAdService keeps Editor play SDK-free. Never blocks boot.
    /// </summary>
    public sealed class NullConsentService : IConsentService
    {
        public bool CanRequestAds => true;
        public bool PrivacyOptionsRequired => false;

        public void Gather(Action onComplete) => onComplete?.Invoke();

        public void ShowPrivacyOptions(Action onClosed) => onClosed?.Invoke();
    }
}
