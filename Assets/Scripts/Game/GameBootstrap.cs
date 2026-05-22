using UnityEngine;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.UI;

namespace WordPuzzle
{
    /// <summary>
    /// Central bootstrap: Initializes all systems, wires dependencies,
    /// and starts the game flow. This is the ONLY place where dependencies
    /// are wired together.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager;

        private GameStateManager stateManager;
        private ModeController modeController;
        private PuzzleGenerator puzzleGenerator;
        private IGameMode activeMode;

        private void Start()
        {
            // Validate critical dependencies
            if (uiManager == null)
            {
                Debug.LogError("UIManager not assigned to GameBootstrap!");
                enabled = false;
                return;
            }

            InitializeGameSystems();
            WireEventHandlers();
            ShowMainMenu();
        }

        private void InitializeGameSystems()
        {
            // Create core game systems
            stateManager = new GameStateManager();
            modeController = new ModeController(stateManager);

            // Create puzzle generator with word graph and tier cache
            var wordGraph = new WordGraph();
            var tierCache = new System.Collections.Generic.Dictionary<int, TierData>();
            puzzleGenerator = new PuzzleGenerator(wordGraph, tierCache);

            Debug.Log("Game systems initialized");
        }

        private void WireEventHandlers()
        {
            if (uiManager == null)
                return;

            // Wire main menu mode selection
            uiManager.GetMainMenu().OnClassicModeSelected += StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected += StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected += StartTimeAttackMode;

            // Wire gameplay
            uiManager.GetGameplay().OnWordSubmitted += OnWordSubmitted;

            // Wire results
            uiManager.GetResults().OnPlayAgain += PlayAgain;
            uiManager.GetResults().OnMainMenu += ShowMainMenu;

            Debug.Log("Event handlers wired");
        }

        private void OnDestroy()
        {
            if (uiManager == null)
                return;

            // Unsubscribe from all events to prevent memory leaks
            uiManager.GetMainMenu().OnClassicModeSelected -= StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected -= StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected -= StartTimeAttackMode;
            uiManager.GetGameplay().OnWordSubmitted -= OnWordSubmitted;
            uiManager.GetResults().OnPlayAgain -= PlayAgain;
            uiManager.GetResults().OnMainMenu -= ShowMainMenu;

            Debug.Log("Event handlers cleaned up");
        }

        private void Update()
        {
            if (activeMode != null)
            {
                activeMode.Tick(Time.deltaTime);
                UpdateGameplayUI();
                CheckGameOver();
            }
        }

        private void ShowMainMenu()
        {
            Debug.Log("Showing main menu");
            activeMode = null;
            uiManager.ShowMainMenu();
        }

        private void StartClassicMode()
        {
            Debug.Log("Starting Classic Mode");
            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartPuzzleShowMode()
        {
            Debug.Log("Starting Puzzle Show Mode");
            activeMode = new PuzzleShowMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartTimeAttackMode()
        {
            Debug.Log("Starting Time Attack Mode");
            activeMode = new TimeAttackMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartNewGame()
        {
            var puzzle = puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);

            // Validate puzzle generation
            if (puzzle == null)
            {
                Debug.LogError("Failed to generate puzzle!");
                return;
            }

            modeController.StartGame(puzzle);
            uiManager.ShowGameplay();

            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetPuzzleDisplay(state.puzzle.startWord, state.puzzle.endWord);
            uiManager.GetGameplay().SetScore(0);
            UpdateGameplayUI();

            Debug.Log($"Game started: {state.puzzle.startWord} → {state.puzzle.endWord}");
        }

        private void OnWordSubmitted(string word)
        {
            modeController.HandleWordSubmission(word);
            var state = stateManager.GetCurrentState();

            // Visual feedback
            bool wordAdded = state.wordChain.Count > 1;
            if (wordAdded)
            {
                uiManager.GetGameplay().ShowFeedback("✓", Color.green);
                Debug.Log($"Word accepted: {word}");
            }
            else
            {
                uiManager.GetGameplay().ShowFeedback("✗", Color.red);
                Debug.Log($"Word rejected: {word}");
            }
        }

        private void UpdateGameplayUI()
        {
            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetWordChain(state.wordChain.ToArray());
            uiManager.GetGameplay().SetScore(state.score);

            if (activeMode is TimeAttackMode tam)
            {
                uiManager.GetGameplay().SetTimer(tam.GetTimeRemaining());
            }
        }

        private void CheckGameOver()
        {
            bool isGameOver = false;

            if (activeMode is ClassicMode cm)
                isGameOver = cm.IsGameOver();
            else if (activeMode is TimeAttackMode tam)
                isGameOver = tam.IsTimeUp();
            else if (activeMode is PuzzleShowMode psm)
                isGameOver = psm.IsGameOver();

            if (isGameOver)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            Debug.Log("Game over - showing results");
            activeMode = null;
            var stats = modeController.GetCurrentStats();
            uiManager.GetResults().DisplayStats(stats);
            uiManager.ShowResults();
        }

        private void PlayAgain()
        {
            // Return to main menu (user can select same or different mode)
            Debug.Log("Play again - returning to main menu");
            ShowMainMenu();
        }
    }
}
