/// <summary>
/// Analytics event spine (Task 41B). Flat, Firebase-shaped, no SDK types — impls live in the
/// Game assembly (LogAnalytics = Debug.Log live default; FirebaseAnalytics is the documented
/// swap-in once google-services.json lands; NullAnalytics for tests).
///
/// Taxonomy (snake_case, the WHOLE surface — no PII, no typed words, no constructed IDs):
///   session_start · tutorial_step{step} · tutorial_done{skipped} · mode_start{mode} ·
///   puzzle_complete{mode,steps,win} · daily_result{grade,stars,par,steps,detours,mistakes,
///   used_powerup,streak} · share_tapped{mode} · shop_open · purchase_attempt{product} ·
///   purchase_result{product,status} · powerup_bundle_bought{kind,size} ·
///   ad_rewarded{placement} · ad_interstitial
/// </summary>
public interface IAnalytics
{
    void Log(string eventName);
    void Log(string eventName, params (string key, object value)[] p);
}
