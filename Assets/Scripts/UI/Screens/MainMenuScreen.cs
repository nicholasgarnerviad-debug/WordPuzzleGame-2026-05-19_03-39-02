using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private Button classicModeButton;
        [SerializeField] private Button puzzleShowButton;
        [SerializeField] private Button timeAttackButton;
        [SerializeField] private Button libraryButton;
        [SerializeField] private Button settingsButton;

        // Task 1C — Daily puzzle entry point.
        [SerializeField] private Button dailyButton;
        [SerializeField] private TextMeshProUGUI dailyButtonLabel;

        // Task 9F — Statistics screen.
        [SerializeField] private Button statsButton;

        // Task 9G — Resume in-progress puzzle.
        [SerializeField] private Button resumeButton;
        [SerializeField] private TextMeshProUGUI resumeButtonLabel;

        public event Action OnClassicModeSelected;
        public event Action OnPuzzleShowSelected;
        public event Action OnTimeAttackSelected;
        public event Action OnLibrarySelected;
        public event Action OnSettingsSelected;
        public event Action OnDailySelected;
        public event Action OnStatsSelected;
        public event Action OnResumeSelected;

        private void OnEnable()
        {
            if (classicModeButton != null)
                classicModeButton.onClick.AddListener(() => OnClassicModeSelected?.Invoke());
            if (puzzleShowButton != null)
                puzzleShowButton.onClick.AddListener(() => OnPuzzleShowSelected?.Invoke());
            if (timeAttackButton != null)
                timeAttackButton.onClick.AddListener(() => OnTimeAttackSelected?.Invoke());
            if (libraryButton != null)
                libraryButton.onClick.AddListener(() => OnLibrarySelected?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnSettingsSelected?.Invoke());
            if (dailyButton != null)
                dailyButton.onClick.AddListener(() => OnDailySelected?.Invoke());
            if (statsButton != null)
                statsButton.onClick.AddListener(() => OnStatsSelected?.Invoke());
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => OnResumeSelected?.Invoke());
        }

        private void OnDisable()
        {
            if (classicModeButton != null)
                classicModeButton.onClick.RemoveAllListeners();
            if (puzzleShowButton != null)
                puzzleShowButton.onClick.RemoveAllListeners();
            if (timeAttackButton != null)
                timeAttackButton.onClick.RemoveAllListeners();
            if (libraryButton != null)
                libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null)
                settingsButton.onClick.RemoveAllListeners();
            if (dailyButton != null)
                dailyButton.onClick.RemoveAllListeners();
            if (statsButton != null)
                statsButton.onClick.RemoveAllListeners();
            if (resumeButton != null)
                resumeButton.onClick.RemoveAllListeners();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void SelectClassicMode() => OnClassicModeSelected?.Invoke();
        public void SelectPuzzleShowMode() => OnPuzzleShowSelected?.Invoke();
        public void SelectTimeAttackMode() => OnTimeAttackSelected?.Invoke();
        public void SelectSettings() => OnSettingsSelected?.Invoke();
        public void SelectDaily() => OnDailySelected?.Invoke();

        /// <summary>
        /// Refresh the DAILY button label to reflect today's completion state.
        /// Called by GameBootstrap on app start and whenever returning to MainMenu.
        /// </summary>
        public void SetDailyState(bool completedToday, int currentStreak)
        {
            if (dailyButtonLabel == null) return;
            dailyButtonLabel.text = completedToday
                ? $"DAILY  ✓  {currentStreak}"   // check + streak count
                : "DAILY";
        }

        /// <summary>
        /// Task 9G — show or hide the Resume button.
        /// Pass a non-empty description (e.g. start→end) to display on the button label.
        /// </summary>
        public void SetResumeVisible(bool visible, string description = "")
        {
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(visible);
            if (resumeButtonLabel != null && !string.IsNullOrEmpty(description))
                resumeButtonLabel.text = $"RESUME  {description}";
        }
    }
}
