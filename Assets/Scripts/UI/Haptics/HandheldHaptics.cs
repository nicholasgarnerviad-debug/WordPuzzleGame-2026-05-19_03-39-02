using System;

namespace WordPuzzle.UI
{
    /// <summary>
    /// IHaptics implementation backed by UnityEngine.Handheld.Vibrate (Task 7B).
    /// Enabled-gate is injected so callers can wire it to SettingsData.hapticsEnabled.
    /// </summary>
    /// <remarks>
    /// TODO: Handheld.Vibrate is a coarse full-device buzz; fine-grained per-event
    /// haptics need a plugin (e.g. NiceVibrations/Lofelt). NOT added per task constraints.
    /// </remarks>
    public sealed class HandheldHaptics : IHaptics
    {
        private readonly Func<bool> _enabled;
        private readonly Action _vibrate;

        /// <param name="enabled">Returns true when haptics are permitted (reads SettingsData.hapticsEnabled).</param>
        /// <param name="vibrate">Custom vibrate implementation; defaults to UnityEngine.Handheld.Vibrate().</param>
        public HandheldHaptics(Func<bool> enabled, Action vibrate = null)
        {
            _enabled = enabled ?? (() => true);
            _vibrate = vibrate ?? (() => UnityEngine.Handheld.Vibrate());
        }

        /// <inheritdoc/>
        public void LightTap()  { if (_enabled()) _vibrate(); }

        /// <inheritdoc/>
        public void MediumTap() { if (_enabled()) _vibrate(); }

        /// <inheritdoc/>
        public void Buzz()      { if (_enabled()) _vibrate(); }
    }
}
