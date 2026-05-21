using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WordPuzzleGame.UI.Animations;

public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private Button classicModeButton;
    [SerializeField] private Button puzzleShowButton;
    [SerializeField] private Button timeAttackButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button settingsButton;

    private ModeController modeController;
    private UIManager uiManager;

    public void InjectDependencies(ModeController mc, UIManager ui)
    {
        modeController = mc;
        uiManager = ui;
        UpdateButtonListeners();
    }

    private void UpdateButtonListeners()
    {
        // Unsubscribe from previous listeners
        if (classicModeButton != null) classicModeButton.onClick.RemoveAllListeners();
        if (puzzleShowButton != null) puzzleShowButton.onClick.RemoveAllListeners();
        if (timeAttackButton != null) timeAttackButton.onClick.RemoveAllListeners();
        if (shopButton != null) shopButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();

        // Subscribe to mode buttons
        if (classicModeButton != null)
            classicModeButton.onClick.AddListener(() => StartMode(ModeType.Classic));
        else
            Debug.LogWarning("[MainMenu] Classic mode button not assigned");

        if (puzzleShowButton != null)
            puzzleShowButton.onClick.AddListener(() => StartMode(ModeType.PuzzleShow));
        else
            Debug.LogWarning("[MainMenu] Puzzle show button not assigned");

        if (timeAttackButton != null)
            timeAttackButton.onClick.AddListener(() => StartMode(ModeType.TimeAttack));
        else
            Debug.LogWarning("[MainMenu] Time attack button not assigned");

        // Subscribe to utility buttons
        if (shopButton != null)
            shopButton.onClick.AddListener(() => Debug.Log("Shop coming soon"));

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => Debug.Log("Settings coming soon"));
    }

    private void StartMode(ModeType modeType)
    {
        if (modeController == null)
        {
            Debug.LogWarning("[MainMenuScreen] ModeController not injected");
            return;
        }

        Debug.Log($"[MainMenu] Starting {modeType}");

        // Play tap animation on clicked button
        Button clickedButton = GetButtonForMode(modeType);
        if (clickedButton != null)
        {
            RectTransform rectTransform = clickedButton.GetComponent<RectTransform>();
            if (rectTransform != null)
                StartCoroutine(UIAnimations.ScaleButtonTap(rectTransform));
        }

        // Switch mode
        modeController.SwitchMode(modeType);

        // Animate out and show gameplay
        StartCoroutine(TransitionToGameplay());
    }

    private Button GetButtonForMode(ModeType modeType)
    {
        return modeType switch
        {
            ModeType.Classic => classicModeButton,
            ModeType.PuzzleShow => puzzleShowButton,
            ModeType.TimeAttack => timeAttackButton,
            _ => null
        };
    }

    private IEnumerator TransitionToGameplay()
    {
        // Wait for button tap animation to complete
        yield return new WaitForSeconds(0.1f);

        // Get CanvasGroup for fade out
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            yield return StartCoroutine(UIAnimations.FadeTransition(canvasGroup, false, 0.3f));
            canvasGroup.alpha = 0;
        }

        // Show gameplay screen
        uiManager.ShowScreen<GameplayScreen>();

        // Reset for next menu visit
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
}
