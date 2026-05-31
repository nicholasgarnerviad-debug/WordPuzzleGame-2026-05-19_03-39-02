using UnityEngine;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.UI;
using WordPuzzle.Persistence;
using WordPuzzle.Game;
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

        // Task 1 — Daily puzzle + streak.
        private IClock dailyClock = new SystemClock();
        private DailyPuzzleService dailyPuzzleService;
        private DailyProgress cachedDailyProgress;
        private bool isDailyRun;                 // true while the active mode is the daily puzzle
        private PuzzleDefinition pendingDailyPuzzle;
        private int pendingDailyIndex = -1;

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
                LoadDailyProgressBlocking(dataManager);
                InitializeDailyPuzzleService();

                stateManager = new GameStateManager(wordValidator, dataManager);

                // Spec §1 — relay submission results (accepted / typed rejection) to the
                // UI feedback layer. Subscribed once at boot, detached in OnDestroy.
                stateManager.OnWordSubmissionResult += OnWordSubmissionResultHandler;

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
            uiManager.GetMainMenu().OnDailySelected += StartDailyMode;
            uiManager.GetMainMenu().OnTimeAttackSelected += ShowTimeAttackSetup;
            uiManager.GetMainMenu().OnLibrarySelected += ShowLibrary;
            uiManager.GetMainMenu().OnSettingsSelected += ShowSettings;

            // Wire library back button + puzzle-tap → PuzzleShowMode (§5 library wiring)
            if (uiManager.GetLibrary() != null)
            {
                uiManager.GetLibrary().OnBackToMenu += ShowMainMenu;
                uiManager.GetLibrary().OnPuzzleSelected += OnLibraryPuzzleSelected;
            }

            // §5.4 — TimeAttack setup screen
            if (uiManager.GetTimeAttackSetup() != null)
            {
                uiManager.GetTimeAttackSetup().OnBackToMenu += ShowMainMenu;
                uiManager.GetTimeAttackSetup().OnConfigConfirmed += StartTimeAttackModeWithConfig;
            }

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
            uiManager.GetGameplay().OnAddTimeUsed += OnAddTimeUsed;

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

            // Spec §4 — detach TimeAttack bonus-seconds listener.
            if (activeMode is TimeAttackMode tamCleanup)
            {
                tamCleanup.OnTimeAdded -= OnTimeAttackTimeAdded;
            }

            if (uiManager == null)
                return;

            // Unsubscribe from all events to prevent memory leaks
            uiManager.GetMainMenu().OnClassicModeSelected -= StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected -= StartPuzzleShowMode;
            uiManager.GetMainMenu().OnDailySelected -= StartDailyMode;
            uiManager.GetMainMenu().OnTimeAttackSelected -= ShowTimeAttackSetup;
            uiManager.GetMainMenu().OnLibrarySelected -= ShowLibrary;
            uiManager.GetMainMenu().OnSettingsSelected -= ShowSettings;

            if (uiManager.GetLibrary() != null)
            {
                uiManager.GetLibrary().OnBackToMenu -= ShowMainMenu;
                uiManager.GetLibrary().OnPuzzleSelected -= OnLibraryPuzzleSelected;
            }

            if (uiManager.GetTimeAttackSetup() != null)
            {
                uiManager.GetTimeAttackSetup().OnBackToMenu -= ShowMainMenu;
                uiManager.GetTimeAttackSetup().OnConfigConfirmed -= StartTimeAttackModeWithConfig;
            }

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
            uiManager.GetGameplay().OnAddTimeUsed -= OnAddTimeUsed;

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
            isDailyRun = false;
            pendingDailyPuzzle = null;
            pendingDailyIndex = -1;
            uiManager.ShowMainMenu();
            RefreshDailyButtonState();
        }

        // Task 1C — keep the DAILY button label in sync with persisted streak state.
        private void RefreshDailyButtonState()
        {
            var menu = uiManager?.GetMainMenu();
            if (menu == null) return;
            if (cachedDailyProgress == null)
            {
                menu.SetDailyState(false, 0);
                return;
            }
            DailyStreakRules.RefreshTodayFlag(cachedDailyProgress, dailyClock.TodayIso);
            menu.SetDailyState(cachedDailyProgress.todayCompleted, cachedDailyProgress.currentStreak);
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
            isDailyRun = false;
            pendingDailyPuzzle = null;
            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        // Task 1A — Daily puzzle entry point.
        // Reuses ClassicMode mechanics (no timer, no tier-gate) but injects a
        // deterministic puzzle from DailyPuzzleService instead of the random generator.
        private void StartDailyMode()
        {
            if (dailyPuzzleService == null || dailyPuzzleService.PoolCount == 0)
            {
                Debug.LogError("[Daily] DailyPuzzleService not initialized or pool empty; aborting.");
                return;
            }
            pendingDailyPuzzle = dailyPuzzleService.GetTodayPuzzle();
            pendingDailyIndex = dailyPuzzleService.TodayIndex();
            if (pendingDailyPuzzle == null)
            {
                Debug.LogError("[Daily] Today's puzzle is null; pool may be malformed.");
                return;
            }
            isDailyRun = true;
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
        // Task 1B — load DailyProgress at boot.
        private void LoadDailyProgressBlocking(IDataManager dataManager)
        {
            try
            {
                var task = dataManager.LoadDailyProgressAsync();
                cachedDailyProgress = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"DailyProgress load failed; using defaults: {ex.Message}");
                cachedDailyProgress = new DailyProgress();
            }
            if (cachedDailyProgress == null) cachedDailyProgress = new DailyProgress();
            DailyStreakRules.RefreshTodayFlag(cachedDailyProgress, dailyClock.TodayIso);
        }

        // Task 1A — load daily puzzle pool from Resources and bind to the system clock.
        private void InitializeDailyPuzzleService()
        {
            dailyPuzzleService = DailyPuzzleService.LoadFromResources(dailyClock);
            if (dailyPuzzleService.PoolCount == 0)
                Debug.LogWarning("[Daily] pool empty — DAILY button will be disabled at runtime.");
        }

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

        // §5 — Show the Time Attack setup screen (replaces direct mode start).
        private void ShowTimeAttackSetup()
        {
            uiManager.ShowTimeAttackSetup();
        }

        // §5.4 — Build a TimeAttackMode from the config chosen on the setup screen.
        private void StartTimeAttackModeWithConfig(TimeAttackConfig config)
        {
            var cfg = config ?? TimeAttackConfig.Default60();
            var tam = new TimeAttackMode(cfg);
            tam.OnTimeAdded += OnTimeAttackTimeAdded;
            activeMode = tam;
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        // §5 library wiring — start PuzzleShowMode at the tier owning the tapped puzzle
        // and load that specific puzzle as the first puzzle of the run.
        private void OnLibraryPuzzleSelected(int puzzleId)
        {
            int tierId = FindTierIdForPuzzle(puzzleId);
            if (tierId <= 0)
            {
                Debug.LogError($"Library tap: puzzleId {puzzleId} not found in any tier");
                return;
            }

            var psm = new PuzzleShowMode();
            if (tierPuzzleIdLookup != null) psm.SetTierPuzzleLookup(tierPuzzleIdLookup);
            if (cachedPuzzleProgress != null) psm.LoadProgress(cachedPuzzleProgress);
            psm.OnPuzzleCompleted += OnPuzzleShowPuzzleCompleted;
            psm.OnTierAdvanced += OnPuzzleShowTierAdvanced;

            activeMode = psm;
            modeController.SetMode(activeMode);
            StartSpecificPuzzle(puzzleId, tierId);
        }

        // Helper — walk the authoritative tier cache to resolve a puzzleId to its tier.
        private int FindTierIdForPuzzle(int puzzleId)
        {
            if (tierCacheRef == null) return -1;
            foreach (var kvp in tierCacheRef)
            {
                if (kvp.Value?.puzzles == null) continue;
                foreach (var p in kvp.Value.puzzles)
                {
                    if (p != null && p.puzzleId == puzzleId) return kvp.Key;
                }
            }
            return -1;
        }

        // Helper — locate a PuzzleDefinition by id across all tiers.
        private PuzzleDefinition FindPuzzleDefinitionById(int puzzleId)
        {
            if (tierCacheRef == null) return null;
            foreach (var kvp in tierCacheRef)
            {
                if (kvp.Value?.puzzles == null) continue;
                foreach (var p in kvp.Value.puzzles)
                {
                    if (p != null && p.puzzleId == puzzleId) return p;
                }
            }
            return null;
        }

        // Library-tap variant of StartNewGame that bypasses random tier-puzzle selection
        // and loads the explicit puzzle the player picked.
        private void StartSpecificPuzzle(int puzzleId, int tierId)
        {
            var def = FindPuzzleDefinitionById(puzzleId);
            if (def == null)
            {
                Debug.LogError($"StartSpecificPuzzle: definition for puzzleId {puzzleId} not found");
                return;
            }

            Difficulty difficulty = tierId <= 2 ? Difficulty.Easy
                                  : tierId <= 4 ? Difficulty.Medium
                                  : Difficulty.Hard;

            var puzzle = new WordPuzzleModel(
                def.puzzleId,
                def.startWord,
                def.endWord,
                def.optimalSteps,
                def.solution,
                def.seedValue,
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

        // §5 — Player tapped +Time on the gameplay screen. Dispatch via state manager
        // so the action is journaled and the GameStateManager.OnTimeAdded event fires.
        private void OnAddTimeUsed()
        {
            if (stateManager == null) return;
            stateManager.Dispatch(new UseAddTimeAction());
            UpdatePowerUpUI();
        }

        // Spec §4 — surface AddTime / Survival bonus seconds to the UI feedback layer.
        private void OnTimeAttackTimeAdded(float seconds)
        {
            var gameplay = uiManager?.GetGameplay();
            if (gameplay == null) return;
            gameplay.ShowFeedback($"+{Mathf.RoundToInt(seconds)}s", Color.cyan);
        }

        private void StartNewGame()
        {
            PuzzleDefinition puzzleDefinition;
            Difficulty difficulty;

            if (isDailyRun && pendingDailyPuzzle != null)
            {
                // Task 1A — deterministic daily puzzle.
                puzzleDefinition = pendingDailyPuzzle;
                difficulty = Difficulty.Easy;
            }
            else if (activeMode is PuzzleShowMode psm)
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

        // Spec §1 — relay GameStateManager.OnWordSubmissionResult to the UI feedback layer.
        // Minimal stub so the wiring in InitializeGameSystems compiles. mode-coder6 may
        // refine this with typed reason rendering.
        private void OnWordSubmissionResultHandler(WordPuzzle.State.SubmissionResult result)
        {
            var gameplay = uiManager?.GetGameplay();
            if (gameplay == null) return;
            if (result.accepted)
            {
                gameplay.ShowFeedback(string.IsNullOrEmpty(result.reason) ? "✓" : result.reason, Color.green);
            }
            else if (!string.IsNullOrEmpty(result.reason))
            {
                gameplay.ShowFeedback(result.reason, Color.red);
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

            // §5.5 — Timer + AddTime visibility per-mode.
            bool isTimeAttack = activeMode is TimeAttackMode;
            gameplay.SetTimerVisible(isTimeAttack);
            gameplay.SetAddTimeVisible(isTimeAttack);

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
            // §5.1 — AddTime charges (TimeAttack only; toggled separately via SetAddTimeVisible).
            gameplay.SetAddTimeCount(state.addTimesRemaining);
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
            // Snapshot daily-run state before we tear down, then route to results.
            bool wasDailyRun = isDailyRun;
            int dailyIndex = pendingDailyIndex;
            isDailyRun = false;
            pendingDailyPuzzle = null;
            pendingDailyIndex = -1;

            activeMode = null;
            var stats = modeController.GetCurrentStats();
            uiManager.GetResults().DisplayStats(stats);

            if (wasDailyRun)
            {
                RecordDailyCompletionAndSurface(dailyIndex);
            }

            uiManager.ShowResults();
        }

        // Task 1B/1C — apply streak rules + persist + drive the ResultsScreen daily widgets.
        private async void RecordDailyCompletionAndSurface(int puzzleIndex)
        {
            string todayIso = dailyClock.TodayIso;
            if (cachedDailyProgress == null) cachedDailyProgress = new DailyProgress();
            bool alreadyCountedToday = cachedDailyProgress.lastCompletedDateIso == todayIso;

            DailyStreakRules.ApplyCompletion(cachedDailyProgress, todayIso, puzzleIndex);

            if (dataManagerRef != null)
            {
                try { await dataManagerRef.SaveDailyProgressAsync(cachedDailyProgress); }
                catch (System.Exception ex) { Debug.LogError($"[Daily] persist failed: {ex.Message}"); }
            }

            uiManager.GetResults().ShowDailyStreak(
                cachedDailyProgress.currentStreak,
                cachedDailyProgress.longestStreak,
                alreadyCountedToday);
        }

        private void PlayAgain()
        {
            ShowMainMenu();
        }
    }
}
