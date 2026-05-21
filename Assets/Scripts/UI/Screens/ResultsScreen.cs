using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalScoreStatText;
    [SerializeField] private TMP_Text durationStatText;
    [SerializeField] private TMP_Text wordsFoundStatText;
    [SerializeField] private TMP_Text accuracyStatText;
    [SerializeField] private TMP_Text bestWordStatText;
    [SerializeField] private TMP_Text currentStreakStatText;
    [SerializeField] private TMP_Text longestStreakStatText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private ModeController modeController;
    private UIManager uiManager;

    public void InjectDependencies(ModeController mc, UIManager ui)
    {
        modeController = mc;
        uiManager      = ui;
    }

    private void Start()
    {
        playAgainButton?.onClick.AddListener(PlayNextPuzzle);
        mainMenuButton?.onClick.AddListener(ReturnToMenu);
    }

    public void ShowResults(ModeStats stats)
    {
        if (headerText != null) headerText.text = "Game Complete!";
        if (finalScoreText != null) finalScoreText.text = $"{stats.score}";

        if (finalScoreStatText != null) finalScoreStatText.text = $"Final Score: {stats.score} pts";
        if (durationStatText != null) durationStatText.text = $"Duration: {stats.totalTime}s";
        if (wordsFoundStatText != null) wordsFoundStatText.text = $"Words Found: {stats.puzzlesCompleted}";
        if (accuracyStatText != null) accuracyStatText.text = $"Accuracy: {stats.accuracy}%";
        if (bestWordStatText != null) bestWordStatText.text = $"Best Word: {stats.bestWord}";
        if (currentStreakStatText != null) currentStreakStatText.text = $"Current Streak: {stats.currentStreak}";
        if (longestStreakStatText != null) longestStreakStatText.text = $"Longest Streak: {stats.longestStreak}";
    }

    private void PlayNextPuzzle()
    {
        if (modeController == null) return;
        modeController.SwitchMode(modeController.LastMode);
        uiManager.ShowScreen<GameplayScreen>();
    }

    private void ReturnToMenu()
    {
        uiManager.ShowScreen<MainMenuScreen>();
    }
}
