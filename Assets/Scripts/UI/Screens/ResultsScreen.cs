using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
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

        private UnityAction playAgainAction;
        private UnityAction mainMenuAction;

        private void OnEnable()
        {
            if (playAgainButton != null)
            {
                playAgainAction = new UnityAction(() => OnPlayAgain?.Invoke());
                playAgainButton.onClick.AddListener(playAgainAction);
            }

            if (mainMenuButton != null)
            {
                mainMenuAction = new UnityAction(() => OnMainMenu?.Invoke());
                mainMenuButton.onClick.AddListener(mainMenuAction);
            }
        }

        private void OnDisable()
        {
            if (playAgainButton != null && playAgainAction != null)
                playAgainButton.onClick.RemoveListener(playAgainAction);

            if (mainMenuButton != null && mainMenuAction != null)
                mainMenuButton.onClick.RemoveListener(mainMenuAction);
        }

        public void DisplayStats(GameModeStats stats)
        {
            if (modeNameText != null)
                modeNameText.text = $"{stats.modeName} Mode Results";

            if (wordsFoundText != null)
                wordsFoundText.text = $"Words Found: {stats.wordsFound}";

            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {stats.accuracy:F1}%";

            if (timeText != null)
                timeText.text = $"Time: {stats.totalTime:F1}s";

            if (scoreText != null)
                scoreText.text = $"Score: {stats.score}";
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
