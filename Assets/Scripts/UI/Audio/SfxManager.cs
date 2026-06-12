using UnityEngine;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Pooled SFX playback manager (Task 7C).
    /// Clips are assigned in-scene via SerializeField; all may be null (no-op).
    /// Volume is derived from SettingsData — muted or zero volume => no playback.
    /// No AudioMixer: volume applied directly to AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxManager : MonoBehaviour
    {
        private const int POOL_SIZE = 4;

        [SerializeField] private AudioClip keyPressClip;
        [SerializeField] private AudioClip wordAcceptClip;
        [SerializeField] private AudioClip wordRejectClip;
        [SerializeField] private AudioClip winStingClip;
        [SerializeField] private AudioClip starPopClip;     // Task 45 — daily star pop (no clip yet)
        [SerializeField] private AudioClip celebrationClip; // Task 45 — celebration modal (no clip yet)

        private AudioSource[] _pool;
        private int _poolIndex;
        private SettingsData _settings;

        private void Awake()
        {
            _pool = new AudioSource[POOL_SIZE];
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool[i] = src;
            }
        }

        /// <summary>Called by GameBootstrap after load and after every OnSettingsSaved.</summary>
        public void SetSettings(SettingsData s) => _settings = s;

        /// <summary>
        /// Pure static helper — returns the effective SFX volume given current settings.
        /// Returns 0 when muted or either volume is zero. Exposed for unit testing.
        /// </summary>
        public static float EffectiveSfxVolume(SettingsData s)
        {
            if (s == null || s.muted) return 0f;
            return Mathf.Clamp01(s.masterVolume) * Mathf.Clamp01(s.sfxVolume);
        }

        public void PlayKeyPress()  => Play(keyPressClip);
        public void PlayAccept()    => Play(wordAcceptClip);
        public void PlayReject()    => Play(wordRejectClip);
        public void PlayWin()       => Play(winStingClip);

        // Task 45 — celebration slots (results payout + modals). No clips ship yet (AudioMixer is
        // §13 tech debt) so these are deliberate no-ops until clips drop into the scene fields.
        public void PlayStarPop()     => Play(starPopClip);
        public void PlayCelebration() => Play(celebrationClip);

        private void Play(AudioClip clip)
        {
            float v = EffectiveSfxVolume(_settings);
            if (v <= 0f || clip == null) return;
            var src = GetPooledSource();
            src.volume = v;
            src.PlayOneShot(clip);
        }

        private AudioSource GetPooledSource()
        {
            // Round-robin: prefer a source that is not currently playing.
            for (int i = 0; i < POOL_SIZE; i++)
            {
                int idx = (_poolIndex + i) % POOL_SIZE;
                if (!_pool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % POOL_SIZE;
                    return _pool[idx];
                }
            }
            // All busy — evict oldest (current _poolIndex).
            var evicted = _pool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % POOL_SIZE;
            return evicted;
        }
    }
}
