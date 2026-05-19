using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private Button classicModeButton;
    [SerializeField] private Button puzzleShowButton;
    [SerializeField] private Button timeAttackButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button settingsButton;

    private void Start()
    {
        classicModeButton.onClick.AddListener(() => StartMode("Classic"));
        puzzleShowButton.onClick.AddListener(() => StartMode("PuzzleShow"));
        timeAttackButton.onClick.AddListener(() => StartMode("TimeAttack"));
        shopButton.onClick.AddListener(() => Logger.Log("Shop not yet implemented"));
        settingsButton.onClick.AddListener(() => Logger.Log("Settings not yet implemented"));
    }

    private void StartMode(string modeName)
    {
        Logger.Log($"Starting {modeName} mode");
    }
}
