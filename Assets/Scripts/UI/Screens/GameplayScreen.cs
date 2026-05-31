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
    /// See: GameplayScreen UI Spec v1.
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

        // Phase 3: Tier indicator
        [SerializeField] private TextMeshProUGUI tierIndicatorText;

        // On-screen keyboard
        [SerializeField] private OnScreenKeyboard keyboard;
        [SerializeField] private TextMeshProUGUI currentInputText;

        // ---------- New SerializedFields (UI Spec v1) ----------
        [SerializeField] private RectTransform startWordRow;
        [SerializeField] private RectTransform endWordRow;
        [SerializeField] private RectTransform currentInputRow;
        [SerializeField] private RectTransform chainScrollContent;
        [SerializeField] private LetterTile letterTilePrefab;

        // Internal layout/animation tuning
        private const float GAP_LARGE = 12f;
        private const float GAP_INPUT = 10f;
        private const float GAP_CHAIN = 8f;
        private const float SIZE_LARGE = 110f;
        private const float SIZE_MED   = 90f;
        private const float SIZE_SMALL = 70f;

        // ---------- State ----------
        private string currentInput = "";
        private string currentEndWord = "";

        public event Action<string> OnWordSubmitted;
        public event Action OnBackToMenu;
        public event Action OnHintUsed;
        public event Action OnRevealUsed;
        public event Action OnUndoStep;

        // ============================================================
        //  Lifecycle
        // ============================================================
        private void OnEnable()
        {
            if (submitButton != null) submitButton.onClick.AddListener(SubmitWord);
            if (wordInputField != null) wordInputField.onSubmit.AddListener(OnInputSubmit);
            if (backButton != null) backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());

            if (hintButton != null) hintButton.onClick.AddListener(() => OnHintUsed?.Invoke());
            if (revealButton != null) revealButton.onClick.AddListener(() => OnRevealUsed?.Invoke());
            if (undoButton != null) undoButton.onClick.AddListener(() => OnUndoStep?.Invoke());

            if (keyboard != null)
            {
                keyboard.OnLetterPressed += HandleLetterPressed;
                keyboard.OnBackspacePressed += HandleBackspacePressed;
                keyboard.OnEnterPressed += HandleEnterPressed;
            }

            // Hide legacy text overlays (accessibility/legacy fallback only).
            FadeTextAlpha(puzzleDisplayText, 0f);
            FadeTextAlpha(currentInputText, 0f);
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveAllListeners();
            if (wordInputField != null) wordInputField.onSubmit.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();

            if (hintButton != null) hintButton.onClick.RemoveAllListeners();
            if (revealButton != null) revealButton.onClick.RemoveAllListeners();
            if (undoButton != null) undoButton.onClick.RemoveAllListeners();

            if (keyboard != null)
            {
                keyboard.OnLetterPressed -= HandleLetterPressed;
                keyboard.OnBackspacePressed -= HandleBackspacePressed;
                keyboard.OnEnterPressed -= HandleEnterPressed;
            }
        }

        // ============================================================
        //  Keyboard handlers
        // ============================================================
        private void HandleLetterPressed(char c)
        {
            currentInput += char.ToLower(c);
            UpdateCurrentInputDisplay();
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

            // Legacy text path (kept invisible for accessibility/back-compat).
            if (currentInputText != null) currentInputText.text = upper;
            if (keyboard != null) keyboard.SetCurrentInput(upper);

            // Only render input tiles once a puzzle is actually active.
            // Without an end-word, target length is unknown and we should not render empty tiles.
            if (string.IsNullOrEmpty(currentEndWord))
            {
                ClearChildren(currentInputRow);
                return;
            }

            SetCurrentInputTiles(upper, currentEndWord.Length);
        }

        public void ClearCurrentInput()
        {
            currentInput = "";
            UpdateCurrentInputDisplay();
        }

        // ============================================================
        //  Public UI API (preserved)
        // ============================================================
        public void SetPuzzleDisplay(string startWord, string endWord)
        {
            currentEndWord = endWord ?? string.Empty;

            // Legacy text (alpha 0 — accessibility / fallback).
            if (puzzleDisplayText != null)
            {
                puzzleDisplayText.text = $"{startWord} → {endWord}";
                FadeTextAlpha(puzzleDisplayText, 0f);
            }

            SetStartWordTiles(startWord);
            SetEndWordTiles(endWord, new HashSet<int>());
        }

        public void SetWordChain(string[] words)
        {
            // Legacy text path (alpha 0 — accessibility / fallback).
            if (wordChainText != null)
            {
                wordChainText.text = string.Join(" → ", words ?? Array.Empty<string>());
                FadeTextAlpha(wordChainText, 0f);
            }

            ClearChainRows();
            if (words == null) return;
            foreach (var w in words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                AppendChainRowInternal(w.ToUpper(), animate: false);
            }
            SnapChainToTop();
        }

        /// <summary>Highlights specific letter indices on the end-word row (hint reveal).</summary>
        public void SetRevealedIndices(HashSet<int> indices)
        {
            SetEndWordTiles(currentEndWord, indices);
        }

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = $"Score: {score}";
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

        public void SetTierIndicator(string text)
        {
            if (tierIndicatorText != null) tierIndicatorText.text = text ?? string.Empty;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        // ============================================================
        //  Public Tile API (UI Spec v1)
        // ============================================================

        /// <summary>Builds the start-word row: N tiles, state=DefaultPrefilled, large size if word ≤5 else medium.</summary>
        public void SetStartWordTiles(string word)
        {
            if (startWordRow == null) return;
            float size = (string.IsNullOrEmpty(word) || word.Length <= 5) ? SIZE_LARGE : SIZE_MED;
            EnsureHorizontalLayout(startWordRow, GAP_LARGE);
            ClearChildren(startWordRow);

            if (string.IsNullOrEmpty(word)) return;
            for (int i = 0; i < word.Length; i++)
            {
                var t = InstantiateTile(startWordRow);
                if (t == null) continue;
                t.SetSize(size);
                t.SetLetter(char.ToUpper(word[i]));
                t.SetState(TileState.DefaultPrefilled);
            }
        }

        /// <summary>Builds the end-word row; tiles at revealed indices use RevealedByHint, others DefaultEmpty.</summary>
        public void SetEndWordTiles(string word, HashSet<int> revealedIndices)
        {
            currentEndWord = word ?? string.Empty;
            if (endWordRow == null) return;
            float size = (string.IsNullOrEmpty(word) || word.Length <= 5) ? SIZE_LARGE : SIZE_MED;
            EnsureHorizontalLayout(endWordRow, GAP_LARGE);
            ClearChildren(endWordRow);

            if (string.IsNullOrEmpty(word)) return;
            for (int i = 0; i < word.Length; i++)
            {
                var t = InstantiateTile(endWordRow);
                if (t == null) continue;
                t.SetSize(size);
                bool revealed = revealedIndices != null && revealedIndices.Contains(i);
                if (revealed)
                {
                    t.SetLetter(char.ToUpper(word[i]));
                    t.SetState(TileState.RevealedByHint);
                }
                else
                {
                    t.Clear();
                    t.SetState(TileState.DefaultEmpty);
                }
            }
        }

        /// <summary>Builds the current-input row: targetLength tiles, sized 90px, gap 10.</summary>
        public void SetCurrentInputTiles(string input, int targetLength)
        {
            if (currentInputRow == null) return;

            // Guard: no puzzle yet (targetLength <= 0 or end-word unset) → clear the row, render nothing.
            if (targetLength <= 0 || string.IsNullOrEmpty(currentEndWord))
            {
                ClearChildren(currentInputRow);
                return;
            }

            int len = Mathf.Max(0, targetLength);
            EnsureHorizontalLayout(currentInputRow, GAP_INPUT);
            ClearChildren(currentInputRow);

            input ??= string.Empty;
            int caretPos = Mathf.Clamp(input.Length, 0, len);

            for (int i = 0; i < len; i++)
            {
                var t = InstantiateTile(currentInputRow);
                if (t == null) continue;
                t.SetSize(SIZE_MED);

                if (i < input.Length)
                {
                    t.SetLetter(char.ToUpper(input[i]));
                    t.SetState(TileState.CurrentInputTyped);
                }
                else if (i == caretPos && i < len)
                {
                    t.Clear();
                    t.SetState(TileState.CurrentInputCaret);
                }
                else
                {
                    t.Clear();
                    t.SetState(TileState.DefaultEmpty);
                }
            }
        }

        /// <summary>Adds a new mini-tile chain row at the top of chainScrollContent, with slide-down + fade-in.</summary>
        public void AppendChainRow(string word)
        {
            AppendChainRowInternal(word, animate: true);
            SnapChainToTop();
        }

        /// <summary>Drives the undo slide-X+fade animation, then destroys the last chain row.</summary>
        public void PopLastChainRow()
        {
            if (chainScrollContent == null || chainScrollContent.childCount == 0) return;
            var last = chainScrollContent.GetChild(chainScrollContent.childCount - 1);
            if (last == null) return;
            StartCoroutine(UndoSlideAndDestroy(last as RectTransform));
        }

        /// <summary>Shake animation on the current input row per §6.</summary>
        public void ShakeCurrentInput()
        {
            if (currentInputRow == null) return;
            StartCoroutine(ShakeRoutine(currentInputRow, 0.32f));

            // Flash each tile danger 250ms.
            for (int i = 0; i < currentInputRow.childCount; i++)
            {
                var tile = currentInputRow.GetChild(i).GetComponent<LetterTile>();
                if (tile != null)
                    StartCoroutine(tile.FlashColor(HexToColor("#D9534F"), 0.25f));
            }
        }

        /// <summary>Staggered FlipReveal across every tile in the end-word row.</summary>
        public void FlipRevealEndWord()
        {
            if (endWordRow == null) return;
            StartCoroutine(StaggeredFlipReveal(endWordRow, 0.06f));
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
                // No prefab assigned — build a barebones tile at runtime.
                var go = new GameObject("LetterTile", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.AddComponent<LetterTile>();
            }
            return t;
        }

        private static void EnsureHorizontalLayout(RectTransform row, float spacing)
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

        private void ClearChainRows()
        {
            ClearChildren(chainScrollContent);
        }

        private void AppendChainRowInternal(string word, bool animate)
        {
            if (chainScrollContent == null) return;
            EnsureVerticalLayout(chainScrollContent, 6f);

            var rowGO = new GameObject($"ChainRow", typeof(RectTransform));
            var rowRT = (RectTransform)rowGO.transform;
            rowRT.SetParent(chainScrollContent, false);
            EnsureHorizontalLayout(rowRT, GAP_CHAIN);
            var le = rowGO.AddComponent<LayoutElement>();
            le.minHeight = SIZE_SMALL;
            le.preferredHeight = SIZE_SMALL;

            if (!string.IsNullOrEmpty(word))
            {
                for (int i = 0; i < word.Length; i++)
                {
                    var t = InstantiateTile(rowRT);
                    if (t == null) continue;
                    t.SetSize(SIZE_SMALL);
                    t.SetLetter(char.ToUpper(word[i]));
                    t.SetState(TileState.CorrectInChain);
                }
            }

            if (animate)
                StartCoroutine(SlideDownFadeIn(rowRT, 80f, 0.26f));
        }

        private static void EnsureVerticalLayout(RectTransform t, float spacing)
        {
            if (t == null) return;
            var vlg = t.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = t.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
        }

        private void SnapChainToTop()
        {
            // If the chainScrollContent has a parent ScrollRect, snap to top.
            if (chainScrollContent == null) return;
            var sr = chainScrollContent.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        // ============================================================
        //  Animations (coroutine-based, no DOTween)
        // ============================================================

        private IEnumerator SlideDownFadeIn(RectTransform rt, float dropPx, float duration)
        {
            if (rt == null) yield break;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            Vector3 startPos = rt.localPosition + new Vector3(0f, dropPx, 0f);
            Vector3 endPos = rt.localPosition;
            rt.localPosition = startPos;
            cg.alpha = 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f); // ease-OutCubic
                rt.localPosition = Vector3.Lerp(startPos, endPos, eased);
                cg.alpha = eased;
                yield return null;
            }
            rt.localPosition = endPos;
            cg.alpha = 1f;
        }

        private IEnumerator UndoSlideAndDestroy(RectTransform rt)
        {
            if (rt == null) yield break;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

            Vector3 start = rt.localPosition;
            Vector3 end = start + new Vector3(200f, 0f, 0f);
            float duration = 0.24f;
            float t = 0f;

            // Tint child tiles to danger during slide.
            for (int i = 0; i < rt.childCount; i++)
            {
                var tile = rt.GetChild(i).GetComponent<LetterTile>();
                if (tile != null) tile.SetColor(HexToColor("#D9534F"));
            }

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p * p; // ease-InCubic
                rt.localPosition = Vector3.Lerp(start, end, eased);
                cg.alpha = 1f - eased;
                yield return null;
            }

            if (Application.isPlaying) UnityEngine.Object.Destroy(rt.gameObject);
            else UnityEngine.Object.DestroyImmediate(rt.gameObject);
        }

        private IEnumerator ShakeRoutine(RectTransform rt, float duration)
        {
            if (rt == null) yield break;
            Vector3 origin = rt.localPosition;
            // Keyframes: 0 → +12 → -12 → +8 → -8 → 0
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
