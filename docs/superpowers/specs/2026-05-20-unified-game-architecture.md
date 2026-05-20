# Unified Word Puzzle Game Architecture
## Comprehensive Design Document

**Created:** 2026-05-20  
**Quality Bar:** Enterprise-grade (85%+ test coverage, performance profiling, mobile-first optimization)  
**Timeline:** 1 month  
**Testing Strategy:** 60% unit / 30% integration / 10% E2E  

---

## 1. SYSTEM OVERVIEW

### The Game
A word ladder puzzle game where players transform one word into another by changing one letter at a time. Three game modes exist with different progression philosophies:
- **Classic Mode** — Infinite casual play with random puzzles
- **Puzzle Show** — Tiered progression through curated pre-generated puzzles
- **Time Attack** — Pressure-based endless mode with decreasing time limits

### Core Requirements
- ✅ Real-time state persistence (every action saved)
- ✅ Cross-mode state sharing (economy, hints, progression)
- ✅ Hybrid puzzle generation (pre-generated tiers + random seeded)
- ✅ Mobile-first (60 FPS, optimized for low-end devices)
- ✅ Enterprise testing (85%+ coverage with balanced approach)
- ✅ Architecture ready for telemetry (hooks included, implementation deferred)

---

## 2. LAYERED ARCHITECTURE

```
┌────────────────────────────────────────────┐
│          UI LAYER                          │
│  (Screens, Components, Input Handling)     │
├────────────────────────────────────────────┤
│          MODE LAYER                        │
│  (ClassicMode, PuzzleShowMode,             │
│   TimeAttackMode, ModeController)          │
├────────────────────────────────────────────┤
│          CORE ENGINE LAYER                 │
│  (PuzzleGenerator, WordValidator,          │
│   GameStateManager, EconomyManager)        │
├────────────────────────────────────────────┤
│          PERSISTENCE LAYER                 │
│  (DataManager, SaveSystem,                 │
│   PlayerProgress, TierData)                │
└────────────────────────────────────────────┘
```

**Dependency Flow:** UI → Modes → Engine → Persistence  
**Rule:** No upward dependencies. Layers only depend on layers below them.

---

## 3. LAYER DETAILS

### 3.1 PERSISTENCE LAYER

**Responsibility:** Store and retrieve all game state with real-time synchronization.

**Key Classes:**

```csharp
// Core persistence interface
public interface IDataManager
{
    // Real-time save (called on every action)
    Task SaveGameStateAsync(GameStateSnapshot snapshot);
    
    // Load on app startup
    Task<GameStateSnapshot> LoadGameStateAsync();
    
    // Progress tracking
    Task UpdatePlayerProgressAsync(PlayerProgress progress);
    Task<PlayerProgress> GetPlayerProgressAsync();
}

// Snapshot of game state at any moment
public class GameStateSnapshot
{
    public string currentMode;              // "Classic", "PuzzleShow", "TimeAttack"
    public int currentPuzzleId;
    public string[] currentWordChain;       // Words typed so far
    public string currentInput;             // In-progress word being typed
    public int lives;
    public int hintsUsed, revealsUsed, undosUsed;
    public long timestamp;
    public string sessionId;
}

// Tier-based puzzle data for Puzzle Show mode
public class TierData
{
    public int tierId;
    public List<PuzzleDefinition> puzzles;  // Pre-generated
    public bool isUnlocked;
    public DateTime unlockedAt;
}

public class PuzzleDefinition
{
    public int puzzleId;
    public string startWord;
    public string endWord;
    public int optimalSteps;
    public string[] solution;               // Pre-computed solution path
    public int seedValue;                   // For reproducibility
}

// Player progression across all modes
public class PlayerProgress
{
    public int totalCoins;
    public int totalPuzzlesCompleted;
    
    // Puzzle Show tracking
    public int highestTierUnlocked;
    public Dictionary<int, TierProgress> tierProgress;
    
    // Cross-mode totals
    public int totalHintsEarned;
    public int totalRevealsEarned;
    public int totalUndosEarned;
    
    // Per-mode stats
    public ClassicModeStats classicStats;
    public TimeAttackModeStats timeAttackStats;
}
```

