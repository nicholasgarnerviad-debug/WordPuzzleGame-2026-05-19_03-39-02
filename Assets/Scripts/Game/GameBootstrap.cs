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
            InitializeGameSystems();
            WireEventHandlers();
            ShowMainMenu();
        }

        private void InitializeGameSystems()
        {
            // Create core game systems
            stateManager = new GameStateManager();
            modeController = new ModeController(stateManager);

            // Create puzzle generator
            var wordValidator = new WordValidator();
            var wordGraphBuilder = new WordGraphBuilder();
            puzzleGenerator = new PuzzleGenerator(wordGraphBuilder, wordValidator);
        }

        private void WireEventHandlers()
        {
            // Wire main menu mode selection
            uiManager.GetMainMenu().OnClassicModeSelected += StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected += StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected += StartTimeAttackMode;

            // Wire gameplay
            uiManager.GetGameplay().OnWordSubmitted += OnWordSubmitted;

            // Wire results
            uiManager.GetResults().OnPlayAgain += PlayAgain;
            uiManager.GetResults().OnMainMenu += ShowMainMenu;
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
            activeMode = null;
            uiManager.ShowMainMenu();
        }

        private void StartClassicMode()
        {
            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartPuzzleShowMode()
        {
            activeMode = new PuzzleShowMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartTimeAttackMode()
        {
            activeMode = new TimeAttackMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartNewGame()
        {
            var puzzle = puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
            modeController.StartGame(puzzle);
            uiManager.ShowGameplay();

            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetPuzzleDisplay(state.puzzle.startWord, state.puzzle.endWord);
            uiManager.GetGameplay().SetScore(0);
            UpdateGameplayUI();
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
            }
            else
            {
                uiManager.GetGameplay().ShowFeedback("✗", Color.red);
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
            activeMode = null;
            var stats = modeController.GetCurrentStats();
            uiManager.GetResults().DisplayStats(stats);
            uiManager.ShowResults();
        }

        private void PlayAgain()
        {
            ShowMainMenu();
        }
    }
}
