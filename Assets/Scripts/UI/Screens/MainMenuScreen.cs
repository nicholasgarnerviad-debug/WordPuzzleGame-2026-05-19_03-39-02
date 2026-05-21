using UnityEngine;
using UnityEngine.UI;

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
        uiManager      = ui;
    }

    private void Start()
    {
        classicModeButton?.onClick.AddListener(() => StartMode(ModeType.Classic));
        puzzleShowButton?.onClick.AddListener(() => StartMode(ModeType.PuzzleShow));
        timeAttackButton?.onClick.AddListener(() => StartMode(ModeType.TimeAttack));
        shopButton?.onClick.AddListener(() => Logger.Log("Shop coming soon"));
        settingsButton?.onClick.AddListener(() => Logger.Log("Settings coming soon"));
    }

    private void StartMode(ModeType modeType)
    {
        if (modeController == null)
        {
            Logger.LogWarning("[MainMenuScreen] ModeController not injected");
            return;
        }
        modeController.SwitchMode(modeType);
        uiManager.ShowScreen<GameplayScreen>();
    }
}
