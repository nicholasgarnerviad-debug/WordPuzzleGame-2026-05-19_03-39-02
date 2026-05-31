using System;

/// <summary>
/// Ad-service abstraction. Placed in Game.Puzzle (no dependencies) so every
/// assembly — including tests — can reference it without circular deps.
///
/// Design rules (Task 6B):
/// • ShowRewarded is ALWAYS opt-in; caller shows a confirmation prompt first.
/// • Reward callback fires only on the SDK's explicit "earned reward" event.
/// • Interstitial cadence is controlled by AdPolicyService (frequency cap).
/// • Never auto-play ads; never show mid-puzzle.
/// </summary>
public interface IAdService
{
    /// <summary>True when a rewarded ad is loaded and ready to present.</summary>
    bool IsRewardedReady { get; }

    /// <summary>True when an interstitial is loaded and ready to present.</summary>
    bool IsInterstitialReady { get; }

    /// <summary>
    /// Request the SDK to load a rewarded ad in the background.
    /// Safe to call repeatedly; no-ops if already loading/loaded.
    /// </summary>
    void LoadRewarded();

    /// <summary>Request the SDK to load an interstitial in the background.</summary>
    void LoadInterstitial();

    /// <summary>
    /// Show the loaded rewarded ad. <paramref name="onRewarded"/> fires exactly once
    /// if and only if the SDK confirms the user fully watched the ad.
    /// <paramref name="onClosed"/> fires when the ad overlay is dismissed (regardless of reward).
    /// Both callbacks are dispatched on the Unity main thread.
    /// </summary>
    void ShowRewarded(Action onRewarded, Action onClosed);

    /// <summary>
    /// Show the loaded interstitial. <paramref name="onClosed"/> fires on dismiss.
    /// Caller (AdPolicyService) is responsible for frequency-cap enforcement before calling.
    /// </summary>
    void ShowInterstitial(Action onClosed);
}
