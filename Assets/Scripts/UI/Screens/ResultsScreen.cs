using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using WordPuzzleGame.UI.Animations;

public class ResultsScreen : MonoBehaviour
{
    [Header("Display Components")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalScoreStatText;
    [SerializeField] private TextMeshProUGUI durationStatText;
    [SerializeField] private TextMeshProUGUI wordsFoundStatText;
    [SerializeField] private TextMeshProUGUI accuracyStatText;
    [SerializeField] private TextMeshProUGUI bestWordStatText;
    [SerializeField] private TextMeshProUGUI currentStreakStatText;
    [SerializeField] private TextMeshProUGUI longestStreakStatText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private ModeController modeController;
    private UIManager uiManager;
    private ModeStats currentStats;

    public void InjectDependencies(ModeController mc, UIManager ui)
    {
        modeController = mc;
        uiManager      = ui;
    }

    private void OnEnable()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDisable()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    public void ShowResults(ModeStats stats)
    {
        currentStats = stats;
        DisplayResults();
    }

    private void DisplayResults()
    {
        // Header
        if (headerText != null)
            headerText.text = "Game Complete!";

        // Final score
        if (finalScoreText != null)
            finalScoreText.text = currentStats.coinsEarned.ToString();

        // Display all stats
        if (finalScoreStatText != null)
            finalScoreStatText.text = $"Final Score: {currentStats.coinsEarned} pts";

        if (durationStatText != null)
            durationStatText.text = $"Duration: {FormatTime(currentStats.totalTime)}";

        if (wordsFoundStatText != null)
            wordsFoundStatText.text = $"Words Found: {currentStats.puzzlesCompleted}";

        if (accuracyStatText != null)
            accuracyStatText.text = $"Accuracy: {CalculateAccuracy():F1}%";

        if (bestWordStatText != null)
            bestWordStatText.text = $"Best Word: {currentStats.modeName}";

        if (currentStreakStatText != null)
            currentStreakStatText.text = $"Current Streak: N/A";

        if (longestStreakStatText != null)
            longestStreakStatText.text = $"Longest Streak: N/A";

        // Fade in animation
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(UIAnimations.FadeTransition(canvasGroup, fadeIn: true, duration: 0.5f));
        }
    }

    private float CalculateAccuracy()
    {
        // Accuracy calculation is based on available stats
        // For now, returns 100% as placeholder since ModeStats doesn't have attempt tracking
        return 100f;
    }

    private string FormatTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{mins}:{secs:D2}";
    }

    private void OnPlayAgainClicked()
    {
        if (canvasGroup != null)
            StartCoroutine(UIAnimations.FadeTransition(canvasGroup, fadeIn: false, duration: 0.3f));

        StartCoroutine(PlayAgainAfterDelay());
    }

    private IEnumerator PlayAgainAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);

        // Start new game in same mode
        if (modeController != null)
            modeController.SwitchMode(modeController.LastMode);

        if (uiManager != null)
            uiManager.ShowScreen<GameplayScreen>();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void OnMainMenuClicked()
    {
        if (canvasGroup != null)
            StartCoroutine(UIAnimations.FadeTransition(canvasGroup, fadeIn: false, duration: 0.3f));

        StartCoroutine(MainMenuAfterDelay());
    }

    private IEnumerator MainMenuAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);

        if (uiManager != null)
            uiManager.ShowScreen<MainMenuScreen>();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
