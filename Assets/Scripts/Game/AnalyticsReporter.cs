using WordPuzzle.Puzzle;

namespace WordPuzzle
{
    /// <summary>
    /// The ONE place taxonomy events are assembled (Task 41B) — plain C#, constructor-injected
    /// IAnalytics, owned by GameBootstrap. Seam classes that can't see IAnalytics (UI assembly
    /// has no Puzzle reference) raise their existing C# events; GameBootstrap forwards here.
    ///
    /// Contracts the tests pin down:
    /// • daily_result fires exactly once per completed daily run (ReportDailyResult);
    ///   the one-and-done re-tap routes through ReportDailyReShow, which emits NOTHING.
    /// • puzzle_complete has one emission point per surface (Classic win panel OR EndGame —
    ///   Classic never reaches EndGame, so never both for one puzzle).
    /// </summary>
    public sealed class AnalyticsReporter
    {
        private readonly IAnalytics analytics;

        public AnalyticsReporter(IAnalytics analytics)
        {
            this.analytics = analytics ?? new NullAnalytics();
        }

        public void SessionStart() => analytics.Log("session_start");

        public void TutorialStep(int step) => analytics.Log("tutorial_step", ("step", step));
        public void TutorialDone(bool skipped) => analytics.Log("tutorial_done", ("skipped", skipped));

        public void ModeStart(string mode) => analytics.Log("mode_start", ("mode", mode));

        public void PuzzleComplete(string mode, int steps, bool win)
            => analytics.Log("puzzle_complete", ("mode", mode), ("steps", steps), ("win", win));

        /// <summary>Run-end emission — the ONLY daily_result source.</summary>
        public void DailyResult(PathScoreResult ps, int streak)
            => analytics.Log("daily_result",
                ("grade", ps.grade.ToString()),
                ("stars", ps.stars),
                ("par", ps.par),
                ("steps", ps.playerSteps),
                ("detours", ps.detours),
                ("mistakes", ps.mistakesUsed),
                ("used_powerup", ps.usedPowerUp),
                ("streak", streak));

        /// <summary>
        /// The one-and-done re-tap (ShowStoredDailyResult) routes through here BY CONTRACT and
        /// emits nothing — re-viewing a finished daily is not a result. Tests + the Task-41
        /// canary guard this stays empty of daily_result.
        /// </summary>
        public void DailyReShow() { }

        public void ShareTapped(string mode) => analytics.Log("share_tapped", ("mode", mode));

        public void ShopOpen() => analytics.Log("shop_open");
        public void PurchaseAttempt(string product) => analytics.Log("purchase_attempt", ("product", product));
        public void PurchaseResult(string product, string status)
            => analytics.Log("purchase_result", ("product", product), ("status", status));
        public void PowerUpBundleBought(string kind, int size)
            => analytics.Log("powerup_bundle_bought", ("kind", kind), ("size", size));

        public void AdRewarded(string placement) => analytics.Log("ad_rewarded", ("placement", placement));
        public void AdInterstitial() => analytics.Log("ad_interstitial");
    }
}
