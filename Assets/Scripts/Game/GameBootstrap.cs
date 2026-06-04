using UnityEngine;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.UI;
using WordPuzzle.Persistence;
using WordPuzzle.Game;
// Task 7 — juice subsystems live in WordPuzzle.UI namespace (same as UIAnimations).
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

        // Task 16 — post-win flow state.
        private bool awaitingClassicNext;        // compact win panel up; gate CheckGameOver
        private TimeAttackConfig lastTimeAttackConfig;  // for ResultsScreen "Play Again" → new run
        private int timeAttackLaddersCompleted;  // ladders solved in the active Time Attack run
        private enum PostWin { None, TimeAttack, PuzzleShow, Daily }
        private PostWin lastWinContext = PostWin.None;  // routes ResultsScreen "Play Again"
        private int pendingPuzzleShowNextTier;          // >0 when a tier just unlocked (offer "Tier N ▸")
        private PuzzleDefinition pendingDailyPuzzle;
        private int pendingDailyIndex = -1;

        // Task 3A — Tutorial onboarding.
        private OnboardingData cachedOnboarding;
        private bool isTutorialRun;
        private PuzzleDefinition pendingTutorialPuzzle;
        private bool tutorialOverlaySubscribed;
        [SerializeField] private WordPuzzle.UI.TutorialOverlay tutorialOverlay;

        // Task 2A — Share result. Snapshot captured on EndGame, consumed on Share tap.
        private IShareService shareService = new ClipboardShareService();
        private ShareCardBuilder.ShareInput lastShareInput;

        // Task 6A/6B — Economy + Ad services.
        private IEconomyManager economyManager;
        private IAdService adService;
        private AdPolicyService adPolicy;
        private IStoreService storeService;                      // Task 33 — real-money store (mock in editor)
        private WordPuzzle.UI.ShopScreen shopScreen;     // Task 33 — runtime Shop overlay
        private int lastDailyRewardCoins = 0;            // Task 36 36K — amount the daily doubler re-grants
        private bool dailyDoublerConsumed = false;       // Task 36 36K — one doubler per daily result
        private WordPuzzle.UI.DailyRewardPopup dailyRewardPopup;  // Task 36 36K — login claim + streak repair overlay

        // Task 7B/7C — Juice: haptics + SFX.
        [SerializeField] private SfxManager sfxManager;
        private IHaptics haptics;

        // Task 9F — Statistics screen.
        // Task 9G — Resume: snapshot loaded at boot; PuzzleDefinition cached if resumable.
        private PlayerProgress cachedPlayerProgress;
        private GameStateSnapshot cachedResumeSnapshot;
        private PuzzleDefinition cachedResumePuzzle;
        private string cachedResumeMode;  // "PuzzleShow" | "Daily" — set by TryLoadResumable

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
                LoadOnboardingBlocking(dataManager);
                LoadPlayerProgressBlocking(dataManager);
                // Task 20B — backfill/self-heal tier unlock from real completion data, in case a
                // prior save never persisted an earned unlock (both caches are now loaded).
                BackfillTierUnlockOnLoad();
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
                LoadCommonWords(wordGraph, puzzleGenerator);

                // Task 6A — Economy manager (persisted via IDataManager).
                economyManager = new EconomyManager(dataManager);

                // Task 33 — full real economy: seed each puzzle's hint/reveal charges from the player's
                // OWNED inventory (falls back to BalanceConfig defaults until the economy finishes loading).
                stateManager.SetOwnedPowerUpProvider(() =>
                {
                    var p = economyManager?.GetCurrentProgress();
                    return p != null
                        ? (p.totalHintsEarned, p.totalRevealsEarned)
                        : (BalanceConfig.DefaultHintsPerPuzzle, BalanceConfig.DefaultRevealsPerPuzzle);
                });

                // Task 6B — Ad service + policy. AdService is a MonoBehaviour on this
                // GameObject (added in the Inspector or at runtime). Fall back to a no-op
                // so the game runs in Editor without an ad SDK.
                adService = GetComponent<IAdService>() ?? (IAdService)new NullAdService();
                adPolicy  = new AdPolicyService(adService);

                // Task 33 — initialize the economy, then apply the once-only starting inventory (5 each)
                // + the once-per-day grant (+2 each), and reflect the persisted remove-ads flag into the ad
                // policy. Fire-and-forget (like the original init); adPolicy is already constructed above.
                _ = InitializeEconomyAndGrantsAsync();

                // Task 9G — attempt to load a resumable snapshot (requires daily pool ready).
                LoadResumeSnapshotBlocking(dataManager);

                // Task 7 — wire juice subsystems after all systems + UI are ready.
                ApplyJuiceSettings();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize game systems: {ex.Message}\n{ex.StackTrace}");
                enabled = false;
            }
        }

        /// <summary>
        /// Task 5C — load Assets/Resources/Data/common_words.json (schema: {"words":[...]}).
        /// Words are added to the wordGraph (so they participate in adjacency) and the
        /// common-word filter is injected into the puzzle generator.
        /// Missing file is non-fatal: generation falls back to the full graph.
        /// </summary>
        private void LoadCommonWords(WordGraph wordGraph, PuzzleGenerator generator)
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/common_words");
            if (asset == null)
            {
                Debug.LogWarning("[CommonWords] common_words.json not found in Resources/Data/ — generation uses full graph.");
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<WordLibraryWrapper>(asset.text);
                if (wrapper?.words == null || wrapper.words.Length == 0)
                {
                    Debug.LogWarning("[CommonWords] common_words.json parsed but 'words' array is empty — skipping filter.");
                    return;
                }

                var set = new System.Collections.Generic.HashSet<string>();
                foreach (var word in wrapper.words)
                {
                    if (string.IsNullOrWhiteSpace(word)) continue;
                    string normalized = word.Trim().ToLowerInvariant();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^[a-z]+$")) continue;
                    wordGraph.AddWord(normalized);
                    set.Add(normalized);
                }

                generator.SetCommonWords(set);
                Debug.Log($"[CommonWords] loaded {set.Count} common words; generation filter active.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CommonWords] Failed to parse common_words.json: {ex.Message}");
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

            SetupShop(); // Task 33 — create the runtime Shop overlay + route the coin-pill tap to it.
            SetupDailyRewards(); // Task 36 36K — create the Daily Rewards overlay (login claim + streak repair).

            // Wire main menu mode selection
            uiManager.GetMainMenu().OnClassicModeSelected += StartClassicMode;
            uiManager.GetMainMenu().OnPuzzleShowSelected += StartPuzzleShowMode;
            uiManager.GetMainMenu().OnDailySelected += StartDailyMode;
            uiManager.GetMainMenu().OnTimeAttackSelected += ShowTimeAttackSetup;
            uiManager.GetMainMenu().OnLibrarySelected += ShowLibrary;
            uiManager.GetMainMenu().OnSettingsSelected += ShowSettings;
            uiManager.GetMainMenu().OnStatsSelected += ShowStats;
            uiManager.GetMainMenu().OnResumeSelected += ResumeGame;

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

            // Global top-right settings gear (shown on every screen by UIManager) → open Settings.
            uiManager.OnGlobalSettingsRequested += ShowSettings;

            // Wire settings screen
            if (uiManager.GetSettings() != null)
            {
                uiManager.GetSettings().OnBackToMenu += ShowMainMenu;
                uiManager.GetSettings().OnSettingsSaved += OnSettingsSaved;
                uiManager.GetSettings().OnResetProgressConfirmed += OnResetProgressConfirmed;
                uiManager.GetSettings().OnReplayTutorialRequested += OnReplayTutorialRequested;
            }

            // Task 9F — Stats screen back-button.
            if (uiManager.GetStats() != null)
                uiManager.GetStats().OnBackToMenu += ShowMainMenu;

            // Wire gameplay
            uiManager.GetGameplay().OnWordSubmitted += OnWordSubmitted;
            uiManager.GetGameplay().OnBackToMenu += ShowMainMenu;

            // Keystroke routing: each key dispatches through GameState so
            // state.currentInput is the single source of truth — no local wipe.
            uiManager.GetGameplay().OnLetterTyped += OnGameplayLetterTyped;
            uiManager.GetGameplay().OnBackspace += OnGameplayBackspace;

            // Phase 2: Wire power-up handlers
            uiManager.GetGameplay().OnHintUsed += OnHintUsed;
            uiManager.GetGameplay().OnRevealUsed += OnRevealUsed;
            uiManager.GetGameplay().OnUndoStep += OnUndoStep;
            uiManager.GetGameplay().OnAddTimeUsed += OnAddTimeUsed;

            // Task 16B — compact win-panel actions (endless Classic).
            uiManager.GetGameplay().OnNextPuzzle += OnWinNextPuzzle;
            uiManager.GetGameplay().OnWinHome += OnWinHome;

            // Wire results
            uiManager.GetResults().OnPlayAgain += PlayAgain;
            uiManager.GetResults().OnMainMenu += ShowMainMenu;
            uiManager.GetResults().OnShareRequested += ShareLastResult;
            uiManager.GetResults().OnNextTier += OnResultsNextTier;
            uiManager.GetResults().OnDoubleReward += OnDoubleDailyReward;
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
            uiManager.GetMainMenu().OnStatsSelected -= ShowStats;
            uiManager.GetMainMenu().OnResumeSelected -= ResumeGame;

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

            uiManager.OnGlobalSettingsRequested -= ShowSettings;

            if (uiManager.GetSettings() != null)
            {
                uiManager.GetSettings().OnBackToMenu -= ShowMainMenu;
                uiManager.GetSettings().OnSettingsSaved -= OnSettingsSaved;
                uiManager.GetSettings().OnResetProgressConfirmed -= OnResetProgressConfirmed;
                uiManager.GetSettings().OnReplayTutorialRequested -= OnReplayTutorialRequested;
            }

            if (uiManager.GetStats() != null)
                uiManager.GetStats().OnBackToMenu -= ShowMainMenu;

            if (tutorialOverlay != null)
            {
                tutorialOverlay.OnSkipRequested     -= HandleTutorialSkip;
                tutorialOverlay.OnSuccessBeatFinished -= HandleTutorialSuccess;
            }
            uiManager.GetGameplay().OnWordSubmitted -= OnWordSubmitted;
            uiManager.GetGameplay().OnBackToMenu -= ShowMainMenu;

            // Keystroke routing unsubscribe.
            uiManager.GetGameplay().OnLetterTyped -= OnGameplayLetterTyped;
            uiManager.GetGameplay().OnBackspace -= OnGameplayBackspace;

            // Phase 2: Unsubscribe power-up handlers
            uiManager.GetGameplay().OnHintUsed -= OnHintUsed;
            uiManager.GetGameplay().OnRevealUsed -= OnRevealUsed;
            uiManager.GetGameplay().OnUndoStep -= OnUndoStep;
            uiManager.GetGameplay().OnAddTimeUsed -= OnAddTimeUsed;
            uiManager.GetGameplay().OnNextPuzzle -= OnWinNextPuzzle;
            uiManager.GetGameplay().OnWinHome -= OnWinHome;

            uiManager.GetResults().OnPlayAgain -= PlayAgain;
            uiManager.GetResults().OnMainMenu -= ShowMainMenu;
            uiManager.GetResults().OnShareRequested -= ShareLastResult;
            uiManager.GetResults().OnNextTier -= OnResultsNextTier;
            uiManager.GetResults().OnDoubleReward -= OnDoubleDailyReward;
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

        // Task 33 — create the real-money store (mock in-editor) + the runtime Shop overlay, and route the
        // coin pill's tap to open it. The Shop is a full-screen overlay under the Canvas (no scene edit).
        private void SetupShop()
        {
            if (shopScreen != null) return;
            if (economyManager == null || uiManager == null) return;

            // Mock store for the Editor; real billing is the clearly-stubbed PlatformStoreServiceStub.
            storeService = new MockStoreService(
                economyManager,
                ShopCatalog.Load(),
                onRemoveAdsGranted:   () => { if (adPolicy != null) adPolicy.AdsRemoved = true; },
                onStarterPackGranted: () => { if (adPolicy != null) adPolicy.AdsRemoved = true; }); // 3-day ad-free window opens now

            var menu = uiManager.GetMainMenu();
            var canvas = menu != null ? menu.transform.parent as RectTransform : null;
            if (canvas == null) return;

            var go = new GameObject("ShopScreen", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            shopScreen = go.AddComponent<WordPuzzle.UI.ShopScreen>();
            shopScreen.Configure(economyManager, storeService, RefreshCoinPill,
                BalanceConfig.PowerUpBundleSizes, BalanceConfig.HintBundlePrices, BalanceConfig.UndoBundlePrices,
                BalanceConfig.RevealBundlePrices, BalanceConfig.TimeBundlePrices,
                watchCoinsRemaining: () => economyManager.WatchCoinsRemainingToday(dailyClock.TodayIso),
                watchForCoins: RequestWatchForCoins);
            go.SetActive(false);

            uiManager.OnShopRequested += OpenShop;
            RefreshCoinPill();
        }

        private void OpenShop()
        {
            if (shopScreen == null) return;
            RefreshCoinPill();
            shopScreen.Open();
        }

        private void RefreshCoinPill()
        {
            int coins = economyManager?.GetCurrentProgress()?.totalCoins ?? 0;
            uiManager?.SetCoinBalance(coins);
        }

        private void ShowMainMenu()
        {
            activeMode = null;
            isDailyRun = false;
            pendingDailyPuzzle = null;
            pendingDailyIndex = -1;
            // Task 16 — tear down any compact win panel state.
            awaitingClassicNext = false;
            uiManager.GetGameplay()?.HideWinPanel();
            uiManager.ShowMainMenu();
            RefreshCoinPill();
            RefreshDailyButtonState();
            RefreshResumeAffordance();
            MaybeShowDailyRewards(); // Task 36 36K — login claim / streak repair overlay when applicable
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
            DailyStreakRules.RefreshPlayedFlag(cachedDailyProgress, dailyClock.TodayIso);
            // Daily 2.0 — one-and-done: lock the daily once it has been PLAYED (win OR loss), not only solved.
            menu.SetDailyState(cachedDailyProgress.todayPlayed, cachedDailyProgress.currentStreak);
        }

        // ── Task 36 36K — Daily Rewards overlay (login claim + streak repair) ──────────────
        private void SetupDailyRewards()
        {
            if (dailyRewardPopup != null) return;
            if (uiManager == null) return;
            var menu = uiManager.GetMainMenu();
            var canvas = menu != null ? menu.transform.parent as RectTransform : null;
            if (canvas == null) return;

            var go = new GameObject("DailyRewardPopup", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            dailyRewardPopup = go.AddComponent<WordPuzzle.UI.DailyRewardPopup>();
            dailyRewardPopup.Configure(onClosed: RefreshCoinPill);
            go.SetActive(false);
        }

        // Show the overlay when a login reward is claimable and/or a slipped streak can be repaired.
        // No-op until the economy has loaded, so a launch race never NREs.
        private void MaybeShowDailyRewards()
        {
            if (dailyRewardPopup == null || economyManager == null) return;
            var prog = economyManager.GetCurrentProgress();
            if (prog == null) return;   // economy still loading — retry on the next menu visit

            string today = dailyClock.TodayIso;
            bool loginAvail = economyManager.IsLoginRewardAvailable(today);
            int loginCoins = economyManager.PeekLoginRewardCoins();
            int loginDay = prog.loginRewardIndex + 1;   // 1-based position in the 7-day cycle

            bool repairAvail = cachedDailyProgress != null && DailyStreakRules.CanRepair(
                today, cachedDailyProgress.lastPlayedDateIso, cachedDailyProgress.lastRepairDateIso,
                BalanceConfig.StreakRepairCooldownDays);

            if (!loginAvail && !repairAvail) return;

            bool affordable = prog.totalCoins >= BalanceConfig.StreakRepairCoinCost;
            bool adReady = adService != null && adService.IsRewardedReady;

            dailyRewardPopup.ShowRewards(
                loginAvail, loginCoins, loginDay, cb => ClaimLoginReward(cb),
                repairAvail, BalanceConfig.StreakRepairCoinCost, affordable, adReady,
                (useAd, cb) => RepairStreak(useAd, cb));
        }

        private async void ClaimLoginReward(System.Action<int> cb)
        {
            int coins = 0;
            if (economyManager != null)
            {
                try { coins = await economyManager.ClaimLoginRewardAsync(dailyClock.TodayIso); }
                catch (System.Exception ex) { Debug.LogWarning($"[Economy] login reward claim failed: {ex.Message}"); }
            }
            RefreshCoinPill();
            cb?.Invoke(coins);
        }

        // Repair yesterday's missed streak via coins or a rewarded ad. Bridge-only (does NOT mark today
        // played — Q3). The coin path spends StreakRepairCoinCost; the ad path needs a real ad service.
        private void RepairStreak(bool useAd, System.Action<bool> cb)
        {
            if (cachedDailyProgress == null) { cb?.Invoke(false); return; }
            string today = dailyClock.TodayIso;
            int cooldown = BalanceConfig.StreakRepairCooldownDays;
            if (!DailyStreakRules.CanRepair(today, cachedDailyProgress.lastPlayedDateIso,
                    cachedDailyProgress.lastRepairDateIso, cooldown))
            {
                cb?.Invoke(false);
                return;
            }

            if (useAd)
            {
                if (adService == null || !adService.IsRewardedReady) { cb?.Invoke(false); return; }
                adService.ShowRewarded(
                    onRewarded: () => { DoRepairApply(today, cooldown); cb?.Invoke(true); },
                    onClosed: () => { });
            }
            else
            {
                _ = RepairWithCoins(today, cooldown, cb);
            }
        }

        private async System.Threading.Tasks.Task RepairWithCoins(string today, int cooldown, System.Action<bool> cb)
        {
            bool ok = false;
            if (economyManager != null)
            {
                try { ok = await economyManager.SpendCoinsAsync(BalanceConfig.StreakRepairCoinCost, "streak_repair"); }
                catch (System.Exception ex) { Debug.LogWarning($"[Economy] streak repair spend failed: {ex.Message}"); }
            }
            if (ok) { DoRepairApply(today, cooldown); RefreshCoinPill(); }
            cb?.Invoke(ok);
        }

        private void DoRepairApply(string today, int cooldown)
        {
            DailyStreakRules.ApplyRepair(cachedDailyProgress, today, cooldown);
            if (dataManagerRef != null)
            {
                try { _ = dataManagerRef.SaveDailyProgressAsync(cachedDailyProgress); }
                catch (System.Exception ex) { Debug.LogError($"[Daily] repair persist failed: {ex.Message}"); }
            }
            RefreshDailyButtonState();   // reflect the bridged streak on the Daily button
        }

        private void ShowLibrary()
        {
            // Task 15C — inject saved Puzzle Show progress so the library colours cards by
            // real completion state and shows accurate tier unlock/progress.
            var lib = uiManager.GetLibrary();
            if (lib != null && cachedPuzzleProgress != null)
            {
                lib.SetProgress(
                    cachedPuzzleProgress.completedPuzzleIds,
                    cachedPuzzleProgress.inProgressPuzzleIds,
                    cachedPuzzleProgress.currentTier);
            }
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

            // Task 7A — sync motion gate + sfx volume immediately.
            UIAnimations.ReduceMotion = cachedSettings.reduceMotion;
            if (sfxManager != null) sfxManager.SetSettings(cachedSettings);

            // Task 9E — re-apply accessible palette on every settings save.
            WordPuzzle.UI.AccessiblePalette.Apply(cachedSettings);

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
            if (OnboardingRules.ShouldRouteToTutorial(cachedOnboarding))
            {
                StartTutorialRun();
                return;
            }
            StartNormalClassic();
        }

        private void StartNormalClassic()
        {
            isTutorialRun = false;
            pendingTutorialPuzzle = null;
            isDailyRun = false;
            pendingDailyPuzzle = null;
            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
        }

        private void StartTutorialRun()
        {
            isTutorialRun = true;
            isDailyRun = false;
            pendingDailyPuzzle = null;
            pendingTutorialPuzzle = TutorialPuzzle.Create();

            if (tutorialOverlay == null)
            {
                // Never strand the player — skip straight to the real puzzle.
                CompleteTutorial(false);
                return;
            }

            activeMode = new ClassicMode();
            modeController.SetMode(activeMode);
            StartNewGame();
            EnsureTutorialOverlaySubscribed();
            tutorialOverlay.Begin();
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
            // Task 20A — reconcile the authoritative unlocked tier from the just-updated completion
            // set BEFORE saving, so a threshold-crossing completion persists the unlock (the mode
            // advances currentTier only AFTER firing this event, so we cannot trust its value here).
            bool playerChanged = RaiseTierUnlockInMemory();
            try
            {
                await dataManagerRef.SavePuzzleProgressAsync(cachedPuzzleProgress);
                if (playerChanged && cachedPlayerProgress != null)
                    await dataManagerRef.UpdatePlayerProgressAsync(cachedPlayerProgress);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to persist puzzle progress: {ex.Message}");
            }
        }

        // ── Task 20 — single source of truth for tier unlock (reconciled from completion data) ──

        /// <summary>
        /// Raise the authoritative unlock state to match earned completions: bumps
        /// PuzzleProgressData.currentTier (what the Library reads) AND PlayerProgress.highestTierUnlocked
        /// (kept in sync so the two never drift), and refreshes the in-memory tier cache so a live
        /// ShowLibrary reflects it. Pure in-memory + idempotent; NEVER lowers an unlocked tier. No I/O.
        /// Returns true if PlayerProgress.highestTierUnlocked changed (so the caller can persist it).
        /// </summary>
        private bool RaiseTierUnlockInMemory()
        {
            if (cachedPuzzleProgress == null || tierPuzzleIdLookup == null) return false;

            var done = new System.Collections.Generic.HashSet<int>(
                cachedPuzzleProgress.completedPuzzleIds ?? new System.Collections.Generic.List<int>());
            int reconciled = PuzzleShowMode.ReconcileHighestUnlockedTier(done, tierPuzzleIdLookup);

            if (reconciled > cachedPuzzleProgress.currentTier)
                cachedPuzzleProgress.currentTier = reconciled;   // never lower

            int unlockedThrough = cachedPuzzleProgress.currentTier;

            bool playerChanged = false;
            if (cachedPlayerProgress != null && unlockedThrough > cachedPlayerProgress.highestTierUnlocked)
            {
                cachedPlayerProgress.highestTierUnlocked = unlockedThrough;
                playerChanged = true;
            }

            // Refresh the in-memory tier cache (cascade) so the Library reflects the unlock live.
            if (tierCacheRef != null)
                foreach (var kvp in tierCacheRef)
                    if (kvp.Key <= unlockedThrough && kvp.Value != null)
                        kvp.Value.isUnlocked = true;

            return playerChanged;
        }

        // Task 20B — on boot, self-heal a save whose earned completions outrank its stored unlock,
        // then persist the corrected values via the existing seams.
        private async void BackfillTierUnlockOnLoad()
        {
            int beforeTier = cachedPuzzleProgress?.currentTier ?? 1;
            bool playerChanged = RaiseTierUnlockInMemory();
            bool puzzleChanged = (cachedPuzzleProgress?.currentTier ?? 1) != beforeTier;
            if (!puzzleChanged && !playerChanged) return;
            try
            {
                if (puzzleChanged && cachedPuzzleProgress != null)
                    await dataManagerRef.SavePuzzleProgressAsync(cachedPuzzleProgress);
                if (playerChanged && cachedPlayerProgress != null)
                    await dataManagerRef.UpdatePlayerProgressAsync(cachedPlayerProgress);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Tier-unlock backfill persist failed: {ex.Message}");
            }
        }

        // Spec §3.7 — unlock new tier in the in-memory tier cache (do NOT touch JSON).
        private void OnPuzzleShowTierAdvanced(int oldTier, int newTier)
        {
            // Task 16 — remember the freshly unlocked tier so the results screen can offer "Tier N ▸".
            if (newTier <= BalanceConfig.MaxTier) pendingPuzzleShowNextTier = newTier;
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
            DailyStreakRules.RefreshPlayedFlag(cachedDailyProgress, dailyClock.TodayIso);
        }

        // Task 3A — load OnboardingData at boot (mirrors LoadDailyProgressBlocking).
        private void LoadOnboardingBlocking(IDataManager dataManager)
        {
            try
            {
                var task = dataManager.LoadOnboardingAsync();
                cachedOnboarding = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Onboarding load failed; using defaults: {ex.Message}");
                cachedOnboarding = new OnboardingData();
            }
            if (cachedOnboarding == null) cachedOnboarding = new OnboardingData();
        }

        // Task 3A — complete the tutorial (called by overlay events or fallback).
        private async void CompleteTutorial(bool skipped)
        {
            isTutorialRun = false;
            pendingTutorialPuzzle = null;
            cachedOnboarding = OnboardingRules.MarkCompleted(cachedOnboarding, skipped);
            if (dataManagerRef != null)
            {
                try { await dataManagerRef.SaveOnboardingAsync(cachedOnboarding); }
                catch (System.Exception ex) { Debug.LogError($"[Tutorial] persist failed: {ex.Message}"); }
            }
            if (tutorialOverlay != null) tutorialOverlay.Hide();
            StartNormalClassic();
        }

        // Task 3A — settings screen "Replay Tutorial" handler.
        private async void OnReplayTutorialRequested()
        {
            cachedOnboarding = OnboardingRules.Reset(cachedOnboarding);
            if (dataManagerRef != null)
            {
                try { await dataManagerRef.SaveOnboardingAsync(cachedOnboarding); }
                catch (System.Exception ex) { Debug.LogError($"[Tutorial] reset persist failed: {ex.Message}"); }
            }
            // Next time Classic is tapped, the gate in StartClassicMode reroutes to tutorial.
        }

        // Task 3A — subscribe overlay events exactly once.
        private void EnsureTutorialOverlaySubscribed()
        {
            if (tutorialOverlaySubscribed || tutorialOverlay == null) return;
            tutorialOverlay.OnSkipRequested      += HandleTutorialSkip;
            tutorialOverlay.OnSuccessBeatFinished += HandleTutorialSuccess;
            tutorialOverlaySubscribed = true;
        }

        private void HandleTutorialSkip()    => CompleteTutorial(true);
        private void HandleTutorialSuccess() => CompleteTutorial(false);

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
            ApplyJuiceSettings();
        }

        // Task 7 — apply motion/haptics/sfx settings from cachedSettings.
        private void ApplyJuiceSettings()
        {
            if (cachedSettings == null) return;

            // 7A — motion gate (static field read by UIAnimations + LetterTile coroutines).
            UIAnimations.ReduceMotion = cachedSettings.reduceMotion;

            // Task 9E (boot wire) — seed accessible palette so colorblind/large-text
            // are active before the player ever opens Settings.
            WordPuzzle.UI.AccessiblePalette.Apply(cachedSettings);

            // 7B — haptics: construct once; use NullHaptics on platforms without Handheld.
            if (haptics == null)
            {
#if UNITY_ANDROID || UNITY_IOS
                haptics = new HandheldHaptics(() => cachedSettings != null && cachedSettings.hapticsEnabled);
#else
                haptics = new NullHaptics();
#endif
                var gameplay = uiManager?.GetGameplay();
                if (gameplay != null) gameplay.SetHaptics(haptics);
            }

            // 7C — sfx: GetComponent fallback if not serialized.
            if (sfxManager == null) sfxManager = GetComponent<SfxManager>();
            if (sfxManager != null)
            {
                sfxManager.SetSettings(cachedSettings);
                var gameplay = uiManager?.GetGameplay();
                if (gameplay != null) gameplay.SetSfxManager(sfxManager);
            }
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
            lastTimeAttackConfig = cfg;            // Task 16 — remember for results "Play Again"
            timeAttackLaddersCompleted = 0;        // fresh run
            isDailyRun = false;
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

        // Task 33 — boot economy sequence: init -> starting inventory (once) -> daily grant (once/day)
        // -> reflect the persisted remove-ads flag into the ad policy so interstitials stay suppressed.
        private async System.Threading.Tasks.Task InitializeEconomyAndGrantsAsync()
        {
            if (economyManager == null) return;
            await economyManager.InitializeAsync();
            await economyManager.ApplyStartingInventoryIfNeeded();
            await economyManager.GrantDailyIfDue(dailyClock.TodayIso);

            var prog = economyManager.GetCurrentProgress();
            // Suppress interstitials if ads are permanently removed OR a temporary ad-free window
            // (e.g. the Starter Pack's 3-day window) is still active. Recomputed each boot.
            long nowUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (adPolicy != null && prog != null && (prog.removeAds || economyManager.IsAdFreeActive(nowUnix)))
                adPolicy.AdsRemoved = true;

            // 36K — the login reward may be claimable on launch. The menu is already showing by the time the
            // economy finishes loading, so surface the Daily Rewards overlay now if we're on the menu.
            var menu = uiManager?.GetMainMenu();
            if (menu != null && menu.gameObject.activeInHierarchy) MaybeShowDailyRewards();
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

        /// <summary>
        /// Spec §2 — Classic mode picks a random word length in [3,7] and attempts to
        /// generate a solvable ladder.  If generation fails for length L it retries with
        /// L-1, L-2, … down to 3.  Length 3 always falls through to the hard-coded
        /// fallback inside PuzzleGenerator, so a non-null result is guaranteed.
        /// </summary>
        private PuzzleDefinition GenerateClassicPuzzle()
        {
            int length = UnityEngine.Random.Range(3, 8); // [3, 7] inclusive
            for (int l = length; l >= 3; l--)
            {
                var def = puzzleGenerator.GenerateRandomPuzzleOfLength(l);
                if (def != null && def.startWord.Length == l && def.endWord.Length == l
                    && def.startWord != def.endWord)
                    return def;
            }
            return puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        }

        private void StartNewGame()
        {
            PuzzleDefinition puzzleDefinition;
            Difficulty difficulty;

            if (isTutorialRun && pendingTutorialPuzzle != null)
            {
                puzzleDefinition = pendingTutorialPuzzle;
                difficulty = Difficulty.Easy;
            }
            else if (isDailyRun && pendingDailyPuzzle != null)
            {
                // Task 1A — deterministic daily puzzle.
                puzzleDefinition = pendingDailyPuzzle;
                difficulty = Difficulty.Easy;
            }
            else if (activeMode is PuzzleShowMode psm)
            {
                // Task 18E — prefer a puzzle the player hasn't completed yet so "Next Puzzle"
                // advances through the tier; fall back to any tier puzzle if all are done.
                var completed = new System.Collections.Generic.HashSet<int>(psm.CompletedPuzzleIds);
                puzzleDefinition = puzzleGenerator.GetUnplayedTierPuzzle(psm.CurrentTier, completed)
                                   ?? puzzleGenerator.GetRandomTierPuzzle(psm.CurrentTier);
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
                puzzleDefinition = GenerateClassicPuzzle();
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

            // Daily 2.0 (Task 36) — arm the par-scored daily AFTER StartNewPuzzle (which resets the
            // working state). par = the puzzle's validated optimal step count.
            if (isDailyRun)
                stateManager.ConfigureDailyRun(BalanceConfig.DailyMistakeBudget, puzzleDefinition.optimalSteps);

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
                uiManager.GetGameplay().ShowFeedback("", Color.green);
            }
            else
            {
                uiManager.GetGameplay().ShowFeedback("", Color.red);
            }
        }

        // Spec §1 — relay GameStateManager.OnWordSubmissionResult to the UI feedback layer.
        // Also fires Task 7 juice hooks (accept/reject sfx + haptics).
        private void OnWordSubmissionResultHandler(WordPuzzle.State.SubmissionResult result)
        {
            var gameplay = uiManager?.GetGameplay();
            if (gameplay == null) return;

            if (result.accepted)
            {
                gameplay.ShowFeedback(string.IsNullOrEmpty(result.reason) ? "" : result.reason, Color.green);
                // Task 7 — acceptance juice (sfx + haptics + glow settle).
                gameplay.OnWordAccepted();
            }
            else
            {
                if (!string.IsNullOrEmpty(result.reason))
                    gameplay.ShowFeedback(result.reason, Color.red);
                // Task 7 — rejection juice (shake gated on ReduceMotion inside, sfx + buzz).
                gameplay.OnWordRejected();
            }

            if (isTutorialRun && tutorialOverlay != null)
            {
                var st = stateManager.GetCurrentState();
                bool reachedEnd = st.wordChain != null
                    && st.wordChain.Count > 0
                    && string.Equals(
                        st.wordChain[st.wordChain.Count - 1],
                        pendingTutorialPuzzle.endWord,
                        System.StringComparison.OrdinalIgnoreCase);
                tutorialOverlay.OnSubmission(result.accepted, reachedEnd);
            }
        }

        // Keystroke dispatch handlers — route keyboard taps through GameState so
        // state.currentInput is authoritative and UpdateGameplayUI never wipes typed letters.
        private void OnGameplayLetterTyped(char c)
        {
            stateManager.Dispatch(new PressLetterAction(c));
            UpdateGameplayUI();
        }

        private void OnGameplayBackspace()
        {
            stateManager.Dispatch(new DeleteLetterAction());
            UpdateGameplayUI();
        }

        // Phase 2: Power-up event handlers
        private void OnHintUsed()
        {
            int before = stateManager.GetCurrentState().hintsRemaining;
            stateManager.Dispatch(new UseHintAction(0));
            // Task 33 — a consumed hint also spends one from the persisted OWNED inventory.
            if (stateManager.GetCurrentState().hintsRemaining < before)
                _ = economyManager?.UseHintAsync();
            UpdatePowerUpUI();
        }

        private void OnRevealUsed()
        {
            int before = stateManager.GetCurrentState().revealsRemaining;
            stateManager.Dispatch(new UseRevealAction());
            // Task 33 — a consumed reveal also spends one from the persisted OWNED inventory.
            if (stateManager.GetCurrentState().revealsRemaining < before)
                _ = economyManager?.UseRevealAsync();
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

            // Daily 2.0 (Task 36) — show "Par N · Mistakes left: M" during the daily; release otherwise.
            if (isDailyRun)
                gameplay.SetDailyPar(stateManager.GetDailyPar(), stateManager.GetMistakesRemaining());
            else
                gameplay.SetDailyPar(-1, -1);

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
            // Tutorial completion is driven by the overlay success beat, not EndGame.
            if (isTutorialRun) return;
            if (activeMode == null) return;
            // Task 16 — compact win panel is up (endless Classic); wait for the player's choice.
            if (awaitingClassicNext) return;

            // Resolve the active mode family + completion signals, then ask the pure router.
            ModeKind kind;
            bool timeUp = false;
            var tam = activeMode as TimeAttackMode;
            if (tam != null) { kind = ModeKind.TimeAttack; timeUp = tam.IsGameOver(); }
            else if (activeMode is PuzzleShowMode) kind = ModeKind.PuzzleShow;
            else kind = ModeKind.Classic;

            bool puzzleComplete = IsCurrentPuzzleComplete();

            // Daily 2.0 (Task 36) — running out of mistakes FAILS the daily (not a win, but the run is
            // over). The pure router only knows win/timeUp, so detect the fail here and end the run.
            if (isDailyRun)
            {
                var dailyResult = stateManager?.GetDailyResult();
                if (dailyResult.HasValue && dailyResult.Value.failed) { EndGame(); return; }
            }

            switch (PostWinRouter.Decide(kind, isDailyRun, puzzleComplete, timeUp))
            {
                case PostWinSurface.AdvanceNextLadder:
                    timeAttackLaddersCompleted++;
                    uiManager?.GetGameplay()?.OnGameWon();
                    StartNewGame();         // next ladder, same run (timer preserved via timerSeeded)
                    break;
                case PostWinSurface.CompactWinPanel:
                    ShowClassicWinPanel();  // endless Classic → compact inline panel
                    break;
                case PostWinSurface.FullResults:
                    EndGame();              // Daily / Puzzle Show / Time Attack run-end
                    break;
                // PostWinSurface.None: nothing to do this frame.
            }
        }

        private bool IsCurrentPuzzleComplete()
        {
            var s = stateManager?.GetCurrentState();
            return s != null && s.IsPuzzleComplete;
        }

        // Task 16B — endless Classic: overlay the compact win panel and grant the normal
        // completion bookkeeping (coins/stats) WITHOUT routing to the full results page.
        private void ShowClassicWinPanel()
        {
            awaitingClassicNext = true;

            int steps = 0;
            var s = stateManager?.GetCurrentState();
            if (s?.wordChain != null) steps = Mathf.Max(0, s.wordChain.Count - 1);

            // Completion bookkeeping (mirrors EndGame, minus results/ads): stats, coins, clear resume.
            IncrementModeStats(true, activeMode);
            cachedResumeSnapshot = null; cachedResumePuzzle = null; cachedResumeMode = null;
            GrantPuzzleReward(false);
            adPolicy?.RecordPuzzleCompleted();

            uiManager.GetGameplay().ShowWinPanel(steps);
        }

        // Task 16B — compact panel "Next Puzzle": stay in Classic, fresh clean board.
        private void OnWinNextPuzzle()
        {
            awaitingClassicNext = false;
            uiManager.GetGameplay().HideWinPanel();
            StartNewGame();   // activeMode is still the live ClassicMode
        }

        // Task 16B — compact panel "Home".
        private void OnWinHome()
        {
            awaitingClassicNext = false;
            uiManager.GetGameplay().HideWinPanel();
            ShowMainMenu();
        }

        // Task 16 — Puzzle Show results "Tier N ▸": open the library tier-select so the
        // player can step into the newly unlocked tier (explicit upgrade choice).
        private void OnResultsNextTier()
        {
            ShowLibrary();
        }

        private void EndGame()
        {
            // Task 7 — win juice (sfx + buzz).
            uiManager?.GetGameplay()?.OnGameWon();

            // Snapshot daily-run state before we tear down, then route to results.
            bool wasDailyRun = isDailyRun;
            int dailyIndex = pendingDailyIndex;

            // Task 2A — capture the share input BEFORE the active mode is cleared.
            // (state.wordChain / current puzzle are still valid here.)
            CaptureShareInput(wasDailyRun, dailyIndex);

            isDailyRun = false;
            pendingDailyPuzzle = null;
            pendingDailyIndex = -1;

            var snapshotMode = activeMode;
            activeMode = null;
            var stats = modeController.GetCurrentStats();
            uiManager.GetResults().DisplayStats(stats);

            // Task 9F — increment mode-specific counters before showing results.
            // Determine win: chain tail equals endWord.
            bool isWin = false;
            try
            {
                var curState = stateManager.GetCurrentState();
                if (curState?.wordChain != null && curState.wordChain.Count > 0 &&
                    curState.puzzle != null)
                {
                    string tail = curState.wordChain[curState.wordChain.Count - 1];
                    isWin = string.Equals(tail, curState.puzzle.endWord,
                        System.StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* state may be unavailable; treat as loss */ }
            IncrementModeStats(isWin, snapshotMode);

            // Task 9G — clear resume cache so MainMenu won't offer stale "Resume".
            cachedResumeSnapshot = null;
            cachedResumePuzzle   = null;
            cachedResumeMode     = null;

            if (wasDailyRun)
            {
                RecordDailyCompletionAndSurface(dailyIndex, isWin);
            }

            // Task 6A — Award puzzle-completion coins. Daily run stacks an extra bonus.
            GrantPuzzleReward(wasDailyRun);

            // Task 6B — Tick ad policy; may show an interstitial between sessions.
            // Always between-session (activeMode is already null), never mid-puzzle.
            adPolicy?.RecordPuzzleCompleted();
            adPolicy?.TryShowInterstitial();

            // Task 16 — configure which post-win actions the full results page offers.
            ConfigureResultsSurface(snapshotMode, wasDailyRun);

            uiManager.ShowResults();
        }

        // Task 16 — context-aware results buttons + remember how "Play Again" should route.
        private void ConfigureResultsSurface(IGameMode snapshotMode, bool wasDailyRun)
        {
            var results = uiManager.GetResults();
            if (wasDailyRun)
            {
                lastWinContext = PostWin.Daily;
                results.ConfigureForDaily();                 // no "Play Again"; Home only
            }
            else if (snapshotMode is PuzzleShowMode)
            {
                lastWinContext = PostWin.PuzzleShow;
                bool hasNextTier = pendingPuzzleShowNextTier > 0
                    && pendingPuzzleShowNextTier <= BalanceConfig.MaxTier;
                results.ConfigureForPuzzleShow(hasNextTier, pendingPuzzleShowNextTier);
                pendingPuzzleShowNextTier = 0; // consumed
            }
            else // Time Attack run-end (Classic never reaches EndGame — it uses the panel).
            {
                lastWinContext = PostWin.TimeAttack;
                results.ConfigureForEndless(timeAttackLaddersCompleted); // "Play Again" → new run
            }
        }

        /// <summary>Task 6A — add completion coins (and daily bonus) via the economy manager.</summary>
        private async void GrantPuzzleReward(bool isDaily)
        {
            if (economyManager == null) return;
            // Daily payout is the par-scaled reward (granted in RecordDailyCompletionAndSurface); skip the
            // flat completion + bonus here so the daily isn't double-paid — and a FAILED daily (which now
            // also reaches EndGame) never earns a completion reward.
            if (isDaily) return;
            try
            {
                await economyManager.AddCoinsAsync(BalanceConfig.PuzzleCompletionReward, "puzzle_completion");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Economy] GrantPuzzleReward failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Task 6B — Opt-in rewarded ad: show the "Watch ad for +1 Hint" prompt.
        /// Called from gameplay UI (e.g. when hints run out). Never auto-invoked.
        /// </summary>
        public void RequestRewardedHintAd()
        {
            if (adService == null || !adService.IsRewardedReady) return;
            adService.ShowRewarded(
                onRewarded: async () =>
                {
                    if (economyManager == null) return;
                    try { await economyManager.AddHintsAsync(BalanceConfig.RewardedAdHintGrant, "rewarded_ad"); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Economy] rewarded hint grant failed: {ex.Message}"); }
                },
                onClosed: () => { /* UI can re-enable the hint button here */ }
            );
        }

        /// <summary>
        /// Task 6B — Opt-in rewarded "Continue" in Time Attack.
        /// Grants SurvivalRewardSeconds to the active TimeAttackMode's clock.
        /// Must only be called after time expires; never mid-puzzle.
        /// </summary>
        public void RequestRewardedContinue()
        {
            if (adService == null || !adService.IsRewardedReady) return;
            if (!(activeMode is TimeAttackMode tam)) return;

            adService.ShowRewarded(
                onRewarded: () =>
                {
                    stateManager?.ConfigureAddTimePowerUp(0, 0); // charges already consumed
                    tam.GrantContinueSeconds(BalanceConfig.SurvivalRewardSeconds);
                },
                onClosed: () => { }
            );
        }

        // Task 2A — assemble the ShareInput from the just-finished run.
        private void CaptureShareInput(bool wasDailyRun, int dailyIndex)
        {
            try
            {
                var state = stateManager?.GetCurrentState();
                if (state == null || state.puzzle == null) { lastShareInput = null; return; }

                var input = new ShareCardBuilder.ShareInput
                {
                    startWord = state.puzzle.startWord,
                    endWord = state.puzzle.endWord,
                    chain = new System.Collections.Generic.List<string>(state.wordChain ?? new System.Collections.Generic.List<string>()),
                    totalTimeSeconds = state.elapsedTime,
                };

                if (wasDailyRun)
                {
                    input.mode = ShareCardBuilder.ModeKind.Daily;
                    input.dailyIndex = dailyIndex >= 0 ? dailyIndex : (int?)null;
                    if (cachedDailyProgress != null)
                    {
                        // Streak values reflect post-completion state after RecordDailyCompletionAndSurface;
                        // for the share payload we use the post-apply values (incremented streak).
                        input.streakCurrent = cachedDailyProgress.currentStreak;
                        input.streakBest = cachedDailyProgress.longestStreak;
                    }

                    // Phase 4 — capture the path-shape data (still live before teardown): per-step row
                    // classes + the par-scored result. Drives the SHAPE-ONLY daily card (no words).
                    var stepClasses = stateManager?.GetDailyStepClasses();
                    if (stepClasses != null)
                        input.dailyStepClasses = new System.Collections.Generic.List<int>(stepClasses);
                    var dr = stateManager?.GetDailyResult();
                    if (dr.HasValue)
                    {
                        input.par = dr.Value.par;
                        input.playerSteps = dr.Value.playerSteps;
                        input.stars = dr.Value.stars;
                        input.dailyFailed = dr.Value.failed;
                    }
                }
                else if (activeMode is PuzzleShowMode psm)
                {
                    input.mode = ShareCardBuilder.ModeKind.PuzzleShow;
                    input.puzzleShowTier = psm.CurrentTier;
                }
                else if (activeMode is TimeAttackMode tam)
                {
                    input.mode = ShareCardBuilder.ModeKind.TimeAttack;
                    var cfg = tam.Config;
                    if (cfg != null)
                    {
                        input.timeAttackBaseSeconds = Mathf.RoundToInt(cfg.baseTimeSeconds);
                        input.timeAttackSurvival = cfg.subMode == TimeAttackSubMode.Survival;
                    }
                }
                else
                {
                    input.mode = ShareCardBuilder.ModeKind.Classic;
                }

                lastShareInput = input;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Share] CaptureShareInput failed: {ex.Message}");
                lastShareInput = null;
            }
        }

        private void ShareLastResult()
        {
            if (lastShareInput == null)
            {
                Debug.LogWarning("[Share] No share input available; nothing to copy.");
                return;
            }
            // For daily runs, RecordDailyCompletionAndSurface runs after CaptureShareInput
            // and updates the streak in-place. Pull the latest values just before sharing.
            if (lastShareInput.mode == ShareCardBuilder.ModeKind.Daily && cachedDailyProgress != null)
            {
                lastShareInput.streakCurrent = cachedDailyProgress.currentStreak;
                lastShareInput.streakBest = cachedDailyProgress.longestStreak;
            }
            string text = ShareCardBuilder.Build(lastShareInput);
            bool ok = shareService.Share(text);
            uiManager.GetResults().ShowToast(ok ? "Copied!" : "Copy failed");
        }

        // Task 1B/1C — apply streak rules + persist + drive the ResultsScreen daily widgets.
        private async void RecordDailyCompletionAndSurface(int puzzleIndex, bool solved)
        {
            string todayIso = dailyClock.TodayIso;
            if (cachedDailyProgress == null) cachedDailyProgress = new DailyProgress();
            bool alreadyPlayedToday = cachedDailyProgress.lastPlayedDateIso == todayIso;

            // Daily 2.0 (Task 36) — a PLAYED day (solve OR fail) advances the streak and a fail keeps it.
            // ApplyPlayed is the streak authority now, replacing the old completion-only ApplyCompletion.
            DailyStreakRules.ApplyPlayed(cachedDailyProgress, todayIso, solved);
            cachedDailyProgress.todayPuzzleIndex = puzzleIndex;

            if (dataManagerRef != null)
            {
                try { await dataManagerRef.SaveDailyProgressAsync(cachedDailyProgress); }
                catch (System.Exception ex) { Debug.LogError($"[Daily] persist failed: {ex.Message}"); }
            }

            // Surface the par-scored result (grade/stars or "Failed today") alongside the streak.
            var pathScore = stateManager?.GetDailyResult();
            if (pathScore.HasValue)
            {
                var ps = pathScore.Value;
                uiManager.GetResults().ShowDailyResult(ps.stars, ps.par, ps.playerSteps, ps.failed,
                    puzzleIndex, cachedDailyProgress.currentStreak);

                // Phase 5 (36K) — par-scaled daily coin reward (replaces the flat daily bonus). Granted on
                // solve OR fail (Failed = consolation); GrantPuzzleReward skips the daily to avoid double-pay.
                if (economyManager != null)
                {
                    try
                    {
                        int reward = BalanceConfig.DailyCoinReward(ps.stars, ps.failed);
                        await economyManager.AddCoinsAsync(reward, "daily_par");
                        lastDailyRewardCoins = reward;      // the doubler re-grants exactly this much
                        dailyDoublerConsumed = false;

                        // 36K — one-time streak-milestone coin pop (7/30/100); surface it as a toast.
                        int milestonePaid = await economyManager.AwardStreakMilestonesAsync(cachedDailyProgress.currentStreak);
                        if (milestonePaid > 0)
                            uiManager.GetResults().ShowToast($"{cachedDailyProgress.currentStreak}-day streak!  +{milestonePaid} coins");
                    }
                    catch (System.Exception ex) { Debug.LogWarning($"[Economy] daily reward/milestone failed: {ex.Message}"); }

                    // 36K — offer the reward doubler whenever a reward was granted (rewarded-ad faucet).
                    uiManager.GetResults().ConfigureDailyDoubler(lastDailyRewardCoins > 0);
                }
            }

            uiManager.GetResults().ShowDailyStreak(
                cachedDailyProgress.currentStreak,
                cachedDailyProgress.longestStreak,
                alreadyPlayedToday);
        }

        // Task 36 36K — daily reward doubler: watch a rewarded ad to add today's daily reward a 2nd time.
        private void OnDoubleDailyReward()
        {
            if (economyManager == null || lastDailyRewardCoins <= 0 || dailyDoublerConsumed) return;
            if (adService == null || !adService.IsRewardedReady)
            {
                uiManager.GetResults().ShowToast("Rewarded ads aren't available yet");
                return;
            }
            adService.ShowRewarded(
                onRewarded: async () =>
                {
                    dailyDoublerConsumed = true;
                    int bonus = lastDailyRewardCoins;
                    try { await economyManager.AddCoinsAsync(bonus, "daily_doubler"); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Economy] doubler grant failed: {ex.Message}"); }
                    RefreshCoinPill();
                    uiManager.GetResults().MarkDoublerClaimed($"+{bonus} bonus coins!");
                },
                onClosed: () => { });
        }

        // Task 36 36K — watch-for-coins faucet (invoked from the Shop). Plays a rewarded ad, grants the
        // capped daily watch reward, and reports the coins back so the Shop can refresh. Reports 0 when no
        // ad is available (NullAdService) so the Shop shows a graceful "not available yet" message.
        private void RequestWatchForCoins(System.Action<int> onResult)
        {
            if (economyManager == null) { onResult?.Invoke(0); return; }
            if (adService == null || !adService.IsRewardedReady) { onResult?.Invoke(0); return; }
            adService.ShowRewarded(
                onRewarded: async () =>
                {
                    int coins = 0;
                    try { coins = await economyManager.GrantWatchCoinsAsync(dailyClock.TodayIso); }
                    catch (System.Exception ex) { Debug.LogWarning($"[Economy] watch-coins grant failed: {ex.Message}"); }
                    RefreshCoinPill();
                    onResult?.Invoke(coins);
                },
                onClosed: () => { });
        }

        // Task 18E — does the player's current Puzzle Show tier still have any unplayed puzzle?
        // (Defaults to true so we never get stuck if progress/lookup is unavailable.)
        private bool CurrentTierHasUnplayed()
        {
            if (cachedPuzzleProgress == null || tierPuzzleIdLookup == null) return true;
            int tier = cachedPuzzleProgress.currentTier;
            if (!tierPuzzleIdLookup.TryGetValue(tier, out var ids) || ids == null) return true;
            var done = new System.Collections.Generic.HashSet<int>(
                cachedPuzzleProgress.completedPuzzleIds ?? new System.Collections.Generic.List<int>());
            foreach (var id in ids)
                if (!done.Contains(id)) return true;
            return false;
        }

        // Task 16C — the full results page's primary action now re-routes into the
        // relevant mode instead of dumping the player at the main menu.
        private void PlayAgain()
        {
            switch (lastWinContext)
            {
                case PostWin.TimeAttack:
                    // Fresh Time Attack run with the last-chosen config.
                    StartTimeAttackModeWithConfig(lastTimeAttackConfig);
                    break;
                case PostWin.PuzzleShow:
                    // "Next Puzzle" — another UNPLAYED puzzle in the current tier; if the tier is
                    // fully complete (Task 18E), route to the library grid instead of replaying.
                    if (CurrentTierHasUnplayed())
                        StartPuzzleShowMode();
                    else
                        ShowLibrary();
                    break;
                case PostWin.Daily:
                    // Daily has no "Play Again" (button hidden); guard routes Home if reached.
                    ShowMainMenu();
                    break;
                default:
                    // Fallback (e.g. legacy Classic results) — start a fresh Classic puzzle.
                    StartClassicMode();
                    break;
            }
        }

        // ─── Task 9F — Statistics screen ─────────────────────────────────────────

        private void ShowStats()
        {
            var screen = uiManager.GetStats();
            if (screen == null) return;
            screen.Populate(cachedDailyProgress, cachedPlayerProgress);
            uiManager.ShowStats();
        }

        private void LoadPlayerProgressBlocking(IDataManager dataManager)
        {
            try
            {
                var task = dataManager.GetPlayerProgressAsync();
                cachedPlayerProgress = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PlayerProgress load failed; using defaults: {ex.Message}");
                cachedPlayerProgress = new PlayerProgress();
            }
            if (cachedPlayerProgress == null) cachedPlayerProgress = new PlayerProgress();
        }

        /// <summary>
        /// Task 9F — Increment mode-specific play/win counters after a game ends.
        /// <paramref name="endedMode"/> is the mode that just finished (activeMode is
        /// already null when this is called).  Persisted via UpdatePlayerProgressAsync.
        /// </summary>
        private async void IncrementModeStats(bool wasWin, IGameMode endedMode = null)
        {
            if (dataManagerRef == null || cachedPlayerProgress == null) return;

            cachedPlayerProgress.totalPuzzlesCompleted++;

            if (endedMode is TimeAttackMode)
            {
                cachedPlayerProgress.timeAttackStats.gamesPlayed++;
            }
            else
            {
                // Classic / Daily / PuzzleShow all count as classic-family games.
                cachedPlayerProgress.classicStats.gamesPlayed++;
                if (wasWin) cachedPlayerProgress.classicStats.gamesWon++;
            }

            try
            {
                await dataManagerRef.UpdatePlayerProgressAsync(cachedPlayerProgress);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Stats] IncrementModeStats persist failed: {ex.Message}");
            }
        }

        // ─── Task 9G — Resume ────────────────────────────────────────────────────

        /// <summary>
        /// Pure, testable resumability check.  Returns true and populates
        /// <paramref name="def"/> when the snapshot represents a resolvable
        /// in-progress puzzle.  Never throws.
        /// <para>
        /// ASSUMPTION: Resume covers tier/daily puzzles (id-resolvable via tier
        /// cache or daily pool).  Random Classic puzzles are not id-resolvable from
        /// the snapshot alone, so they return false (hide Resume gracefully).
        /// </para>
        /// </summary>
        public static bool TryGetResumable(
            GameStateSnapshot snapshot,
            System.Func<int, PuzzleDefinition> resolve,
            out PuzzleDefinition def)
        {
            def = null;
            try
            {
                if (snapshot == null) return false;
                if (snapshot.wordChain == null || snapshot.wordChain.Length < 1) return false;
                if (string.IsNullOrEmpty(snapshot.currentMode) ||
                    snapshot.currentMode == "Menu") return false;

                def = resolve?.Invoke(snapshot.currentPuzzleId);
                if (def == null) return false;

                // Already won: chain tail == endWord.
                string tail = snapshot.wordChain[snapshot.wordChain.Length - 1];
                if (string.Equals(tail, def.endWord, System.StringComparison.OrdinalIgnoreCase))
                {
                    def = null;
                    return false;
                }

                return true;
            }
            catch
            {
                def = null;
                return false;
            }
        }

        private void LoadResumeSnapshotBlocking(IDataManager dataManager)
        {
            try
            {
                var task = dataManager.LoadGameStateAsync();
                var snap = task.IsCompleted ? task.Result : task.GetAwaiter().GetResult();

                PuzzleDefinition def;
                bool resumable = TryGetResumable(snap, ResolveAnyPuzzleById, out def);
                if (resumable)
                {
                    cachedResumeSnapshot = snap;
                    cachedResumePuzzle   = def;
                    // Determine mode string for the affordance label.
                    cachedResumeMode = snap.currentMode ?? "Gameplay";
                }
                else
                {
                    cachedResumeSnapshot = null;
                    cachedResumePuzzle   = null;
                    cachedResumeMode     = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Resume] Snapshot load failed: {ex.Message}");
                cachedResumeSnapshot = null;
                cachedResumePuzzle   = null;
            }
        }

        /// <summary>
        /// Resolve a puzzleId by checking tier cache first, then daily pool.
        /// Returns null if not found (random Classic, removed puzzle, etc.).
        /// </summary>
        private PuzzleDefinition ResolveAnyPuzzleById(int puzzleId)
        {
            var fromTier = FindPuzzleDefinitionById(puzzleId);
            if (fromTier != null) return fromTier;

            // Check daily pool.
            if (dailyPuzzleService != null && dailyPuzzleService.PoolCount > 0)
            {
                // DailyPuzzleService exposes pool only via GetTodayPuzzle/TodayIndex;
                // load the raw pool via Resources to resolve by id.
                return FindDailyPuzzleById(puzzleId);
            }
            return null;
        }

        private PuzzleDefinition FindDailyPuzzleById(int puzzleId)
        {
            // Load daily pool from Resources and scan for matching id.
            // Non-fatal: returns null on any error.
            try
            {
                var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Data/daily_puzzles");
                if (asset == null) return null;
                var wrapper = UnityEngine.JsonUtility.FromJson<DailyPoolWrapper>(asset.text);
                if (wrapper?.puzzles == null) return null;
                foreach (var p in wrapper.puzzles)
                {
                    if (p != null && p.puzzleId == puzzleId) return p;
                }
            }
            catch { /* non-fatal */ }
            return null;
        }

        [System.Serializable]
        private class DailyPoolWrapper
        {
            public PuzzleDefinition[] puzzles;
        }

        private void RefreshResumeAffordance()
        {
            var menu = uiManager?.GetMainMenu();
            if (menu == null) return;

            if (cachedResumePuzzle == null)
            {
                menu.SetResumeVisible(false);
                return;
            }

            string desc = $"{cachedResumePuzzle.startWord}→{cachedResumePuzzle.endWord}";
            menu.SetResumeVisible(true, desc);
        }

        private void ResumeGame()
        {
            if (cachedResumePuzzle == null || cachedResumeSnapshot == null)
            {
                Debug.LogWarning("[Resume] No resumable puzzle; ignoring tap.");
                return;
            }

            var def = cachedResumePuzzle;
            var snap = cachedResumeSnapshot;

            // Clear cached resume so it cannot be re-entered.
            cachedResumeSnapshot = null;
            cachedResumePuzzle   = null;
            cachedResumeMode     = null;

            // Reconstruct the puzzle model.
            var puzzle = new WordPuzzleModel(
                def.puzzleId,
                def.startWord,
                def.endWord,
                def.optimalSteps,
                def.solution,
                def.seedValue,
                Difficulty.Easy
            );

            // Start the puzzle fresh (this overwrites SaveState), then restore chain.
            isTutorialRun = false;
            isDailyRun = false;
            pendingDailyPuzzle = null;
            activeMode = new WordPuzzle.Modes.ClassicMode();
            modeController.SetMode(activeMode);

            modeController.StartGame(puzzle);
            uiManager.ShowGameplay();

            // Re-apply the saved word chain (skip start word which is already in chain).
            if (snap.wordChain != null)
            {
                for (int i = 1; i < snap.wordChain.Length; i++)
                {
                    string w = snap.wordChain[i];
                    if (!string.IsNullOrEmpty(w))
                        stateManager.Dispatch(new SubmitWordAction(w));
                }
            }

            // Restore typed-but-not-submitted input.
            if (!string.IsNullOrEmpty(snap.currentInput))
            {
                foreach (char c in snap.currentInput)
                    stateManager.Dispatch(new PressLetterAction(c));
            }

            var state = stateManager.GetCurrentState();
            uiManager.GetGameplay().SetPuzzleDisplay(state.puzzle.startWord, state.puzzle.endWord);
            uiManager.GetGameplay().SetScore(state.score);
            uiManager.GetGameplay().ShowFeedback("", UnityEngine.Color.white);
            UpdateGameplayUI();
        }
    }
}
