using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.UI.Components;

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
        [SerializeField] private Button backButton;

        // Phase 2: Power-up UI components
        [SerializeField] private Button hintButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private TextMeshProUGUI hintCountText;
        [SerializeField] private TextMeshProUGUI revealCountText;

        // Phase 3: Tier indicator for Puzzle Show mode
        [SerializeField] private TextMeshProUGUI tierIndicatorText;

        // On-screen keyboard
        [SerializeField] private OnScreenKeyboard keyboard;
        [SerializeField] private TextMeshProUGUI currentInputText;

        private string currentInput = "";

        public event Action<string> OnWordSubmitted;
        public event Action OnBackToMenu;
        public event Action OnHintUsed;
        public event Action OnRevealUsed;
        public event Action OnUndoStep;

        private void OnEnable()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitWord);
            if (wordInputField != null)
                wordInputField.onSubmit.AddListener(OnInputSubmit);
            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());

            // Phase 2: Wire power-up buttons
            if (hintButton != null)
                hintButton.onClick.AddListener(() => OnHintUsed?.Invoke());
            if (revealButton != null)
                revealButton.onClick.AddListener(() => OnRevealUsed?.Invoke());
            if (undoButton != null)
                undoButton.onClick.AddListener(() => OnUndoStep?.Invoke());

            // On-screen keyboard
            if (keyboard != null)
            {
                keyboard.OnLetterPressed += HandleLetterPressed;
                keyboard.OnBackspacePressed += HandleBackspacePressed;
                keyboard.OnEnterPressed += HandleEnterPressed;
            }
        }

        private void OnDisable()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveAllListeners();
            if (wordInputField != null)
                wordInputField.onSubmit.RemoveAllListeners();
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();

            // Phase 2: Clean up power-up buttons
            if (hintButton != null)
                hintButton.onClick.RemoveAllListeners();
            if (revealButton != null)
                revealButton.onClick.RemoveAllListeners();
            if (undoButton != null)
                undoButton.onClick.RemoveAllListeners();

            // On-screen keyboard
            if (keyboard != null)
            {
                keyboard.OnLetterPressed -= HandleLetterPressed;
                keyboard.OnBackspacePressed -= HandleBackspacePressed;
                keyboard.OnEnterPressed -= HandleEnterPressed;
            }
        }

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
            if (currentInputText != null)
                currentInputText.text = currentInput.ToUpper();
            if (keyboard != null)
                keyboard.SetCurrentInput(currentInput.ToUpper());
        }

        public void ClearCurrentInput()
        {
            currentInput = "";
            UpdateCurrentInputDisplay();
        }

        public void SetPuzzleDisplay(string startWord, string endWord)
        {
            if (puzzleDisplayText != null)
                puzzleDisplayText.text = $"{startWord} → {endWord}";
        }

        public void SetWordChain(string[] words)
        {
            if (wordChainText != null)
                wordChainText.text = string.Join(" → ", words);
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        public void SetTimer(float timeRemaining)
        {
            if (timerText != null)
                timerText.text = $"Time: {Mathf.Max(0, timeRemaining):F1}s";
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

        // Phase 2: Power-up UI methods
        public void SetHintCount(int remaining)
        {
            if (hintCountText != null)
                hintCountText.text = $"Hints: {remaining}";
            if (hintButton != null)
                hintButton.interactable = (remaining > 0);
        }

        public void SetRevealCount(int remaining)
        {
            if (revealCountText != null)
                revealCountText.text = $"Reveal: {remaining}";
            if (revealButton != null)
                revealButton.interactable = (remaining > 0);
        }

        public void EnableUndoButton(bool enable)
        {
            if (undoButton != null)
                undoButton.interactable = enable;
        }

        public void SetTierIndicator(string text)
        {
            if (tierIndicatorText != null)
                tierIndicatorText.text = text ?? string.Empty;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

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
