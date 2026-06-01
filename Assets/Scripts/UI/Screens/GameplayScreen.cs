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

        // Legacy label palette (kept for FROM/TO + steps subtitle).
        // Task 8A: LBL_TO demoted from gold #C9B458 to text-muted #8A93A1.
        // The TO-row label is secondary info; gold is reserved for focus/hint/win moments.
        private static readonly Color LBL_FROM       = HexToColor("#7A828F");
        private static readonly Color LBL_TO         = HexToColor("#8A93A1");
        private static readonly Color LBL_STEPS      = HexToColor("#8A93A1");

        // Task 8A/8C — secondary UI labels (score, etc.) use text-muted so they don't compete.
        private static readonly Color C_LABEL_SECONDARY = HexToColor("#8A93A1");

        // ---------- Spec §3 layout constants ----------
        private const float TILE_SIZE_LADDER = 64f;
        private const float TILE_GAP_H       = 6f;  // §3.2 inter-tile gap
        private const float ROW_GAP_V        = 8f;  // §3 inter-row gap
        private const float ROW_LABEL_PAD_L  = 64f; // §3 row left padding for label
        private const float AUTOSCROLL_DURATION = 0.18f; // §3.4 180ms ease-out
        private const float CHAIN_ROW_HEIGHT  = TILE_SIZE_LADDER + 8f;

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

        public event Action<string> OnWordSubmitted;
        public event Action OnBackToMenu;
        public event Action OnHintUsed;
        public event Action OnRevealUsed;
        public event Action OnUndoStep;
        public event Action OnAddTimeUsed;

        // ============================================================
        //  Lifecycle
        // ============================================================
        private void OnEnable()
        {
            if (submitButton != null) submitButton.onClick.AddListener(SubmitWord);
            if (wordInputField != null) wordInputField.onSubmit.AddListener(OnInputSubmit);
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
                var lbl = backButton.GetComponentInChildren<TMP_Text>(true);
                if (lbl != null)
                {
                    lbl.text = "HOME";
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.fontSize = 28f;
                    lbl.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
                    lbl.alignment = TextAlignmentOptions.Center;
                }
            }

            if (hintButton != null) hintButton.onClick.AddListener(() => OnHintUsed?.Invoke());
            if (revealButton != null) revealButton.onClick.AddListener(() => OnRevealUsed?.Invoke());
            if (undoButton != null) undoButton.onClick.AddListener(() => OnUndoStep?.Invoke());
            if (addTimeButton != null) addTimeButton.onClick.AddListener(() => OnAddTimeUsed?.Invoke());

            if (keyboard != null)
            {
                keyboard.OnLetterPressed += HandleLetterPressed;
                keyboard.OnBackspacePressed += HandleBackspacePressed;
                keyboard.OnEnterPressed += HandleEnterPressed;
            }

            HideLegacyText();

            ReparentBadge(hintCountText, hintButton, new Vector2(38f, 32f));
            ReparentBadge(revealCountText, revealButton, new Vector2(38f, 32f));
            ReparentBadge(addTimeCountText, addTimeButton, new Vector2(38f, 32f));

            ConfigureChainScrollRect();

            StyleRowLabel(startWordLabel, "FROM", LBL_FROM);
            StyleRowLabel(endWordLabel, "TO", LBL_TO);

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

            // Task 8A — tier indicator demoted: secondary info must not compete with gold focal elements.
            // Changed from Bold+gold(#C9B458)+28pt to Normal+text-muted(#8A93A1)+20pt, still centered.
            if (tierIndicatorText != null)
            {
                tierIndicatorText.fontStyle = FontStyles.Normal;
                tierIndicatorText.color = new Color32(0x8A, 0x93, 0xA1, 0xFF);
                tierIndicatorText.fontSize = 20;
                tierIndicatorText.alignment = TextAlignmentOptions.Center;
                var rt = tierIndicatorText.rectTransform;
                rt.anchoredPosition = new Vector2(0f, 795f);
                rt.sizeDelta = new Vector2(700f, 48f);
                tierIndicatorText.gameObject.SetActive(true);
            }
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
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = localOffset;
            rt.sizeDelta = new Vector2(64f, 36f);
            badge.alignment = TextAlignmentOptions.Center;
            badge.fontSize = 22f;
            badge.fontStyle = FontStyles.Bold;
            badge.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
            badge.raycastTarget = false;
            badge.transform.SetAsLastSibling();
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

        // ============================================================
        //  Keyboard handlers
        // ============================================================
        private void HandleLetterPressed(char c)
        {
            currentInput += char.ToLower(c);
            UpdateCurrentInputDisplay();

            // Task 7B/7C — letter placed juice.
            _sfx?.PlayKeyPress();
            _haptics?.LightTap();

            // Task 7A — tile lock-in punch on the just-filled tile (respect ReduceMotion).
            if (!UIAnimations.ReduceMotion && currentInputRow != null)
            {
                int idx = currentInput.Length - 1;
                if (idx >= 0 && idx < currentInputRow.childCount)
                {
                    var tile = currentInputRow.GetChild(idx).GetComponent<LetterTile>();
                    if (tile != null) StartCoroutine(tile.PunchScale());
                }
            }
        }

        private void HandleBackspacePressed()
        {
            if (currentInput.Length > 0)
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateCurrentInputDisplay();
        }

        private void HandleEnterPressed()
        {
            if (!string.IsNullOrWhiteSpace(currentInput))
            {
                OnWordSubmitted?.Invoke(currentInput);
                ClearCurrentInput();
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

        /// <summary>§6: Idempotent. Sets persistent FROM/TO row labels + tile content.</summary>
        public void SetStartAndEndWords(string startWord, string endWord)
        {
            currentStartWord = startWord ?? string.Empty;
            currentEndWord = endWord ?? string.Empty;

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
            currentInput = (typedSoFar ?? string.Empty).ToLower();
            if (currentInputText != null) currentInputText.text = currentInput.ToUpper();
            if (keyboard != null) keyboard.SetCurrentInput(currentInput.ToUpper());

            RenderCurrentInputRow();
        }

        /// <summary>§6: index &lt; 0 clears hint highlight; otherwise tile[index] in current-input uses gold style.</summary>
        public void SetHintLetterIndex(int index)
        {
            hintLetterIndex = index;
            RenderCurrentInputRow();
        }

        /// <summary>§6: null/empty hides preview row; otherwise shows ghost row with tile[changedIndex] in gold.</summary>
        public void SetRevealedNextWord(string word, int changedIndex)
        {
            revealedNextWord = string.IsNullOrEmpty(word) ? string.Empty : word.ToLower();
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
            if (hintCountText != null) hintCountText.text = $"Hints: {remaining}";
            if (hintButton != null) hintButton.interactable = (remaining > 0);
        }

        public void SetRevealCount(int remaining)
        {
            if (revealCountText != null) revealCountText.text = $"Reveal: {remaining}";
            if (revealButton != null) revealButton.interactable = (remaining > 0);
        }

        public void EnableUndoButton(bool enable)
        {
            if (undoButton != null) undoButton.interactable = enable;
        }

        // §5.1 — AddTime power-up surface (TimeAttack only).
        public void SetAddTimeCount(int remaining)
        {
            if (addTimeCountText != null) addTimeCountText.text = $"+Time: {remaining}";
            if (addTimeButton != null) addTimeButton.interactable = (remaining > 0);
        }

        public void SetAddTimeVisible(bool visible)
        {
            if (addTimeButton != null) addTimeButton.gameObject.SetActive(visible);
            if (addTimeCountText != null) addTimeCountText.gameObject.SetActive(visible);
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
                if (lastRow != null) StartCoroutine(UIAnimations.RowAcceptSettle(lastRow));
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
            EnsureHorizontalLayout(rowRT, TILE_GAP_H, leftPad: (int)ROW_LABEL_PAD_L);

            var le = rowGO.AddComponent<LayoutElement>();
            le.minHeight = CHAIN_ROW_HEIGHT;
            le.preferredHeight = CHAIN_ROW_HEIGHT;
            le.flexibleWidth = 1f;

            if (string.IsNullOrEmpty(curr)) return;

            for (int k = 0; k < curr.Length; k++)
            {
                var t = InstantiateTile(rowRT);
                if (t == null) continue;
                t.SetSize(TILE_SIZE_LADDER);
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
                t.SetSize(TILE_SIZE_LADDER);
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
                t.SetSize(TILE_SIZE_LADDER);

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
                t.SetSize(TILE_SIZE_LADDER);
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
            le.minHeight = CHAIN_ROW_HEIGHT;
            le.preferredHeight = CHAIN_ROW_HEIGHT;
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
                case LadderTileStyle.ChainUnchanged:
                case LadderTileStyle.EndWordNeutral:
                    tile.SetState(TileState.DefaultPrefilled);
                    tile.SetColor(C_TILE_DEFAULT_FILL);
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
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = spacing;
            hlg.padding = new RectOffset(leftPad, 0, 0, 0);
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
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            // Task 8C: ROW_GAP_V (8px) between chain rows; tight top/bottom pad so chain
            // doesn't crowd the FROM/TO rows on small portrait devices (1080x1920 and smaller).
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
