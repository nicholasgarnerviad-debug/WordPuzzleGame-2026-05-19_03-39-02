using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.UI;

namespace WordPuzzle.UI.Components
{
    /// <summary>
    /// Tile state per GameplayScreen UI Spec v1 §4.
    /// </summary>
    public enum TileState
    {
        DefaultEmpty,
        DefaultPrefilled,
        RevealedByHint,
        CurrentInputTyped,
        CurrentInputCaret,
        CorrectInChain,
        InvalidFlash
    }

    /// <summary>
    /// Single prefab-style LetterTile component with a state machine.
    /// Renders a rounded-rect background (9-sliced UISprite), an optional shadow child,
    /// and a TextMeshProUGUI letter. Animations are coroutine-based — no DOTween.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LetterTile : MonoBehaviour
    {
        // ---------- Inspector refs (optional; auto-created when null) ----------
        [SerializeField] private Image background;
        [SerializeField] private Image shadow;
        [SerializeField] private TextMeshProUGUI letterText;
        [SerializeField] private Button button;             // legacy: optional click target
        [SerializeField] private TMP_FontAsset font;

        // Palette (UI spec v1)
        private static readonly Color C_BG          = HexToColor("#0F1217");
        private static readonly Color C_SURFACE     = HexToColor("#1B1F27");
        private static readonly Color C_SURFACE_2   = HexToColor("#242936");
        private static readonly Color C_ACCENT      = HexToColor("#6AAA64");
        private static readonly Color C_ACCENT_SOFT = HexToColor("#538D4E");
        private static readonly Color C_HINT_GOLD   = HexToColor("#C9B458");
        private static readonly Color C_DANGER      = HexToColor("#D9534F");
        private static readonly Color C_TEXT        = HexToColor("#F5F7FA");
        private static readonly Color C_TEXT_MUTED  = HexToColor("#8A93A6");
        private static readonly Color C_BORDER      = HexToColor("#3A4150");

        private RectTransform rectTransform;
        private Image borderImage;       // outline-only child used to draw the border
        private TileState currentState = TileState.DefaultEmpty;
        private float currentSize = 90f;
        private char currentLetter = ' ';

        private Coroutine caretCoroutine;
        private Coroutine invalidFlashCoroutine;

        // ---------- Legacy compatibility (kept so tests/editor scripts still build) ----------
        public delegate void TileClickedCallback();
        public delegate void LetterPressedCallback(char letter);
        public event TileClickedCallback OnTileClicked;
        public event LetterPressedCallback OnLetterPressed;
        public char Letter => currentLetter;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            EnsureChildren();
            ApplyStateVisuals();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        // ============================================================
        //  Public API (UI Spec v1 §4)
        // ============================================================

        /// <summary>Sets the displayed letter (uppercased).</summary>
        public void SetLetter(char c)
        {
            currentLetter = c;
            if (letterText != null)
                letterText.text = c == ' ' ? string.Empty : char.ToUpper(c).ToString();
        }

        /// <summary>Clears the letter so the tile shows no character.</summary>
        public void Clear()
        {
            currentLetter = ' ';
            if (letterText != null)
                letterText.text = string.Empty;
        }

        /// <summary>Switches the tile to the given visual state.</summary>
        public void SetState(TileState state)
        {
            currentState = state;
            ApplyStateVisuals();
        }

        /// <summary>Sets the tile size (width and height in pixels).</summary>
        public void SetSize(float size)
        {
            currentSize = Mathf.Max(8f, size);
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(currentSize, currentSize);

            var le = GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = currentSize;
            le.preferredHeight = currentSize;
            le.minWidth = currentSize;
            le.minHeight = currentSize;

            if (letterText != null)
                letterText.fontSize = Mathf.Max(12f, currentSize * 0.55f);
        }

        /// <summary>Punch-scale animation (ease-OutBack feel) over the given duration.</summary>
        public IEnumerator PunchScale(float magnitude = 1.06f, float duration = 0.15f)
        {
            if (UIAnimations.ReduceMotion)
            {
                if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
                rectTransform.localScale = Vector3.one;
                yield break;
            }
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            Vector3 baseScale = Vector3.one;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float bump = Mathf.Sin(p * Mathf.PI);
                float s = 1f + (magnitude - 1f) * (1f + EaseOutBack(bump) * 0.0f); // keep simple sin envelope
                rectTransform.localScale = baseScale * (1f + (magnitude - 1f) * bump);
                yield return null;
            }

            rectTransform.localScale = baseScale;
        }