**Storage Strategy:**
- **Real-time writes:** Async to avoid blocking 60 FPS
- **Write batching:** Group multiple state changes into single write (e.g., every 0.5s or 5 actions)
- **Local file system:** Use PlayerPrefs for small data, JSON files for larger snapshots
- **Future:** Ready for cloud sync (hook prepared but not implemented)

**Testing (Unit):**
- Save/load cycle preserves all data
- Concurrent writes don't corrupt state
- Load fails gracefully (restore from backup)
- Large snapshots load in <50ms

---

### 3.2 CORE ENGINE LAYER

**Responsibility:** All game logic, puzzle generation, validation, and state management.

**Key Classes:**

#### A. Puzzle Generation Engine

```csharp
public interface IPuzzleGenerator
{
    // For Puzzle Show: Load pre-generated tier puzzle
    PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndexInTier);
    
    // For Classic & Time Attack: Generate random puzzle
    PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty);
}

public class PuzzleGeneratorImpl : IPuzzleGenerator
{
    // Word graph built from dictionary (loaded once at startup)
    private WordGraph wordGraph;
    
    // Pre-loaded tier definitions
    private Dictionary<int, TierData> tierCache;
    
    public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        // 1. Pick two words at appropriate distance for difficulty
        // 2. Verify shortest path exists
        // 3. Generate seed for reproducibility
        // 4. Return puzzle definition
        
        // PERFORMANCE: This should complete in <100ms for mobile 60 FPS
    }
}

// Word graph data structure (pre-computed)
public class WordGraph
{
    // Adjacent words: { "cat": ["bat", "car", "can"], ... }
    private Dictionary<string, List<string>> adjacencyList;
    
    public List<string> GetShortestPath(string start, string end)
    {
        // BFS algorithm - deterministic
    }
    
    public bool CanSolve(string start, string end)
    {
        // Quick check without full path computation
    }
}
```

#### B. Word Validator

```csharp
public interface IWordValidator
{
    // Check if word is valid and valid next step from current word
    ValidationResult ValidateWord(
        string word, 
        string previousWord,
        WordPuzzle puzzle);
    
    // Check if guess is the solution
    bool IsSolutionStep(string word, WordPuzzle puzzle);
}

public class WordValidatorImpl : IWordValidator
{
    private HashSet<string> validWords;  // Entire dictionary in memory
    
    public ValidationResult ValidateWord(string word, string previousWord, WordPuzzle puzzle)
    {
        // 1. Check word exists
        // 2. Check one letter difference from previous
        // 3. Check not already in chain
        // 4. Return detailed result
        
        return new ValidationResult
        {
            isValid = true,
            message = "Valid word",
            isNextStep = true,  // One letter away
            isProgress = true   // Moving toward solution
        };
    }
}

public class ValidationResult
{
    public bool isValid;              // Word exists in dictionary
    public string message;
    public bool isNextStep;           // Exactly one letter different
    public bool isProgress;           // Moves closer to solution
    public int distanceToStart;
    public int distanceToEnd;
}
```

#### C. Game State Manager

```csharp
public interface IGameStateManager
{
    // Get current game state
    GameState GetCurrentState();
    
    // Dispatch actions (like Redux)
    void Dispatch(GameAction action);
    
    // Subscribe to state changes
    IDisposable Subscribe(Action<GameState> observer);
}

public class GameState
{
    public string[] wordChain;        // Valid words typed
    public string currentInput;       // In-progress word
    public int lives;
    public bool isWon;
    public bool isLost;
    
    // Hint/reveal/undo state
    public int? hintedLetterIndex;
    public string[] revealedWord;
    public int previousChainLength;   // For undo
}

public abstract class GameAction { }

public class PressLetterAction : GameAction
{
    public char letter;
}

public class SubmitWordAction : GameAction
{
    public string word;
}

public class UseHintAction : GameAction
{
    public int letterIndex;
}

public class GameStateManager : IGameStateManager
{
    public void Dispatch(GameAction action)
    {
        switch (action)
        {
            case PressLetterAction a:
                HandleLetterPress(a.letter);
                break;
            case SubmitWordAction a:
                HandleSubmit(a.word);
                break;
            // ... etc
        }
        
        // CRITICAL: Notify persistence layer to save
        dataManager.SaveGameStateAsync(CreateSnapshot());
    }
    
    private void HandleSubmit(string word)
    {
        var validation = wordValidator.ValidateWord(word, currentState.wordChain.Last(), currentPuzzle);
        
        if (validation.isValid && validation.isNextStep)
        {
            currentState.wordChain = currentState.wordChain.Append(word).ToArray();
            currentState.currentInput = "";
            
            if (word == currentPuzzle.endWord)
            {
                currentState.isWon = true;
            }
        }
        else
        {
            currentState.lives--;
            if (currentState.lives <= 0)
                currentState.isLost = true;
        }
    }
}
```

