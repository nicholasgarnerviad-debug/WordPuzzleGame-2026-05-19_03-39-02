using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LetterTile : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI letterText;

    private char letter;
    public event Action<char> OnLetterPressed;

    public void Initialize(char letter)
    {
        this.letter = letter;
        letterText.text = letter.ToString().ToUpper();
        button.onClick.AddListener(() => PressLetter());
    }

    private void PressLetter()
    {
        OnLetterPressed?.Invoke(letter);
    }

    public void SetEnabled(bool enabled)
    {
        button.interactable = enabled;
    }
}
