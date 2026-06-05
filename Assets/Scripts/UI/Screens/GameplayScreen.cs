using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.UI.Components;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Gameplay screen UI. Renders puzzle state via LetterTile rows.
    /// All animations are coroutine-based — no DOTween.
    /// See: Architect5 Spec §3 (ladder layout) + §6 (public API).
    /// </summary>
    public class GameplayScreen : MonoBehaviour
    {
        // ---------- Existing (preserved) SerializedFields ----------
        [SerializeField] private TextMeshProUGUI puzzleDisplayText;
        [SerializeField] private TextMeshProUGUI wordChainText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TMP_InputField wordInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button backButton;

        // Phase 2: Power-up UI components
        [SerializeField] private Button hintButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private TextMeshProUGUI hintCountText;
        [SerializeField] private TextMeshProUGUI revealCountText;

        // §5.1 — AddTime power-up (TimeAttack only).
        [SerializeField] private Button addTimeButton;
        [SerializeField] private TextMeshProUGUI addTimeCountText;

        // Phase 3: Tier indicator
        [SerializeField] private TextMeshProUGUI tierIndicatorText;

        // On-screen keyboard
        [SerializeField] private OnScreenKeyboard keyboard;
        [SerializeField] private TextMeshProUGUI currentInputText;

        // ---------- Ladder layout rows (Spec §3) ----------
        [SerializeField] private RectTransform startWordRow;
        [SerializeField] private RectTransform endWordRow;
        [SerializeField] private RectTransform currentInputRow;
        [SerializeField] private RectTransform chainScrollContent;
        [SerializeField] private ScrollRect chainScrollRect;
        [SerializeField] private LetterTile letterTilePrefab;

        // Spec §3.4 — reveal preview row (only shown while reveal is active).
        // If not wired in the scene this is created lazily under chainScrollContent.
        [SerializeField] private RectTransform revealPreviewRow;

        // FROM/TO row labels & steps-remaining
        [SerializeField] private TextMeshProUGUI startWordLabel;
        [SerializeField] private TextMeshProUGUI endWordLabel;
        [SerializeField] private TextMeshProUGUI stepsRemainingText;
        // Daily 2.0 (Task 36) — when true, the daily HUD owns stepsRemainingText (SetStepsRemaining yields).
        private bool _dailyHudActive;

        // ---------- Spec §3.2 palette ----------
        private static readonly Color C_TILE_DEFAULT_FILL    = HexToColor("#2A2D3A");
        private static readonly Color C_TILE_DEFAULT_BORDER  = HexToColor("#3E4250");
        private static readonly Color C_TILE_TEXT            = HexToColor("#E8E8E8");
        private static readonly Color C_TILE_CHANGED_FILL    = HexToColor("#6AAA64"); // green — chain CHANGED
        private static readonly Color C_TILE_CHANGED_TEXT    = HexToColor("#FFFFFF");
        private static readonly Color C_INPUT_EMPTY_FILL     = HexToColor("#1E2030");
        private static readonly Color C_INPUT_FILLED_BORDER  = HexToColor("#4A4F5E");
        private static readonly Color C_HINT_GOLD            = HexToColor("#C9B458"); // gold — hint highlight
        private static readonly Color C_HINT_TEXT_ON_GOLD    = HexToColor("#1A1B26");
        private static readonly Color C_REVEAL_BORDER_MUTED  = HexToColor("#5A6270");
        private static readonly Color C_LABEL_DIM            = HexToColor("#8A8F9C");
        private static readonly Color C_LABEL_REACHED        = HexToColor("#6AAA64");

        // Task 10B — power-up bar (Hint/Undo/Reveal) styling.
        private static readonly Color C_PU_SURFACE   = HexToColor("#1B1F27"); // bg-surface panel (legacy)
        private static readonly Color C_PU_BORDER    = HexToColor("#8A93A1"); // Task 25 — visible muted outline ring
        private static readonly Color C_PU_LABEL     = HexToColor("#E7E1C4"); // text-primary (enabled)
        private static readonly Color C_PU_LABEL_DIM = HexToColor("#5A6270"); // text-dim (0-count/disabled)

        // Legacy label palette (kept for FROM/TO + steps subtitle).
        // Task 8A: LBL_TO demoted from gold #C9B458 to text-muted #8A93A1.
        // The TO-row label is secondary info; gold is reserved for focus/hint/win moments.
        private static readonly Color LBL_FROM       = HexToColor("#7A828F");
        private static readonly Color LBL_TO         = HexToColor("#8A93A1");
        private static readonly Color LBL_STEPS      = HexToColor("#8A93A1");

        // Task 8A/8C — secondary UI labels (score, etc.) use text-muted so they don't compete.
        private static readonly Color C_LABEL_SECONDARY = HexToColor("#8A93A1");

        // ---------- Spec §3 layout constants — sized for iPhone 13 Pro Max portrait ----------
        // Tile sizing is now ADAPTIVE: computed per-puzzle word length so 3-7 tiles
        // always fit within USABLE_WIDTH with inter-tile gaps.
        private const float USABLE_WIDTH      = 960f; // available px after left/right margin on 1080 canvas
        private const float TILE_SIZE_DEFAULT = 140f; // cap for short words (3 tiles)
        private const float TILE_SIZE_MAX     = 150f; // absolute cap
        private const float TILE_GAP_H       = 10f;  // §3.2 inter-tile gap
        // Task 10A: single source of truth for vertical spacing between ladder rungs. Used for
        // chain-history rows (VLG spacing) AND pushed to LadderLayoutDriver for the macro
        // start/input/end gaps, so every step is separated by the SAME tasteful gap (~⅓ tile).
        private const float RUNG_GAP         = 38f;
        private const float ROW_GAP_V        = RUNG_GAP; // §3 inter-row gap (chain history)
        private const float ROW_LABEL_PAD_L  = 0f;   // centre-align tiles, no left padding
        private const float AUTOSCROLL_DURATION = 0.18f; // §3.4 180ms ease-out

        // Computed once per puzzle from word length — shared by ALL rows so they stay aligned.
        private float _tileSize = TILE_SIZE_DEFAULT;
        private float _chainRowHeight = TILE_SIZE_DEFAULT + 10f;

        // Task 10A — cached LadderLayoutDriver (same GameObject); fed adaptive metrics per puzzle.
        private LadderLayoutDriver _ladderDriver;

        // Kept for compat; now driven by _chainRowHeight.
        private float CHAIN_ROW_HEIGHT => _chainRowHeight;

        // Task 7B/7C — juice hooks (null-safe; no-op when unset).
        private IHaptics _haptics;
        private SfxManager _sfx;

        /// <summary>Task 7B — inject haptics provider (call from GameBootstrap).</summary>
        public void SetHaptics(IHaptics haptics) => _haptics = haptics;

        /// <summary>Task 7C — inject SFX manager (call from GameBootstrap).</summary>
        public void SetSfxManager(SfxManager sfx) => _sfx = sfx;

        // ---------- State ----------
        private string currentInput = "";
        private string currentStartWord = "";
        private string currentEndWord = "";
        private IReadOnlyList<string> currentChain;       // last value passed to SetChain
        private int hintLetterIndex = -1;                 // §6: -1 = no hint highlight
        private string revealedNextWord = "";             // §6: "" = no preview
        private int revealedChangedIndex = -1;            // computed from revealedNextWord vs chain tail
        private Coroutine smoothScrollRoutine;

        // Task 31A — render-cache guards (reset in OnEnable). UpdateGameplayUI() ticks EVERY frame to
        // refresh the timer, and each Set*() below rebuilds rows / re-fires the auto-scroll. Skipping that
        // work when the value is unchanged kills the per-frame Reveal flicker (and is a big perf win).
        // Dedicated caches so the legacy standalone setters can't accidentally suppress a needed render.
        private bool _seenStartEnd, _seenChain, _seenInput, _seenHint, _seenReveal;
        private string _rcStart = "", _rcEnd = "", _rcChainKey = "", _rcInput = "", _rcReveal = "";
        private int _rcHintIdx = -1, _rcRevealIdx = -1;

        public event Action<string> OnWordSubmitted;
        public event Action OnBackToMenu;
        public event Action OnHintUsed;
        public event Action OnRevealUsed;
        public event Action OnUndoStep;
        public event Action OnAddTimeUsed;

        // Routed keystrokes — GameBootstrap dispatches these through GameState so
        // state.currentInput is the single source of truth. GameplayScreen no longer
        // mutates its local currentInput field directly on key press.
        public event Action<char> OnLetterTyped;
        public event Action OnBackspace;

        // Task 16B — compact win panel (endless Classic). Next → fresh puzzle, same mode.
        public event Action OnNextPuzzle;
        public event Action OnWinHome;

        // ============================================================
        //  Lifecycle
        // ============================================================
        private void OnEnable()
        {
            // Task 31A — force a fresh full render on (re)show; the guards below then skip unchanged per-frame ticks.
            _seenStartEnd = _seenChain = _seenInput = _seenHint = _seenReveal = false;
            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background
            // Task 32 — hide the vestigial legacy SubmitButton: a solid green (#6AAA64) rect anchored at the
            // bottom that peeked out as a "tiny green bar" at the very bottom of the screen. Submission now
            // goes through the on-screen keyboard's GO key; this button read from the inactive legacy
            // WordInputField, so it did nothing. Deactivated at runtime (no scene edit), which also clears its
            // stray raycast target. SubmitWord() is kept for the legacy text-input path.
            if (submitButton != null) submitButton.gameObject.SetActive(false);
            if (wordInputField != null) wordInputField.onSubmit.AddListener(OnInputSubmit);
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
                StyleHomeButton();
            }

            if (hintButton != null) hintButton.onClick.AddListener(() => OnHintUsed?.Invoke());
            if (revealButton != null) revealButton.onClick.AddListener(() => OnRevealUsed?.Invoke());
            if (undoButton != null) undoButton.onClick.AddListener(() => OnUndoStep?.Invoke());
            if (addTimeButton != null) addTimeButton.onClick.AddListener(() => OnAddTimeUsed?.Invoke());

            // Classic polish (W5) — subtle press squish on the power-up buttons (same calm vocabulary as the
            // keys). Cleared with the rest in OnDisable; re-added here on each enable (no double-subscribe).
            AddPressFeedback(hintButton);
            AddPressFeedback(revealButton);
            AddPressFeedback(undoButton);
            AddPressFeedback(addTimeButton);

            if (keyboard != null)
            {
                keyboard.OnLetterPressed += HandleLetterPressed;
                keyboard.OnBackspacePressed += HandleBackspacePressed;
                keyboard.OnEnterPressed += HandleEnterPressed;
            }

            HideLegacyText();

            // Display-only text must never swallow taps meant for buttons (e.g. HOME).
            DisableDecorativeRaycasts();

            ReparentBadge(hintCountText, hintButton, new Vector2(38f, 32f));
            ReparentBadge(revealCountText, revealButton, new Vector2(38f, 32f));
            ReparentBadge(addTimeCountText, addTimeButton, new Vector2(38f, 32f));

            StylePowerUpButton(hintButton, hintCountText);
            StylePowerUpButton(revealButton, revealCountText);
            StylePowerUpButton(undoButton, null);
            StylePowerUpButton(addTimeButton, addTimeCountText);

            // Task 10B — seat the power-up bar just above the keyboard (deferred one frame so the
            // canvas/keyboard rect is laid out before we read its top edge).
            if (isActiveAndEnabled) StartCoroutine(SeatPowerUpBarDeferred());

            ConfigureChainScrollRect();

            // Issue 3: Hide FROM/TO labels — cleaner look; tutorial teaches the concept.
            // Keep SerializeField refs intact; just disable the GameObjects.
            if (startWordLabel != null) startWordLabel.gameObject.SetActive(false);
            if (endWordLabel != null) endWordLabel.gameObject.SetActive(false);

            if (stepsRemainingText != null)
            {
                stepsRemainingText.color = LBL_STEPS;
                stepsRemainingText.alignment = TextAlignmentOptions.Center;
                stepsRemainingText.fontStyle = FontStyles.Italic;
                stepsRemainingText.fontSize = 20f;
                if (string.IsNullOrEmpty(stepsRemainingText.text))
                    stepsRemainingText.text = string.Empty;
            }

            EnsureRevealPreviewRow();
            HideRevealPreviewRow();

            // Issue 4: Disable HighlightFrame when TutorialOverlay is inactive (prevents stray box).
            DisableStrayHighlightFrame();

            // Issue 6: Reposition header elements below the notch safe area.
            RepositionHeaderBelowNotch();

            // Task 8A — tier indicator demoted: secondary info must not compete with gold focal elements.
            // Changed from Bold+gold(#C9B458)+28pt to Normal+text-muted(#8A93A1)+20pt, still centered.
            if (tierIndicatorText != null)
            {
                tierIndicatorText.fontStyle = FontStyles.Normal;
                tierIndicatorText.color = new Color32(0x8A, 0x93, 0xA1, 0xFF);
                tierIndicatorText.fontSize = 20;
                tierIndicatorText.alignment = TextAlignmentOptions.Center;
                tierIndicatorText.gameObject.SetActive(true);
            }

            // Fix Score/Tier overlap — lay the header out deterministically (see LayoutHeader).
            LayoutHeader();
        }

        // Header layout — score centered just below the notch, tier as a small subtitle directly
        // below it. Deterministic so the two never collide (both previously rendered at centre).
        private void LayoutHeader()
        {
            PlaceHeaderText(scoreText,         topY: -130f, width: 620f, height: 64f);
            // Tier (Puzzle Show) and Timer (Time Attack) are mutually exclusive — share the slot just
            // below the score, keeping the top-right corner clear for the global Settings gear.
            PlaceHeaderText(tierIndicatorText, topY: -202f, width: 620f, height: 34f);
            PlaceHeaderText(timerText,         topY: -202f, width: 620f, height: 34f);
        }

        private static void PlaceHeaderText(TMP_Text t, float topY, float width, float height)
        {
            if (t == null) return;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);   // top-centre of the screen
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta = new Vector2(width, height);
            t.alignment = TextAlignmentOptions.Center;
        }

        // HOME button: a house icon (Lucide) in a subtle surface pill — clearly visible and a
        // comfortable tap target. Falls back to a "HOME" text label if the icon asset is missing,
        // so the button never breaks.
        private static readonly Color C_HOME_TINT = new Color32(0xE7, 0xE1, 0xC4, 0xFF); // text-primary

        private static Sprite _homeIconSprite;
        private static Sprite GetHomeIconSprite()
        {
            if (_homeIconSprite != null) return _homeIconSprite;
            var tex = Resources.Load<Texture2D>("Icons/home");
            if (tex == null) return null;
            _homeIconSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            return _homeIconSprite;
        }

        private void StyleHomeButton()
        {
            if (backButton == null) return;

            var icon = GetHomeIconSprite();

            var rt = backButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                var sd = rt.sizeDelta;
                // Square-ish icon button (or wider for the HOME-text fallback); comfortable tap target.
                sd.x = Mathf.Max(sd.x, icon != null ? 88f : 150f);
                sd.y = Mathf.Max(sd.y, 80f);
                rt.sizeDelta = sd;
            }

            var img = backButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0f, 0f, 0f, 0f); // transparent — icon only, no grey box
                img.raycastTarget = true;              // keep the full rect tappable (≥44px)
            }

            var lbl = backButton.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null)
            {
                lbl.raycastTarget = false;        // label must not intercept the tap
                if (icon != null)
                {
                    lbl.text = string.Empty;      // the icon replaces the text
                }
                else
                {
                    lbl.text = "HOME";            // fallback when the icon asset is unavailable
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.fontSize = 26f;
                    lbl.color = C_HOME_TINT;
                    lbl.alignment = TextAlignmentOptions.Center;
                }
            }

            // House icon as a centered child Image.
            var iconTf = backButton.transform.Find("HomeIcon");
            Image iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                var go = new GameObject("HomeIcon", typeof(RectTransform));
                go.transform.SetParent(backButton.transform, false);
                var irt = go.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(52f, 52f);
                iconImg = go.AddComponent<Image>();
            }
            iconImg.enabled = icon != null;
            iconImg.sprite = icon;
            iconImg.color = C_HOME_TINT;          // tint the white glyph to text-primary
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
        }

        // Display-only text must never swallow taps meant for buttons (e.g. HOME).
        private void DisableDecorativeRaycasts()
        {
            TMP_Text[] decorative =
            {
                scoreText, feedbackText, timerText, tierIndicatorText, stepsRemainingText,
                puzzleDisplayText, wordChainText, currentInputText
            };
            foreach (var t in decorative)
                if (t != null) t.raycastTarget = false;
        }

        private void HideLegacyText()
        {
            if (wordChainText != null)
            {
                FadeTextAlpha(wordChainText, 0f);
                if (wordChainText.gameObject.activeSelf) wordChainText.gameObject.SetActive(false);
            }
            if (puzzleDisplayText != null)
            {
                FadeTextAlpha(puzzleDisplayText, 0f);
                if (puzzleDisplayText.gameObject.activeSelf) puzzleDisplayText.gameObject.SetActive(false);
            }
            if (currentInputText != null)
            {
                FadeTextAlpha(currentInputText, 0f);
                if (currentInputText.gameObject.activeSelf) currentInputText.gameObject.SetActive(false);
            }
        }

        private static void ReparentBadge(TextMeshProUGUI badge, Button host, Vector2 localOffset)
        {
            if (badge == null || host == null) return;
            var rt = badge.rectTransform;
            rt.SetParent(host.transform, false);
            // Anchor to top-right corner of the button
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Task 10B — inset fully INSIDE the top-right corner (~5px margin) so the badge
            // never clips over the button's top edge.
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(30f, 30f);
            badge.alignment = TextAlignmentOptions.Center;
            badge.fontSize = 17f;
            badge.fontStyle = FontStyles.Bold;
            badge.color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            badge.raycastTarget = false;

            // Add a dark circular bg pill behind the count digit
            var imgGO = badge.transform.Find("__BadgeBg");
            if (imgGO == null)
            {
                var go = new GameObject("__BadgeBg", typeof(RectTransform));
                go.transform.SetParent(badge.transform, false);
                var brt = go.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color = new Color(0xC9 / 255f, 0x21 / 255f, 0x5C / 255f, 0.9f); // #C9215C pill
                img.raycastTarget = false;
                go.transform.SetAsFirstSibling();
            }

            badge.transform.SetAsLastSibling();
        }

        // Task 10B — base style + enabled/disabled look for a power-up button.
        // badge is the count TMP (reparented under the button); pass null for Undo (no count).
        private static void StylePowerUpButton(Button btn, TMP_Text badge)
        {
            if (btn == null) return;
            var label = FindPowerUpLabel(btn, badge);
            if (label != null) label.fontStyle = FontStyles.Bold;
            UIThemeManager.ApplyOutlineButton(btn.GetComponent<Image>(), C_PU_BORDER); // Task 25 — ghost button
            ApplyPowerUpVisual(btn, badge, btn.interactable);
        }

        // Finds the button's text label (its direct-child TMP that is NOT the count badge).
        private static TMP_Text FindPowerUpLabel(Button btn, TMP_Text badge)
        {
            if (btn == null) return null;
            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == badge) continue;
                if (t.transform.parent != btn.transform) continue; // direct child only
                return t;
            }
            return null;
        }

        // Task 10B — bg-surface panel + text-primary label when usable; text-dim + dimmed panel
        // when count is 0 (disabled look). Does NOT change interactivity/dispatch logic.
        private static void ApplyPowerUpVisual(Button btn, TMP_Text badge, bool enabled)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                var c = C_PU_BORDER; // Task 25 — tint the outline ring; dim when the power-up is unusable
                c.a = enabled ? 1f : 0.45f;
                img.color = c;
            }
            var label = FindPowerUpLabel(btn, badge);
            if (label != null) label.color = enabled ? C_PU_LABEL : C_PU_LABEL_DIM;
            if (badge != null)
            {
                var bc = badge.color; bc.a = enabled ? 1f : 0.5f; badge.color = bc;
                var bgT = badge.transform.Find("__BadgeBg");
                if (bgT != null)
                {
                    var bg = bgT.GetComponent<Image>();
                    if (bg != null) { var p = bg.color; p.a = enabled ? 0.9f : 0.35f; bg.color = p; }
                }
            }
        }

        // Task 10B — seat the Hint/Undo/Reveal bar just slightly above the VISIBLE keyboard keys.
        // The KeyboardRoot rect is taller than the keys (keys sit at its bottom), so we measure the
        // top edge of the highest key button rather than the panel rect. Robust across devices.
        private void SeatPowerUpBar()
        {
            if (keyboard == null) return;
            var keebRt = keyboard.GetComponent<RectTransform>();
            var selfRt = transform as RectTransform;
            if (keebRt == null || selfRt == null) return;

            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];

            // Visible keyboard top = highest top-edge among the key buttons.
            float keysTopLocalY = float.NegativeInfinity;
            foreach (var key in keebRt.GetComponentsInChildren<Button>())
            {
                if (key == null) continue;
                var krt = key.transform as RectTransform;
                if (krt == null) continue;
                krt.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
                float topLocal = selfRt.InverseTransformPoint(corners[1]).y;
                if (topLocal > keysTopLocalY) keysTopLocalY = topLocal;
            }
            if (float.IsNegativeInfinity(keysTopLocalY)) return; // no keys found — leave as authored

            const float barGap = 14f; // small gap so the bar sits just above the keys

            // Task 21A — distribute the VISIBLE power-up buttons evenly across the bar width so the
            // bar reflows for 3 (Hint/Undo/Reveal) or 4 (+ +TIME in timed modes) with no overflow,
            // clipping, or overlap. +TIME visibility is driven by SetAddTimeVisible (TimeAttack only),
            // so activeSelf is the authoritative button count. Buttons are centre-anchored (pivot.x
            // 0.5), so anchoredPosition.x is the slot centre and sizeDelta.x is the slot width.
            var all = new[] { hintButton, undoButton, revealButton, addTimeButton };
            int n = 0;
            foreach (var b in all)
                if (b != null && b.gameObject.activeSelf) n++;
            if (n == 0) return;

            const float sideMargin = 24f;
            const float btnGap = 12f;
            float barWidth = selfRt.rect.width - 2f * sideMargin;
            float slotW = (barWidth - (n - 1) * btnGap) / n;
            float left = -barWidth * 0.5f;

            int idx = 0;
            foreach (var b in all)
            {
                if (b == null || !b.gameObject.activeSelf) continue;
                var rt = b.GetComponent<RectTransform>();
                if (rt == null) { idx++; continue; }

                var sd = rt.sizeDelta; sd.x = slotW; rt.sizeDelta = sd;
                var ap = rt.anchoredPosition;
                ap.x = left + slotW * 0.5f + idx * (slotW + btnGap);
                ap.y = keysTopLocalY + barGap + rt.rect.height * 0.5f;
                rt.anchoredPosition = ap;
                idx++;
            }
        }

        private IEnumerator SeatPowerUpBarDeferred()
        {
            yield return null;       // let the canvas/keyboard rect settle for one frame
            SeatPowerUpBar();
        }

        private void ConfigureChainScrollRect()
        {
            if (chainScrollRect == null && chainScrollContent != null)
                chainScrollRect = chainScrollContent.GetComponentInParent<ScrollRect>();
            if (chainScrollRect == null) return;

            chainScrollRect.horizontal = false;
            chainScrollRect.vertical = true;
            chainScrollRect.movementType = ScrollRect.MovementType.Clamped;
            chainScrollRect.inertia = true;
            chainScrollRect.decelerationRate = 0.135f;
            chainScrollRect.scrollSensitivity = 30f;

            // Task 12A — THE root-cause fix. The chain Content has a ContentSizeFitter, so it grows to
            // full height; with no clipping mask it overflowed the capped scroll region and overlapped
            // the input/target rows below. Wire viewport/content and add a RectMask2D so long chains
            // CLIP + SCROLL instead of colliding.
            var viewportRT = chainScrollRect.GetComponent<RectTransform>();
            if (chainScrollRect.viewport == null) chainScrollRect.viewport = viewportRT;
            if (chainScrollRect.content == null)  chainScrollRect.content  = chainScrollContent;
            if (chainScrollRect.GetComponent<RectMask2D>() == null)
                chainScrollRect.gameObject.AddComponent<RectMask2D>();

            // Content grows downward from the top so the newest row appends at the bottom and
            // verticalNormalizedPosition = 0 reveals the latest played word (the auto-scroll target).
            if (chainScrollContent != null)
            {
                chainScrollContent.anchorMin = new Vector2(0f, 1f);
                chainScrollContent.anchorMax = new Vector2(1f, 1f);
                chainScrollContent.pivot     = new Vector2(0.5f, 1f);
            }
        }

        private static void StyleRowLabel(TextMeshProUGUI label, string fallback, Color color)
        {
            if (label == null) return;
            if (string.IsNullOrEmpty(label.text)) label.text = fallback;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 8f;
            label.fontSize = 22f;
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveAllListeners();
            if (wordInputField != null) wordInputField.onSubmit.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();

            if (hintButton != null) hintButton.onClick.RemoveAllListeners();
            if (revealButton != null) revealButton.onClick.RemoveAllListeners();
            if (undoButton != null) undoButton.onClick.RemoveAllListeners();
            if (addTimeButton != null) addTimeButton.onClick.RemoveAllListeners();

            if (keyboard != null)
            {
                keyboard.OnLetterPressed -= HandleLetterPressed;
                keyboard.OnBackspacePressed -= HandleBackspacePressed;
                keyboard.OnEnterPressed -= HandleEnterPressed;
            }

            if (smoothScrollRoutine != null)
            {
                StopCoroutine(smoothScrollRoutine);
                smoothScrollRoutine = null;
            }
        }

        // Classic polish (W5) — attach a subtle press squish to a power-up button. Routed through the shared
        // UIAnimations.ScaleButtonTap: ReduceMotion-gated (instant when motion is off) + a short coroutine
        // (no per-frame GC). Buttons are centre-pivot in the manually-seated bar, so localScale won't fight layout.
        private void AddPressFeedback(Button btn)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt == null) return;
            btn.onClick.AddListener(() => { if (isActiveAndEnabled) StartCoroutine(UIAnimations.ScaleButtonTap(rt)); });
        }

        // ============================================================
        //  Keyboard handlers
        // ============================================================
        private void HandleLetterPressed(char c)
        {
            // Route through state — GameBootstrap dispatches PressLetterAction and
            // calls UpdateGameplayUI which sets currentInput from state.currentInput.
            OnLetterTyped?.Invoke(c);

            // Task 7B/7C — letter placed juice.
            _sfx?.PlayKeyPress();
            _haptics?.LightTap();

            // Task 7A — tile lock-in punch on the just-filled tile (respect ReduceMotion).
            // currentInput is updated synchronously by SetCurrentInput before this runs
            // because GameBootstrap calls UpdateGameplayUI immediately after dispatch.
            if (!UIAnimations.ReduceMotion && currentInputRow != null)
            {
                int idx = currentInput.Length - 1;
                if (idx >= 0 && idx < currentInputRow.childCount)
                {
                    var tile = currentInputRow.GetChild(idx).GetComponent<LetterTile>();
                    if (tile != null)
                    {
                        StartCoroutine(tile.PunchScale());
                        StartCoroutine(tile.DropInSettle()); // Task 29C — letter settles into the tile ("placing a rung")
                    }
                }
            }
        }

        private void HandleBackspacePressed()
        {
            // Route through state — GameBootstrap dispatches DeleteLetterAction and
            // calls UpdateGameplayUI which sets currentInput from state.currentInput.
            OnBackspace?.Invoke();
        }

        private void HandleEnterPressed()
        {
            // currentInput is authoritative from state (set via SetCurrentInput).
            if (!string.IsNullOrWhiteSpace(currentInput))
            {
                OnWordSubmitted?.Invoke(currentInput);
                // Do not clear locally — GameBootstrap dispatches SubmitWordAction which
                // resets state.currentInput to ""; UpdateGameplayUI will call SetCurrentInput("")
                // which clears the row cleanly.
            }
        }

        private void UpdateCurrentInputDisplay()
        {
            string upper = currentInput.ToUpper();

            if (currentInputText != null) currentInputText.text = upper;
            HideLegacyText();
            if (keyboard != null) keyboard.SetCurrentInput(upper);

            if (string.IsNullOrEmpty(currentEndWord))
            {
                ClearChildren(currentInputRow);
                return;
            }

            RenderCurrentInputRow();
        }

        public void ClearCurrentInput()
        {
            currentInput = "";
            UpdateCurrentInputDisplay();
        }

        // ============================================================
        //  Public UI API — §6 (architect5 spec)
        // ============================================================

        /// <summary>
        /// Computes adaptive tile size so N tiles + gaps fit within USABLE_WIDTH.
        /// Formula: min(TILE_SIZE_DEFAULT, (USABLE_WIDTH - (N-1)*TILE_GAP_H) / N), capped at TILE_SIZE_MAX.
        /// All rows in a puzzle share this size so their tiles stay column-aligned.
        /// </summary>
        private void RecomputeTileSize(int wordLen)
        {
            if (wordLen <= 0) wordLen = 1;
            float adaptive = (USABLE_WIDTH - (wordLen - 1) * TILE_GAP_H) / wordLen;
            _tileSize = Mathf.Clamp(adaptive, 60f, Mathf.Min(TILE_SIZE_DEFAULT, TILE_SIZE_MAX));
            _chainRowHeight = _tileSize + 10f;

            // Task 10A: keep macro rows (start/input/end) the same height as chain rows and share
            // one uniform rung gap, so steps read as distinct rungs at any word length (3–7).
            if (_ladderDriver == null) _ladderDriver = GetComponent<LadderLayoutDriver>();
            if (_ladderDriver != null) _ladderDriver.SetMetrics(_chainRowHeight, RUNG_GAP);
        }

        /// <summary>§6: Idempotent. Sets persistent FROM/TO row labels + tile content.</summary>
        public void SetStartAndEndWords(string startWord, string endWord)
        {
            string s = startWord ?? string.Empty;
            string e = endWord ?? string.Empty;
            // Task 31A — skip the per-frame start/target tile rebuild when unchanged.
            if (_seenStartEnd && s == _rcStart && e == _rcEnd) return;
            _seenStartEnd = true; _rcStart = s; _rcEnd = e;

            currentStartWord = s;
            currentEndWord = e;

            // Recompute tile size based on word length (3-7 letters adaptive).
            int wordLen = Mathf.Max(
                string.IsNullOrEmpty(currentStartWord) ? 0 : currentStartWord.Length,
                string.IsNullOrEmpty(currentEndWord)   ? 0 : currentEndWord.Length);
            RecomputeTileSize(wordLen);

            if (puzzleDisplayText != null) puzzleDisplayText.text = $"{startWord} → {endWord}";
            HideLegacyText();

            BuildLadderRowTiles(startWordRow, currentStartWord, isStartRow: true, reached: false);
            bool endReached = ChainEndsAtEndWord();
            BuildLadderRowTiles(endWordRow, currentEndWord, isStartRow: false, reached: endReached);
            ApplyEndLabelColor(endReached);
        }

        /// <summary>§6: Re-renders history (element 0 = start word; 1..N-1 = chain with §3.5 diff highlighting).</summary>
        public void SetChain(IReadOnlyList<string> chain)
        {
            // Task 31A — skip the per-frame chain rebuild + auto-scroll when the chain content is unchanged.
            string key = chain == null ? string.Empty : string.Join("", chain);
            if (_seenChain && key == _rcChainKey) { currentChain = chain; return; }
            _seenChain = true; _rcChainKey = key;

            currentChain = chain;
            if (wordChainText != null)
            {
                wordChainText.text = chain == null
                    ? string.Empty
                    : string.Join(" → ", chain);
            }
            HideLegacyText();

            RenderChainRows();
            RecomputeRevealedChangedIndex();
            RenderRevealPreviewRow();

            // §3.4 auto-scroll after chain mutation.
            ScrollSoCurrentInputAboveEndWord();

            // End-word REACHED state may need to flip after chain growth.
            bool endReached = ChainEndsAtEndWord();
            BuildLadderRowTiles(endWordRow, currentEndWord, isStartRow: false, reached: endReached);
            ApplyEndLabelColor(endReached);
        }

        /// <summary>§6: Fills current-input row tiles. Applies hint highlight if hintLetterIndex>=0.</summary>
        public void SetCurrentInput(string typedSoFar)
        {
            string normalized = (typedSoFar ?? string.Empty).ToLower();
            // Task 31A — skip the per-frame active-row rebuild when the typed input is unchanged.
            if (_seenInput && normalized == _rcInput) return;
            _seenInput = true; _rcInput = normalized;

            currentInput = normalized;
            if (currentInputText != null) currentInputText.text = currentInput.ToUpper();
            if (keyboard != null) keyboard.SetCurrentInput(currentInput.ToUpper());

            RenderCurrentInputRow();
        }

        /// <summary>§6: index &lt; 0 clears hint highlight; otherwise tile[index] in current-input uses gold style.</summary>
        public void SetHintLetterIndex(int index)
        {
            // Task 31A — skip the per-frame active-row rebuild when the hint index is unchanged.
            if (_seenHint && index == _rcHintIdx) return;
            _seenHint = true; _rcHintIdx = index;
            hintLetterIndex = index;
            RenderCurrentInputRow();
        }

        /// <summary>§6: null/empty hides preview row; otherwise shows ghost row with tile[changedIndex] in gold.</summary>
        public void SetRevealedNextWord(string word, int changedIndex)
        {
            string normalized = string.IsNullOrEmpty(word) ? string.Empty : word.ToLower();
            // Task 31A — THE flicker fix: skip the per-frame preview rebuild + auto-scroll when unchanged,
            // so Reveal applies its result ONCE instead of re-rendering + re-scrolling every frame.
            if (_seenReveal && normalized == _rcReveal && changedIndex == _rcRevealIdx) return;
            _seenReveal = true; _rcReveal = normalized; _rcRevealIdx = changedIndex;

            revealedNextWord = normalized;
            revealedChangedIndex = changedIndex;
            RenderRevealPreviewRow();
            ScrollSoCurrentInputAboveEndWord();
        }

        // ============================================================
        //  Public UI API — preserved legacy surface
        // ============================================================

        /// <summary>Legacy facade — forwards to §6 SetStartAndEndWords.</summary>
        public void SetPuzzleDisplay(string startWord, string endWord)
        {
            SetStartAndEndWords(startWord, endWord);
        }

        /// <summary>Legacy facade — forwards to §6 SetChain (which now owns chain rendering + diff highlight).</summary>
        public void SetWordChain(string[] words)
        {
            SetChain(words == null ? (IReadOnlyList<string>)Array.Empty<string>() : words);
        }

        /// <summary>Legacy: end-word hint-letter overlay. Kept for back-compat (no-op when ladder is in use).</summary>
        public void SetRevealedIndices(HashSet<int> indices)
        {
            // §3 end-word row no longer uses per-letter reveal — fully turns green when chain reaches it.
            // Kept for API stability; intentional no-op.
        }

        public void SetScore(int score)
        {
            if (scoreText == null) return;
            scoreText.text = $"Score: {score}";
            // Task 8A/8C: score is secondary info — muted so it doesn't pull focus from tiles.
            scoreText.color = C_LABEL_SECONDARY;
        }

        public void SetTimer(float timeRemaining)
        {
            if (timerText != null) timerText.text = $"Time: {Mathf.Max(0, timeRemaining):F1}s";
        }

        public void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
        }

        public void ClearInput()
        {
            if (wordInputField != null)
            {
                wordInputField.text = "";
                wordInputField.ActivateInputField();
            }
            ClearCurrentInput();
        }

        public void SetHintCount(int remaining)
        {
            if (hintCountText != null) hintCountText.text = remaining.ToString();
            if (hintButton != null) hintButton.interactable = (remaining > 0);
            ApplyPowerUpVisual(hintButton, hintCountText, remaining > 0); // Task 10B disabled look
        }

        public void SetRevealCount(int remaining)
        {
            if (revealCountText != null) revealCountText.text = remaining.ToString();
            if (revealButton != null) revealButton.interactable = (remaining > 0);
            ApplyPowerUpVisual(revealButton, revealCountText, remaining > 0); // Task 10B disabled look
        }

        public void EnableUndoButton(bool enable)
        {
            if (undoButton != null) undoButton.interactable = enable;
            ApplyPowerUpVisual(undoButton, null, enable); // Task 10B disabled look
        }

        // §5.1 — AddTime power-up surface (TimeAttack only).
        public void SetAddTimeCount(int remaining)
        {
            if (addTimeCountText != null) addTimeCountText.text = remaining.ToString();
            if (addTimeButton != null) addTimeButton.interactable = (remaining > 0);
            ApplyPowerUpVisual(addTimeButton, addTimeCountText, remaining > 0); // Task 10B disabled look
        }

        private bool? _addTimeVisible;
        public void SetAddTimeVisible(bool visible)
        {
            if (addTimeButton != null) addTimeButton.gameObject.SetActive(visible);
            if (addTimeCountText != null) addTimeCountText.gameObject.SetActive(visible);
            // Task 21A — reflow the bar (3 vs 4 buttons) only when visibility actually changes;
            // this is called every UpdateGameplayUI, so guard against per-frame relayout.
            if (_addTimeVisible != visible)
            {
                _addTimeVisible = visible;
                if (isActiveAndEnabled) StartCoroutine(SeatPowerUpBarDeferred());
            }
        }

        public void SetTimerVisible(bool visible)
        {
            if (timerText != null) timerText.gameObject.SetActive(visible);
        }

        public void SetTierIndicator(string text)
        {
            if (tierIndicatorText != null) tierIndicatorText.text = text ?? string.Empty;
        }

        public void SetStepsRemaining(int stepsRemaining, int optimalSteps)
        {
            if (stepsRemainingText == null) return;
            // Daily 2.0 (Task 36) — the daily HUD owns this slot (Par · Mistakes left); don't clobber it.
            if (_dailyHudActive) return;
            if (stepsRemaining <= 0)
            {
                stepsRemainingText.text = string.Empty;
            }
            else if (stepsRemaining == 1)
            {
                stepsRemainingText.text = $"1 step to go  ·  optimal {optimalSteps}";
            }
            else
            {
                stepsRemainingText.text = $"{stepsRemaining} steps to go  ·  optimal {optimalSteps}";
            }
        }

        /// <summary>
        /// Daily 2.0 (Task 36) — show "Par N  ·  Mistakes left: M" in the steps slot during a daily run.
        /// Call with par &lt; 0 to release the slot back to SetStepsRemaining (non-daily modes).
        /// </summary>
        public void SetDailyPar(int par, int mistakesLeft)
        {
            _dailyHudActive = par >= 0;
            // ALWAYS write the slot: par < 0 RELEASES it (empty) so a finished daily's
            // "Par · Mistakes left" can never linger into Classic/tutorial (Task 38 fix — the old early
            // return left the stale daily HUD on screen because nothing else writes this slot).
            if (stepsRemainingText != null) stepsRemainingText.text = ComposeDailyHud(par, mistakesLeft);
        }

        /// <summary>Pure: the daily HUD string for the steps slot, or "" when released (par &lt; 0). Testable.</summary>
        public static string ComposeDailyHud(int par, int mistakesLeft)
            => par >= 0 ? $"Par {par}  ·  Mistakes left: {mistakesLeft}" : string.Empty;

        public void SetWordLabels(string fromText = "FROM", string toText = "TO")
        {
            if (startWordLabel != null && !string.IsNullOrEmpty(fromText)) startWordLabel.text = fromText;
            if (endWordLabel != null && !string.IsNullOrEmpty(toText)) endWordLabel.text = toText;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Legacy: standalone start-word row builder.</summary>
        public void SetStartWordTiles(string word)
        {
            currentStartWord = word ?? string.Empty;
            BuildLadderRowTiles(startWordRow, currentStartWord, isStartRow: true, reached: false);
        }

        /// <summary>Legacy: standalone end-word row builder.</summary>
        public void SetEndWordTiles(string word, HashSet<int> revealedIndices)
        {
            currentEndWord = word ?? string.Empty;
            bool reached = ChainEndsAtEndWord();
            BuildLadderRowTiles(endWordRow, currentEndWord, isStartRow: false, reached: reached);
            ApplyEndLabelColor(reached);
        }

        /// <summary>Legacy: standalone current-input builder. Routes through SetCurrentInput.</summary>
        public void SetCurrentInputTiles(string input, int targetLength)
        {
            SetCurrentInput(input);
        }

        /// <summary>Legacy chain append — adds the row and re-renders highlights.</summary>
        public void AppendChainRow(string word)
        {
            if (currentChain == null)
            {
                SetChain(new List<string> { word });
                return;
            }
            var list = new List<string>(currentChain);
            list.Add(word ?? string.Empty);
            SetChain(list);
        }

        public void PopLastChainRow()
        {
            if (currentChain == null || currentChain.Count == 0) return;
            var list = new List<string>(currentChain);
            list.RemoveAt(list.Count - 1);
            SetChain(list);
        }

        public void ShakeCurrentInput()
        {
            if (currentInputRow == null) return;
            // Task 7A — skip shake when ReduceMotion is on (still show reason text via ShowFeedback).
            if (!UIAnimations.ReduceMotion)
            {
                StartCoroutine(ShakeRoutine(currentInputRow, 0.2f));
                for (int i = 0; i < currentInputRow.childCount; i++)
                {
                    var tile = currentInputRow.GetChild(i).GetComponent<LetterTile>();
                    if (tile != null)
                        StartCoroutine(tile.FlashColor(HexToColor("#D9534F"), 0.25f));
                }
            }
        }

        /// <summary>Task 7 — called by GameBootstrap on word accepted.</summary>
        public void OnWordAccepted()
        {
            _sfx?.PlayAccept();
            _haptics?.MediumTap();

            // Brief glow settle on chain's newest row (unless ReduceMotion).
            if (!UIAnimations.ReduceMotion && chainScrollContent != null
                && chainScrollContent.childCount > 0)
            {
                var lastRow = chainScrollContent.GetChild(chainScrollContent.childCount - 1)
                    as RectTransform;
                if (lastRow != null) StartCoroutine(UIAnimations.RowClimbSettle(lastRow)); // Task 29C — upward "climb"
            }
        }

        /// <summary>Task 7 — called by GameBootstrap on word rejected.</summary>
        public void OnWordRejected()
        {
            ShakeCurrentInput();
            _sfx?.PlayReject();
            _haptics?.Buzz();
        }

        /// <summary>Task 7/8B — called by GameBootstrap on EndGame (win).</summary>
        public void OnGameWon()
        {
            _sfx?.PlayWin();
            _haptics?.Buzz();
            // Task 8B — celebratory ascent beat on the TO row (after sfx/haptic).
            StartCoroutine(WinAscentBeat());
        }

        // ================================================================
        //  Task 16B — compact inline win panel (endless Classic)
        // ================================================================
        private GameObject winPanel;
        private TextMeshProUGUI winStepsText;

        /// <summary>
        /// Overlay a small win card on the finished board. steps = moves taken.
        /// Task 18C — play the celebratory win beat (TO row gold→green ascent) FIRST, then
        /// bring up the panel, so the payoff reads clearly. ReduceMotion → instant, no wait.
        /// </summary>
        public void ShowWinPanel(int steps)
        {
            if (winPanel == null) BuildWinPanel();
            if (winStepsText != null)
                winStepsText.text = steps == 1 ? "Solved in 1 step" : $"Solved in {steps} steps";

            OnGameWon(); // win sting + haptic + WinAscentBeat (gold→green ascent on the TO row)

            if (UIAnimations.ReduceMotion)
            {
                ActivateWinPanel(animate: false);
            }
            else
            {
                StartCoroutine(ShowWinPanelAfterBeat());
            }
        }

        private const float WIN_BEAT_HOLD = 0.45f; // let the ascent beat read before the panel

        private IEnumerator ShowWinPanelAfterBeat()
        {
            yield return new WaitForSecondsRealtime(WIN_BEAT_HOLD);
            ActivateWinPanel(animate: true);
        }

        private void ActivateWinPanel(bool animate)
        {
            if (winPanel == null) return;
            winPanel.transform.SetAsLastSibling();
            winPanel.SetActive(true);
            if (animate) StartCoroutine(FadeInWinPanel());
        }

        public void HideWinPanel()
        {
            if (winPanel != null) winPanel.SetActive(false);
        }

        private IEnumerator FadeInWinPanel()
        {
            var cg = winPanel.GetComponent<CanvasGroup>();
            if (cg == null) yield break;
            cg.alpha = 0f;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / 0.18f);
                yield return null;
            }
            cg.alpha = 1f;
        }

        private void BuildWinPanel()
        {
            winPanel = new GameObject("WinPanel", typeof(RectTransform));
            winPanel.transform.SetParent(transform, false);
            var prt = (RectTransform)winPanel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            winPanel.AddComponent<CanvasGroup>();

            // Dim backdrop (also blocks taps to the board behind).
            var dim = winPanel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            // Centered card.
            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(winPanel.transform, false);
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(560f, 360f);
            var cardImg = card.AddComponent<Image>();
            UIThemeManager.ApplyRoundedButton(cardImg); // Task 22B — match the shared bubbly corner language
            cardImg.color = new Color32(0x24, 0x29, 0x36, 0xFF); // surface-2

            MakeWinText(card.transform, "Title", "SOLVED", 54, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -54f), new Color32(0x6A, 0xAA, 0x64, 0xFF), FontStyles.Bold); // green
            winStepsText = MakeWinText(card.transform, "Steps", "", 30, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -120f), new Color32(0x8A, 0x93, 0xA1, 0xFF), FontStyles.Normal); // muted

            // Task 25 — ghost buttons: colour is now the BORDER, labels are LIGHT (the old NEXT label
            // was near-black #0F1217 and would vanish on the transparent-over-black button).
            MakeWinButton(card.transform, "NextButton", "NEXT PUZZLE",
                new Vector2(0.5f, 0f), new Vector2(0f, 116f), new Vector2(420f, 76f),
                new Color32(0xC9, 0xB4, 0x58, 0xFF), new Color32(0xF5, 0xF7, 0xFA, 0xFF),
                () => OnNextPuzzle?.Invoke());   // gold border, light label (hero)
            MakeWinButton(card.transform, "HomeButton", "Home",
                new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(420f, 56f),
                new Color32(0x8A, 0x93, 0xA1, 0xFF), new Color32(0xE7, 0xE1, 0xC4, 0xFF),
                () => OnWinHome?.Invoke());       // muted border, cream label
        }

        private static TextMeshProUGUI MakeWinText(Transform parent, string name, string text, float size,
            Vector2 aMin, Vector2 aMax, Vector2 pos, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(-40f, 56f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false; tmp.enableWordWrapping = false;
            return tmp;
        }

        private void MakeWinButton(Transform parent, string name, string label,
            Vector2 anchor, Vector2 pos, Vector2 size, Color border, Color fg, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            UIThemeManager.ApplyOutlineButton(img, border); // Task 25 — ghost button (transparent centre)
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = MakeWinText(go.transform, "Label", label, 28, Vector2.zero, Vector2.one, Vector2.zero, fg, FontStyles.Bold);
            lbl.rectTransform.anchoredPosition = Vector2.zero;
            lbl.rectTransform.sizeDelta = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Task 8B — WinAscentBeat: transitions endWordRow tiles toward accent-green
        /// with a brief upward Y nudge + scale settle, ease-out, ~500ms total.
        /// If ReduceMotion is on, applies the green end-state instantly with no motion.
        /// Does NOT fire sfx or haptics (those already fired in OnGameWon).
        /// </summary>
        private IEnumerator WinAscentBeat()
        {
            if (endWordRow == null) yield break;

            // Apply green end-state to all TO-row tiles immediately (color transition).
            ApplyEndRowWinColors();

            if (UIAnimations.ReduceMotion)
            {
                // Accessibility: instant end-state, no motion.
                yield break;
            }

            const float duration = 0.50f;   // ~500ms total, ease-out, not bouncy
            const float nudgeY   = 12f;     // upward Y nudge in local pixels
            const float peakScale = 1.04f;  // gentle scale peak — not cartoon

            Vector3 originPos   = endWordRow.localPosition;
            Vector3 peakPos     = originPos + new Vector3(0f, nudgeY, 0f);
            Vector3 originScale = endWordRow.localScale;
            Vector3 peakScaleV  = originScale * peakScale;

            float half = duration * 0.5f;
            float t = 0f;

            // Rise phase: nudge up + scale out (ease-out-cubic)
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / half));
                endWordRow.localPosition = Vector3.LerpUnclamped(originPos, peakPos, p);
                endWordRow.localScale    = Vector3.LerpUnclamped(originScale, peakScaleV, p);
                yield return null;
            }

            t = 0f;

            // Settle phase: return to origin (ease-out-cubic)
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / half));
                endWordRow.localPosition = Vector3.LerpUnclamped(peakPos, originPos, p);
                endWordRow.localScale    = Vector3.LerpUnclamped(peakScaleV, originScale, p);
                yield return null;
            }

            // Ensure exact rest state.
            endWordRow.localPosition = originPos;
            endWordRow.localScale    = originScale;
        }

        /// <summary>Task 8B — sets all endWordRow tiles to the accent-green won state.</summary>
        private void ApplyEndRowWinColors()
        {
            if (endWordRow == null) return;
            for (int i = 0; i < endWordRow.childCount; i++)
            {
                var tile = endWordRow.GetChild(i).GetComponent<LetterTile>();
                if (tile == null) continue;
                ApplyTileStyle(tile, LadderTileStyle.EndWordReached);
            }
        }

        public void FlipRevealEndWord()
        {
            if (endWordRow == null) return;
            StartCoroutine(StaggeredFlipReveal(endWordRow, 0.06f));
        }

        // ============================================================
        //  Ladder rendering — §3.2 styles + §3.5 diff highlight
        // ============================================================

        private void RenderChainRows()
        {
            if (chainScrollContent == null) return;
            EnsureChainContentLayout(chainScrollContent);
            ClearChildren(chainScrollContent);

            // §3.5 Row 0 = start word (rendered separately in startWordRow). Skip index 0
            // and render history rows for indices 1..N-1 with diff highlighting.
            if (currentChain == null || currentChain.Count == 0) return;
            for (int i = 1; i < currentChain.Count; i++)
            {
                string prev = currentChain[i - 1] ?? string.Empty;
                string curr = currentChain[i] ?? string.Empty;
                BuildChainHistoryRow(prev, curr);
            }

            RenderLadderHighlight(); // §6 internal helper: idempotent re-color pass.
        }

        /// <summary>§6 internal helper — re-applies §3.5 diff coloring to chain history rows.</summary>
        private void RenderLadderHighlight()
        {
            if (chainScrollContent == null || currentChain == null) return;

            int rowIdx = 0;
            for (int i = 1; i < currentChain.Count; i++, rowIdx++)
            {
                if (rowIdx >= chainScrollContent.childCount) break;
                var row = chainScrollContent.GetChild(rowIdx) as RectTransform;
                if (row == null) continue;
                ApplyChainRowDiffColors(row, currentChain[i - 1], currentChain[i]);
            }
        }

        private void BuildChainHistoryRow(string prev, string curr)
        {
            var rowGO = new GameObject("ChainRow", typeof(RectTransform));
            var rowRT = (RectTransform)rowGO.transform;
            rowRT.SetParent(chainScrollContent, false);
            rowRT.SetAsLastSibling();
            EnsureHorizontalLayout(rowRT, TILE_GAP_H, leftPad: 0);

            var le = rowGO.AddComponent<LayoutElement>();
            le.minHeight = _chainRowHeight;
            le.preferredHeight = _chainRowHeight;
            // flexibleWidth not needed: VLG childForceExpandWidth stretches the row to Content width.

            if (string.IsNullOrEmpty(curr)) return;

            for (int k = 0; k < curr.Length; k++)
            {
                var t = InstantiateTile(rowRT);
                if (t == null) continue;
                t.SetSize(_tileSize);
                t.SetLetter(char.ToUpper(curr[k]));

                bool changed = IsChangedPosition(prev, curr, k);
                ApplyTileStyle(t, changed ? LadderTileStyle.ChainChanged : LadderTileStyle.ChainUnchanged);
            }
        }

        private static bool IsChangedPosition(string prev, string curr, int k)
        {
            if (string.IsNullOrEmpty(prev) || string.IsNullOrEmpty(curr)) return false;
            if (prev.Length != curr.Length) return false;
            if (k < 0 || k >= curr.Length) return false;
            return char.ToLowerInvariant(prev[k]) != char.ToLowerInvariant(curr[k]);
        }

        private void ApplyChainRowDiffColors(RectTransform row, string prev, string curr)
        {
            if (row == null || string.IsNullOrEmpty(curr)) return;
            int n = Mathf.Min(row.childCount, curr.Length);
            for (int k = 0; k < n; k++)
            {
                var tile = row.GetChild(k).GetComponent<LetterTile>();
                if (tile == null) continue;
                bool changed = IsChangedPosition(prev, curr, k);
                ApplyTileStyle(tile, changed ? LadderTileStyle.ChainChanged : LadderTileStyle.ChainUnchanged);
            }
        }

        private void BuildLadderRowTiles(RectTransform row, string word, bool isStartRow, bool reached)
        {
            if (row == null) return;
            EnsureHorizontalLayout(row, TILE_GAP_H, leftPad: (int)ROW_LABEL_PAD_L);
            ClearChildren(row);
            if (string.IsNullOrEmpty(word)) return;

            for (int k = 0; k < word.Length; k++)
            {
                var t = InstantiateTile(row);
                if (t == null) continue;
                t.SetSize(_tileSize);
                t.SetLetter(char.ToUpper(word[k]));

                LadderTileStyle style;
                if (isStartRow)
                    style = LadderTileStyle.StartWord;
                else
                    style = reached ? LadderTileStyle.EndWordReached : LadderTileStyle.EndWordNeutral;

                ApplyTileStyle(t, style);
            }
        }

        private void RenderCurrentInputRow()
        {
            if (currentInputRow == null) return;
            if (string.IsNullOrEmpty(currentEndWord))
            {
                ClearChildren(currentInputRow);
                return;
            }

            int targetLen = currentEndWord.Length;
            EnsureHorizontalLayout(currentInputRow, TILE_GAP_H, leftPad: (int)ROW_LABEL_PAD_L);
            ClearChildren(currentInputRow);

            string input = currentInput ?? string.Empty;

            for (int k = 0; k < targetLen; k++)
            {
                var t = InstantiateTile(currentInputRow);
                if (t == null) continue;
                t.SetSize(_tileSize);

                bool isHintHighlight = (hintLetterIndex == k);
                bool hasLetter = k < input.Length;

                if (hasLetter) t.SetLetter(char.ToUpper(input[k]));
                else t.Clear();

                if (isHintHighlight)
                    ApplyTileStyle(t, LadderTileStyle.InputHintHighlight);
                else if (hasLetter)
                    ApplyTileStyle(t, LadderTileStyle.InputFilled);
                else
                    ApplyTileStyle(t, LadderTileStyle.InputEmpty);
            }
        }

        private void RecomputeRevealedChangedIndex()
        {
            if (string.IsNullOrEmpty(revealedNextWord) || currentChain == null || currentChain.Count == 0)
            {
                revealedChangedIndex = -1;
                return;
            }
            string tail = currentChain[currentChain.Count - 1] ?? string.Empty;
            int sharedLen = Mathf.Min(tail.Length, revealedNextWord.Length);
            for (int k = 0; k < sharedLen; k++)
            {
                if (char.ToLowerInvariant(tail[k]) != char.ToLowerInvariant(revealedNextWord[k]))
                {
                    revealedChangedIndex = k;
                    return;
                }
            }
            revealedChangedIndex = (tail.Length == revealedNextWord.Length) ? -1 : sharedLen;
        }

        private void RenderRevealPreviewRow()
        {
            EnsureRevealPreviewRow();
            if (revealPreviewRow == null) return;

            if (string.IsNullOrEmpty(revealedNextWord))
            {
                HideRevealPreviewRow();
                return;
            }

            revealPreviewRow.gameObject.SetActive(true);
            // Reveal row should appear at the bottom of chain history (just above current-input).
            revealPreviewRow.SetAsLastSibling();

            EnsureHorizontalLayout(revealPreviewRow, TILE_GAP_H, leftPad: (int)ROW_LABEL_PAD_L);
            ClearChildren(revealPreviewRow);

            for (int k = 0; k < revealedNextWord.Length; k++)
            {
                var t = InstantiateTile(revealPreviewRow);
                if (t == null) continue;
                t.SetSize(_tileSize);
                t.SetLetter(char.ToUpper(revealedNextWord[k]));

                ApplyTileStyle(t, (k == revealedChangedIndex)
                    ? LadderTileStyle.RevealChanged
                    : LadderTileStyle.RevealUnchanged);
            }
        }

        private void HideRevealPreviewRow()
        {
            if (revealPreviewRow == null) return;
            ClearChildren(revealPreviewRow);
            revealPreviewRow.gameObject.SetActive(false);
        }

        private void EnsureRevealPreviewRow()
        {
            if (revealPreviewRow != null) return;
            if (chainScrollContent == null) return;

            var go = new GameObject("RevealPreviewRow", typeof(RectTransform));
            revealPreviewRow = (RectTransform)go.transform;
            revealPreviewRow.SetParent(chainScrollContent, false);
            EnsureHorizontalLayout(revealPreviewRow, TILE_GAP_H, leftPad: (int)ROW_LABEL_PAD_L);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = _chainRowHeight;
            le.preferredHeight = _chainRowHeight;
            le.flexibleWidth = 1f;
            go.SetActive(false);
        }

        private bool ChainEndsAtEndWord()
        {
            if (currentChain == null || currentChain.Count == 0) return false;
            if (string.IsNullOrEmpty(currentEndWord)) return false;
            string tail = currentChain[currentChain.Count - 1] ?? string.Empty;
            return string.Equals(tail, currentEndWord, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyEndLabelColor(bool reached)
        {
            if (endWordLabel == null) return;
            endWordLabel.color = reached ? C_LABEL_REACHED : LBL_TO;
        }

        // ============================================================
        //  §3.2 Tile-style application
        // ============================================================
        private enum LadderTileStyle
        {
            StartWord,
            ChainUnchanged,
            ChainChanged,
            InputEmpty,
            InputFilled,
            InputHintHighlight,
            RevealUnchanged,
            RevealChanged,
            EndWordNeutral,
            EndWordReached
        }

        private static void ApplyTileStyle(LetterTile tile, LadderTileStyle style)
        {
            if (tile == null) return;

            // Drive LetterTile.SetState (for shadow/caret behavior) then SetColor to enforce
            // the exact §3.2 fill. Text color is set via the inner TMP label.
            switch (style)
            {
                case LadderTileStyle.StartWord:
                    // Task 27 — start word row = see-through TEAL outline (menu Resume colour), the origin.
                    tile.SetState(TileState.DefaultPrefilled);
                    tile.SetOutline(MenuPalette.ResumeFill);
                    SetTextColor(tile, C_TILE_TEXT);
                    break;

                case LadderTileStyle.EndWordNeutral:
                    // Task 27 — target word row = see-through ORANGE outline (menu Daily colour), the goal.
                    tile.SetState(TileState.DefaultPrefilled);
                    tile.SetOutline(MenuPalette.DailyFill);
                    SetTextColor(tile, C_TILE_TEXT);
                    break;

                case LadderTileStyle.ChainUnchanged:
                    // Task 30 — played chain rows are SEE-THROUGH with a calm CYAN outline (ghost look),
                    // matching the start/target outline language instead of the old solid grey brick.
                    // ChainChanged (green correct-letter) stays a fill, so highlights still read within the row.
                    tile.SetState(TileState.DefaultPrefilled);
                    tile.SetOutline(MenuPalette.ChainOutline);
                    SetTextColor(tile, C_TILE_TEXT);
                    break;

                case LadderTileStyle.ChainChanged:
                    tile.SetState(TileState.CorrectInChain);
                    tile.SetColor(C_TILE_CHANGED_FILL);
                    SetTextColor(tile, C_TILE_CHANGED_TEXT);
                    break;

                case LadderTileStyle.InputEmpty:
                    tile.SetState(TileState.DefaultEmpty);
                    tile.SetColor(C_INPUT_EMPTY_FILL);
                    SetTextColor(tile, C_TILE_TEXT);
                    break;

                case LadderTileStyle.InputFilled:
                    tile.SetState(TileState.DefaultPrefilled);
                    tile.SetColor(C_TILE_DEFAULT_FILL);
                    SetTextColor(tile, C_TILE_TEXT);
                    break;

                case LadderTileStyle.InputHintHighlight:
                    tile.SetState(TileState.RevealedByHint);
                    tile.SetColor(C_HINT_GOLD);
                    SetTextColor(tile, C_HINT_TEXT_ON_GOLD);
                    break;

                case LadderTileStyle.RevealUnchanged:
                    // Outline-only ghost — transparent fill, muted border tone via text+color.
                    tile.SetState(TileState.DefaultEmpty);
                    tile.SetColor(new Color(0f, 0f, 0f, 0f));
                    SetTextColor(tile, C_REVEAL_BORDER_MUTED);
                    break;

                case LadderTileStyle.RevealChanged:
                    // Outline-only ghost — transparent fill, gold text.
                    tile.SetState(TileState.DefaultEmpty);
                    tile.SetColor(new Color(0f, 0f, 0f, 0f));
                    SetTextColor(tile, C_HINT_GOLD);
                    break;

                case LadderTileStyle.EndWordReached:
                    tile.SetState(TileState.CorrectInChain);
                    tile.SetColor(C_TILE_CHANGED_FILL);
                    SetTextColor(tile, C_TILE_CHANGED_TEXT);
                    break;
            }
        }

        private static void SetTextColor(LetterTile tile, Color color)
        {
            if (tile == null) return;
            // Use the dedicated setter to avoid grabbing the StateGlyph TMP instead of the letter label.
            tile.SetLetterColor(color);
        }

        // ============================================================
        //  Internals — tile row management
        // ============================================================
        private LetterTile InstantiateTile(Transform parent)
        {
            LetterTile t;
            if (letterTilePrefab != null)
            {
                t = Instantiate(letterTilePrefab, parent);
            }
            else
            {
                var go = new GameObject("LetterTile", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.AddComponent<LetterTile>();
            }
            return t;
        }

        private static void EnsureHorizontalLayout(RectTransform row, float spacing, int leftPad = 0)
        {
            if (row == null) return;
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = spacing;
            hlg.padding = new RectOffset(leftPad, leftPad, 0, 0);
        }

        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child.gameObject);
                else UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void EnsureChainContentLayout(RectTransform t)
        {
            if (t == null) return;
            var vlg = t.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = t.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            // Issue 2 fix: childControlWidth+childForceExpandWidth=true so each chain row
            // stretches to the full Content width. This makes every rung's HLG (MiddleCenter)
            // center tiles across the same pixel X-range as the full-width FROM/input/TO rows.
            // Without this, rows were only as wide as their tile content, shifting them right.
            vlg.childControlWidth = true;
            // Task 30 — childControlHeight MUST be true so the VLG honours each row's
            // LayoutElement.preferredHeight (= _chainRowHeight) and actually applies `spacing`
            // between rows. With it false, the group ignored the preferred height and the rung
            // gap collapsed, so consecutive played rows stacked flush (the grey-brick block).
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            // RUNG_GAP (38px) between chain rows — same rung gap the start/input/target rows use —
            // with tight top/bottom pad so the chain doesn't crowd the FROM/TO rows on small portrait.
            vlg.spacing = ROW_GAP_V;
            vlg.padding = new RectOffset(8, 8, 8, 8);

            var csf = t.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = t.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        // ============================================================
        //  §3.4 Auto-scroll — current input row 8px above end-word row
        // ============================================================
        private void ScrollSoCurrentInputAboveEndWord()
        {
            if (chainScrollRect == null)
            {
                if (chainScrollContent != null)
                    chainScrollRect = chainScrollContent.GetComponentInParent<ScrollRect>();
            }
            if (chainScrollRect == null || chainScrollContent == null) return;

            Canvas.ForceUpdateCanvases();

            if (smoothScrollRoutine != null)
            {
                StopCoroutine(smoothScrollRoutine);
                smoothScrollRoutine = null;
            }

            // §3.4 — if time is paused, snap immediately.
            bool instant = Mathf.Approximately(Time.timeScale, 0f);
            if (instant)
            {
                chainScrollRect.verticalNormalizedPosition = 0f;
                return;
            }

            smoothScrollRoutine = StartCoroutine(SmoothScrollToBottom(AUTOSCROLL_DURATION));
        }

        private IEnumerator SmoothScrollToBottom(float duration)
        {
            // Wait one frame so ContentSizeFitter resolves height before computing target.
            yield return null;

            if (chainScrollRect == null) yield break;

            float start = chainScrollRect.verticalNormalizedPosition;
            float target = 0f; // bottom — newest entry visible (input row sits ROW_GAP_V above end-word row).
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f); // ease-OutCubic
                chainScrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, eased);
                yield return null;
            }

            chainScrollRect.verticalNormalizedPosition = target;
            smoothScrollRoutine = null;
        }

        // ============================================================
        //  Animations (coroutine-based, no DOTween)
        // ============================================================
        private IEnumerator ShakeRoutine(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            Vector3 origin = rt.localPosition;
            float[] keys = { 0f, 12f, -12f, 8f, -8f, 0f };
            float segment = duration / (keys.Length - 1);

            for (int k = 0; k < keys.Length - 1; k++)
            {
                float t = 0f;
                while (t < segment)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / segment);
                    rt.localPosition = origin + new Vector3(Mathf.Lerp(keys[k], keys[k + 1], p), 0f, 0f);
                    yield return null;
                }
            }
            rt.localPosition = origin;
        }

        private IEnumerator StaggeredFlipReveal(RectTransform row, float stagger)
        {
            if (row == null) yield break;
            for (int i = 0; i < row.childCount; i++)
            {
                var tile = row.GetChild(i).GetComponent<LetterTile>();
                if (tile != null) StartCoroutine(tile.FlipReveal(0.22f));
                yield return new WaitForSecondsRealtime(stagger);
            }
        }

        // ============================================================
        //  Helpers
        // ============================================================
        private static void FadeTextAlpha(TextMeshProUGUI t, float alpha)
        {
            if (t == null) return;
            var c = t.color;
            c.a = alpha;
            t.color = c;
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }

        // ============================================================
        //  Issue 5: Disable stray HighlightFrame when TutorialOverlay inactive
        // ============================================================
        private void DisableStrayHighlightFrame()
        {
            // TutorialOverlay lives as a sibling of GameplayScreen under Canvas.
            // Its HighlightFrame child is disabled in the scene (m_IsActive=0) so it
            // won't render. As a runtime guard, also ensure it stays off when the
            // overlay itself is not active.
            var canvas = transform.parent;
            if (canvas == null) return;

            var overlay = canvas.GetComponentInChildren<TutorialOverlay>(true);
            if (overlay == null || overlay.gameObject.activeSelf) return;

            // Overlay is inactive — ensure every Image child is also inactive.
            var imgs = overlay.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
                if (img != null && img.gameObject.activeSelf)
                    img.gameObject.SetActive(false);
        }

        // ============================================================
        //  Issue 6: Header reposition below notch (no SafeArea script)
        // ============================================================
        // Moves BackButton, ScoreText, TierIndicatorText so their top edges
        // sit ~130px below the canvas top (well below the iPhone 13 Pro Max notch).
        // Safe-area offset is baked in as a constant — avoids [ExecuteAlways] scripts
        // that caused the black-screen regression.
        private void RepositionHeaderBelowNotch()
        {
            const float notchClearance = 130f; // px below canvas top edge
            // Canvas height = 1920; from anchor=(0,1) top, anchoredPosition.y is negative downward.
            // Target top-edge offset from canvas top = notchClearance → anchoredPosition.y = -notchClearance.

            RepositionHeaderElement(backButton?.GetComponent<RectTransform>(), notchClearance);
            RepositionHeaderElement(scoreText?.GetComponent<RectTransform>(),  notchClearance);
            RepositionHeaderElement(tierIndicatorText?.GetComponent<RectTransform>(), notchClearance);
            RepositionHeaderElement(timerText?.GetComponent<RectTransform>(), notchClearance);
        }

        private static void RepositionHeaderElement(RectTransform rt, float notchClearance)
        {
            if (rt == null) return;
            // Only act on top-anchored elements (anchorMin.y == anchorMax.y == 1)
            // to avoid moving elements that live in a different layout band.
            if (!Mathf.Approximately(rt.anchorMin.y, 1f) || !Mathf.Approximately(rt.anchorMax.y, 1f)) return;

            var ap = rt.anchoredPosition;
            // anchoredPosition.y is measured upward from anchor; for top-anchor it's negative downward.
            // We want the top of the element at notchClearance from canvas top.
            // With pivot.y=1: top-edge = |anchoredPosition.y|, so set it to -notchClearance.
            // With pivot.y=0.5: centre at notchClearance + height/2 from top → anchoredPosition.y = -(notchClearance + height/2).
            float targetY;
            if (Mathf.Approximately(rt.pivot.y, 1f))
                targetY = -notchClearance;
            else
                targetY = -(notchClearance + rt.rect.height * 0.5f);

            if (ap.y > targetY) // already below notch — don't move further down
            {
                ap.y = targetY;
                rt.anchoredPosition = ap;
            }
        }

        // ============================================================
        //  Submit pipeline (preserved)
        // ============================================================
        private void SubmitWord()
        {
            if (wordInputField != null && !string.IsNullOrWhiteSpace(wordInputField.text))
            {
                OnWordSubmitted?.Invoke(wordInputField.text.ToLower());
                ClearInput();
            }
            else if (!string.IsNullOrWhiteSpace(currentInput))
            {
                HandleEnterPressed();
            }
        }

        private void OnInputSubmit(string value)
        {
            SubmitWord();
        }
    }
}
