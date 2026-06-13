using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    /// <summary>
    /// First-launch onboarding overlay (Tutorial redo — Layer 1). Built at RUNTIME (no scene
    /// authoring, no gold) over the normal gameplay flow:
    ///
    ///   ShowWelcome()  — optional offer: PLAY TUTORIAL / SKIP (shown over the menu on first launch).
    ///   Begin(par)     — the "learn by doing" lesson on the real tutorial puzzle:
    ///     Beat 1 (rule)         "Change ONE letter to make a new word."   (highlight active row)
    ///     [first accepted move]
    ///     Beat 2 (guided)       "Nice — one letter changed, one step closer."   ┐ tap-to-continue
    ///     Beat 3 (routes)       "There's often more than one path — aim SHORT." │ (info beats, the
    ///     Beat 4 (par/mistakes) "Par is the fewest steps (here: N) ..."         ┘  board is gated)
    ///     Beat 5 (finish)       "Now reach the end word."                   (highlight end row)
    ///     [reach target] → win beat → OnSuccessBeatFinished
    ///
    /// GameBootstrap subscribes: OnSkipRequested -> CompleteTutorial(skipped:true);
    /// OnSuccessBeatFinished -> CompleteTutorial(skipped:false). ShowWelcome's callbacks drive
    /// Play (-> StartTutorialRun) and Skip (-> skip to menu). All motion honors ReduceMotion.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        // Scene anchors (positions only) reused to point the highlight at the start / end rows.
        [SerializeField] private RectTransform startRowAnchor;
        [SerializeField] private RectTransform endRowAnchor;
        // Legacy scene children (superseded by the runtime UI below) — hidden in EnsureBuilt if present.
        [SerializeField] private TextMeshProUGUI calloutText;   // legacy; hidden
        [SerializeField] private Button skipButton;             // legacy; hidden
        [SerializeField] private RectTransform highlightFrame;  // reused for the row highlight (restyled)

        public event Action OnSkipRequested;
        public event Action OnSuccessBeatFinished;

        // Task 41B — lesson beat shown (1=Rule … 5=Finish). The UI assembly can't see
        // IAnalytics; GameBootstrap subscribes and forwards to the analytics reporter.
        public event Action<int> OnStepShown;

        // --- Centralized copy (no scattered magic strings) ---
        private const string WelcomeTitle = "Welcome to Star Ladder";
        private const string WelcomeBody  = "New here? Take a quick, hands-on tour of the basics.";
        private const string PlayLabel    = "PLAY TUTORIAL";
        private const string SkipLabel    = "SKIP";
        private const string Beat1Rule    = "Change ONE letter to make a new word.";
        private const string Beat2Moved   = "Nice — one letter changed, one step closer.";
        private const string Beat3Routes  = "There's often more than one path — aim for a SHORT one.";
        private const string Beat4ParFmt  = "Par is the fewest steps possible (here: {0}). In daily play rule-breaking guesses are limited — typos bounce free, so plan your route.";
        private const string Beat5Finish  = "Now reach the end word — one step at a time.";
        private const string WinText      = "You've got it! 🎉";
        private const string TapHint      = "tap to continue  ▸";
        private const float  SuccessBeatDelay = 1.1f;
        private const float  FadeDuration     = 0.18f;

        private enum Step { None, Welcome, Rule, Moved, Routes, ParMistakes, Finish, Winning }
        private Step _step = Step.None;
        private int  _par  = 2;

        private bool _built;
        private Action _onPlay, _onSkip;
        private Coroutine _successCo, _fadeCo;

        // Runtime UI
        private GameObject _welcomePanel, _calloutCard;
        private RectTransform _welcomeCard;
        private Button _tapCatcher, _lessonSkip, _playButton, _welcomeSkipButton;
        private TMP_Text _calloutLabel, _tapHintLabel;
        private CanvasGroup _calloutGroup;

        // ── Public contract ───────────────────────────────────────────────────────────

        private void Awake() => EnsureBuilt(); // build + hide legacy children even if the scene left this active

        /// <summary>First-launch offer. <paramref name="onPlay"/> runs the lesson; <paramref name="onSkip"/> skips to the menu.</summary>
        public void ShowWelcome(Action onPlay, Action onSkip)
        {
            _onPlay = onPlay; _onSkip = onSkip;
            gameObject.SetActive(true);
            EnsureBuilt();
            _step = Step.Welcome;
            _welcomePanel.SetActive(true);
            _calloutCard.SetActive(false);
            _tapCatcher.gameObject.SetActive(false);
            SetHighlight(null);
            if (_welcomeCard != null && isActiveAndEnabled)
                StartCoroutine(UIAnimations.StaggeredPop(new[] { _welcomeCard })); // ReduceMotion-safe pop
        }

        /// <summary>Start the lesson on the real puzzle (Beat 1). <paramref name="par"/> = the puzzle's optimal steps.</summary>
        public void Begin(int par)
        {
            _par = Mathf.Max(1, par);
            gameObject.SetActive(true);
            EnsureBuilt();
            if (_welcomePanel != null) _welcomePanel.SetActive(false);
            _step = Step.Rule;
            OnStepShown?.Invoke(1);   // Task 41B
            ShowCallout(Beat1Rule, infoBeat: false);
            SetHighlight(startRowAnchor);
        }

        /// <summary>Driven by GameBootstrap on each submission during the lesson.</summary>
        public void OnSubmission(bool accepted, bool reachedEnd)
        {
            if (_step == Step.None || _step == Step.Welcome) return;

            if (reachedEnd)
            {
                _step = Step.Winning;
                ShowCallout(WinText, infoBeat: false);
                SetHighlight(null);
                if (_successCo != null) StopCoroutine(_successCo);
                _successCo = StartCoroutine(SuccessBeatRoutine());
                return;
            }

            // First accepted move kicks off the guided info beats (board gated until Beat 5).
            if (accepted && _step == Step.Rule)
            {
                _step = Step.Moved;
                OnStepShown?.Invoke(2);   // Task 41B
                ShowCallout(Beat2Moved, infoBeat: true);
                SetHighlight(null);
            }
        }

        /// <summary>Deactivate + reset.</summary>
        public void Hide()
        {
            if (_successCo != null) { StopCoroutine(_successCo); _successCo = null; }
            if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
            _step = Step.None;
            gameObject.SetActive(false);
        }

        // ── Step advance (info beats: tap to continue) ──────────────────────────────────
        private void AdvanceInfoBeat()
        {
            switch (_step)
            {
                case Step.Moved:
                    _step = Step.Routes;
                    OnStepShown?.Invoke(3);   // Task 41B
                    ShowCallout(Beat3Routes, infoBeat: true);
                    break;
                case Step.Routes:
                    _step = Step.ParMistakes;
                    OnStepShown?.Invoke(4);   // Task 41B
                    ShowCallout(string.Format(Beat4ParFmt, _par), infoBeat: true);
                    break;
                case Step.ParMistakes:
                    _step = Step.Finish;
                    OnStepShown?.Invoke(5);   // Task 41B
                    ShowCallout(Beat5Finish, infoBeat: false); // board freed — let them finish
                    SetHighlight(endRowAnchor);
                    break;
            }
        }

        // ── Runtime build (once) ────────────────────────────────────────────────────────
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            // Hide legacy scene children (superseded by the runtime UI; keeps any old gold off-screen).
            if (calloutText != null) calloutText.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);

            BuildTapCatcher();
            BuildCalloutCard();
            BuildWelcomePanel();
            StyleHighlight();
        }

        // Full-rect transparent button BEHIND the callout: tapping anywhere advances an info beat.
        private void BuildTapCatcher()
        {
            var go = new GameObject("TapCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Stretch((RectTransform)go.transform);
            var img = go.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0f); img.raycastTarget = true;
            _tapCatcher = go.GetComponent<Button>();
            _tapCatcher.transition = Selectable.Transition.None;
            _tapCatcher.onClick.AddListener(AdvanceInfoBeat);
            go.SetActive(false);
        }

        // Modern cyan-outline callout card near the top, with the lesson text, a "tap to continue" hint,
        // and a Skip button. Card visuals don't eat taps (so the catcher behind advances info beats).
        private void BuildCalloutCard()
        {
            _calloutCard = new GameObject("CalloutCard", typeof(RectTransform), typeof(Image),
                                          typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
            _calloutCard.transform.SetParent(transform, false);
            var rt = (RectTransform)_calloutCard.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -220f);
            rt.sizeDelta = new Vector2(900f, 0f);
            var img = _calloutCard.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(img, MenuPalette.TitleColor); // cyan ring, transparent centre
            var vlg = _calloutCard.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 10f; vlg.padding = new RectOffset(34, 34, 26, 22);
            vlg.childAlignment = TextAnchor.UpperCenter;
            _calloutCard.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _calloutGroup = _calloutCard.GetComponent<CanvasGroup>();

            _calloutLabel = MakeText(_calloutCard.transform, "", TypeRole.Body, MenuPalette.SecondaryLabel, TextAlignmentOptions.Center);
            _calloutLabel.enableWordWrapping = true; // holds under Large Text

            _tapHintLabel = MakeText(_calloutCard.transform, TapHint, TypeRole.Caption, MenuPalette.SecondaryBorder, TextAlignmentOptions.Center);

            _lessonSkip = MakeOutlineButton(_calloutCard.transform, SkipLabel, MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel, 200f, 56f);
            _lessonSkip.onClick.AddListener(() => OnSkipRequested?.Invoke());

            _calloutCard.SetActive(false);
        }

        // Centered welcome card: title, one line, PLAY TUTORIAL + SKIP. A dim backing blocks the menu.
        // Modern modal recipe (matches DailyRewardPopup): heavy SurfaceVoid scrim + a SOLID card —
        // the old outline-only card had a transparent centre, so the menu bled through the copy.
        private void BuildWelcomePanel()
        {
            _welcomePanel = new GameObject("WelcomePanel", typeof(RectTransform), typeof(Image));
            _welcomePanel.transform.SetParent(transform, false);
            Stretch((RectTransform)_welcomePanel.transform);
            var dim = _welcomePanel.GetComponent<Image>();
            dim.color = new Color(Palette.SurfaceVoid.r, Palette.SurfaceVoid.g, Palette.SurfaceVoid.b, 0.86f); // Task 46 — SurfaceVoid dim (modal-grade alpha, same as DailyRewardPopup)
            dim.raycastTarget = true;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image),
                                      typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(_welcomePanel.transform, false);
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero; crt.sizeDelta = new Vector2(820f, 0f);
            var cimg = card.GetComponent<Image>(); cimg.raycastTarget = true;
            UIThemeManager.ApplySolidCard(cimg, MenuPalette.TitleColor); // the one card seam (fill + aqua ring overlay)
            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 18f; vlg.padding = new RectOffset(44, 44, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            card.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _welcomeCard = crt;

            var title = MakeText(card.transform, WelcomeTitle, TypeRole.Title, MenuPalette.TitleColor, TextAlignmentOptions.Center);
            title.gameObject.AddComponent<LayoutElement>().minHeight = 54f;
            var body = MakeText(card.transform, WelcomeBody, TypeRole.Body, MenuPalette.SecondaryLabel, TextAlignmentOptions.Center);
            body.enableWordWrapping = true;
            body.gameObject.AddComponent<LayoutElement>().minHeight = 70f;

            // Task 46 — both welcome actions meet the ≥88px hit target (96 = the ghost-tier minimum).
            // Task 43 tiers: PLAY TUTORIAL is this surface's ONE filled hero (the DAILY gradient);
            // SKIP recedes to a ghost — tinted text on an invisible full-width hit target.
            _playButton = MakeOutlineButton(card.transform, PlayLabel, MenuPalette.TitleColor, MenuPalette.SecondaryLabel, 0f, 96f);
            UIThemeManager.ApplyFilledHeroButton(_playButton, Palette.ModeDaily, Palette.ModePuzzleShow);
            _playButton.onClick.AddListener(() => { _welcomePanel.SetActive(false); _onPlay?.Invoke(); });
            _welcomeSkipButton = MakeOutlineButton(card.transform, SkipLabel, MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel, 0f, 96f);
            UIThemeManager.ApplyGhostButton(_welcomeSkipButton, Palette.TextMuted);
            _welcomeSkipButton.onClick.AddListener(() => { _welcomePanel.SetActive(false); _onSkip?.Invoke(); });

            _welcomePanel.SetActive(false);
        }

        private void StyleHighlight()
        {
            if (highlightFrame == null) return;
            var img = highlightFrame.GetComponent<Image>();
            if (img != null) UIThemeManager.ApplyOutlineButton(img, MenuPalette.TitleColor); // cyan, no gold
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────────
        private void ShowCallout(string text, bool infoBeat)
        {
            if (_calloutCard == null) return;
            _calloutCard.SetActive(true);
            if (_calloutLabel != null) _calloutLabel.text = text;
            if (_tapHintLabel != null) _tapHintLabel.gameObject.SetActive(infoBeat); // hint only when a tap advances
            if (_tapCatcher != null) _tapCatcher.gameObject.SetActive(infoBeat);     // gate the board only on info beats
            if (_lessonSkip != null) _lessonSkip.gameObject.SetActive(true);
            FadeInCallout();
        }

        private void FadeInCallout()
        {
            if (_calloutGroup == null) return;
            if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
            if (UIAnimations.ReduceMotion || !isActiveAndEnabled) { _calloutGroup.alpha = 1f; return; }
            _fadeCo = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            float t = 0f;
            _calloutGroup.alpha = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _calloutGroup.alpha = Mathf.Clamp01(t / FadeDuration);
                yield return null;
            }
            _calloutGroup.alpha = 1f;
            _fadeCo = null;
        }

        private void SetHighlight(RectTransform anchor)
        {
            if (highlightFrame == null) return;
            highlightFrame.gameObject.SetActive(anchor != null);
            if (anchor != null) highlightFrame.position = anchor.position;
        }

        private IEnumerator SuccessBeatRoutine()
        {
            yield return new WaitForSecondsRealtime(SuccessBeatDelay);
            _successCo = null;
            OnSuccessBeatFinished?.Invoke();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private TMP_Text MakeText(Transform parent, string text, TypeRole role, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            TypeScale.Apply(t, role); // Task 42
            t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true;
            return t;
        }

        private Button MakeOutlineButton(Transform parent, string label, Color border, Color labelColor, float preferredWidth, float height)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height;
            if (preferredWidth > 0f) { le.preferredWidth = preferredWidth; le.flexibleWidth = 0f; } else le.flexibleWidth = 1f;
            var t = MakeText(go.transform, label, TypeRole.Label, labelColor, TextAlignmentOptions.Center);
            var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var btn = go.GetComponent<Button>();
            UIThemeManager.ApplyOutlineButton(btn, border, labelColor);
            return btn;
        }
    }
}
