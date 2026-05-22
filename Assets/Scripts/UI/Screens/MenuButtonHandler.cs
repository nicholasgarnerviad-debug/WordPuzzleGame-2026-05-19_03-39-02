using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class MenuButtonHandler : MonoBehaviour
    {
        private Button classicButton;
        private Button puzzleShowButton;
        private Button timeAttackButton;
        private MainMenuScreen mainMenuScreen;

        private void Start()
        {
            mainMenuScreen = GetComponentInParent<MainMenuScreen>();
            if (mainMenuScreen == null)
            {
                Debug.LogError("MainMenuScreen not found in parent");
                return;
            }

            // Find buttons in children
            var buttons = GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                if (btn.name == "ClassicModeButton")
                    classicButton = btn;
                else if (btn.name == "PuzzleShowButton")
                    puzzleShowButton = btn;
                else if (btn.name == "TimeAttackButton")
                    timeAttackButton = btn;
            }

            // Wire click listeners
            if (classicButton != null)
                classicButton.onClick.AddListener(() => mainMenuScreen.SelectClassicMode());
            if (puzzleShowButton != null)
                puzzleShowButton.onClick.AddListener(() => mainMenuScreen.SelectPuzzleShowMode());
            if (timeAttackButton != null)
                timeAttackButton.onClick.AddListener(() => mainMenuScreen.SelectTimeAttackMode());
        }
    }
}
