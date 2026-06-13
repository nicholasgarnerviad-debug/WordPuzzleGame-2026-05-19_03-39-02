using System;
using UnityEngine;
using GoogleMobileAds.Ump.Api;

namespace WordPuzzle
{
    /// <summary>
    /// Track 2 (v1.0 audit) — the real UMP consent flow behind IConsentService (41A planned
    /// this as "Phase 2"; this is that phase). Runs Google's documented sequence:
    /// ConsentInformation.Update → ConsentForm.LoadAndShowConsentFormIfRequired → report
    /// CanRequestAds. Any failure completes the gather WITHOUT enabling ads (ConsentGate
    /// then keeps MobileAds.Initialize unreachable) — the game itself never blocks on consent.
    ///
    /// Exactly-once contract: Update-error path and form-callback path are mutually exclusive,
    /// so <see cref="Gather"/> invokes onComplete exactly once, success or failure.
    /// Callback threading: the GMA Unity plugin (v11) marshals UMP callbacks through its Unity
    /// layer; AdService additionally sets RaiseAdEventsOnUnityMainThread before any of this runs.
    /// </summary>
    public sealed class UmpConsentService : IConsentService
    {
        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public void Gather(Action onComplete)
        {
            var request = new ConsentRequestParameters();
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    // Offline / misconfigured: complete the gather; ads stay gated off.
                    Debug.LogWarning($"[UmpConsent] Update failed: {updateError.Message}");
                    onComplete?.Invoke();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                        Debug.LogWarning($"[UmpConsent] Form failed: {formError.Message}");
                    onComplete?.Invoke();
                });
            });
        }

        public void ShowPrivacyOptions(Action onClosed)
        {
            if (!PrivacyOptionsRequired) { onClosed?.Invoke(); return; }
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null)
                    Debug.LogWarning($"[UmpConsent] Privacy options failed: {error.Message}");
                onClosed?.Invoke();
            });
        }
    }
}
