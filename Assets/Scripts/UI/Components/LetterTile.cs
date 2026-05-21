using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LetterTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image background;

    private static readonly Color NormalColor = Color.white;
    private static readonly Color HintColor   = new Color(1f, 0.85f, 0.2f); // gold

    private char letter;
    public char Letter => letter;

    public event Action<char> OnLetterPressed;

    public void Initialize(char c)
    {
        letter = c;
        if (letterText  != null) letterText.text = c.ToString().ToUpper();
        if (background  != null) background.color = NormalColor;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnLetterPressed?.Invoke(letter));
    }

    public void SetEnabled(bool enabled)
    {
        button.interactable = enabled;
        if (background != null && enabled)
            background.color = NormalColor;
    }

    public void HighlightHint()
    {
        if (background != null) background.color = HintColor;
    }
}
