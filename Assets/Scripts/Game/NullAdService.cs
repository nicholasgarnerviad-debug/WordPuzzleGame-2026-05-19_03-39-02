using System;

namespace WordPuzzle
{
    /// <summary>
    /// No-op IAdService used in the Unity Editor and in EditMode tests where
    /// the real AdMob SDK is not present. Rewarded callbacks are never fired
    /// (correctly modelling "ad not available"), interstitials are silently skipped.
    /// </summary>
    public sealed class NullAdService : IAdService
    {
        public bool IsRewardedReady     => false;
        public bool IsInterstitialReady => false;

        public void LoadRewarded()     { }
        public void LoadInterstitial() { }

        public void ShowRewarded(Action onRewarded, Action onClosed)
        {
            // No reward granted — only onClosed fires, matching a dismissed/failed ad.
            onClosed?.Invoke();
        }

        public void ShowInterstitial(Action onClosed)
        {
            onClosed?.Invoke();
        }
    }
}