#### D. Economy Manager

```csharp
public interface IEconomyManager
{
    // Cross-mode economy access
    Task<int> GetCoinsAsync();
    Task AddCoinsAsync(int amount, string source);  // source = "puzzle_complete", "time_attack_bonus"
    
    Task<int> GetHintsAsync();
    Task UseHintAsync();
    
    Task<int> GetRevealsAsync();
    Task UseRevealAsync();
    
    Task<int> GetUndosAsync();
    Task UseUndoAsync();
}

public class EconomyManager : IEconomyManager
{
    private IDataManager dataManager;
    private PlayerProgress progress;
    
    public async Task AddCoinsAsync(int amount, string source)
    {
        progress.totalCoins += amount;
        
        // Log for telemetry (hook prepared for future implementation)
        telemetryService.LogEconomyEvent("coins_earned", new { amount, source });
        
        await dataManager.UpdatePlayerProgressAsync(progress);
    }
}
```

**Testing (Unit + Integration):**
- Puzzle generation always produces valid solutions
- Word validator catches invalid next steps
- Game state reducer deterministic (same input = same output)
- Economy operations atomic (no partial updates)
- Cross-mode economy properly shared

---

### 3.3 MODE LAYER

**Responsibility:** Game loop logic for each mode. Orchestrates engine + UI interactions.

**Key Classes:**

```csharp
// All modes implement this interface
public interface IGameMode
{
    void Initialize(GameModeContext context);
    void StartGame();
    void HandleInput(GameAction action);
    void Update(float deltaTime);  // For time-based logic
    void OnGameOver();
    ModeStats GetStats();
}

public class GameModeContext
{
    public IPuzzleGenerator puzzleGenerator;
    public IWordValidator wordValidator;
    public IGameStateManager stateManager;
    public IEconomyManager economy;
    public IDataManager dataManager;
}

// ===== CLASSIC MODE =====
public class ClassicMode : IGameMode
{
    private IGameStateManager stateManager;
    private IPuzzleGenerator generator;
    private WordPuzzle currentPuzzle;
    
    public void StartGame()
    {
        // Pick random difficulty
        var difficulty = UnityEngine.Random.value > 0.5f ? Difficulty.Easy : Difficulty.Medium;
        currentPuzzle = generator.GenerateRandomPuzzle(difficulty);
        stateManager.StartNewGame(currentPuzzle);
    }
    
    public void HandleInput(GameAction action)
    {
        stateManager.Dispatch(action);
        
        var state = stateManager.GetCurrentState();
        if (state.isWon)
        {
            OnPuzzleComplete();
        }
        else if (state.isLost)
        {
            OnGameOver();
        }
    }
    
    private void OnPuzzleComplete()
    {
        var reward = CalculateReward();  // Based on lives remaining, steps taken
        economy.AddCoinsAsync(reward, "classic_mode");
        
        // Auto-load next puzzle for continuous play
        StartGame();
    }
}

// ===== PUZZLE SHOW MODE =====
public class PuzzleShowMode : IGameMode
{
    private int currentTier = 1;
    private int currentPuzzleIndex = 0;
    private int tiersUnlocked = 1;
    
    public void StartGame()
    {
        LoadPuzzleAtPosition(currentTier, currentPuzzleIndex);
    }
    
    private void LoadPuzzleAtPosition(int tier, int index)
    {
        var puzzle = puzzleGenerator.GetTierPuzzle(tier, index);
        stateManager.StartNewGame(puzzle);
    }
    
    public void HandleInput(GameAction action)
    {
        stateManager.Dispatch(action);
        
        var state = stateManager.GetCurrentState();
        if (state.isWon)
        {
            OnPuzzleComplete();
        }
        else if (state.isLost)
        {
            OnGameOver();
        }
    }
    
    private void OnPuzzleComplete()
    {
        currentPuzzleIndex++;
        
        var tierData = dataManager.GetTierData(currentTier);
        if (currentPuzzleIndex >= tierData.puzzles.Count)
        {
            // Tier complete - unlock next
            currentTier++;
            currentPuzzleIndex = 0;
            
            // Update progress
            var progress = dataManager.GetPlayerProgress();
            if (currentTier > progress.highestTierUnlocked)
            {
                progress.highestTierUnlocked = currentTier;
                // Trigger tier unlock celebration UI
            }
        }
        
        // Reward coins
        economy.AddCoinsAsync(Constants.PUZZLE_SHOW_REWARD, "puzzle_show");
        
        // Load next puzzle or end session
        if (currentTier <= MAX_TIERS)
            LoadPuzzleAtPosition(currentTier, currentPuzzleIndex);
    }
}

// ===== TIME ATTACK MODE =====
public class TimeAttackMode : IGameMode
{
    private float timeRemaining;
    private float currentTimeLimit = 90f;
    private int roundCount = 0;
    
    public void StartGame()
    {
        roundCount = 0;
        StartNewRound();
    }
    
    private void StartNewRound()
    {
        roundCount++;
        currentTimeLimit = Mathf.Max(30f, currentTimeLimit - 5f);
        timeRemaining = currentTimeLimit;
        
        var puzzle = generator.GenerateRandomPuzzle(Difficulty.Medium);
        stateManager.StartNewGame(puzzle);
    }
    
    public void Update(float deltaTime)
    {
        timeRemaining -= deltaTime;
        
        // UI notified of time remaining
        OnTimeChanged(timeRemaining);
        
        if (timeRemaining <= 0)
        {
            OnTimeUp();
        }
    }
    
    public void HandleInput(GameAction action)
    {
        stateManager.Dispatch(action);
        
        var state = stateManager.GetCurrentState();
        if (state.isWon)
        {
            // Completed puzzle before time
            economy.AddCoinsAsync(
                Constants.TIME_ATTACK_REWARD + Constants.TIME_BONUS_PER_SECOND * (int)timeRemaining,
                "time_attack");
            
            StartNewRound();
        }
    }
    
    private void OnTimeUp()
    {
        // Game over
    }
}

// Mode controller coordinates mode switching
public class ModeController
{
    private IGameMode activeMode;
    private GameModeContext context;
    
    public void SwitchMode(ModeType modeType)
    {
        if (activeMode != null)
            activeMode.OnGameOver();
        
        activeMode = CreateMode(modeType);
        activeMode.Initialize(context);
    }
    
    public void Update(float deltaTime)
    {
        if (activeMode != null)
            activeMode.Update(deltaTime);
    }
}
```

