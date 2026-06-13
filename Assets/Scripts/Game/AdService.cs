using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;
using WordPuzzle.Puzzle;

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
    ///
    /// Task 39 hardening:
    /// • 39A: ad events are forced onto the Unity main thread (see Awake).
    /// • 39B: failed loads retry with AdRetryPolicy exponential backoff instead of
    ///   silently killing the rewarded faucets for the session.
    /// • 39C: the reward callback is deferred to the closed handler (matching the
    ///   documented contract) and event unsubscribes use stored delegate fields.
    /// </summary>
    public class AdService : MonoBehaviour, IAdService
    {
        // ── Inspector-configurable placeholders ──────────────────────────────
        [Tooltip("Rewarded ad unit ID. Replace with a real ID before shipping.")]
        [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";   // AdMob test ID

        [Tooltip("Interstitial ad unit ID. Replace with a real ID before shipping.")]
        [SerializeField] private string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // AdMob test ID

        [Tooltip("Editor only — allow GMA v11 placeholder ads in Play Mode. OFF keeps the test " +
                 "suite deterministic (gameplay tests assume the never-ready ad state).")]
#pragma warning disable 0414 // read only inside the UNITY_EDITOR branch of Awake
        [SerializeField] private bool enableAdsInEditor = false;
#pragma warning restore 0414

        // ── Runtime state ────────────────────────────────────────────────────
        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;
        private bool sdkInitialized;

        // Pending callbacks — stored so the ad's event handlers can invoke them
        // once the ad is dismissed (main-thread safe via Unity event system).
        private Action pendingRewardCallback;
        private Action pendingRewardedClosedCallback;
        private Action pendingInterstitialClosedCallback;

        // 39C — the SDK reported OnUserEarnedReward for the showing ad; the reward
        // callback fires from the closed handler so UI is fully restored first.
        private bool rewardEarned;

        // 39C — registered event handlers, stored so DestroyRewarded/DestroyInterstitial
        // can unsubscribe the EXACT delegate instances (a `-= new lambda` is a no-op).
        private Action rewardedClosedHandler;
        private Action<AdError> rewardedFailedHandler;
        private Action interstitialClosedHandler;
        private Action<AdError> interstitialFailedHandler;

        // 39B — load-retry state, tracked per ad type. Counters reset on success.
        private int rewardedRetryAttempt;
        private int interstitialRetryAttempt;
        private Coroutine rewardedRetryRoutine;
        private Coroutine interstitialRetryRoutine;

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

            rewardEarned = false;
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

        // 41A — consent gate ahead of ad init. NullConsentService (Editor/test default,
        // completes immediately) until the UMP impl is wired; resolved in Awake.
        private IConsentService consent;

        /// <summary>The active consent impl (Settings reads PrivacyOptionsRequired off it).</summary>
        public IConsentService Consent => consent;

        private void Awake()
        {
            // 39A — the SDK raises ad events on background threads by default; our
            // callbacks touch Unity objects and the economy, so force main-thread.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            // Track 1 (v1.0 audit) — production unit IDs live in Resources/Config/ad_units.json
            // (this component is added at runtime by BootstrapInitializer, so there is no scene
            // Inspector to set them on; the doc rule stands: real IDs never hardcoded in source).
            // Missing/empty config keeps the test-ID defaults above.
            LoadUnitIdConfig();

            // 41A — MobileAds.Initialize is UNREACHABLE until the consent flow completes
            // and reports ads may be requested (AdMob policy for EEA/UK traffic).
#if UNITY_EDITOR
            // Editor: Null consent (instant), and ad init stays OFF unless explicitly enabled —
            // GMA v11 placeholder ads would otherwise pop mid-PlayMode-test.
            consent = new NullConsentService();
            if (!enableAdsInEditor) return; // sdkInitialized stays false → never ready, like NullAdService
#else
            // Device: the real UMP flow (Track 2). Failure/declined keeps ads gated off; the
            // game itself never blocks on consent.
            consent = new UmpConsentService();
#endif
            consent.Gather(() =>
            {
                if (!ConsentGate.ShouldInitAds(gathered: true, canRequestAds: consent.CanRequestAds))
                {
                    Debug.LogWarning("[AdService] Consent gathered but ads not requestable; skipping init.");
                    return;
                }

                MobileAds.Initialize(status =>
                {
                    sdkInitialized = true;
                    GameLog.Log("[AdService] MobileAds initialized."); // editor-only (release-stripped)
                    LoadRewarded();
                    LoadInterstitial();
                });
            });
        }

        [Serializable]
        private class AdUnitConfig
        {
            public string rewardedAdUnitId;
            public string interstitialAdUnitId;
        }

        private void LoadUnitIdConfig()
        {
            var asset = Resources.Load<TextAsset>("Config/ad_units");
            if (asset == null) return; // no config — keep the test-ID defaults
            AdUnitConfig cfg = null;
            try { cfg = JsonUtility.FromJson<AdUnitConfig>(asset.text); } catch { cfg = null; }
            if (cfg == null) return;
            if (!string.IsNullOrEmpty(cfg.rewardedAdUnitId))     rewardedAdUnitId     = cfg.rewardedAdUnitId;
            if (!string.IsNullOrEmpty(cfg.interstitialAdUnitId)) interstitialAdUnitId = cfg.interstitialAdUnitId;
        }

        private void OnDestroy()
        {
            CancelRewardedRetry();
            CancelInterstitialRetry();
            DestroyRewarded();
            DestroyInterstitial();
        }

        // ── Load callbacks ───────────────────────────────────────────────────

        private void OnRewardedLoaded(RewardedAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"[AdService] Rewarded load failed: {error.GetMessage()}");
                ScheduleRewardedRetry();
                return;
            }

            rewardedRetryAttempt = 0;   // success resets the backoff curve

            rewardedAd = ad;
            rewardedClosedHandler = OnRewardedClosed;
            rewardedFailedHandler = OnRewardedShowFailed;
            rewardedAd.OnAdFullScreenContentClosed += rewardedClosedHandler;
            rewardedAd.OnAdFullScreenContentFailed += rewardedFailedHandler;
        }

        private void OnInterstitialLoaded(InterstitialAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogWarning($"[AdService] Interstitial load failed: {error.GetMessage()}");
                ScheduleInterstitialRetry();
                return;
            }

            interstitialRetryAttempt = 0;   // success resets the backoff curve

            interstitialAd = ad;
            interstitialClosedHandler = OnInterstitialClosed;
            interstitialFailedHandler = OnInterstitialShowFailed;
            interstitialAd.OnAdFullScreenContentClosed += interstitialClosedHandler;
            interstitialAd.OnAdFullScreenContentFailed += interstitialFailedHandler;
        }

        // ── 39B: load retry with exponential backoff ────────────────────────

        private void ScheduleRewardedRetry()
        {
            CancelRewardedRetry();
            float delay = AdRetryPolicy.NextDelaySeconds(rewardedRetryAttempt);
            rewardedRetryAttempt++;
            rewardedRetryRoutine = StartCoroutine(RetryAfter(delay, () =>
            {
                rewardedRetryRoutine = null;
                LoadRewarded();
            }));
        }

        private void ScheduleInterstitialRetry()
        {
            CancelInterstitialRetry();
            float delay = AdRetryPolicy.NextDelaySeconds(interstitialRetryAttempt);
            interstitialRetryAttempt++;
            interstitialRetryRoutine = StartCoroutine(RetryAfter(delay, () =>
            {
                interstitialRetryRoutine = null;
                LoadInterstitial();
            }));
        }

        private static IEnumerator RetryAfter(float delaySeconds, Action retry)
        {
            // Realtime so the timer keeps ticking through timescale changes and
            // resumes correctly after paused/backgrounded states.
            yield return new WaitForSecondsRealtime(delaySeconds);
            retry();
        }

        private void CancelRewardedRetry()
        {
            if (rewardedRetryRoutine == null) return;
            StopCoroutine(rewardedRetryRoutine);
            rewardedRetryRoutine = null;
        }

        private void CancelInterstitialRetry()
        {
            if (interstitialRetryRoutine == null) return;
            StopCoroutine(interstitialRetryRoutine);
            interstitialRetryRoutine = null;
        }

        // ── Ad event handlers ────────────────────────────────────────────────

        /// <summary>
        /// SDK guarantees this fires ONLY when the user earns the reward (full watch).
        /// Store intent; fire after the ad closes so UI is fully restored first.
        /// </summary>
        private void OnUserEarnedReward(Reward reward)
        {
            // 39C — flag only; OnRewardedClosed grants so the overlay is gone first.
            rewardEarned = true;
        }

        private void OnRewardedClosed()
        {
            // 39C — grant the earned reward (if any) BEFORE the closed callback so
            // callers observe the documented order. Dismiss/failure never grants.
            var rewardCb = pendingRewardCallback;
            pendingRewardCallback = null;
            if (rewardEarned)
            {
                rewardEarned = false;
                rewardCb?.Invoke();
            }

            var cb = pendingRewardedClosedCallback;
            pendingRewardedClosedCallback = null;
            cb?.Invoke();
            DestroyRewarded();
            LoadRewarded();   // Pre-load next.
        }

        private void OnRewardedShowFailed(AdError error)
        {
            // Show failed → no reward was earned; closed callback only.
            OnRewardedClosed();
        }

        private void OnInterstitialClosed()
        {
            var cb = pendingInterstitialClosedCallback;
            pendingInterstitialClosedCallback = null;
            cb?.Invoke();
            DestroyInterstitial();
            LoadInterstitial();  // Pre-load next.
        }

        private void OnInterstitialShowFailed(AdError error)
        {
            OnInterstitialClosed();
        }

        // ── Cleanup helpers ──────────────────────────────────────────────────

        private void DestroyRewarded()
        {
            if (rewardedAd == null) return;
            // 39C — unsubscribe the exact registered delegate instances.
            if (rewardedClosedHandler != null)
                rewardedAd.OnAdFullScreenContentClosed -= rewardedClosedHandler;
            if (rewardedFailedHandler != null)
                rewardedAd.OnAdFullScreenContentFailed -= rewardedFailedHandler;
            rewardedClosedHandler = null;
            rewardedFailedHandler = null;
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        private void DestroyInterstitial()
        {
            if (interstitialAd == null) return;
            // 39C — unsubscribe the exact registered delegate instances.
            if (interstitialClosedHandler != null)
                interstitialAd.OnAdFullScreenContentClosed -= interstitialClosedHandler;
            if (interstitialFailedHandler != null)
                interstitialAd.OnAdFullScreenContentFailed -= interstitialFailedHandler;
            interstitialClosedHandler = null;
            interstitialFailedHandler = null;
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }
}
