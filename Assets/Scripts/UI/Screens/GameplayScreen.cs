using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WordPuzzle.UI
{
    public class GameplayScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI puzzleDisplayText;
        [SerializeField] private TextMeshProUGUI wordChainText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TMP_InputField wordInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI feedbackText;

        public event Action<string> OnWordSubmitted;

        private void OnEnable()
        {
            submitButton.onClick.AddListener(SubmitWord);
            wordInputField.onSubmit.AddListener(OnInputSubmit);
        }

        private void OnDisable()
        {
            submitButton.onClick.RemoveAllListeners();
            wordInputField.onSubmit.RemoveAllListeners();
        }

        public void SetPuzzleDisplay(string startWord, string endWord)
        {
            puzzleDisplayText.text = $"{startWord} → {endWord}";
        }

        public void SetWordChain(string[] words)
        {
            wordChainText.text = string.Join(" → ", words);
        }

        public void SetScore(int score)
        {
            scoreText.text = $"Score: {score}";
        }

        public void SetTimer(float timeRemaining)
        {
            timerText.text = $"Time: {Mathf.Max(0, timeRemaining):F1}s";
        }

        public void ShowFeedback(string message, Color color)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }

        public void ClearInput()
        {
            wordInputField.text = "";
            wordInputField.ActivateInputField();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void SubmitWord()
        {
            if (!string.IsNullOrWhiteSpace(wordInputField.text))
            {
                OnWordSubmitted?.Invoke(wordInputField.text.ToLower());
                ClearInput();
            }
        }

        private void OnInputSubmit(string value)
        {
            SubmitWord();
        }
    }
}
