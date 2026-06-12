using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.UI; // KeyboardPalette + UIAnimations live in WordPuzzle.UI (UIThemeManager is global)

namespace WordPuzzle.UI.Components
{
    public class OnScreenKeyboard : MonoBehaviour
    {
        [SerializeField] private Transform keyboardRoot;
        [SerializeField] private TextMeshProUGUI currentInputDisplay;

        public event Action<char> OnLetterPressed;
        public event Action OnBackspacePressed;
        public event Action OnEnterPressed;

        private static readonly string[] Rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        // Key sizing for 1080px-wide canvas: 10 keys + 9 gaps = 10*88+9*6 = 934px (fits 1080)
        private const float KeyWidth = 88f;
        private const float KeyHeight = 82f;
        private const float KeySpacing = 8f;   // slightly more gap so keys read as dark panel
        private const float RowSpacing = 10f;
        // Task 29B — round the keys with the app's shared bubbly 9-slice. A higher ppu multiplier shrinks
        // the baked 44px corner down to a tidy key-sized radius (the wide menu buttons use multiplier 1).
        private const float KeyCornerPpuMultiplier = 2.5f;

        // Key palette — centralized in UITheme.KeyboardPalette (no inline hex here). KeyFill is the deep
        // indigo-purple (Classic polish pass); DEL red, GO green. Letters are TypeScale Label (Task 42).
        private static readonly Color C_KEY_FILL    = KeyboardPalette.KeyFill;
        private static readonly Color C_DEL_FILL    = KeyboardPalette.DelFill;
        private static readonly Color C_GO_FILL     = KeyboardPalette.GoFill;

        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
        private bool _built;

        private void Awake()
        {
            if (_built) return;
            _built = true;

            // Pin our own RectTransform: bottom-stretch anchor, pivot at (0.5,0),
            // so anchoredPosition.y = padding above the canvas bottom edge.
            // This ensures the keyboard is never clipped below the screen.
            var selfRT = GetComponent<RectTransform>();
            if (selfRT != null)
            {
                selfRT.anchorMin        = new Vector2(0f, 0f);
                selfRT.anchorMax        = new Vector2(1f, 0f);
                selfRT.pivot            = new Vector2(0.5f, 0f);
                selfRT.sizeDelta        = new Vector2(0f, 320f);  // slightly taller to breathe
                selfRT.anchoredPosition = new Vector2(0f, 0f);
            }

            // Task 32 — the keyboard panel is TRANSPARENT so the static space background fills the whole
            // lower screen; the rounded keys (solid deep-indigo Panel + bright letters) float on top and stay legible.
            // The Image is kept (not removed) but raycastTarget stays false — it's now an invisible layer,
            // never a tap surface (each key carries its own raycast target), so taps are unaffected.
            var bgImg = GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = gameObject.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0f); // fully transparent — no grey backing rectangle
            bgImg.raycastTarget = false;

            BuildKeyboard();
        }