**Testing (Unit + Integration):**
- Mode correctly loads puzzles
- Mode properly tracks progression
- Economy rewards calculated correctly
- Cross-mode state sharing works (hints earned in one mode usable in another)
- Time Attack countdown accurate

---

### 3.4 UI LAYER

**Responsibility:** Visual representation and input capture.

**Key Classes:**

```csharp
// Screen for all gameplay
public class GameplayScreen : MonoBehaviour
{
    private IGameMode gameMode;
    private IGameStateManager stateManager;
    
    // UI components
    private WordChainDisplay wordChainDisplay;
    private CurrentWordInput currentWordInput;
    private LetterTile[] letterTiles;
    private HintButton hintButton;
    private LivesDisplay livesDisplay;
    private TimeDisplay timeDisplay;  // For Time Attack mode
    private TierDisplay tierDisplay;   // For Puzzle Show mode
    
    private void Update()
    {
        // Subscribe to state changes
        var state = stateManager.GetCurrentState();
        
        // Update UI based on state
        wordChainDisplay.SetWords(state.wordChain);
        currentWordInput.SetText(state.currentInput);
        livesDisplay.SetLives(state.lives);
        
        if (state.hintedLetterIndex.HasValue)
            HighlightLetter(state.hintedLetterIndex.Value);
    }
    
    private void OnHintButtonPressed()
    {
        gameMode.HandleInput(new UseHintAction { letterIndex = 0 });
    }
    
    private void OnLetterTapped(char letter)
    {
        gameMode.HandleInput(new PressLetterAction { letter = letter });
    }
    
    private void OnSubmitPressed()
    {
        var word = currentWordInput.GetText();
        gameMode.HandleInput(new SubmitWordAction { word = word });
    }
}

// Results screen after game
public class ResultsScreen : MonoBehaviour
{
    public void Show(ModeStats stats)
    {
        // Display score, coins earned, etc.
        // "Play again" button switches back to gameplay
    }
}
```

