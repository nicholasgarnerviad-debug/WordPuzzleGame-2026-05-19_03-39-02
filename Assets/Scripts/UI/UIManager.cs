using System;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    /// <summary>
    /// Central UI manager. Coordinates screen transitions and event wiring.
    /// Singleton pattern for easy access from mode implementations.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private MainMenuScreen mainMenuScreen;
        [SerializeField] private GameplayScreen gameplayScreen;
        [SerializeField] private ResultsScreen resultsScreen;
        [SerializeField] private PuzzleLibraryScreen libraryScreen;
        [SerializeField] private SettingsScreen settingsScreen;
        [SerializeField] private TimeAttackSetupScreen timeAttackSetupScreen;
        // Task 9F — Statistics screen.
        [SerializeField] private StatsScreen statsScreen;

        // Global settings affordance — one shared gear shown top-right on every screen (opens Settings).
        // GameBootstrap subscribes to OnGlobalSettingsRequested and routes it to its populate-and-show.
        [SerializeField] private Sprite settingsIconSprite;
        public event Action OnGlobalSettingsRequested;
        private GameObject globalSettingsButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateGlobalSettingsButton();
        }

        public void ShowMainMenu()
        {
            mainMenuScreen.Show();
            SetGlobalSettingsVisible(true);   // gear is visible everywhere except the Settings screen
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Hide();
        }

        public void ShowGameplay()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Show();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Hide();
        }

        public void ShowResults()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Show();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Hide();
        }

        public void ShowLibrary()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Show();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Hide();
        }

        public void ShowSettings()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Show();
            SetGlobalSettingsVisible(false);  // don't show a settings gear on the Settings screen
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Hide();
        }

        // §5.4 — Time Attack setup screen routing.
        public void ShowTimeAttackSetup()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Show();
            if (statsScreen != null) statsScreen.Hide();
        }

        // Task 9F — Statistics screen routing.
        public void ShowStats()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
            if (settingsScreen != null) settingsScreen.Hide();
            if (timeAttackSetupScreen != null) timeAttackSetupScreen.Hide();
            if (statsScreen != null) statsScreen.Show();
        }

        // Creates the one shared settings gear shown in the top-right corner of every screen.
        private void CreateGlobalSettingsButton()
        {
            if (globalSettingsButton != null || mainMenuScreen == null) return;
            var canvas = mainMenuScreen.transform.parent as RectTransform;
            if (canvas == null) return;

            var go = new GameObject("GlobalSettingsButton", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);   // top-right corner
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -140f);      // inset; below the notch
            rt.sizeDelta = new Vector2(88f, 88f);                // ~HOME size

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);   // transparent — icon only, no grey box
            bg.raycastTarget = true;                // invisible but still taps the full rect (≥44px)
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => OnGlobalSettingsRequested?.Invoke());

            if (settingsIconSprite != null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform));
                iconGO.transform.SetParent(go.transform, false);
                var irt = iconGO.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(52f, 52f);
                var icon = iconGO.AddComponent<Image>();
                icon.sprite = settingsIconSprite;
                icon.color = new Color(0x8A / 255f, 0x93 / 255f, 0xA1 / 255f, 1f); // muted token #8A93A1
                icon.raycastTarget = false;
                icon.preserveAspect = true;
            }

            go.transform.SetAsLastSibling(); // render above the screens
            globalSettingsButton = go;
        }

        private void SetGlobalSettingsVisible(bool visible)
        {
            if (globalSettingsButton != null) globalSettingsButton.SetActive(visible);
        }

        // Screen accessors
        public MainMenuScreen GetMainMenu() => mainMenuScreen;
        public GameplayScreen GetGameplay() => gameplayScreen;
        public ResultsScreen GetResults() => resultsScreen;
        public PuzzleLibraryScreen GetLibrary() => libraryScreen;
        public SettingsScreen GetSettings() => settingsScreen;
        public TimeAttackSetupScreen GetTimeAttackSetup() => timeAttackSetupScreen;
        public StatsScreen GetStats() => statsScreen;

    }
}
