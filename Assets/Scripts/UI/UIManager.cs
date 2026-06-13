using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // Task 33 — a canvas-level coin balance pill (gold token + count). Tap opens the Shop.
        public event Action OnShopRequested;
        private GameObject coinPill;
        private TMP_Text coinPillText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateGlobalSettingsButton();
            CreateCoinPill();
        }

        public void ShowMainMenu()
        {
            mainMenuScreen.Show();
            SetCoinPillVisible(true);          // Task 33 — coin pill shown on the menu
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
            SetCoinPillVisible(false);
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
            SetCoinPillVisible(false);
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
            SetCoinPillVisible(false);
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
            SetCoinPillVisible(false);
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
            SetCoinPillVisible(false);
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
            SetCoinPillVisible(false);
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
                icon.color = Palette.TextMuted; // muted token
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

        // Task 33 — a canvas-level coin balance pill (gold token + count). Tap opens the Shop.
        private void CreateCoinPill()
        {
            if (coinPill != null || mainMenuScreen == null) return;
            var canvas = mainMenuScreen.transform.parent as RectTransform;
            if (canvas == null) return;

            var go = new GameObject("CoinPill", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // top-left
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -140f);       // mirrors the gear's inset, below the notch
            rt.sizeDelta = new Vector2(200f, 70f);

            var bg = go.GetComponent<Image>();
            UIThemeManager.ApplyRoundedButton(bg, 2.5f);
            bg.color = new Color(Palette.Surface.r, Palette.Surface.g, Palette.Surface.b, 0.72f); // subtle dark pill for legibility
            bg.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnShopRequested?.Invoke());

            var star = UIThemeManager.CreateStarToken(go.transform); // gold star — the currency mark
            var crt = star.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.anchoredPosition = new Vector2(14f, 0f);
            crt.sizeDelta = new Vector2(40f, 40f);

            var txtGO = new GameObject("Count", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(64f, 0f); trt.offsetMax = new Vector2(-12f, 0f);
            coinPillText = txtGO.AddComponent<TextMeshProUGUI>();
            coinPillText.text = "0";
            TypeScale.Apply(coinPillText, TypeRole.Body); // Task 42 — Body 32; gold token re-applied below
            coinPillText.color = Palette.Coins;
            coinPillText.alignment = TextAlignmentOptions.MidlineLeft;
            coinPillText.raycastTarget = false;
            coinPillText.enableWordWrapping = false;

            go.transform.SetAsLastSibling();
            coinPill = go;
            SetCoinPillVisible(false);   // shown only while the menu is up
        }

        /// <summary>Task 33 — update the coin pill's number (GameBootstrap calls this when coins change).</summary>
        public void SetCoinBalance(int coins)
        {
            if (coinPillText != null) coinPillText.text = coins.ToString();
        }

        private void SetCoinPillVisible(bool visible)
        {
            if (coinPill != null) coinPill.SetActive(visible);
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
