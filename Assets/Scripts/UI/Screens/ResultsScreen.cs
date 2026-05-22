using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Modes;

namespace WordPuzzle.UI
{
    public class ResultsScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI modeNameText;
        [SerializeField] private TextMeshProUGUI wordsFoundText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        public event Action OnPlayAgain;
        public event Action OnMainMenu;

        private void OnEnable()
        {
            playAgainButton.onClick.AddListener(() => OnPlayAgain?.Invoke());
            mainMenuButton.onClick.AddListener(() => OnMainMenu?.Invoke());
        }

        private void OnDisable()
        {
            playAgainButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.RemoveAllListeners();
        }

        public void DisplayStats(GameModeStats stats)
        {
            modeNameText.text = $"{stats.modeName} Mode Results";
            wordsFoundText.text = $"Words Found: {stats.wordsFound}";
            accuracyText.text = $"Accuracy: {stats.accuracy:F1}%";
            timeText.text = $"Time: {stats.totalTime:F1}s";
            scoreText.text = $"Score: {stats.score}";
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
