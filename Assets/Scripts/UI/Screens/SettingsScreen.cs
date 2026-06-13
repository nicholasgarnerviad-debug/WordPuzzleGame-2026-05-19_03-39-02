using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Settings screen (Spec §3.1; layout REBUILT AT RUNTIME — Settings polish pass).
    /// The grouped sections (Audio / Accessibility / Data) are built in code under a scroll
    /// view — the same code-driven approach as StatsScreen — so nothing is hand-placed or
    /// clipped and the page survives Large Text. Persists via IDataManager.SaveSettingsAsync
    /// ("settings_v1") with a 250ms debounce.
    ///
    /// Control reality (honest, not faked):
    ///  • Master volume + Mute drive AudioListener.volume in real time.
    ///  • SFX/Music sliders persist but have NO audio bus yet (no AudioMixer) — flagged on-screen.
    ///  • Reduce Motion (UIAnimations.ReduceMotion) and Haptics (IHaptics seam) take effect app-wide.
    ///  • Colorblind / High Contrast / Large Text currently drive the gameplay tile palette + tile
    ///    text (AccessiblePalette, consumed by LetterTile). Full app-wide reach is a separate task.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        // --- Reused scene objects (kept; everything else under the root is hidden + rebuilt) ---
        [SerializeField] private Button homeButton;
        [SerializeField] private GameObject resetConfirmOverlay;
        [SerializeField] private Button resetConfirmCancelButton;
        [SerializeField] private Button resetConfirmResetButton;
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TextMeshProUGUI toastLabel;
        [SerializeField] private TextMeshProUGUI versionLabel;

        // Events for bootstrap/UIManager wiring (unchanged public API).
        public event Action OnBackToMenu;
        /// <summary>Fired (debounced) when settings should be persisted.</summary>
        public event Action<SettingsData> OnSettingsSaved;
        /// <summary>Fired when user confirms the destructive Reset Progress action.</summary>
        public event Action OnResetProgressConfirmed;
        /// <summary>Fired when user requests to replay the tutorial.</summary>
        public event Action OnReplayTutorialRequested;

        // v1.0 audit Track 2 — UMP privacy-options re-prompt (visible only when Required).
        public event Action OnPrivacyOptionsRequested;
        private Transform _privacySection;

        private SettingsData currentSettings = new SettingsData();
        private bool suppressEvents;

        private const float SAVE_DEBOUNCE_SECONDS = 0.25f;
        private Coroutine pendingSaveCoroutine;
        private Coroutine pendingToastCoroutine;

        // --- Runtime-built controls ---
        private Slider _masterSlider, _sfxSlider, _musicSlider;
        private TMP_Text _masterVal, _sfxVal, _musicVal;
        private Toggle _muteToggle, _reduceMotionToggle, _hapticsToggle, _colorBlindToggle;
        private Button _resetButton, _replayButton;
        // Runtime-built reset-confirm modal (replaces the scene-authored overlay, which rendered
        // transparent + behind the cards — no scrim, wrong sibling order).
        private GameObject _resetModal;
        private RectTransform _resetModalCard;
        // Switch visuals so each toggle can show a clear, colorblind-safe ON/OFF (track colour + knob position).
        private readonly Dictionary<Toggle, (Image track, RectTransform knob)> _switchVisuals = new Dictionary<Toggle, (Image, RectTransform)>();
        private readonly Dictionary<Toggle, Coroutine> _switchAnims = new Dictionary<Toggle, Coroutine>();
        private bool _built;

        // --- Palette tokens (no inline hex) ---
        private static Color Accent  => MenuPalette.TitleColor;     // cyan — fill / ON state
        private static Color Track   => GameAccents.CardOutline;    // slate — groove / OFF state
        private static Color Knob    => MenuPalette.SecondaryLabel; // cream — handle / knob
        private static Color Label   => MenuPalette.SecondaryLabel; // cream — control labels
        private static Color Muted   => MenuPalette.SecondaryBorder;// muted — captions / notes
        private static Color Header  => MenuPalette.TitleColor;     // cyan — section headers

        private void Awake()
        {
            if (versionLabel != null) versionLabel.text = "v" + Application.version;
        }

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject, UIThemeManager.ReadabilityScrimAlpha); // shared backdrop + readability scrim
            EnsureBuilt();
            if (resetConfirmOverlay != null) resetConfirmOverlay.SetActive(false);
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        private void OnDisable()
        {
            // Flush a pending debounced save before the screen goes inactive so transitions don't drop input.
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

        public void Show() { gameObject.SetActive(true); UIAnimations.PlayScreenEntrance(this); }

        /// <summary>Toggle the PRIVACY band (UMP privacy options) — caller knows the consent state.</summary>
        public void SetPrivacyOptionsVisible(bool visible)
        {
            if (_privacySection != null) _privacySection.gameObject.SetActive(visible);
        }
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Populate UI fields from a loaded SettingsData. Does not raise OnSettingsSaved during
        /// seeding (no save loops). Builds the layout first if needed.
        /// </summary>
        public void Populate(SettingsData settings)
        {
            if (settings == null) settings = new SettingsData();
            currentSettings = settings.Clone();
            EnsureBuilt();

            suppressEvents = true;
            try
            {
                if (_masterSlider != null) _masterSlider.value = currentSettings.masterVolume;
                if (_sfxSlider != null)    _sfxSlider.value    = currentSettings.sfxVolume;
                if (_musicSlider != null)  _musicSlider.value  = currentSettings.musicVolume;

                SetToggle(_muteToggle,          currentSettings.muted);
                SetToggle(_reduceMotionToggle,  currentSettings.reduceMotion);
                SetToggle(_hapticsToggle,       currentSettings.hapticsEnabled);
                SetToggle(_colorBlindToggle,    currentSettings.colorBlindMode != ColorBlindMode.Off);

                UpdateValueLabel(_masterVal, currentSettings.masterVolume);
                UpdateValueLabel(_sfxVal,    currentSettings.sfxVolume);
                UpdateValueLabel(_musicVal,  currentSettings.musicVolume);
            }
            finally { suppressEvents = false; }

            ApplyAudioListenerVolume(currentSettings);
            AccessiblePalette.Apply(currentSettings); // seed the accessible palette immediately
        }

        /// <summary>Returns a clone of the in-screen settings (does not save).</summary>
        public SettingsData GetCurrentSettings() => currentSettings.Clone();

        private void SetToggle(Toggle t, bool on)
        {
            if (t == null) return;
            t.isOn = on;
            RefreshSwitchVisual(t, on, false); // snap visual to match even if isOn didn't change (no event fired)
        }

        // ============================================================
        //  Runtime layout (built once)
        // ============================================================
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            NormalizeRoot();              // defensive: full-screen, scale 1 — fixes the off-left clipping
            HideAuthoredChildren();       // hide the broken scene-authored controls; keep the reused chrome
            BuildTitle();
            AnchorHome();
            AnchorVersion();
            BuildScrollContent();
            BuildResetConfirmModal();
        }

        // The off-left clipping came from a mis-anchored / scaled scene root: force the screen to fill the
        // canvas at scale 1 so nothing is pushed off the edges (and chrome stays on-screen).
        private void NormalizeRoot()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        // Hide every direct child except the reused chrome (HOME, version, the guarded reset modal, the toast).
        private void HideAuthoredChildren()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (IsKept(child)) continue;
                child.gameObject.SetActive(false);
            }
        }

        private bool IsKept(Transform child)
        {
            return (homeButton != null && child == homeButton.transform)
                || (versionLabel != null && child == versionLabel.transform)
                || (resetConfirmOverlay != null && child == resetConfirmOverlay.transform)
                || (toastRoot != null && child == toastRoot.transform);
        }

        private void BuildTitle()
        {
            var t = MakeText(transform, "SETTINGS", TypeRole.Headline, Header, TextAlignmentOptions.Center);
            t.characterSpacing = 6f;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -120f); // below the notch
            rt.sizeDelta = new Vector2(700f, 70f);
        }

        // HOME — top-left, fully on-screen (clears the notch).
        private void AnchorHome()
        {
            if (homeButton == null) return;
            homeButton.onClick.RemoveListener(OnHomeClicked);
            homeButton.onClick.AddListener(OnHomeClicked);
            // Task 43 tier 3 — HOME recedes to a ghost (matches stats/shop/library). The ghost
            // helper owns the label role + tint, so set the text first.
            var lbl = homeButton.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null)
            {
                lbl.text = "HOME";
                lbl.alignment = TextAlignmentOptions.Center;
            }
            UIThemeManager.ApplyGhostButton(homeButton, Palette.AccentPeriwinkle);
            var rt = homeButton.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(40f, -110f);
                // ≥96px hit height — the ghost tier's comfortable minimum (Task 46 / 43).
                if (rt.sizeDelta.x < 150f || rt.sizeDelta.y < 96f) rt.sizeDelta = new Vector2(190f, 96f);
                rt.localScale = Vector3.one;
            }
        }

        private void AnchorVersion()
        {
            if (versionLabel == null) return;
            var rt = versionLabel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            TypeScale.Apply(versionLabel, TypeRole.Caption); // Task 42 — build version is Caption
            versionLabel.alignment = TextAlignmentOptions.Center;
            versionLabel.color = Muted;
        }

        // A scroll view filling the space between the title and the version line, so the sections never
        // clip and the page survives Large Text (overflow scrolls instead of running off-screen).
        private void BuildScrollContent()
        {
            var scrollGo = new GameObject("SettingsScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(60f, 90f);    // leave room for version
            srt.offsetMax = new Vector2(-60f, -210f); // leave room for the title
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            var vimg = viewport.GetComponent<Image>(); vimg.color = new Color(0f, 0f, 0f, 0f); vimg.raycastTarget = true;
            scroll.viewport = vrt;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(0f, 0f); crt.offsetMax = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 22f; vlg.padding = new RectOffset(0, 0, 0, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt;

            // ── AUDIO ──
            var audio = MakeSection(crt, "AUDIO");
            _masterSlider = MakeSliderRow(audio, "Master", out _masterVal, OnMasterVolumeChanged);
            _musicSlider  = MakeSliderRow(audio, "Music",  out _musicVal,  OnMusicVolumeChanged);
            _sfxSlider    = MakeSliderRow(audio, "SFX",    out _sfxVal,    OnSfxVolumeChanged);
            _muteToggle   = MakeToggleRow(audio, "Mute",   OnMuteToggleChanged);
            var note = MakeText(audio, "Music & SFX volumes are saved but silent until audio is added.",
                                TypeRole.Caption, Muted, TextAlignmentOptions.Left);
            note.enableWordWrapping = true;
            note.gameObject.AddComponent<LayoutElement>().minHeight = 36f;

            // ── ACCESSIBILITY ──
            var a11y = MakeSection(crt, "ACCESSIBILITY");
            _reduceMotionToggle = MakeToggleRow(a11y, "Reduce Motion",   OnReduceMotionChanged);
            _hapticsToggle      = MakeToggleRow(a11y, "Haptics",         OnHapticsToggleChanged);
            _colorBlindToggle   = MakeToggleRow(a11y, "Colorblind Mode", OnColorBlindModeChanged);
            var note2 = MakeText(a11y, "Colorblind Mode recolors the gameplay tiles.",
                                 TypeRole.Caption, Muted, TextAlignmentOptions.Left);
            note2.enableWordWrapping = true;
            note2.gameObject.AddComponent<LayoutElement>().minHeight = 32f;

            // ── DATA ──
            var data = MakeSection(crt, "DATA");
            _resetButton = MakeActionButton(data, "RESET PROGRESS", GameAccents.Danger, Knob, OnResetClicked);
            var warn = MakeText(data, "Erases all puzzle progress permanently.",
                                TypeRole.Caption, GameAccents.Danger, TextAlignmentOptions.Left);
            warn.enableWordWrapping = true;
            warn.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            _replayButton = MakeActionButton(data, "REPLAY TUTORIAL", Muted, Knob, OnReplayTutorialClicked);

            // ── PRIVACY (v1.0 audit Track 2) — UMP consent re-prompt. The whole band stays
            // hidden unless the consent impl reports options are Required (EEA/UK traffic);
            // GameBootstrap refreshes the visibility every time Settings opens.
            _privacySection = MakeSection(crt, "PRIVACY");
            MakeActionButton(_privacySection, "PRIVACY OPTIONS", Muted, Knob,
                () => OnPrivacyOptionsRequested?.Invoke());
            var pnote = MakeText(_privacySection, "Review or change your ads consent choices.",
                                 TypeRole.Caption, Muted, TextAlignmentOptions.Left);
            pnote.enableWordWrapping = true;
            pnote.gameObject.AddComponent<LayoutElement>().minHeight = 32f;
            _privacySection.gameObject.SetActive(false);

            // The scene-authored confirm overlay rendered transparent + behind the cards; it's
            // superseded by BuildResetConfirmModal(). Force it off so it can never flash through.
            if (resetConfirmOverlay != null) resetConfirmOverlay.SetActive(false);
        }

        // A proper destructive-confirm modal (the shared modal recipe: full SurfaceVoid scrim +
        // a SOLID danger-ringed card on top, last-sibling so it covers everything and the scrim
        // swallows taps). Built once, hidden; shown by OnResetClicked.
        private void BuildResetConfirmModal()
        {
            _resetModal = new GameObject("ResetConfirmModal", typeof(RectTransform), typeof(Image));
            _resetModal.transform.SetParent(transform, false);
            var mrt = (RectTransform)_resetModal.transform;
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            mrt.localScale = Vector3.one;
            var scrim = _resetModal.GetComponent<Image>();
            scrim.color = new Color(Palette.SurfaceVoid.r, Palette.SurfaceVoid.g, Palette.SurfaceVoid.b, 0.86f);
            scrim.raycastTarget = true; // modal — block taps to the screen behind

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image),
                                      typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(_resetModal.transform, false);
            _resetModalCard = (RectTransform)card.transform;
            _resetModalCard.anchorMin = _resetModalCard.anchorMax = new Vector2(0.5f, 0.5f);
            _resetModalCard.pivot = new Vector2(0.5f, 0.5f);
            _resetModalCard.sizeDelta = new Vector2(820f, 0f);
            var cimg = card.GetComponent<Image>(); cimg.raycastTarget = true;
            UIThemeManager.ApplySolidCard(cimg, GameAccents.Danger); // danger ring — destructive surface
            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 18f; vlg.padding = new RectOffset(44, 44, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            card.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = MakeText(card.transform, "Reset all progress?", TypeRole.Title, Header, TextAlignmentOptions.Center);
            title.gameObject.AddComponent<LayoutElement>().minHeight = 54f;
            var body = MakeText(card.transform,
                "This permanently erases puzzle history, coins, and unlocks. This cannot be undone.",
                TypeRole.Body, Muted, TextAlignmentOptions.Center);
            body.enableWordWrapping = true;
            body.gameObject.AddComponent<LayoutElement>().minHeight = 88f;

            // Action row — CANCEL (safe, cream outline) beside RESET (danger). Both ≥96px tall.
            var row = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(card.transform, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.spacing = 16f; hlg.childAlignment = TextAnchor.MiddleCenter;
            row.GetComponent<LayoutElement>().minHeight = 96f;
            MakeModalButton(row.transform, "CANCEL", Muted, OnResetConfirmCancel);
            MakeModalButton(row.transform, "RESET",  GameAccents.Danger, OnResetConfirmReset);

            _resetModal.SetActive(false);
        }

        // A full-width outline action button for the confirm modal (≥96px hit target).
        private void MakeModalButton(Transform parent, string label, Color border, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>(); le.minHeight = 96f; le.preferredHeight = 96f; le.flexibleWidth = 1f;
            var btn = go.GetComponent<Button>();
            var t = MakeText(go.transform, label, TypeRole.Label, border, TextAlignmentOptions.Center);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
            t.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(btn, border, border);
            btn.onClick.AddListener(onClick);
        }

        // ── Section card (outline ring, header, auto-sized) ──
        private Transform MakeSection(Transform parent, string title)
        {
            var go = new GameObject("Section_" + title, typeof(RectTransform), typeof(Image),
                                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplySolidCard(img, GameAccents.CardOutline); // solid card — backdrop art stops bleeding through the rows
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 14f; vlg.padding = new RectOffset(28, 28, 20, 22);
            vlg.childAlignment = TextAnchor.UpperLeft;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var h = MakeText(go.transform, title, TypeRole.Title, Header, TextAlignmentOptions.Left);
            h.characterSpacing = 4f;
            h.gameObject.AddComponent<LayoutElement>().minHeight = 56f; // Title (44) needs more than the old 26pt row
            return go.transform;
        }

        // ── A "Label  [====O----]  NN" slider row; returns the Slider, out-returns the value label. ──
        private Slider MakeSliderRow(Transform section, string label, out TMP_Text valueLabel, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var row = MakeRow(section, 60f);
            var cap = MakeText(row, label, TypeRole.Body, Label, TextAlignmentOptions.Left);
            var capLe = cap.gameObject.AddComponent<LayoutElement>(); capLe.preferredWidth = 150f; capLe.flexibleWidth = 0f;

            var slider = BuildSlider(row);
            slider.onValueChanged.AddListener(onChanged);

            valueLabel = MakeText(row, "0", TypeRole.Body, Knob, TextAlignmentOptions.Right);
            var vle = valueLabel.gameObject.AddComponent<LayoutElement>(); vle.preferredWidth = 64f; vle.flexibleWidth = 0f;
            return slider;
        }

        // Constructs a functional horizontal Slider: slate rounded groove, cyan rounded fill, cream knob.
        private Slider BuildSlider(Transform parent)
        {
            const float trackH = 16f, knobD = 34f;
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>(); le.flexibleWidth = 1f; le.minHeight = 48f;
            var slider = go.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            // The shared bubbly 9-slice carries 44px corner art — at ppu 1 the corners exceed
            // these small rects and render as fat blobs. Divide the radius to fit: track/fill
            // 16px tall -> ~8px radius (44/5.5); the 34px knob -> ~17px radius (44/2.6) = a circle.
            const float stripPpu = 5.5f, knobPpu = 2.6f;

            // Groove (full-width slate track, behind everything).
            var bg = MakeImage(go.transform, "Track", Track);
            UIThemeManager.ApplyRoundedButton(bg, stripPpu);
            StretchCenteredY(bg.rectTransform, 0f, trackH);

            // Fill Area (padded by the knob radius so the fill aligns with the knob's travel).
            var fillArea = MakeRect(go.transform, "Fill Area");
            StretchCenteredY(fillArea, -knobD, trackH);
            var fill = MakeImage(fillArea, "Fill", Accent);
            UIThemeManager.ApplyRoundedButton(fill, stripPpu);
            var frt = fill.rectTransform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            slider.fillRect = frt;

            // Handle Slide Area (padded) + cream knob.
            var handleArea = MakeRect(go.transform, "Handle Slide Area");
            StretchCenteredY(handleArea, -knobD, 0f);
            var handle = MakeImage(handleArea, "Handle", Knob);
            UIThemeManager.ApplyRoundedButton(handle, knobPpu); // a true circle, not an oval blob
            handle.rectTransform.sizeDelta = new Vector2(knobD, knobD);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        // ── A "Label .......... [switch]" toggle row; returns the Toggle. ──
        private Toggle MakeToggleRow(Transform section, string label, UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var row = MakeRow(section, 60f);
            var cap = MakeText(row, label, TypeRole.Body, Label, TextAlignmentOptions.Left);
            cap.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var toggle = BuildSwitch(row);
            toggle.onValueChanged.AddListener(onChanged);                                  // updates the setting (gated by suppressEvents)
            toggle.onValueChanged.AddListener(on => RefreshSwitchVisual(toggle, on, true)); // glide the knob to reflect the new state
            return toggle;
        }

        // A pill switch: rounded track (slate OFF / cyan ON) + a cream knob that slides left/right. The knob
        // position is a non-colour cue, so the state is clear in colorblind mode too.
        private Toggle BuildSwitch(Transform parent)
        {
            const float w = 66f, h = 36f, knobD = 28f;
            var go = new GameObject("Switch", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>(); le.preferredWidth = w; le.minWidth = w; le.minHeight = h;
            var toggle = go.GetComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = null; // we drive visuals manually (avoids the ColorBlock dark-tint pitfall)

            // Same 44px-corner-art rule as the sliders: divide the radius to fit the pill.
            // 36px track -> ~18px radius (44/2.4); 28px knob -> ~14px radius (44/3.1) = a circle.
            var track = MakeImage(go.transform, "SwitchTrack", Track);
            UIThemeManager.ApplyRoundedButton(track, 2.4f);
            var trt = track.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f); trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(w, h);
            toggle.targetGraphic = track;

            var knob = MakeImage(go.transform, "SwitchKnob", Knob);
            UIThemeManager.ApplyRoundedButton(knob, 3.1f);
            var krt = knob.rectTransform;
            krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f); krt.pivot = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(knobD, knobD);

            _switchVisuals[toggle] = (track, krt);
            RefreshSwitchVisual(toggle, toggle.isOn, false);
            return toggle;
        }

        private void RefreshSwitchVisual(Toggle toggle, bool on, bool animate)
        {
            if (toggle == null || !_switchVisuals.TryGetValue(toggle, out var v)) return;
            float targetX = on ? 15f : -15f;
            Color targetCol = on ? Accent : Track;

            if (_switchAnims.TryGetValue(toggle, out var running) && running != null)
            {
                StopCoroutine(running);
                _switchAnims[toggle] = null;
            }

            // Snap on seed/build, when motion is reduced, or while inactive; otherwise glide for a smooth feel.
            if (!animate || UIAnimations.ReduceMotion || !isActiveAndEnabled)
            {
                if (v.knob != null)  v.knob.anchoredPosition = new Vector2(targetX, 0f);
                if (v.track != null) v.track.color = targetCol;
                return;
            }
            _switchAnims[toggle] = StartCoroutine(AnimateSwitch(toggle, v.knob, v.track, targetX, targetCol));
        }

        private IEnumerator AnimateSwitch(Toggle toggle, RectTransform knob, Image track, float targetX, Color targetCol)
        {
            const float dur = 0.13f;
            float t = 0f;
            Vector2 startPos = knob != null ? knob.anchoredPosition : Vector2.zero;
            Color startCol = track != null ? track.color : targetCol;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                if (knob != null)  knob.anchoredPosition = new Vector2(Mathf.Lerp(startPos.x, targetX, p), 0f);
                if (track != null) track.color = Color.Lerp(startCol, targetCol, p);
                yield return null;
            }
            if (knob != null)  knob.anchoredPosition = new Vector2(targetX, 0f);
            if (track != null) track.color = targetCol;
            if (toggle != null) _switchAnims[toggle] = null;
        }

        private Button MakeActionButton(Transform section, string label, Color border, Color labelColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(section, false);
            var le = go.GetComponent<LayoutElement>(); le.minHeight = 68f; le.flexibleWidth = 1f;
            var btn = go.GetComponent<Button>();
            var t = MakeText(go.transform, label, TypeRole.Label, labelColor, TextAlignmentOptions.Center);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
            t.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(btn, border, labelColor);
            btn.onClick.AddListener(onClick);
            return btn;
        }

        // ── tiny builders ──
        private RectTransform MakeRow(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.spacing = 16f; hlg.childAlignment = TextAnchor.MiddleLeft;
            go.GetComponent<LayoutElement>().minHeight = height;
            return (RectTransform)go.transform;
        }

        private TMP_Text MakeText(Transform parent, string text, TypeRole role, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            TypeScale.Apply(t, role); // Task 42 — font/size/weight from the role
            t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.enableWordWrapping = false;
            return t;
        }

        private Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = color;
            return img;
        }

        private RectTransform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Stretch a rect horizontally, centered vertically, with optional horizontal inset and fixed height
        // (height 0 = full height of the parent).
        private static void StretchCenteredY(RectTransform rt, float horizontalSizeDelta, float height)
        {
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(horizontalSizeDelta, height);
            if (height == 0f) { rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f); rt.sizeDelta = new Vector2(horizontalSizeDelta, 0f); }
        }

        // ============================================================
        //  Handlers (unchanged behaviour)
        // ============================================================
        private void OnMasterVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.masterVolume = Mathf.Clamp01(v);
            UpdateValueLabel(_masterVal, currentSettings.masterVolume);
            ApplyAudioListenerVolume(currentSettings);
            ScheduleDebouncedSave();
        }

        private void OnSfxVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.sfxVolume = Mathf.Clamp01(v);
            UpdateValueLabel(_sfxVal, currentSettings.sfxVolume);
            // TODO: route to AudioMixer groups when an audio system is added (no audible effect yet).
            ScheduleDebouncedSave();
        }

        private void OnMusicVolumeChanged(float v)
        {
            if (suppressEvents) return;
            currentSettings.musicVolume = Mathf.Clamp01(v);
            UpdateValueLabel(_musicVal, currentSettings.musicVolume);
            // TODO: route to AudioMixer groups when an audio system is added (no audible effect yet).
            ScheduleDebouncedSave();
        }

        private void OnMuteToggleChanged(bool muted)
        {
            if (suppressEvents) return;
            currentSettings.muted = muted;
            ApplyAudioListenerVolume(currentSettings);
            ScheduleDebouncedSave();
        }

        private void OnReduceMotionChanged(bool value)
        {
            if (suppressEvents) return;
            currentSettings.reduceMotion = value;
            UIAnimations.ReduceMotion = value; // take effect app-wide immediately
            ScheduleDebouncedSave();
        }

        private void OnHapticsToggleChanged(bool value)
        {
            if (suppressEvents) return;
            currentSettings.hapticsEnabled = value;
            ScheduleDebouncedSave();
        }

        private void OnColorBlindModeChanged(bool enabled)
        {
            if (suppressEvents) return;
            currentSettings.colorBlindMode = enabled ? ColorBlindMode.Deuteranopia : ColorBlindMode.Off;
            AccessiblePalette.Apply(currentSettings);
            ScheduleDebouncedSave();
        }

        private void OnHomeClicked() => OnBackToMenu?.Invoke();

        private void OnResetClicked()
        {
            if (_resetModal == null) { OnResetProgressConfirmed?.Invoke(); ShowToast("Progress reset"); return; }
            _resetModal.transform.SetAsLastSibling();   // cover the whole screen
            _resetModal.SetActive(true);
            if (_resetModalCard != null && isActiveAndEnabled)
                StartCoroutine(UIAnimations.StaggeredPop(new[] { _resetModalCard })); // ReduceMotion-safe pop
        }

        private void OnResetConfirmCancel()
        {
            if (_resetModal != null) _resetModal.SetActive(false);
        }

        private void OnResetConfirmReset()
        {
            if (_resetModal != null) _resetModal.SetActive(false);
            OnResetProgressConfirmed?.Invoke();
            ShowToast("Progress reset");
        }

        private void OnReplayTutorialClicked()
        {
            OnReplayTutorialRequested?.Invoke();
            ShowToast("Tutorial will replay");
        }

        // ── Helpers ──
        private static void UpdateValueLabel(TMP_Text label, float v01)
        {
            if (label == null) return;
            label.text = Mathf.RoundToInt(Mathf.Clamp01(v01) * 100f).ToString();
        }

        private void ScheduleDebouncedSave()
        {
            if (!isActiveAndEnabled)
            {
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
        /// Apply current settings to AudioListener.volume. Master gates global output; muted hard-mutes.
        /// </summary>
        public static void ApplyAudioListenerVolume(SettingsData settings)
        {
            if (settings == null) return;
            AudioListener.volume = settings.muted ? 0f : Mathf.Clamp01(settings.masterVolume);
        }
    }
}
