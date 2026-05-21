using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text modeNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsEarnedText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button menuButton;

    private ModeController modeController;
    private UIManager uiManager;

    public void InjectDependencies(ModeController mc, UIManager ui)
    {
        modeController = mc;
        uiManager      = ui;
    }

    private void Start()
    {
        nextButton?.onClick.AddListener(PlayNextPuzzle);
        menuButton?.onClick.AddListener(ReturnToMenu);
    }

    public void ShowResults(ModeStats stats)
    {
        if (modeNameText    != null) modeNameText.text    = stats.modeName;
        if (scoreText       != null) scoreText.text       = $"Puzzles: {stats.puzzlesCompleted}";
        if (coinsEarnedText != null) coinsEarnedText.text = $"Coins: +{stats.coinsEarned}";
        if (timeText        != null) timeText.text        = $"Time: {stats.totalTime}s";
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