**Testing (Integration + E2E):**
- UI responds to state changes
- Input correctly dispatches actions
- Mode transitions work
- Screen performance maintains 60 FPS

---

## 4. DATA FLOW DIAGRAMS

### 4.1 Happy Path: Player Submits Word

```
Player Taps "Submit" 
    ↓
UI.OnSubmitPressed() 
    ↓
gameMode.HandleInput(SubmitWordAction)
    ↓
GameStateManager.Dispatch(SubmitWordAction)
    ↓
WordValidator.ValidateWord()
    ↓
If Valid:
  - Update GameState (add to chain)
  - Check if puzzle won
  - Update Economy if won
  - Save state via DataManager.SaveGameStateAsync()
    ↓
Notify UI (state changed)
    ↓
UI re-renders with updated state
```

### 4.2 Cross-Mode Economy Share

```
Player Completes Puzzle in Puzzle Show Mode
    ↓
PuzzleShowMode.OnPuzzleComplete()
    ↓
economy.AddCoinsAsync(50, "puzzle_show")  // Adds 50 coins
    ↓
EconomyManager updates PlayerProgress.totalCoins
    ↓
DataManager persists to disk
    ↓
Later: Player switches to Classic Mode
    ↓
ClassicMode reads economy.GetCoinsAsync()
    ↓
Same coin balance available (cross-mode shared)
```

---

## 5. FILE STRUCTURE

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Engine/
│   │   │   ├── PuzzleGenerator.cs
│   │   │   ├── WordValidator.cs
│   │   │   ├── GameStateManager.cs
│   │   │   ├── WordGraph.cs
│   │   │   └── EconomyManager.cs
│   │   ├── Persistence/
│   │   │   ├── IDataManager.cs
│   │   │   ├── DataManager.cs
│   │   │   ├── SaveData.cs
│   │   │   └── TierData.cs
│   │   └── Models/
│   │       ├── GameState.cs
│   │       ├── GameAction.cs
│   │       ├── WordPuzzle.cs
│   │       ├── PlayerProgress.cs
│   │       └── ValidationResult.cs
│   ├── Modes/
│   │   ├── IGameMode.cs
│   │   ├── ModeController.cs
│   │   ├── ClassicMode.cs
│   │   ├── PuzzleShowMode.cs
│   │   └── TimeAttackMode.cs
│   ├── UI/
│   │   ├── Screens/
│   │   │   ├── GameplayScreen.cs
│   │   │   ├── ResultsScreen.cs
│   │   │   ├── MainMenuScreen.cs
│   │   │   └── TierSelectScreen.cs
│   │   └── Components/
│   │       ├── WordChainDisplay.cs
│   │       ├── CurrentWordInput.cs
│   │       ├── LetterTile.cs
│   │       ├── HintButton.cs
│   │       ├── LivesDisplay.cs
│   │       ├── TimeDisplay.cs
│   │       └── TierDisplay.cs
│   └── Utilities/
│       ├── Constants.cs
│       ├── Logger.cs
│       └── TelemetryHooks.cs (prepared for future)
├── Resources/
│   ├── Data/
│   │   ├── dictionary.json
│   │   ├── tier_definitions.json
│   │   └── word_graph.json
│   └── UI/
│       ├── Prefabs/
│       └── Styles/
└── Tests/
    ├── Unit/
    │   ├── Engine/
    │   │   ├── PuzzleGeneratorTests.cs
    │   │   ├── WordValidatorTests.cs
    │   │   ├── GameStateManagerTests.cs
    │   │   └── EconomyManagerTests.cs
    │   └── Persistence/
    │       └── DataManagerTests.cs
    ├── Integration/
    │   ├── ModeIntegrationTests.cs
    │   ├── CrossModeEconomyTests.cs
    │   └── PersistenceIntegrationTests.cs
    └── E2E/
        ├── ClassicModeE2ETests.cs
        ├── PuzzleShowModeE2ETests.cs
        └── TimeAttackModeE2ETests.cs
