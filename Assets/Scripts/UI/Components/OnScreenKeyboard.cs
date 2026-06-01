using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        private const float KeySpacing = 6f;
        private const float RowSpacing = 10f;

        private readonly Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
        private bool _built;

        private void Awake()
        {
            if (_built) return;
            _built = true;
            BuildKeyboard();
        }

        private void BuildKeyboard()
        {
            Transform root = keyboardRoot != null ? keyboardRoot : transform;

            float totalHeight = Rows.Length * (KeyHeight + RowSpacing);
            float yStart = totalHeight / 2f - KeyHeight / 2f;

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
                    new Color(0.85f, 0.2f, 0.2f), 120f, () => OnBackspacePressed?.Invoke());
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
                    new Color(0.2f, 0.75f, 0.3f), 120f, () => OnEnterPressed?.Invoke());
            }
        }

        private void CreateLetterButton(Transform root, char letter, float x, float y)
        {
            GameObject obj = CreateButtonObject(root, letter.ToString(), x, y, KeyWidth, KeyHeight,
                Color.white, Color.black, 32f);
            Button btn = obj.GetComponent<Button>();
            char captured = letter;
            btn.onClick.AddListener(() => OnLetterPressed?.Invoke(captured));
            _letterButtons[char.ToUpper(captured)] = btn;
        }

        private void CreateSpecialButton(Transform root, string label, float x, float y,
            Color bgColor, float width, Action action)
        {
            GameObject obj = CreateButtonObject(root, label, x, y, width, KeyHeight,
                bgColor, Color.white, 26f);
            Button btn = obj.GetComponent<Button>();
            btn.onClick.AddListener(() => action?.Invoke());
        }

        private GameObject CreateButtonObject(Transform root, string label, float x, float y,
            float width, float height, Color bgColor, Color textColor, float fontSize)
        {
            var obj = new GameObject(label + "Key", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(root, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);

            var image = obj.GetComponent<Image>();
            image.color = bgColor;

            var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.color = textColor;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

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
            Color original = image.color;
            image.color = new Color(1f, 0.9f, 0.2f);
            yield return new WaitForSeconds(0.3f);
            image.color = original;
        }
    }
}
