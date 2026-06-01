using System;

namespace WordPuzzle.Persistence
{
    /// <summary>
    /// Colorblind simulation mode.
    /// Off = default design-token palette.
    /// Deuteranopia = blue/orange-safe palette (no red/green reliance).
    /// </summary>
    public enum ColorBlindMode
    {
        Off,
        Deuteranopia
    }

    /// <summary>
    /// Persistent user-facing settings (Spec §3.2).
    /// Persisted via PlayerPrefs key "settings_v1" through DataManager.
    /// New fields (Task 9E) default safely; absent JSON keys leave Unity defaults intact.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        // Volume sliders: 0.0 – 1.0
        public float masterVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public float musicVolume = 0.7f;

        // Mute toggle (gates AudioListener.volume).
        public bool muted = false;

        // Task 7A — skip animations for accessibility.
        public bool reduceMotion = false;

        // Task 7B — device haptic feedback.
        public bool hapticsEnabled = true;

        // Task 9E — colorblind palette mode (defaults Off = normal palette).
        public ColorBlindMode colorBlindMode = ColorBlindMode.Off;

        // Task 9E — high-contrast mode (bold borders, max fill/text contrast).
        public bool highContrast = false;

        // Task 9E — text scale multiplier, clamped [1.0, 1.5] at apply time.
        public float textScale = 1.0f;

        // Schema version; bump on breaking shape changes.
        public int version = 1;

        public SettingsData() { }

        public SettingsData Clone()
        {
            return new SettingsData
            {
                masterVolume = this.masterVolume,
                sfxVolume = this.sfxVolume,
                musicVolume = this.musicVolume,
                muted = this.muted,
                reduceMotion = this.reduceMotion,
                hapticsEnabled = this.hapticsEnabled,
                colorBlindMode = this.colorBlindMode,
                highContrast = this.highContrast,
                textScale = this.textScale,
                version = this.version
            };
        }
    }
}
