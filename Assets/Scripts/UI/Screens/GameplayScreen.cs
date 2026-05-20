using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameplayScreen : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text wordsText;
    [SerializeField] private TMP_InputField wordInput;

    private void Start()
    {
        gameController = GetComponent<GameController>();

        submitButton.onClick.AddListener(() => SubmitWord());
        hintButton.onClick.AddListener(() => ShowHint());
        undoButton.onClick.AddListener(() => UndoWord());
    }

    private void SubmitWord()
    {
        string word = wordInput.text.ToLower().Trim();
        if (string.IsNullOrEmpty(word)) return;

        bool valid = gameController.SubmitWord(word);
        if (valid)
        {
            wordInput.text = "";
            UpdateDisplay();
        }
        else
        {
            Logger.LogWarning($"Invalid word: {word}");
        }
    }

    private void ShowHint()
    {
        Logger.Log("Hint requested");
    }

    private void UndoWord()
    {
        Logger.Log("Undo requested");
    }

    private void UpdateDisplay()
    {
        scoreText.text = $"Score: {gameController.GetCurrentScore()}";

        List<string> words = gameController.GetCurrentUserWords();
        wordsText.text = "Words: " + string.Join(", ", words);
    }
}
