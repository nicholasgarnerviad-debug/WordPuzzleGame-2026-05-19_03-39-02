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

        // Palette — Direction B: forward to the canonical Palette (no raw hex). Correct/changed retires
        // green → aqua-spark; hint = warm Coins; invalid = warm Alert.
        private static readonly Color C_BG          = Palette.SurfaceVoid;
        private static readonly Color C_SURFACE     = Palette.Surface;
        private static readonly Color C_SURFACE_2   = Palette.Panel;
        private static readonly Color C_ACCENT      = Palette.AccentAqua;
        private static readonly Color C_ACCENT_SOFT = Palette.AccentAqua;
        private static readonly Color C_HINT_GOLD   = Palette.Coins;
        private static readonly Color C_DANGER      = Palette.Alert;
        private static readonly Color C_TEXT        = Palette.TextPrimary;
        private static readonly Color C_TEXT_MUTED  = Palette.TextMuted;
        private static readonly Color C_BORDER      = Palette.Amethyst;

        private RectTransform rectTransform;
        private Image borderImage;       // outline-only child used to draw the border
        private TextMeshProUGUI stateGlyphText; // non-color accessibility cue (corner glyph)
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

        /// <summary>Sets the letter label color directly (avoids GetComponentInChildren ambiguity with StateGlyph).</summary>
        public void SetLetterColor(Color color)
        {
            if (letterText != null) letterText.color = color;
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
                letterText.fontSize = TypeScale.TileLetterFontSize(currentSize);
            if (stateGlyphText != null)
                stateGlyphText.fontSize = TypeScale.TileStateGlyphFontSize(currentSize);
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
                if (rectTransform == null) yield break; // tile destroyed mid-pop (reconcile/scene change) — bail safely
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float bump = Mathf.Sin(p * Mathf.PI);
                rectTransform.localScale = baseScale * (1f + (magnitude - 1f) * bump);
                yield return null;
            }

            if (rectTransform != null) rectTransform.localScale = baseScale;
        }

        /// <summary>
        /// Task 29C — "placing a rung": as a letter is dropped into the active row, its glyph settles DOWN
        /// into the tile with a quick fade-in. Subtle/premium, ~0.16s, ease-out. Animates only the letter
        /// label, so it never fights the row's HorizontalLayoutGroup (layout-safe). ReduceMotion → instant.
        /// </summary>
        public IEnumerator DropInSettle(float duration = 0.16f)
        {
            EnsureChildren();
            if (letterText == null) yield break;
            var lrt = letterText.rectTransform;
            Vector3 home = lrt.localPosition;
            Color baseColor = letterText.color;

            if (UIAnimations.ReduceMotion)
            {
                lrt.localPosition = home;
                baseColor.a = 1f; letterText.color = baseColor;
                yield break;
            }

            const float fromY = 7f; // start slightly high, settle down into the tile
            float t = 0f;
            while (t < duration)
            {
                if (letterText == null) yield break; // tile rebuilt mid-type — bail safely
                t += Time.unscaledDeltaTime;
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / duration));
                lrt.localPosition = home + new Vector3(0f, Mathf.Lerp(fromY, 0f, p), 0f);
                Color c = baseColor; c.a = Mathf.Lerp(0.25f, 1f, p); letterText.color = c;
                yield return null;
            }
            lrt.localPosition = home;
            baseColor.a = 1f; letterText.color = baseColor;
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

        /// <summary>
        /// Task 27 — render this tile as a SEE-THROUGH outline: transparent centre (the black/space
        /// background shows through) with a bold rounded ring in <paramref name="ringColor"/>. Used for the
        /// start (teal) and target (orange) word rows; the active input row keeps its solid fill, so don't
        /// call this on it. The state machine still owns borderImage.color, so a later SetState (e.g. the
        /// win beat turning the target green) cleanly replaces the ring with a solid fill.
        /// </summary>
        public void SetOutline(Color ringColor)
        {
            EnsureChildren();
            if (background != null) background.color = new Color(0f, 0f, 0f, 0f); // see-through centre
            if (shadow != null) shadow.color = new Color(0f, 0f, 0f, 0f);          // no drop shadow on ghost tiles
            if (borderImage != null)
            {
                borderImage.sprite = TryGetRoundedRectOutlineSprite();
                borderImage.type = Image.Type.Sliced;
                borderImage.color = ringColor;
            }
        }
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

            // Accessibility glyph — top-right corner, non-color state cue.
            if (stateGlyphText == null)
            {
                Transform existing = transform.Find("StateGlyph");
                if (existing != null) stateGlyphText = existing.GetComponent<TextMeshProUGUI>();
                if (stateGlyphText == null)
                {
                    var glyphGO = new GameObject("StateGlyph", typeof(RectTransform));
                    glyphGO.transform.SetParent(transform, false);
                    var grt = glyphGO.GetComponent<RectTransform>();
                    grt.anchorMin = new Vector2(1f, 1f);
                    grt.anchorMax = new Vector2(1f, 1f);
                    grt.pivot = new Vector2(1f, 1f);
                    grt.anchoredPosition = new Vector2(-4f, -4f);
                    grt.sizeDelta = new Vector2(20f, 20f);
                    stateGlyphText = glyphGO.AddComponent<TextMeshProUGUI>();
                    stateGlyphText.alignment = TextAlignmentOptions.TopRight;
                    stateGlyphText.raycastTarget = false;
                    stateGlyphText.enableWordWrapping = false;
                    stateGlyphText.text = string.Empty;
                }
            }

            // Label child.
            if (letterText == null)
            {
                // Search only named "Letter" or "LetterText" children to avoid grabbing StateGlyph.
                Transform existingLabel = transform.Find("Letter") ?? transform.Find("LetterText");
                if (existingLabel != null) letterText = existingLabel.GetComponent<TextMeshProUGUI>();
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
                    letterText.enableWordWrapping = false;
                }
            }
            // Task 42 — both labels route through the type system (covers scene-authored children
            // too, which would otherwise keep the legacy default font; the serialized `font` field
            // is vestigial). Colour is STATE-driven — ApplyStateVisuals owns it after this.
            TypeScale.ApplyTileLetter(letterText, currentSize);
            letterText.color = C_TEXT;
            TypeScale.ApplyTileStateGlyph(stateGlyphText, currentSize);
            // Always ensure the letter label renders on top of Border/Shadow/StateGlyph,
            // and never blocks raycasts (input must reach the tile's Button or parent).
            letterText.raycastTarget = false;
            letterText.transform.SetAsLastSibling();

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
            // Non-color glyph: empty by default; set for CorrectInChain / InvalidFlash.
            string glyph = string.Empty;
            Color glyphC = C_TEXT;

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
                    // Hint: use AccessiblePalette so color adapts; keep gold in Off mode.
                    fill = AccessiblePalette.Hint;
                    hasBorder = false;
                    textC = C_BG;
                    shadowC = new Color(fill.r, fill.g, fill.b, 0.35f);
                    break;

                case TileState.CurrentInputTyped:
                    fill = C_SURFACE_2;
                    border = C_HINT_GOLD;
                    hasBorder = true;
                    textC = C_HINT_GOLD;
                    break;

                case TileState.CurrentInputCaret:
                    // Task 47 — the caret is AQUA (affirmative/"act here", the GO-key language):
                    // gold stays reserved for hints, per the standing palette rule.
                    fill = C_SURFACE_2;
                    border = C_ACCENT;
                    hasBorder = true;
                    textC = C_ACCENT;
                    // Task 47 — the render loop re-applies states every keystroke: stop the prior
                    // pulse before starting a fresh one so coroutines never stack on the tile.
                    if (caretCoroutine != null) StopCoroutine(caretCoroutine);
                    if (isActiveAndEnabled)
                        caretCoroutine = StartCoroutine(CaretPulse());
                    break;

                case TileState.CorrectInChain:
                    // Palette-aware: blue in deuteranopia, green in default.
                    fill = AccessiblePalette.Correct;
                    hasBorder = false;
                    textC = C_TEXT;
                    // Non-color cue: a ✓ glyph is renderable since Task 42 (symbols fallback) but
                    // re-enabling it is a deliberate visual call — the fill stays the cue for now.
                    glyph = string.Empty;
                    glyphC = new Color(1f, 1f, 1f, 0.85f);
                    break;

                case TileState.InvalidFlash:
                    // Palette-aware: orange in deuteranopia, red in default.
                    fill = AccessiblePalette.Error;
                    hasBorder = false;
                    textC = C_TEXT;
                    // Non-color cue: an ✗ glyph is renderable since Task 42 (symbols fallback) but
                    // re-enabling it is a deliberate visual call — flash + shake stay the cue for now.
                    glyph = string.Empty;
                    glyphC = new Color(1f, 1f, 1f, 0.85f);
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
                if (hasBorder)
                {
                    // Task 47 — state borders render as the same rounded RING SetOutline uses. A
                    // null sprite painted the FULL border quad, which made empty input tiles read
                    // as solid muted slabs (the "lavender blocks") instead of edged tiles.
                    borderImage.sprite = TryGetRoundedRectOutlineSprite();
                    borderImage.type = Image.Type.Sliced;
                    borderImage.color = border;
                }
                else
                {
                    borderImage.color = new Color(0f, 0f, 0f, 0f);
                }
            }
            // Apply non-color glyph cue.
            if (stateGlyphText != null)
            {
                stateGlyphText.text = glyph;
                stateGlyphText.color = glyphC;
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
                // Task 47 — the caret state is reachable at rest now: under ReduceMotion the ring
                // holds a STATIC aqua (the affordance survives; the breathing doesn't).
                if (UIAnimations.ReduceMotion)
                {
                    borderImage.color = C_ACCENT;
                    yield return null;
                    continue;
                }
                // A pure alpha breathe on the aqua ring (gold is reserved for hints).
                float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * hz) + 1f) * 0.5f;
                Color c = C_ACCENT;
                c.a = Mathf.Lerp(0.4f, 1.0f, t);
                borderImage.color = c;
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

        // Task 27 / Task 29A — a rounded-rect RING (transparent centre) for see-through outline tiles.
        // Rendered at 96px / outer-radius 18 / 14px stroke with pixelsPerUnit 200 and an 18px 9-slice border,
        // so the displayed corner radius (18 * 100/200 = 9) stays tile-appropriate while the displayed stroke
        // is ~7px — a CONFIDENT, bold outline (Task 29A roughly doubled it from the old ~4px hairline) that reads
        // like the menu's button borders. 9-slicing keeps the ring crisp and a constant width at any tile size.
        private static Sprite cachedOutlineSprite;
        private static Sprite TryGetRoundedRectOutlineSprite()
        {
            if (cachedOutlineSprite != null) return cachedOutlineSprite;

            const int size = 96;
            const int outerR = 18;
            const int stroke = 14;
            const int inset = stroke;            // inner rect inset on every side
            const int innerR = outerR - stroke;  // inner corner radius (4)
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inOuter = InsideRoundedSquare(x, y, 0, size - 1, outerR);
                    bool inInner = InsideRoundedSquare(x, y, inset, size - 1 - inset, innerR);
                    px[y * size + x] = (inOuter && !inInner) ? Color.white : clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            cachedOutlineSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                200f, 0, SpriteMeshType.FullRect, new Vector4(outerR, outerR, outerR, outerR));
            return cachedOutlineSprite;
        }

        // True if (x,y) is inside the rounded square spanning [lo,hi] on both axes with corner radius r.
        private static bool InsideRoundedSquare(int x, int y, int lo, int hi, int r)
        {
            if (x < lo || x > hi || y < lo || y > hi) return false;
            int cxL = lo + r, cxR = hi - r, cyB = lo + r, cyT = hi - r;
            if (x < cxL && y < cyB) return (cxL - x) * (cxL - x) + (cyB - y) * (cyB - y) <= r * r;
            if (x > cxR && y < cyB) return (x - cxR) * (x - cxR) + (cyB - y) * (cyB - y) <= r * r;
            if (x < cxL && y > cyT) return (cxL - x) * (cxL - x) + (y - cyT) * (y - cyT) <= r * r;
            if (x > cxR && y > cyT) return (x - cxR) * (x - cxR) + (y - cyT) * (y - cyT) <= r * r;
            return true;
        }
    }
}
