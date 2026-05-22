using UnityEngine;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.UI;
using WordPuzzle.Persistence;
using WordPuzzleModel = WordPuzzle.Puzzle.WordPuzzle;

namespace WordPuzzle
{
    /// <summary>
    /// Central bootstrap: Initializes all systems, wires dependencies,
    /// and starts the game flow. This is the ONLY place where dependencies
    /// are wired together.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private ModeController modeController;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameplayScreen gameplayScreen;
        [SerializeField] private MainMenuScreen mainMenuScreen;
        [SerializeField] private ResultsScreen resultsScreen;

        private GameStateManager stateManager;
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
            try
            {
                // Create puzzle generator with word graph and tier cache
                var wordGraph = new WordGraph();

                // Load dictionary words from tier definitions into word graph
                LoadDictionaryWordsFromTiers(wordGraph);

                // Create word validator (depends on word graph)
                var wordValidator = new WordValidator(wordGraph);

                // Create data manager (handles persistence and tier loading)
                var dataManager = new DataManager();

                // Create state manager with all dependencies
                stateManager = new GameStateManager(wordValidator, dataManager);

                // Use serialized ModeController or create new one
                if (modeController == null)
                {
                    modeController = new ModeController(stateManager);
                }

                // Create puzzle generator (uses word graph, tierCache will be loaded via IDataManager)
                var tierCache = new System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData>();
                puzzleGenerator = new PuzzleGenerator(wordGraph, tierCache);

                Debug.Log("Game systems initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize game systems: {ex.Message}\n{ex.StackTrace}");
                enabled = false;
            }
        }

        private void LoadDictionaryWordsFromTiers(WordGraph wordGraph)
        {
            // Load tier definitions from Resources/Data/tier_definitions.json
            TextAsset tierFile = Resources.Load<TextAsset>("Data/tier_definitions");

            if (tierFile == null)
            {
                Debug.LogError("tier_definitions.json not found in Resources/Data/");
                return;
            }

            try
            {
                TierDefinitionsWrapper wrapper = JsonUtility.FromJson<TierDefinitionsWrapper>(tierFile.text);

                if (wrapper?.tiers == null)
                {
                    Debug.LogError("tier_definitions.json has no tiers array");
                    return;
                }

                // Extract all unique words from all puzzle solutions
                var uniqueWords = new System.Collections.Generic.HashSet<string>();

                foreach (var tier in wrapper.tiers)
                {
                    // Extract words from all puzzles in this tier
                    if (tier.puzzles != null)
                    {
                        foreach (var puzzle in tier.puzzles)
                        {
                            // Add start and end words
                            if (!string.IsNullOrEmpty(puzzle.startWord))
                                uniqueWords.Add(puzzle.startWord.ToLower());
                            if (!string.IsNullOrEmpty(puzzle.endWord))
                                uniqueWords.Add(puzzle.endWord.ToLower());

                            // Add all solution words
                            if (puzzle.solution != null)
                            {
                                foreach (var word in puzzle.solution)
                                {
                                    if (!string.IsNullOrEmpty(word))
                                        uniqueWords.Add(word.ToLower());
                                }
                            }
                        }
                    }
                }

                // Add all collected words to the word graph
                foreach (var word in uniqueWords)
                {
                    wordGraph.AddWord(word);
                }

                // Build adjacency list for efficient pathfinding
                wordGraph.BuildAdjacencies();

                Debug.Log($"Dictionary loaded: {uniqueWords.Count} unique words from {wrapper.tiers.Length} tiers");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load dictionary from tier definitions: {ex.Message}");
            }
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
                // TODO: Implement game-over checking via mode-agnostic interface method
                // CheckGameOver();
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
            var puzzleDefinition = puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);

            // Validate puzzle generation
            if (puzzleDefinition == null)
            {
                Debug.LogError("Failed to generate puzzle!");
                return;
            }

            // Convert PuzzleDefinition to WordPuzzle for compatibility with GameStateManager
            var puzzle = new WordPuzzleModel(
                puzzleDefinition.puzzleId,
                puzzleDefinition.startWord,
                puzzleDefinition.endWord,
                puzzleDefinition.optimalSteps,
                puzzleDefinition.solution,
                puzzleDefinition.seedValue,
                Difficulty.Easy
            );

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

        // TODO: Implement game-over checking via mode-agnostic interface method
        // private void CheckGameOver()
        // {
        //     bool isGameOver = false;
        //
        //     if (activeMode is ClassicMode cm)
        //         isGameOver = cm.IsGameOver();
        //     else if (activeMode is TimeAttackMode tam)
        //         isGameOver = tam.IsTimeUp();
        //     else if (activeMode is PuzzleShowMode psm)
        //         isGameOver = psm.IsGameOver();
        //
        //     if (isGameOver)
        //     {
        //         EndGame();
        //     }
        // }

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
