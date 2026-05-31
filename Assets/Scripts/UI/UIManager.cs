using UnityEngine;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ShowMainMenu()
        {
            mainMenuScreen.Show();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
        }

        public void ShowGameplay()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Show();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Hide();
        }

        public void ShowResults()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Show();
            if (libraryScreen != null) libraryScreen.Hide();
        }

        public void ShowLibrary()
        {
            mainMenuScreen.Hide();
            gameplayScreen.Hide();
            resultsScreen.Hide();
            if (libraryScreen != null) libraryScreen.Show();
        }

        // Screen accessors
        public MainMenuScreen GetMainMenu() => mainMenuScreen;
        public GameplayScreen GetGameplay() => gameplayScreen;
        public ResultsScreen GetResults() => resultsScreen;
        public PuzzleLibraryScreen GetLibrary() => libraryScreen;

    }
}
