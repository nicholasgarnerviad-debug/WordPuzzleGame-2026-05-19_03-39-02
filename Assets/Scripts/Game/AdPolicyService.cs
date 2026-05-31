using System;
using UnityEngine;

namespace WordPuzzle
{
    /// <summary>
    /// Enforces the interstitial frequency cap and the "remove ads" flag.
    ///
    /// Task 6B rules:
    /// • Interstitials are shown only BETWEEN sessions (after EndGame), never mid-puzzle.
    /// • Minimum gap: BalanceConfig.InterstitialCooldownSeconds real-time seconds AND
    ///   BalanceConfig.InterstitialPuzzleCap completed puzzles since the last impression.
    /// • If AdsRemoved is true no interstitial is ever shown (rewarded video remains opt-in).
    /// • All caps live in BalanceConfig so QA can tune without touching this class.
    /// </summary>
    public class AdPolicyService
    {
        private readonly IAdService adService;

        // Injectable time source (seconds). Defaults to Time.realtimeSinceStartup;
        // tests supply a controllable provider so the cooldown branch is verifiable.
        private readonly Func<float> now;

        // ── State ────────────────────────────────────────────────────────────
        private float lastInterstitialRealTime = float.NegativeInfinity;
        private int puzzlesSinceLastInterstitial = 0;

        // ── Remove-ads stub ──────────────────────────────────────────────────
        /// <summary>
        /// Set to true (e.g. after an IAP) to suppress all interstitials.
        /// Rewarded video is always opt-in and unaffected by this flag.
        /// </summary>
        public bool AdsRemoved { get; set; } = false;

        public AdPolicyService(IAdService adService, Func<float> nowProvider = null)
        {
            this.adService = adService ?? throw new ArgumentNullException(nameof(adService));
            this.now = nowProvider ?? (() => Time.realtimeSinceStartup);
        }

        /// <summary>
        /// Call once each time a puzzle completes so the puzzle-count cap stays current.
        /// </summary>
        public void RecordPuzzleCompleted()
        {
            puzzlesSinceLastInterstitial++;
        }

        /// <summary>
        /// Try to show an interstitial between sessions. Returns false (no-op) when:
        /// • AdsRemoved is true, OR
        /// • cooldown has not elapsed, OR
        /// • puzzle cap has not been reached, OR
        /// • no interstitial is loaded.
        /// Never call mid-puzzle.
        /// </summary>
        public bool TryShowInterstitial(Action onClosed = null)
        {
            if (AdsRemoved)
                return false;

            float elapsed = now() - lastInterstitialRealTime;
            if (elapsed < BalanceConfig.InterstitialCooldownSeconds)
                return false;

            if (puzzlesSinceLastInterstitial < BalanceConfig.InterstitialPuzzleCap)
                return false;

            if (!adService.IsInterstitialReady)
                return false;

            lastInterstitialRealTime = now();
            puzzlesSinceLastInterstitial = 0;
            adService.ShowInterstitial(onClosed);
            return true;
        }

        /// <summary>
        /// Expose cap state for tests / UI (e.g. "N more puzzles until next ad").
        /// </summary>
        public int PuzzlesSinceLastInterstitial => puzzlesSinceLastInterstitial;
    }
}