        /// <summary>Flashes the background to the given color, then reverts to the state color.</summary>
        public IEnumerator FlashColor(Color flash, float duration = 0.25f)
        {
            if (UIAnimations.ReduceMotion) { ApplyStateVisuals(); yield break; }
            if (background == null) yield break;
            Color start = background.color;
            background.color = flash;
            yield return new WaitForSecondsRealtime(duration);
            background.color = start;
        }

        /// <summary>Y-axis rotation 0→90→0 reveal; state visuals re-apply at 90°.</summary>
        public IEnumerator FlipReveal(float duration = 0.22f)
        {
            if (UIAnimations.ReduceMotion)
            {
                if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
                rectTransform.localRotation = Quaternion.identity;
                ApplyStateVisuals();
                yield break;
            }
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            float half = duration * 0.5f;
            float t = 0f;

            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                rectTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 90f, p), 0f);
                yield return null;
            }

            // At 90°, re-apply state — lets caller pre-SetState before invoking so the swap happens mid-flip.
            ApplyStateVisuals();

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                rectTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 0f, p), 0f);
                yield return null;
            }

            rectTransform.localRotation = Quaternion.identity;
        }

        // ============================================================
        //  Legacy API (kept for backwards compatibility — tests/editor)
        // ============================================================

        public void Initialize(char letter) => SetLetter(letter);
        public char GetLetter() => currentLetter;
        public void SetColor(Color color) { if (background != null) background.color = color; }
        public void ResetColor() => ApplyStateVisuals();
        public void HighlightHint() => SetState(TileState.RevealedByHint);
        public void SetEnabled(bool enabled) { if (button != null) button.interactable = enabled; }
        public void OnClick()
        {
            OnTileClicked?.Invoke();
            OnLetterPressed?.Invoke(currentLetter);
        }

        // ============================================================
        //  Internals
        // ============================================================

        private void EnsureChildren()
        {
            // Background image on this GO (rounded-rect, 9-sliced).
            if (background == null)
            {
                background = GetComponent<Image>();
                if (background == null)
                    background = gameObject.AddComponent<Image>();
            }
            background.type = Image.Type.Sliced;
            background.raycastTarget = false;
            background.sprite = TryGetRoundedRectSprite();
            background.color = C_SURFACE;

            // Shadow child — drawn behind, slight Y offset.
            if (shadow == null)
            {
                Transform existing = transform.Find("Shadow");
                if (existing != null) shadow = existing.GetComponent<Image>();
                if (shadow == null)
                {
                    var shadowGO = new GameObject("Shadow", typeof(RectTransform));
                    shadowGO.transform.SetParent(transform, false);
                    shadowGO.transform.SetAsFirstSibling();
                    var srt = shadowGO.GetComponent<RectTransform>();
                    srt.anchorMin = Vector2.zero;
                    srt.anchorMax = Vector2.one;
                    srt.offsetMin = new Vector2(0f, -2f);
                    srt.offsetMax = new Vector2(0f, -2f);
                    shadow = shadowGO.AddComponent<Image>();
                    shadow.sprite = TryGetRoundedRectSprite();
                    shadow.type = Image.Type.Sliced;
                    shadow.raycastTarget = false;
                    shadow.color = new Color(0f, 0f, 0f, 0f);
                }
            }

            // Border child — drawn on top, outline-style.
            if (borderImage == null)
            {
                Transform existing = transform.Find("Border");
                if (existing != null) borderImage = existing.GetComponent<Image>();
                if (borderImage == null)
                {
                    var borderGO = new GameObject("Border", typeof(RectTransform));
                    borderGO.transform.SetParent(transform, false);
                    var brt = borderGO.GetComponent<RectTransform>();
                    brt.anchorMin = Vector2.zero;
                    brt.anchorMax = Vector2.one;
                    brt.offsetMin = Vector2.zero;
                    brt.offsetMax = Vector2.zero;
                    borderImage = borderGO.AddComponent<Image>();
                    borderImage.sprite = TryGetRoundedRectSprite();
                    borderImage.type = Image.Type.Sliced;
                    borderImage.raycastTarget = false;
                    borderImage.color = new Color(0f, 0f, 0f, 0f);
                }
            }

            // Label child.
            if (letterText == null)
            {
                letterText = GetComponentInChildren<TextMeshProUGUI>(true);
                if (letterText == null)
                {
                    var labelGO = new GameObject("Letter", typeof(RectTransform));
                    labelGO.transform.SetParent(transform, false);
                    var lrt = labelGO.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                    letterText = labelGO.AddComponent<TextMeshProUGUI>();
                    letterText.alignment = TextAlignmentOptions.Center;
                    letterText.fontStyle = FontStyles.Bold;
                    letterText.fontSize = Mathf.Max(12f, currentSize * 0.55f);
                    letterText.color = C_TEXT;
                    letterText.raycastTarget = false;
                    letterText.enableWordWrapping = false;
                    if (font != null) letterText.font = font;
                }
            }

            // Optional button (legacy).
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
        }

        private void ApplyStateVisuals()
        {
            if (background == null) EnsureChildren();
            if (background == null) return;

            if (caretCoroutine != null)
            {
                StopCoroutine(caretCoroutine);
                caretCoroutine = null;
            }

            Color fill = C_SURFACE;
            Color border = new Color(0f, 0f, 0f, 0f);
            Color textC = C_TEXT;
            Color shadowC = new Color(0f, 0f, 0f, 0f);
            bool hasBorder = false;

            switch (currentState)
            {
                case TileState.DefaultEmpty:
                    fill = C_SURFACE;
                    border = C_TEXT_MUTED;
                    hasBorder = true;
                    textC = C_TEXT;
                    break;

                case TileState.DefaultPrefilled:
                    fill = C_SURFACE_2;
                    border = C_BORDER;
                    hasBorder = true;
                    textC = C_TEXT;
                    break;

                case TileState.RevealedByHint:
                    fill = C_HINT_GOLD;
                    hasBorder = false;
                    textC = C_BG;
                    shadowC = new Color(C_HINT_GOLD.r, C_HINT_GOLD.g, C_HINT_GOLD.b, 0.35f);
                    break;

                case TileState.CurrentInputTyped:
                    fill = C_SURFACE_2;
                    border = C_HINT_GOLD;
                    hasBorder = true;
                    textC = C_HINT_GOLD;
                    break;

                case TileState.CurrentInputCaret:
                    fill = C_SURFACE_2;
                    border = C_HINT_GOLD;
                    hasBorder = true;
                    textC = C_HINT_GOLD;
                    if (isActiveAndEnabled)
                        caretCoroutine = StartCoroutine(CaretPulse());
                    break;

                case TileState.CorrectInChain:
                    fill = C_ACCENT;
                    hasBorder = false;
                    textC = C_TEXT;
                    break;

                case TileState.InvalidFlash:
                    fill = C_DANGER;
                    hasBorder = false;
                    textC = C_TEXT;
                    if (invalidFlashCoroutine != null) StopCoroutine(invalidFlashCoroutine);
                    if (isActiveAndEnabled)
                        invalidFlashCoroutine = StartCoroutine(AutoRevertInvalid());
                    break;
            }

            background.color = fill;
            if (letterText != null) letterText.color = textC;
            if (shadow != null) shadow.color = shadowC;
            if (borderImage != null)
            {
                borderImage.color = hasBorder ? border : new Color(0f, 0f, 0f, 0f);
            }
        }

        private IEnumerator AutoRevertInvalid()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            invalidFlashCoroutine = null;
            currentState = TileState.DefaultEmpty;
            ApplyStateVisuals();
        }

        private IEnumerator CaretPulse()
        {
            const float hz = 1.5f;
            while (currentState == TileState.CurrentInputCaret && borderImage != null)
            {
                float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * hz) + 1f) * 0.5f;
                Color a = C_HINT_GOLD; a.a = Mathf.Lerp(0.4f, 1.0f, t);
                Color b = C_ACCENT_SOFT; b.a = Mathf.Lerp(0.4f, 1.0f, 1f - t);
                borderImage.color = Color.Lerp(a, b, 0.5f);
                yield return null;
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }

        private static Sprite cachedRoundedSprite;
        private static Sprite TryGetRoundedRectSprite()
        {
            if (cachedRoundedSprite != null) return cachedRoundedSprite;

            // Note: Resources.GetBuiltinResource("UI/Skin/UISprite.psd") is intentionally NOT used here.
            // It emits a Unity LogError before throwing in some editor configurations, polluting the console
            // on every scene load. The programmatic rounded-rect below is fully sufficient and silent.
            const int size = 32;
            const int radius = 6;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;
                    if (x < radius && y < radius)
                        inside = (radius - x) * (radius - x) + (radius - y) * (radius - y) <= radius * radius;
                    else if (x >= size - radius && y < radius)
                        inside = (x - (size - radius - 1)) * (x - (size - radius - 1)) + (radius - y) * (radius - y) <= radius * radius;
                    else if (x < radius && y >= size - radius)
                        inside = (radius - x) * (radius - x) + (y - (size - radius - 1)) * (y - (size - radius - 1)) <= radius * radius;
                    else if (x >= size - radius && y >= size - radius)
                        inside = (x - (size - radius - 1)) * (x - (size - radius - 1)) + (y - (size - radius - 1)) * (y - (size - radius - 1)) <= radius * radius;
                    pixels[y * size + x] = inside ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            cachedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return cachedRoundedSprite;
        }
    }
}