```

---

## 6. TESTING STRATEGY

### Coverage Target: 85%

- **Unit Tests (60%):** Engine logic, validators, state reducer, economy
- **Integration Tests (30%):** Mode + Engine, Persistence + Engine, Cross-mode economy
- **E2E Tests (10%):** Full game flows (start → complete puzzle → results → next puzzle)

### Key Test Scenarios

**Unit:**
```csharp
[Test] public void PuzzleGenerator_ValidatesPath() { }
[Test] public void WordValidator_RejectsInvalidSteps() { }
[Test] public void GameStateManager_DispatchReducerIsDeterministic() { }
[Test] public void EconomyManager_CoinAdditionPersists() { }
[Test] public void DataManager_SaveLoadCycleIsLossless() { }
```

**Integration:**
```csharp
[Test] public void ClassicMode_GeneratesPuzzleOnStart() { }
[Test] public void PuzzleShowMode_ProgressesThroughTiers() { }
[Test] public void TimeAttackMode_DecreasesTimePerRound() { }
[Test] public void Modes_ShareEconomyState() { }
[Test] public void DataManager_PersistsStateInRealTime() { }
```

**E2E:**
```csharp
[Test] public IEnumerator FullGameFlow_ClassicMode_Complete() { }
[Test] public IEnumerator FullGameFlow_PuzzleShow_UnlockNextTier() { }
[Test] public IEnumerator CrashRecovery_LoadsSavedGameState() { }
```

---

## 7. PERFORMANCE BUDGET

**Mobile-First Optimization (60 FPS = 16.7ms per frame)**

| Operation | Budget | Notes |
|-----------|--------|-------|
| Puzzle generation | <100ms | Can happen off-frame |
| Word validation | <1ms | Per-input validation |
| State persistence save | <5ms | Async, batched writes |
| UI render (GameplayScreen) | <10ms | All components combined |
| State subscription notify | <2ms | Per observer update |

**Memory:**
- Word graph in memory: ~5-10MB
- Game state snapshot: ~1KB
- Per-mode runtime: <10MB

---

## 8. DEPENDENCY INJECTION

Each layer is injected with its dependencies to enable testing:

```csharp
// Startup (Bootstrap)
var wordGraph = WordGraphLoader.Load();
var dictionary = DictionaryLoader.Load();

var dataManager = new DataManager();
var puzzleGenerator = new PuzzleGeneratorImpl(wordGraph, dictionary);
var wordValidator = new WordValidatorImpl(dictionary);
var stateManager = new GameStateManager();
var economyManager = new EconomyManager(dataManager);

var context = new GameModeContext
{
    puzzleGenerator = puzzleGenerator,
    wordValidator = wordValidator,
    stateManager = stateManager,
    economyManager = economyManager,
    dataManager = dataManager
};

var modeController = new ModeController(context);
modeController.SwitchMode(ModeType.PuzzleShow);
```

In tests, implementations are replaced with mocks:
```csharp
[SetUp] public void Setup()
{
    mockPuzzleGenerator = new Mock<IPuzzleGenerator>();
    mockStateManager = new Mock<IGameStateManager>();
    
    mode = new ClassicMode();
    mode.Initialize(new GameModeContext { ... });
}
```

---

## 9. FUTURE EXTENSIONS

This architecture supports:
- **New modes** — Implement IGameMode, plug in
- **Telemetry** — Hooks already placed, call real service later
- **Cloud sync** — DataManager interface ready for cloud implementation
- **Difficulty tuning** — Economy service can adjust rewards per analytics
- **New power-ups** — Extend EconomyManager with new currency types
- **IAP integration** — Economy manager ready for premium currency
- **Multiplayer** — Mode state can be serialized and shared

---

## 10. QUALITY GATES

Before moving to implementation, validate:
- [ ] All interfaces have clear contracts (inputs/outputs documented)
- [ ] No upward dependencies exist (layer isolation verified)
- [ ] Dependency injection setup ready for testing
- [ ] Performance budgets achievable (word generation <100ms validated)
- [ ] Test plan covers 85% scenarios
- [ ] Data persistence strategy handles real-time saves
- [ ] Cross-mode economy sharing contracts clear

---

## NEXT STEPS

1. **User approval** of architecture
2. **Writing-plans skill** to break into implementation tasks
3. **Implementation** with test-driven development
4. **Integration** and performance validation
5. **Polish** and optimization

---

**Document Status:** Ready for Review  
**Last Updated:** 2026-05-20
