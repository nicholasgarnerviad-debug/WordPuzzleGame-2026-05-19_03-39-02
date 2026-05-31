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
            // Try to get UIManager from same GameObject if not assigned
            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }

            // Validate critical dependencies
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found on Bootstrap GameObject!");
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
                var wordGraph = new WordGraph();
                var tierCache = new System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData>();

                LoadWordLibrary(wordGraph);
                LoadTierData(wordGraph, tierCache);
                wordGraph.BuildAdjacencies();

                var wordValidator = new WordValidator(wordGraph);
                var dataManager = new DataManager();

                stateManager = new GameStateManager(wordValidator, dataManager);

                if (modeController == null)
                {
                    modeController = new ModeController(stateManager);
                }

                puzzleGenerator = new PuzzleGenerator(wordGraph, tierCache);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize game systems: {ex.Message}\n{ex.StackTrace}");
                enabled = false;
            }
        }

        private void LoadWordLibrary(WordGraph wordGraph)
        {
            TextAsset libraryFile = Resources.Load<TextAsset>("Data/word_library");
            if (libraryFile == null)
            {
                Debug.LogWarning("word_library.json not found in Resources/Data/ — falling back to tier words only");
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<WordLibraryWrapper>(libraryFile.text);
                if (wrapper?.words == null)
                {
                    Debug.LogError("word_library.json could not be parsed (no 'words' array)");
                    return;
                }

                int added = 0;
                foreach (var word in wrapper.words)
                {
                    if (string.IsNullOrWhiteSpace(word)) continue;
                    string normalized = word.Trim().ToLowerInvariant();
                    if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z]+$"))
                    {
                        wordGraph.AddWord(normalized);
                        added++;
                    }
                }

                Debug.Log($"WordLibrary: loaded {added} words from Resources/Data/word_library.json");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse word_library.json: {ex.Message}");
            }
        }

        private void LoadTierData(WordGraph wordGraph, System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData> tierCache)
        {
            TextAsset tierFile = Resources.Load<TextAsset>("Data/tier_definitions");
            if (tierFile == null)
            {
                Debug.LogError("tier_definitions.json not found in Resources/Data/");
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<TierDefinitionsWrapper>(tierFile.text);
                if (wrapper?.tiers == null || wrapper.tiers.Length == 0)
                {
                    Debug.LogError("tier_definitions.json has no valid tiers");
                    return;
                }

                foreach (var tier in wrapper.tiers)
                {
                    tierCache[tier.tierId] = tier;
                    if (tier.puzzles == null) continue;

                    foreach (var puzzle in tier.puzzles)
                    {
                        if (!string.IsNullOrEmpty(puzzle.startWord))
                            wordGraph.AddWord(puzzle.startWord.ToLowerInvariant());
                        if (!string.IsNullOrEmpty(puzzle.endWord))
                            wordGraph.AddWord(puzzle.endWord.ToLowerInvariant());
                        if (puzzle.solution != null)
                        {
                            foreach (var word in puzzle.solution)
                            {
                                if (!string.IsNullOrEmpty(word))
                                    wordGraph.AddWord(word.ToLowerInvariant());
                            }
                        }
                    }
                }

                Debug.Log($"TierData: loaded {wrapper.tiers.Length} tiers");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse tier_definitions.json: {ex.Message}");
            }
        }

        [System.Serializable]
        private class WordLibraryWrapper
        {
            public string[] words;
        }

        private void WireEventHandlers()
        {
            if (uiManager == null)
                return;

            // Wire main menu mode selection
            uiManager.GetMainMenu().OnClassicModeSelected += StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected += StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected += StartTimeAttackMode;
            uiManager.GetMainMenu().OnLibrarySelected += ShowLibrary;

            // Wire library back button
            if (uiManager.GetLibrary() != null)
                uiManager.GetLibrary().OnBackToMenu += ShowMainMenu;

            // Wire gameplay
            uiManager.GetGameplay().OnWordSubmitted += OnWordSubmitted;
            uiManager.GetGameplay().OnBackToMenu += ShowMainMenu;

            // Phase 2: Wire power-up handlers
            uiManager.GetGameplay().OnHintUsed += OnHintUsed;
            uiManager.GetGameplay().OnRevealUsed += OnRevealUsed;
            uiManager.GetGameplay().OnUndoStep += OnUndoStep;

            // Wire results
            uiManager.GetResults().OnPlayAgain += PlayAgain;
            uiManager.GetResults().OnMainMenu += ShowMainMenu;
        }

        private void OnDestroy()
        {
            if (uiManager == null)
                return;

            // Unsubscribe from all events to prevent memory leaks
            uiManager.GetMainMenu().OnClassicModeSelected -= StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected -= StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected -= StartTimeAttackMode;
            uiManager.GetMainMenu().OnLibrarySelected -= ShowLibrary;

            if (uiManager.GetLibrary() != null)
                uiManager.GetLibrary().OnBackToMenu -= ShowMainMenu;
            uiManager.GetGameplay().OnWordSubmitted -= OnWordSubmitted;
            uiManager.GetGameplay().OnBackToMenu -= ShowMainMenu;

            // Phase 2: Unsubscribe power-up handlers
            uiManager.GetGameplay().OnHintUsed -= OnHintUsed;
            uiManager.GetGameplay().OnRevealUsed -= OnRevealUsed;
            uiManager.GetGameplay().OnUndoStep -= OnUndoStep;

            uiManager.GetResults().OnPlayAgain -= PlayAgain;
            uiManager.GetResults().OnMainMenu -= ShowMainMenu;
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

        private void ShowLibrary()
        {
            uiManager.ShowLibrary();
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
            PuzzleDefinition puzzleDefinition;
            Difficulty difficulty;

            if (activeMode is PuzzleShowMode psm)
            {
                puzzleDefinition = puzzleGenerator.GetRandomTierPuzzle(psm.CurrentTier);
                difficulty = psm.CurrentTier <= 2 ? Difficulty.Easy
                          : psm.CurrentTier <= 4 ? Difficulty.Medium
                          : Difficulty.Hard;
            }
            else if (activeMode is TimeAttackMode)
            {
                puzzleDefinition = puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
                difficulty = Difficulty.Easy;
            }
            else
            {
                puzzleDefinition = puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
                difficulty = Difficulty.Easy;
            }

            if (puzzleDefinition == null)
            {
                Debug.LogError("Failed to generate puzzle!");
                return;
            }

            var puzzle = new WordPuzzleModel(
                puzzleDefinition.puzzleId,
                puzzleDefinition.startWord,
                puzzleDefinition.endWord,
                puzzleDefinition.optimalSteps,
                puzzleDefinition.solution,
                puzzleDefinition.seedValue,
                difficulty
            );

            modeController.StartGame(puzzle);
            uiManager.ShowGameplay();

            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetPuzzleDisplay(state.puzzle.startWord, state.puzzle.endWord);
            uiManager.GetGameplay().SetScore(0);
            uiManager.GetGameplay().ShowFeedback("", Color.white);
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

        // Phase 2: Power-up event handlers
        private void OnHintUsed()
        {
            stateManager.Dispatch(new UseHintAction(0));
            UpdatePowerUpUI();
        }

        private void OnRevealUsed()
        {
            stateManager.Dispatch(new UseRevealAction());
            UpdatePowerUpUI();
        }

        private void OnUndoStep()
        {
            stateManager.Dispatch(new UndoStepAction());
            UpdatePowerUpUI();
        }

        private void UpdateGameplayUI()
        {
            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetWordChain(state.wordChain.ToArray());
            uiManager.GetGameplay().SetScore(state.score);
            UpdatePowerUpUI();

            if (activeMode is TimeAttackMode tam)
            {
                uiManager.GetGameplay().SetTimer(tam.GetTimeRemaining());
                uiManager.GetGameplay().SetTierIndicator("");
            }
            else if (activeMode is PuzzleShowMode psm)
            {
                uiManager.GetGameplay().SetTierIndicator($"Tier {psm.CurrentTier} / {PuzzleShowMode.MaxTier}");
            }
            else
            {
                uiManager.GetGameplay().SetTierIndicator("");
            }
        }

        private void UpdatePowerUpUI()
        {
            var state = stateManager.GetCurrentState();
            var gameplay = uiManager.GetGameplay();
            gameplay.SetHintCount(state.hintsRemaining);
            gameplay.SetRevealCount(state.revealsRemaining);
            gameplay.EnableUndoButton(state.wordChain.Count > 1);
            gameplay.SetRevealedIndices(state.revealedLetterIndices);
        }

        private void CheckGameOver()
        {
            if (activeMode == null || !activeMode.IsGameOver()) return;

            // PuzzleShowMode: advance to next tier and continue, or end if all tiers done
            if (activeMode is PuzzleShowMode psm)
            {
                psm.AdvanceTier();
                if (psm.AllTiersComplete)
                {
                    EndGame();
                    return;
                }
                StartNewGame();
                return;
            }

            EndGame();
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
