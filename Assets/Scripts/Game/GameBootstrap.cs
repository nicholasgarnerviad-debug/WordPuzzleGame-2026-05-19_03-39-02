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

        // Spec §3.6: tier-definition cache + IDataManager handle for puzzle-progress.
        private System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData> tierCacheRef;
        private IDataManager dataManagerRef;
        // Authoritative tier -> puzzleIds lookup for PuzzleShowMode.
        private System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>> tierPuzzleIdLookup;
        // Cached progress loaded at boot; injected into PuzzleShowMode on StartPuzzleShowMode.
        private PuzzleProgressData cachedPuzzleProgress;

        // Spec §3 Settings — loaded once at boot, applied to AudioListener.
        private SettingsData cachedSettings;

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
                dataManagerRef = dataManager;
                tierCacheRef = tierCache;

                // Spec §3.3 + §3.7: build authoritative tier->puzzleIds lookup and unlock
                // tiers based on persisted PuzzleProgressData.currentTier.
                BuildTierPuzzleLookup(tierCache);
                LoadPuzzleProgressBlocking(dataManager, tierCache);
                LoadSettingsBlocking(dataManager);

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
            uiManager.GetMainMenu().OnSettingsSelected += ShowSettings;

            // Wire library back button
            if (uiManager.GetLibrary() != null)
                uiManager.GetLibrary().OnBackToMenu += ShowMainMenu;

            // Wire settings screen
            if (uiManager.GetSettings() != null)
            {
                uiManager.GetSettings().OnBackToMenu += ShowMainMenu;
                uiManager.GetSettings().OnSettingsSaved += OnSettingsSaved;
                uiManager.GetSettings().OnResetProgressConfirmed += OnResetProgressConfirmed;
            }

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
            // Spec §3.3.6 — detach mode events to prevent leaks.
            if (activeMode is PuzzleShowMode psm)
            {
                psm.OnPuzzleCompleted -= OnPuzzleShowPuzzleCompleted;
                psm.OnTierAdvanced -= OnPuzzleShowTierAdvanced;
            }

            if (uiManager == null)
                return;

            // Unsubscribe from all events to prevent memory leaks
            uiManager.GetMainMenu().OnClassicModeSelected -= StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected -= StartPuzzleShowMode;
            uiManager.GetMainMenu().OnTimeAttackSelected -= StartTimeAttackMode;
            uiManager.GetMainMenu().OnLibrarySelected -= ShowLibrary;
            uiManager.GetMainMenu().OnSettingsSelected -= ShowSettings;

            if (uiManager.GetLibrary() != null)
                uiManager.GetLibrary().OnBackToMenu -= ShowMainMenu;

            if (uiManager.GetSettings() != null)
            {
                uiManager.GetSettings().OnBackToMenu -= ShowMainMenu;
                uiManager.GetSettings().OnSettingsSaved -= OnSettingsSaved;
                uiManager.GetSettings().OnResetProgressConfirmed -= OnResetProgressConfirmed;
            }
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

        // Spec §3.1 — open the settings screen, seeded with the cached SettingsData.
        private void ShowSettings()
        {
            var settingsScreen = uiManager.GetSettings();
            if (settingsScreen != null)
            {
                if (cachedSettings == null) cachedSettings = new SettingsData();
                settingsScreen.Populate(cachedSettings);
            }
            uiManager.ShowSettings();
        }

        // Spec §3.2 — persist user-edited settings (debounced from SettingsScreen).
        private async void OnSettingsSaved(SettingsData updated)
        {
            if (updated == null) return;
            cachedSettings = updated.Clone();

            // Apply audio immediately so background music/SFX use the new value.
            SettingsScreen.ApplyAudioListenerVolume(cachedSettings);

            if (dataManagerRef == null) return;
            try
            {
                await dataManagerRef.SaveSettingsAsync(cachedSettings);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to persist settings: {ex.Message}");
            }
        }

        // Spec §3.1 — destructive reset; wipes puzzle/player progress, keeps settings.
        private async void OnResetProgressConfirmed()
        {
            if (dataManagerRef == null) return;
            try
            {
                await dataManagerRef.ResetAllAsync();

                // Clear in-memory caches so subsequent runs start fresh.
                cachedPuzzleProgress = new PuzzleProgressData();

                // Re-lock tiers above 1 in the in-memory tier cache.
                if (tierCacheRef != null)
                {
                    foreach (var kvp in tierCacheRef)
                    {
                        if (kvp.Value == null) continue;
                        kvp.Value.isUnlocked = (kvp.Key <= 1);
                        if (kvp.Key > 1) kvp.Value.unlockedTimestamp = 0;
                    }
                }

                Debug.Log("Progress reset. Returning to main menu.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to reset progress: {ex.Message}");
            }
        }

        private void StartClassicMode()
        {
            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartPuzzleShowMode()
        {
            var psm = new PuzzleShowMode();

            // Spec §3.3 — supply authoritative tier→puzzleIds mapping so the gate
            // counts only puzzles belonging to the current tier.
            if (tierPuzzleIdLookup != null)
                psm.SetTierPuzzleLookup(tierPuzzleIdLookup);

            // Spec §3.2 — restore prior progress (created in InitializeGameSystems).
            if (cachedPuzzleProgress != null)
                psm.LoadProgress(cachedPuzzleProgress);

            // Spec §3.3.6 — orchestrator owns persistence.
            psm.OnPuzzleCompleted += OnPuzzleShowPuzzleCompleted;
            // Spec §3.7 — flip TierData.isUnlocked on the in-memory cache.
            psm.OnTierAdvanced += OnPuzzleShowTierAdvanced;

            activeMode = psm;
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        // Spec §3.3.6 — persist after every newly-completed puzzle.
        private async void OnPuzzleShowPuzzleCompleted(int puzzleId)
        {
            if (dataManagerRef == null || !(activeMode is PuzzleShowMode psm)) return;
            cachedPuzzleProgress = psm.ExportProgress();
            try
            {
                await dataManagerRef.SavePuzzleProgressAsync(cachedPuzzleProgress);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to persist puzzle progress: {ex.Message}");
            }
        }

        // Spec §3.7 — unlock new tier in the in-memory tier cache (do NOT touch JSON).
        private void OnPuzzleShowTierAdvanced(int oldTier, int newTier)
        {
            if (tierCacheRef == null) return;
            if (tierCacheRef.TryGetValue(newTier, out var tierData) && tierData != null)
            {
                tierData.isUnlocked = true;
                if (tierData.unlockedTimestamp == 0)
                    tierData.unlockedTimestamp = System.DateTime.UtcNow.Ticks;
            }
        }

        // Spec §3.3 — pre-compute tier->puzzleIds map from authoritative tier_definitions.
        private void BuildTierPuzzleLookup(System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData> tierCache)
        {
            tierPuzzleIdLookup = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>();
            if (tierCache == null) return;
            foreach (var kvp in tierCache)
            {
                var ids = new System.Collections.Generic.HashSet<int>();
                if (kvp.Value?.puzzles != null)
                {
                    foreach (var p in kvp.Value.puzzles)
                    {
                        if (p != null) ids.Add(p.puzzleId);
                    }
                }
                tierPuzzleIdLookup[kvp.Key] = ids;
            }
        }

        // Spec §3.7 — load PuzzleProgressData and apply tier-unlock cascade
        // (any tier with tierId <= currentTier is unlocked).
        private void LoadPuzzleProgressBlocking(IDataManager dataManager,
            System.Collections.Generic.Dictionary<int, WordPuzzle.Puzzle.TierData> tierCache)
        {
            try
            {
                var task = dataManager.LoadPuzzleProgressAsync();
                cachedPuzzleProgress = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PuzzleProgress load failed; using defaults: {ex.Message}");
                cachedPuzzleProgress = new PuzzleProgressData();
            }

            if (cachedPuzzleProgress == null) cachedPuzzleProgress = new PuzzleProgressData();

            // §3.7 cascade unlock.
            int unlockedThrough = cachedPuzzleProgress.currentTier;
            foreach (var kvp in tierCache)
            {
                if (kvp.Key <= unlockedThrough && kvp.Value != null)
                {
                    kvp.Value.isUnlocked = true;
                }
            }
        }

        // Spec §3.3 — load SettingsData on boot and apply master/mute to AudioListener.
        private void LoadSettingsBlocking(IDataManager dataManager)
        {
            try
            {
                var task = dataManager.LoadSettingsAsync();
                cachedSettings = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Settings load failed; using defaults: {ex.Message}");
                cachedSettings = new SettingsData();
            }

            if (cachedSettings == null) cachedSettings = new SettingsData();

            SettingsScreen.ApplyAudioListenerVolume(cachedSettings);
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
            var gameplay = uiManager.GetGameplay();

            // Architect5 spec §6 — state-subscription tick order.
            // 1. Persistent FROM/TO labels + tiles (idempotent).
            gameplay.SetStartAndEndWords(state.puzzle.startWord, state.puzzle.endWord);

            // 2. Chain history with §3.5 diff highlighting.
            gameplay.SetChain(state.wordChain);

            // 3. Live current-input row.
            gameplay.SetCurrentInput(state.currentInput);

            // 4. Hint highlight on current-input row (NEW GameState field).
            gameplay.SetHintLetterIndex(state.hintLetterIndex);

            // 5. Reveal preview row (NEW GameState field). Compute changed-index
            //    from chain tail vs. revealed next word.
            string chainTail = state.wordChain != null && state.wordChain.Count > 0
                ? state.wordChain[state.wordChain.Count - 1]
                : string.Empty;
            int revealChangedIdx = ComputeChangedIndex(chainTail, state.revealedNextWord);
            gameplay.SetRevealedNextWord(state.revealedNextWord, revealChangedIdx);

            // 6. Auto-scroll is triggered internally by SetChain / SetRevealedNextWord per §3.4.

            // Preserved peripheral UI.
            gameplay.SetScore(state.score);
            UpdatePowerUpUI();

            if (activeMode is TimeAttackMode tam)
            {
                gameplay.SetTimer(tam.GetTimeRemaining());
                gameplay.SetTierIndicator("");
            }
            else if (activeMode is PuzzleShowMode psm)
            {
                gameplay.SetTierIndicator($"Tier {psm.CurrentTier} / {PuzzleShowMode.MaxTier}");
            }
            else
            {
                gameplay.SetTierIndicator("");
            }
        }

        // §6 helper — first index where lastChainWord and revealedNextWord differ.
        // Returns -1 when revealed is empty/null. If revealed and last share their full
        // common prefix but lengths differ, returns the trailing index. -1 when identical.
        private static int ComputeChangedIndex(string last, string revealed)
        {
            if (string.IsNullOrEmpty(revealed)) return -1;
            if (string.IsNullOrEmpty(last)) return 0;
            int shared = System.Math.Min(last.Length, revealed.Length);
            for (int k = 0; k < shared; k++)
            {
                if (char.ToLowerInvariant(last[k]) != char.ToLowerInvariant(revealed[k]))
                    return k;
            }
            if (last.Length != revealed.Length) return shared;
            return -1;
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

            // PuzzleShowMode (Spec §3.3): tier advancement is now driven internally
            // by the mode when the gate (N completions) is met. Bootstrap just decides
            // whether to load the next puzzle or end the run.
            if (activeMode is PuzzleShowMode psm)
            {
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
