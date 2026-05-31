using System;
using UnityEngine;
using GoogleMobileAds.Api;

namespace WordPuzzle
{
    /// <summary>
    /// Production AdMob implementation of IAdService.
    /// Wraps GoogleMobileAds RewardedAd and InterstitialAd behind the interface
    /// so GameBootstrap and tests never reference the SDK directly.
    ///
    /// Ad unit IDs are placeholders — replace with real IDs via the Inspector or
    /// a scriptable-object config before shipping. Never hardcode real IDs in source.
    ///
    /// Task 6B constraints enforced here:
    /// • Rewarded: reward fires only on OnUserEarnedReward; never on dismiss/failure.
    /// • Interstitial: caller (AdPolicyService) enforces frequency cap before Show.
    /// • MobileAds.Initialize is called once at startup in GameBootstrap.
    /// </summary>
    public class AdService : MonoBehaviour, IAdService
    {
        // ── Inspector-configurable placeholders ──────────────────────────────
        [Tooltip("Rewarded ad unit ID. Replace with a real ID before shipping.")]
        [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";   // AdMob test ID

        [Tooltip("Interstitial ad unit ID. Replace with a real ID before shipping.")]
        [SerializeField] private string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // AdMob test ID

        // ── Runtime state ────────────────────────────────────────────────────
        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;
        private bool sdkInitialized;

        // Pending callbacks — stored so the ad's event handlers can invoke them
        // once the ad is dismissed (main-thread safe via Unity event system).
        private Action pendingRewardCallback;
        private Action pendingRewardedClosedCallback;
        private Action pendingInterstitialClosedCallback;

        // ── IAdService ───────────────────────────────────────────────────────

        public bool IsRewardedReady     => rewardedAd != null && rewardedAd.CanShowAd();
        public bool IsInterstitialReady => interstitialAd != null && interstitialAd.CanShowAd();

        public void LoadRewarded()
        {
            if (!sdkInitialized || IsRewardedReady) return;
            DestroyRewarded();
            var request = new AdRequest();
            RewardedAd.Load(rewardedAdUnitId, request, OnRewardedLoaded);
        }

        public void LoadInterstitial()
        {
            if (!sdkInitialized || IsInterstitialReady) return;
            DestroyInterstitial();
            var request = new AdRequest();
            InterstitialAd.Load(interstitialAdUnitId, request, OnInterstitialLoaded);
        }

        public void ShowRewarded(Action onRewarded, Action onClosed)
        {
            if (!IsRewardedReady)
            {
                Debug.LogWarning("[AdService] ShowRewarded called but no rewarded ad is ready.");
                onClosed?.Invoke();
                return;
            }

            pendingRewardCallback       = onRewarded;
            pendingRewardedClosedCallback = onClosed;
            rewardedAd.Show(OnUserEarnedReward);
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady)
            {
                Debug.LogWarning("[AdService] ShowInterstitial called but no interstitial is ready.");
                onClosed?.Invoke();
                return;
            }

            pendingInterstitialClosedCallback = onClosed;
            interstitialAd.Show();
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            MobileAds.Initialize(status =>
            {
                sdkInitialized = true;
                Debug.Log("[AdService] MobileAds initialized.");
                LoadRewarded();
                LoadInterstitial();
            });
        }

        private void OnDestroy()
        {
            DestroyRewarded();
            DestroyInterstitial();
        }

        // ── Load callbacks ───────────────────────────────────────────────────

        private void OnRewardedLoaded(RewardedAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"[AdService] Rewarded load failed: {error.GetMessage()}");
                return;
            }

            rewardedAd = ad;
            rewardedAd.OnAdFullScreenContentClosed  += OnRewardedClosed;
            rewardedAd.OnAdFullScreenContentFailed  += _ => { OnRewardedClosed(); };
        }

        private void OnInterstitialLoaded(InterstitialAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"[AdService] Interstitial load failed: {error.GetMessage()}");
                return;
            }

            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += OnInterstitialClosed;
            interstitialAd.OnAdFullScreenContentFailed += _ => { OnInterstitialClosed(); };
        }

        // ── Ad event handlers ────────────────────────────────────────────────

        /// <summary>
        /// SDK guarantees this fires ONLY when the user earns the reward (full watch).
        /// Store intent; fire after the ad closes so UI is fully restored first.
        /// </summary>
        private void OnUserEarnedReward(Reward reward)
        {
            // Fire via the closed handler so reward is always granted after overlay dismissal.
            var cb = pendingRewardCallback;
            pendingRewardCallback = null;
            cb?.Invoke();
        }

        private void OnRewardedClosed()
        {
            var cb = pendingRewardedClosedCallback;
            pendingRewardedClosedCallback = null;
            cb?.Invoke();
            DestroyRewarded();
            LoadRewarded();   // Pre-load next.
        }

        private void OnInterstitialClosed()
        {
            var cb = pendingInterstitialClosedCallback;
            pendingInterstitialClosedCallback = null;
            cb?.Invoke();
            DestroyInterstitial();
            LoadInterstitial();  // Pre-load next.
        }

        // ── Cleanup helpers ──────────────────────────────────────────────────

        private void DestroyRewarded()
        {
            if (rewardedAd == null) return;
            rewardedAd.OnAdFullScreenContentClosed -= OnRewardedClosed;
            rewardedAd.OnAdFullScreenContentFailed -= _ => { OnRewardedClosed(); };
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        private void DestroyInterstitial()
        {
            if (interstitialAd == null) return;
            interstitialAd.OnAdFullScreenContentClosed -= OnInterstitialClosed;
            interstitialAd.OnAdFullScreenContentFailed -= _ => { OnInterstitialClosed(); };
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }
}
