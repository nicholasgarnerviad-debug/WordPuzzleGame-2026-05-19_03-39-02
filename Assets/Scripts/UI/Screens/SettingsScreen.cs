using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Settings screen (Spec §3.1).
    /// Hosts Master/SFX/Music volume sliders, a Mute toggle, and a destructive
    /// Reset Progress button (with confirm modal). Persists via
    /// IDataManager.SaveSettingsAsync (PlayerPrefs key "settings_v1") with a
    /// 250ms debounce. Master volume + mute drive AudioListener.volume in real time.
    /// SFX/Music sliders persist but currently have no audio bus —
    /// TODO: route to AudioMixer groups when an AudioMixer asset exists.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        // --- Sliders (0..1) ---
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        // --- Value labels (display "%d" 0-100) ---
        [SerializeField] private TextMeshProUGUI masterVolumeValueLabel;
        [SerializeField] private TextMeshProUGUI sfxVolumeValueLabel;
        [SerializeField] private TextMeshProUGUI musicVolumeValueLabel;

        // --- Toggle ---
        [SerializeField] private Toggle muteToggle;

        // --- Buttons ---
        [SerializeField] private Button homeButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button replayTutorialButton;

        // --- Confirm modal ---
        [SerializeField] private GameObject resetConfirmOverlay;
        [SerializeField] private Button resetConfirmCancelButton;
        [SerializeField] private Button resetConfirmResetButton;

        // --- Optional toast ---
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TextMeshProUGUI toastLabel;
        [SerializeField] private TextMeshProUGUI versionLabel;

        // Events for bootstrap/UIManager wiring.
        public event Action OnBackToMenu;
        /// <summary>Fired (debounced) when settings should be persisted.</summary>
        public event Action<SettingsData> OnSettingsSaved;
        /// <summary>Fired when user confirms the destructive Reset Progress action.</summary>
        public event Action OnResetProgressConfirmed;
        /// <summary>Fired when user requests to replay the tutorial.</summary>
        public event Action OnReplayTutorialRequested;

        private SettingsData currentSettings = new SettingsData();
        private bool suppressEvents;

        // Debounced save state.
        private const float SAVE_DEBOUNCE_SECONDS = 0.25f;
        private Coroutine pendingSaveCoroutine;

        // Toast state.
        private Coroutine pendingToastCoroutine;

        private void Awake()
        {
            if (versionLabel != null)
                versionLabel.text = "v" + Application.version;
        }

        private void OnEnable()
        {
            HookListeners();
            if (resetConfirmOverlay != null) resetConfirmOverlay.SetActive(false);
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        private void OnDisable()
        {
            UnhookListeners();

            // Flush a pending debounced save before the screen goes inactive
            // so transitions don't drop user input.
            if (pendingSaveCoroutine != null)
            {
                StopCoroutine(pendingSaveCoroutine);
                pendingSaveCoroutine = null;
                OnSettingsSaved?.Invoke(currentSettings.Clone());
            }

            if (pendingToastCoroutine != null)
            {
                StopCoroutine(pendingToastCoroutine);
                pendingToastCoroutine = null;
            }
        }

        private void HookListeners()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeClicked);
                var lbl = homeButton.GetComponentInChildren<TMP_Text>(true);
                if (lbl != null)
                {
                    lbl.text = "HOME";
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.fontSize = 28f;
                    lbl.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
                    lbl.alignment = TextAlignmentOptions.Center;
                }
            }
            if (resetProgressButton != null)
                resetProgressButton.onClick.AddListener(OnResetClicked);
            if (replayTutorialButton != null)
                replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
            if (resetConfirmCancelButton != null)
                resetConfirmCancelButton.onClick.AddListener(OnResetConfirmCancel);
            if (resetConfirmResetButton != null)
                resetConfirmResetButton.onClick.AddListener(OnResetConfirmReset);
        }

        private void UnhookListeners()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (muteToggle != null)
                muteToggle.onValueChanged.RemoveListener(OnMuteToggleChanged);

            if (homeButton != null)
                homeButton.onClick.RemoveListener(OnHomeClicked);
            if (resetProgressButton != null)
                resetProgressButton.onClick.RemoveListener(OnResetClicked);
            if (replayTutorialButton != null)
                replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);
            if (resetConfirmCancelButton != null)
                resetConfirmCancelButton.onClick.RemoveListener(OnResetConfirmCancel);
            if (resetConfirmResetButton != null)
                resetConfirmResetButton.onClick.RemoveListener(OnResetConfirmReset);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Populate UI fields from a loaded SettingsData. Does not raise
        /// OnSettingsSaved during seeding (no save loops).
        /// </summary>
        public void Populate(SettingsData settings)
        {
            if (settings == null) settings = new SettingsData();
            currentSettings = settings.Clone();

            suppressEvents = true;
            try
            {
                if (masterVolumeSlider != null) masterVolumeSlider.value = currentSettings.masterVolume;
                if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSettings.sfxVolume;
                if (musicVolumeSlider != null) musicVolumeSlider.value = currentSettings.musicVolume;

                if (muteToggle != null) muteToggle.isOn = currentSettings.muted;

                UpdateValueLabel(masterVolumeValueLabel, currentSettings.masterVolume);
                UpdateValueLabel(sfxVolumeValueLabel, currentSettings.sfxVolume);
                UpdateValueLabel(musicVolumeValueLabel, currentSettings.musicVolume);
            }
            finally
            {
                suppressEvents = false;
            }

            ApplyAudioListenerVolume(currentSettings);
        }

        /// <summary>Returns a clone of the in-screen settings (does not save).</summary>
        public SettingsData GetCurrentSettings() => currentSettings.Clone();

        // --- Slider handlers ---
        private void OnMasterVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.masterVolume = Mathf.Clamp01(v);
            UpdateValueLabel(masterVolumeValueLabel, currentSettings.masterVolume);
            ApplyAudioListenerVolume(currentSettings);
            ScheduleDebouncedSave();
        }

        private void OnSfxVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.sfxVolume = Mathf.Clamp01(v);
            UpdateValueLabel(sfxVolumeValueLabel, currentSettings.sfxVolume);
            // TODO: route to AudioMixer groups when audio system is added.
            ScheduleDebouncedSave();
        }

        private void OnMusicVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.musicVolume = Mathf.Clamp01(v);
            UpdateValueLabel(musicVolumeValueLabel, currentSettings.musicVolume);
            // TODO: route to AudioMixer groups when audio system is added.
            ScheduleDebouncedSave();
        }

        // --- Toggle handler ---
        private void OnMuteToggleChanged(bool muted)
        {
            if (suppressEvents) return;
            currentSettings.muted = muted;
            ApplyAudioListenerVolume(currentSettings);
            ScheduleDebouncedSave();
        }

        // --- Button handlers ---
        private void OnHomeClicked()
        {
            OnBackToMenu?.Invoke();
        }

        private void OnResetClicked()
        {
            // If a confirm overlay is wired up, show it; otherwise immediately fire.
            if (resetConfirmOverlay != null)
            {
                resetConfirmOverlay.SetActive(true);
            }
            else
            {
                OnResetProgressConfirmed?.Invoke();
                ShowToast("Progress reset");
            }
        }

        private void OnResetConfirmCancel()
        {
            if (resetConfirmOverlay != null) resetConfirmOverlay.SetActive(false);
        }

        private void OnResetConfirmReset()
        {
            if (resetConfirmOverlay != null) resetConfirmOverlay.SetActive(false);
            OnResetProgressConfirmed?.Invoke();
            ShowToast("Progress reset");
        }

        private void OnReplayTutorialClicked()
        {
            OnReplayTutorialRequested?.Invoke();
            ShowToast("Tutorial will replay");
        }

        // --- Helpers ---
        private static void UpdateValueLabel(TextMeshProUGUI label, float v01)
        {
            if (label == null) return;
            int pct = Mathf.RoundToInt(Mathf.Clamp01(v01) * 100f);
            label.text = pct.ToString();
        }

        private void ScheduleDebouncedSave()
        {
            if (!isActiveAndEnabled)
            {
                // Defensive: fire immediately if we can't coroutine.
                OnSettingsSaved?.Invoke(currentSettings.Clone());
                return;
            }
            if (pendingSaveCoroutine != null) StopCoroutine(pendingSaveCoroutine);
            pendingSaveCoroutine = StartCoroutine(DebouncedSaveRoutine());
        }

        private IEnumerator DebouncedSaveRoutine()
        {
            yield return new WaitForSecondsRealtime(SAVE_DEBOUNCE_SECONDS);
            pendingSaveCoroutine = null;
            OnSettingsSaved?.Invoke(currentSettings.Clone());
        }

        public void ShowToast(string text)
        {
            if (toastRoot == null || toastLabel == null) return;
            toastLabel.text = text;
            toastRoot.SetActive(true);

            if (pendingToastCoroutine != null) StopCoroutine(pendingToastCoroutine);
            pendingToastCoroutine = StartCoroutine(HideToastAfter(1.5f));
        }

        private IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (toastRoot != null) toastRoot.SetActive(false);
            pendingToastCoroutine = null;
        }

        /// <summary>
        /// Apply current settings to AudioListener.volume.
        /// Master gates global output; muted hard-mutes (volume = 0).
        /// Per spec, we set AudioListener.volume directly (no AudioListener.pause).
        /// </summary>
        public static void ApplyAudioListenerVolume(SettingsData settings)
        {
            if (settings == null) return;
            AudioListener.volume = settings.muted
                ? 0f
                : Mathf.Clamp01(settings.masterVolume);
        }
    }
}
