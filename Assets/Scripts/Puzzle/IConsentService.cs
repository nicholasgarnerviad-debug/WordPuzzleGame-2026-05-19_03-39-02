using System;

/// <summary>
/// Consent gate ahead of ad initialization (Task 41A). Mirrors the IAdService seam idiom:
/// interface in the Puzzle assembly (Unity/SDK-free), impls in Game — NullConsentService is
/// the Editor/test default; UmpConsentService runs Google's UMP Update → LoadAndShowForm
/// flow on device. MobileAds.Initialize must be unreachable until Gather completes AND
/// CanRequestAds is true.
/// </summary>
public interface IConsentService
{
    /// <summary>True when ads may be requested (consent gathered / not required).</summary>
    bool CanRequestAds { get; }

    /// <summary>True when the impl offers a privacy-options form (UMP "required" state).</summary>
    bool PrivacyOptionsRequired { get; }

    /// <summary>Run the consent flow; ALWAYS invokes onComplete exactly once (success or failure).</summary>
    void Gather(Action onComplete);

    /// <summary>Show the privacy-options form (Settings row). No-op when not required.</summary>
    void ShowPrivacyOptions(Action onClosed);
}

/// <summary>
/// Pure decision: ad init may only happen after the consent flow has completed and reported
/// ads are requestable. Kept trivially small so the ordering contract is unit-testable.
/// </summary>
public static class ConsentGate
{
    public static bool ShouldInitAds(bool gathered, bool canRequestAds)
        => gathered && canRequestAds;
}
