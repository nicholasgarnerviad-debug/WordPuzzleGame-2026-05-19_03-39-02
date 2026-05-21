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
    [SerializeField] private Transform statsContent;
    [SerializeField] private TextMeshProUGUI statItemPrefab;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private ModeController modeController;
    private UIManager uiManager;
    private GameStats currentStats;

    [System.Serializable]
    public struct GameStats
    {
        public int finalScore;
        public float gameDuration;
        public int wordsFound;
        public int validAttempts;
        public int totalAttempts;
        public string bestWord;
        public int currentStreak;
        public int longestStreak;
    }

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

    public void ShowResults(GameStats stats)
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
            finalScoreText.text = currentStats.finalScore.ToString();

        // Clear existing stats
        if (statsContent != null)
        {
            foreach (Transform child in statsContent)
                Destroy(child.gameObject);
        }

        // Display all 7 stats in order
        AddStatItem("Final Score", $"{currentStats.finalScore} pts");
        AddStatItem("Duration", FormatTime(currentStats.gameDuration));
        AddStatItem("Words Found", currentStats.wordsFound.ToString());
        AddStatItem("Accuracy", $"{CalculateAccuracy():F0}%");
        AddStatItem("Best Word", currentStats.bestWord);
        AddStatItem("Current Streak", currentStats.currentStreak.ToString());
        AddStatItem("Longest Streak", currentStats.longestStreak.ToString());

        // Fade in animation
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(UIAnimations.FadeTransition(canvasGroup, fadeIn: true, duration: 0.5f));
        }
    }

    private void AddStatItem(string label, string value)
    {
        if (statItemPrefab == null || statsContent == null)
        {
            Debug.LogWarning("Missing stat item prefab or stats content transform");
            return;
        }

        TextMeshProUGUI item = Instantiate(statItemPrefab, statsContent);
        item.text = $"{label}: {value}";
    }

    private float CalculateAccuracy()
    {
        if (currentStats.totalAttempts == 0)
            return 0f;
        return (currentStats.validAttempts / (float)currentStats.totalAttempts) * 100f;
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
            modeController.SwitchMode(modeController.GetCurrentMode());

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
