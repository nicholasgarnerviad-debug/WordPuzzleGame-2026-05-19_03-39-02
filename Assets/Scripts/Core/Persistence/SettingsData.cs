using System;

namespace WordPuzzle.Persistence
{
    /// <summary>
    /// Persistent user-facing settings (Spec §3.2).
    /// Persisted via PlayerPrefs key "settings_v1" through DataManager.
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
                version = this.version
            };
        }
    }
}