        private void BuildKeyboard()
        {
            Transform root = keyboardRoot != null ? keyboardRoot : transform;

            // With pivot=(0.5,0), y=0 is the bottom of our rect. Stack rows upward.
            // Row 0 (QWERTY) at top, row 2 (ZXCVBNM) at bottom — so row 0 has the highest y.
            float totalHeight = Rows.Length * (KeyHeight + RowSpacing);
            // yStart: top of first row (inside rect, measured from bottom = 0)
            float yStart = totalHeight - KeyHeight / 2f;

            for (int rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
            {
                string row = Rows[rowIndex];
                bool isLastRow = rowIndex == Rows.Length - 1;
                float y = yStart - rowIndex * (KeyHeight + RowSpacing);
                BuildRow(root, row, y, isLastRow);
            }
        }

        private void BuildRow(Transform root, string letters, float y, bool addSpecialKeys)
        {
            int totalKeys = letters.Length + (addSpecialKeys ? 2 : 0);
            float totalWidth = totalKeys * (KeyWidth + KeySpacing) - KeySpacing;
            float xStart = -totalWidth / 2f + KeyWidth / 2f;

            int keyIndex = 0;

            if (addSpecialKeys)
            {
                CreateSpecialButton(root, "DEL", xStart + keyIndex * (KeyWidth + KeySpacing), y,
                    C_DEL_FILL, 120f, () => OnBackspacePressed?.Invoke());
                keyIndex++;
            }

            for (int i = 0; i < letters.Length; i++)
            {
                char c = letters[i];
                float x = xStart + keyIndex * (KeyWidth + KeySpacing);
                CreateLetterButton(root, c, x, y);
                keyIndex++;
            }

            if (addSpecialKeys)
            {
                CreateSpecialButton(root, "GO", xStart + keyIndex * (KeyWidth + KeySpacing), y,
                    C_GO_FILL, 120f, () => OnEnterPressed?.Invoke());
            }
        }

        private void CreateLetterButton(Transform root, char letter, float x, float y)
        {
            GameObject obj = CreateButtonObject(root, letter.ToString(), x, y, KeyWidth, KeyHeight,
                C_KEY_FILL);
            Button btn = obj.GetComponent<Button>();
            char captured = letter;
            btn.onClick.AddListener(() => OnLetterPressed?.Invoke(captured));
            _letterButtons[char.ToUpper(captured)] = btn;
        }

        private void CreateSpecialButton(Transform root, string label, float x, float y,
            Color bgColor, float width, Action action)
        {
            GameObject obj = CreateButtonObject(root, label, x, y, width, KeyHeight,
                bgColor);
            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => action?.Invoke());
        }

        private GameObject CreateButtonObject(Transform root, string label, float x, float y,
            float width, float height, Color bgColor)
        {
            var obj = new GameObject(label + "Key", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(root, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            var image = obj.GetComponent<Image>();
            image.color = bgColor;
            image.raycastTarget = true; // explicit: this Image is the hit-target for the Button
            // Task 29B — rounded key corners on-brand with the rest of the app. Tints with bgColor, so
            // DEL (red) / GO (green) keep their fills; raycast unchanged so taps still register everywhere.
            UIThemeManager.ApplyRoundedButton(image, KeyCornerPpuMultiplier);

            var btn = obj.GetComponent<Button>();
            btn.targetGraphic = image; // wire targetGraphic so Button.interactable state transitions are reliable

            // Classic polish (W5) — subtle press squish on every key (letters + DEL + GO). Routed through the
            // shared UIAnimations.ScaleButtonTap: ReduceMotion-gated (instant when motion is off) + a short
            // coroutine (no per-frame GC). Manual key layout (no LayoutGroup), so animating localScale is safe.
            // 0.88 trough so the key press is clearly FELT (the default 0.95 read as "nothing happening").
            btn.onClick.AddListener(() => { if (isActiveAndEnabled) StartCoroutine(UIAnimations.ScaleButtonTap(rt, 0.20f, 0.88f)); });

            var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            TypeScale.Apply(tmp, TypeRole.Label); // Task 42 — Rungo SemiBold 38, TextPrimary (weight from the asset, not the Bold flag)
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // prevent TMP label from intercepting pointer events meant for the parent Button

            return obj;
        }

        public void SetCurrentInput(string input)
        {
            if (currentInputDisplay != null)
                currentInputDisplay.text = input;
        }

        public void HighlightLetter(char c)
        {
            char upper = char.ToUpper(c);
            if (_letterButtons.TryGetValue(upper, out Button btn))
                StartCoroutine(FlashButton(btn));
        }

        private IEnumerator FlashButton(Button btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = KeyboardPalette.KeyFlash; // gold highlight pulse (centralized token)
            yield return new WaitForSeconds(0.3f);
            image.color = C_KEY_FILL; // restore key fill (deep indigo, KeyboardPalette.KeyFill)
        }
    }
}
