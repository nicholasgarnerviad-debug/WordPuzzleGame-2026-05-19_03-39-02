using UnityEngine;
using WordPuzzle.Modes;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            gameplayScreen.OnWordSubmitted += OnWordSubmitted;
            resultsScreen.OnPlayAgain += OnPlayAgain;
            resultsScreen.OnMainMenu += OnMainMenu;
        }

        private void OnDisable()
        {
            gameplayScreen.OnWordSubmitted -= OnWordSubmitted;
            resultsScreen.OnPlayAgain -= OnPlayAgain;
            resultsScreen.OnMainMenu -= OnMainMenu;
        }

        public void ShowMainMenu()
        {
            mainMenuScreen.Show();
            gameplayScreen.Hide();
            resultsScreen.Hide();
        }

        public void ShowGameplay()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Show();
            resultsScreen.Hide();
        }

        public void ShowResults()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Show();
        }

        // Screen accessors
        public MainMenuScreen GetMainMenu() => mainMenuScreen;
        public GameplayScreen GetGameplay() => gameplayScreen;
        public ResultsScreen GetResults() => resultsScreen;

        // Dummy handlers to wire up (will be overridden by bootstrap)
        private void OnWordSubmitted(string word) { }
        private void OnPlayAgain() { }
        private void OnMainMenu() { }
    }
}
