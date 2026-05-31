# WordPuzzleGame Feature Parity Implementation Plan

**Date**: 2026-05-22  
**Scope**: Add hint, undo, reveal, puzzle generation, and word library features from Project 3 to WordPuzzleGame  
**Constraints**: Exclude blitz mode and multiplayer; ensure library + generation working fully; tier-based progression only in Puzzle Show mode

---

## Phase Overview

| Phase | Focus | Duration | Dependencies |
|-------|-------|----------|--------------|
| **1** | Word library expansion + puzzle generation system | 2 turns | None |
| **2** | Hint/Reveal/Undo mechanics + UI implementation | 3 turns | Phase 1 |
| **3** | Per-mode verification (Classic, Time Attack, Puzzle Show) | 2 turns | Phase 2 |
| **4** | Unit tests + integration validation | 1 turn | Phase 3 |
| **5** | Polish + final verification | 1 turn | Phase 4 |

**Total Estimated Turns**: 9

---

## Phase 1: Word Library & Puzzle Generation System

### Deliverables
- [ ] Expand word library from 23 to 500+ words across all tiers
- [ ] Fix tier_definitions.json (puzzle 3 coil→curl duplicate + incomplete tiers)
- [ ] Implement GeneratePuzzle function with BFS validation (from Project 3's generatePuzzle.ts)
- [ ] Verify puzzle generation works in Classic and Time Attack modes

### Code Changes Required

**1. Assets/Data/word_library.json** (new/expanded)
- Structure: `{ "words": ["word1", "word2", ...], "metadata": { "count": N, "expanded": true } }`
- Expand to 500+ words (sourced from Project 3 tier_definitions.json and common word lists)
- Organize by difficulty if needed for future features

**2. Assets/Data/tier_definitions.json** (fix + expand)
- Current: 73 lines, puzzle 3 is broken (coil→curl duplicate)
- Target: Fix all existing puzzles, add 50+ new puzzles across tiers 1-6
- Structure per Project 3: `{ "tier": N, "puzzles": [{ "start": "word", "end": "word", "minSteps": N }] }`
- Validate with BFS to ensure all puzzles are solvable

**3. Assets/Scripts/Game/PuzzleGenerator.cs** (new)
```csharp
public class PuzzleGenerator
{
    private List<string> wordLibrary;
    private Random random;

    // Load word library from JSON
    public void Initialize(string libraryPath);
    
    // Main generation: either tier-based (Puzzle Show) or random (Classic/Time Attack)
    public Puzzle GeneratePuzzle(int mode, int tier = -1);
    
    // BFS validation: verify puzzle is solvable
    private bool ValidatePuzzle(string start, string end);
    
    // Random puzzle generation for Classic/Time Attack
    private Puzzle GenerateRandomPuzzle();
    
    // Tier-based puzzle selection for Puzzle Show
    private Puzzle GetTierPuzzle(int tier);
}
```

### Scene Changes
- None (data-only phase)

### Tests Required
- [ ] WordLibraryLoader: loads JSON correctly, count matches
- [ ] PuzzleGenerator.GenerateRandomPuzzle: returns valid puzzle
- [ ] PuzzleGenerator.ValidatePuzzle: correctly validates solvable puzzles
- [ ] TierPuzzleLoader: loads tier_definitions.json, puzzle 3 fixed

### Agent Assignments
- **dictionary-builder**: Expand word library, validate solvability
- **state-coder**: Implement PuzzleGenerator.cs with BFS validation

---

## Phase 2: Hint/Reveal/Undo Mechanics + UI

### Deliverables
- [ ] Implement HandleUseHint, HandleUseReveal in GameAction.cs (from Project 3's HintRevealButtons.tsx logic)
- [ ] Complete HandleUndo with full state restoration
- [ ] Add Hint/Reveal buttons to GameplayScreen UI
- [ ] Wire economy dispatch and feedback text updates

### Code Changes Required

**1. Assets/Scripts/Game/GameAction.cs** (implement mechanics)
```csharp
public void HandleUseHint(int puzzleId)
{
    // Consume 1 hint from economy
    if (!economyManager.HasResource("hint", 1)) return;
    economyManager.ConsumeResource("hint", 1);
    
    // Find next unrevealed letter in solution path
    string nextLetter = GetNextUnrevealedLetter(puzzleId);
    gameStateManager.RevealLetter(nextLetter);
    gameplayScreen.UpdatePuzzleDisplay();
    gameplayScreen.ShowFeedback($"Hint: {nextLetter}");
}

public void HandleUseReveal(int puzzleId)
{
    // Consume 1 reveal from economy
    if (!economyManager.HasResource("reveal", 1)) return;
    economyManager.ConsumeResource("reveal", 1);
    
    // Reveal entire solution word
    string solution = puzzleManager.GetCurrentPuzzle().endWord;
    gameStateManager.RevealWord(solution);
    gameplayScreen.UpdatePuzzleDisplay();
    gameplayScreen.ShowFeedback("Word revealed!");
}

public void HandleUndo()
{
    // Pop last word from history
    if (!gameStateManager.CanUndo()) return;
    
    gameStateManager.UndoLastWord();
    // Restore: current word, letter highlights, lives, streak
    gameplayScreen.UpdatePuzzleDisplay();
    gameplayScreen.UpdateScore();
}
```

**2. Assets/Scripts/Game/GameStateManager.cs** (state restoration)
```csharp
public class GameStateManager
{
    // Track full state history for undo: word, lives, streak, revealed letters
    private Stack<GameSnapshot> history;
    
    private struct GameSnapshot
    {
        public List<string> wordPath;
        public int livesRemaining;
        public int currentStreak;
        public HashSet<char> revealedLetters;
    }
    
    public void SaveSnapshot();
    public bool CanUndo() => history.Count > 1;
    public void UndoLastWord();
    public void RevealLetter(string letter);
    public void RevealWord(string word);
}
```

**3. Assets/Scripts/UI/Screens/GameplayScreen.cs** (UI additions)
```csharp
public class GameplayScreen : MonoBehaviour
{
    public Button HintButton { get; set; }
    public Button RevealButton { get; set; }
    public Button UndoButton { get; set; }
    public TextMeshProUGUI HintCountText { get; set; }
    public TextMeshProUGUI RevealCountText { get; set; }
    public TextMeshProUGUI FeedbackText { get; set; }
    
    public void UpdateResourceCounts(int hints, int reveals)
    {
        HintCountText.text = $"Hints: {hints}";
        RevealCountText.text = $"Reveals: {reveals}";
    }
    
    public void ShowFeedback(string message)
    {
        FeedbackText.text = message;
        StartCoroutine(FadeFeedback());
    }
}
```

**4. Assets/Scripts/Game/EconomyManager.cs** (economy wiring)
- Initialize with starting resources (hints: 2, reveals: 1 per game)
- Track consumption via ConsumeResource/HasResource
- Integrate with UI feedback

### Scene Changes

**GameUI.unity - GameplayScreen Panel**
1. Add HintButton (left side, below puzzle display)
   - Label: "Hint (2)"
   - OnClick → GameBootstrap.OnHintButtonPressed
2. Add RevealButton (right side, below puzzle display)
   - Label: "Reveal (1)"
   - OnClick → GameBootstrap.OnRevealButtonPressed
3. Add UndoButton (bottom center)
   - Label: "Undo"
   - OnClick → GameBootstrap.OnUndoButtonPressed
4. Add FeedbackText (top-right corner, fades after 2s)
   - Shows: "Hint: E", "Word revealed!", "Not enough hints", etc.

### Tests Required
- [ ] GameAction.HandleUseHint: consumes resource, reveals letter, updates UI
- [ ] GameAction.HandleUseReveal: consumes resource, reveals word, shows feedback
- [ ] GameStateManager.UndoLastWord: restores word path, lives, streak
- [ ] EconomyManager: tracks resources, prevents over-consumption
- [ ] GameplayScreen: buttons respond to clicks, text updates correctly

### Agent Assignments
- **state-coder**: Implement GameAction mechanics + GameStateManager snapshots
- **screen-coder**: Add buttons/text to GameplayScreen, layout/styling
- **ui-builder**: Wire event handlers in GameBootstrap, test button responsiveness

---

## Phase 3: Per-Mode Verification

### Deliverables
- [ ] ClassicMode: uses random puzzle generation, shows score/streak
- [ ] TimeAttackMode: uses random puzzle generation, timer works, game-over detection
- [ ] PuzzleShowMode: loads tier puzzles, progression works, tier indicator in UI

### Code Changes Required

**1. Assets/Scripts/Game/Modes/ClassicMode.cs**
```csharp
public class ClassicMode : MonoBehaviour, IGameMode
{
    public void Initialize(GameBootstrap bootstrap)
    {
        puzzle = puzzleGenerator.GenerateRandomPuzzle();
        gameStateManager.StartGame(puzzle);
        // No lives/time limit in Classic
    }
    
    public bool IsGameOver() => gameStateManager.HasReachedEnd();
    public GameResult GetResult() => new GameResult { 
        mode = "Classic", 
        score = gameStateManager.GetMoveCount(),
        timerValue = 0 
    };
}
```

**2. Assets/Scripts/Game/Modes/TimeAttackMode.cs**
```csharp
public class TimeAttackMode : MonoBehaviour, IGameMode
{
    private float timeRemaining = 120f; // 2 minutes
    
    public void Initialize(GameBootstrap bootstrap)
    {
        puzzle = puzzleGenerator.GenerateRandomPuzzle();
        gameStateManager.StartGame(puzzle);
        gameplayScreen.ShowTimer(timeRemaining);
    }
    
    public bool IsGameOver() => timeRemaining <= 0 || gameStateManager.HasReachedEnd();
    
    private void Update()
    {
        timeRemaining -= Time.deltaTime;
        gameplayScreen.UpdateTimer(timeRemaining);
        if (IsGameOver()) GameBootstrap.EndGame();
    }
}
```

**3. Assets/Scripts/Game/Modes/PuzzleShowMode.cs** (fix tier loading)
```csharp
public class PuzzleShowMode : MonoBehaviour, IGameMode
{
    private int currentTier = 1;
    
    public void Initialize(GameBootstrap bootstrap)
    {
        LoadTierPuzzle(currentTier);
    }
    
    private void LoadTierPuzzle(int tier)
    {
        puzzle = puzzleGenerator.GetTierPuzzle(tier);
        gameplayScreen.ShowTierIndicator($"Tier {tier}");
        gameStateManager.StartGame(puzzle);
    }
    
    public bool IsGameOver() => gameStateManager.HasReachedEnd();
    public void AdvanceToNextTier()
    {
        if (currentTier < 6) LoadTierPuzzle(++currentTier);
        else GameBootstrap.EndGame(); // All tiers complete
    }
}
```

### Scene Changes
- **GameUI.unity - GameplayScreen Panel**: Add TierIndicator text (visible only in Puzzle Show)

### Tests Required
- [ ] ClassicMode: random puzzle loads, score calculated correctly
- [ ] TimeAttackMode: timer counts down, game ends when time expires
- [ ] PuzzleShowMode: tier puzzles load, tier indicator updates, progression works
- [ ] All modes: game-over detection works, results screen shows correct data

### Agent Assignments
- **mode-verifier**: Test each mode end-to-end, verify game-over detection, results screen

---

## Phase 4: Unit Tests + Integration

### Deliverables
- [ ] All GameAction handlers tested
- [ ] PuzzleGenerator tested
- [ ] GameStateManager snapshots tested
- [ ] Integration test: full game loop (menu → mode selection → play → hint/undo → end)

### Test Structure
```
Assets/Tests/
├── GameAction/
│   ├── HintTests.cs
│   ├── RevealTests.cs
│   └── UndoTests.cs
├── PuzzleGeneration/
│   ├── PuzzleGeneratorTests.cs
│   └── BFSValidationTests.cs
├── GameState/
│   └── GameStateManagerTests.cs
└── Integration/
    └── FullGameLoopTests.cs
```

### Agent Assignments
- **test-writer**: Write all unit + integration tests, verify coverage

---

## Phase 5: Polish + Final Verification

### Deliverables
- [ ] Zero console errors in play mode
- [ ] All buttons respond immediately
- [ ] Feedback text displays and fades correctly
- [ ] Score/timer display updates in real-time
- [ ] Economy resources consume and display correctly
- [ ] Game flow: menu → mode → play → end → results → menu (no stalls/errors)

### Manual Testing Checklist
- [ ] Play Classic mode to completion
- [ ] Play Time Attack mode, verify timer, game-over at time expiry
- [ ] Play Puzzle Show mode, verify tier progression
- [ ] Use Hint button 3+ times, verify consumption and display
- [ ] Use Reveal button, verify word shows
- [ ] Use Undo button, verify state restoration
- [ ] Back button in gameplay, verify return to menu
- [ ] No console errors during full playthrough

### Agent Assignments
- **production-validator**: Final end-to-end testing, polish verification

---

## File Summary

**New Files to Create**
- `Assets/Scripts/Game/PuzzleGenerator.cs` (500 lines)
- `Assets/Data/word_library.json` (expanded)
- `Assets/Tests/GameAction/*.cs` (tests)
- `Assets/Tests/PuzzleGeneration/*.cs` (tests)
- `Assets/Tests/GameState/*.cs` (tests)
- `Assets/Tests/Integration/*.cs` (tests)

**Files to Modify**
- `Assets/Scripts/Game/GameAction.cs` (implement hint/reveal/undo)
- `Assets/Scripts/Game/GameStateManager.cs` (add snapshot history)
- `Assets/Scripts/Game/Modes/ClassicMode.cs` (use random generation)
- `Assets/Scripts/Game/Modes/TimeAttackMode.cs` (fix timer + game-over)
- `Assets/Scripts/Game/Modes/PuzzleShowMode.cs` (fix tier loading)
- `Assets/Scripts/UI/Screens/GameplayScreen.cs` (add buttons/text)
- `Assets/Scripts/Game/GameBootstrap.cs` (wire events)
- `Assets/Data/tier_definitions.json` (fix + expand)
- `Assets/Scenes/GameUI.unity` (add UI elements)

---

## Success Criteria

✅ Word library: 500+ words, no duplicates  
✅ Puzzle generation: random for Classic/Time Attack, tier-based for Puzzle Show  
✅ Hint system: button, consumption, visual feedback  
✅ Reveal system: button, consumption, shows word  
✅ Undo system: button, full state restoration (word + lives + streak)  
✅ All game modes: fully functional with correct game-over detection  
✅ Zero console errors in play mode  
✅ Full game loop: menu → mode → play → end → results → menu works seamlessly

---

## Agent Assignments Summary

| Agent | Phase | Tasks |
|-------|-------|-------|
| **dictionary-builder** | 1 | Word library expansion, tier validation |
| **state-coder** | 1-2 | PuzzleGenerator, GameStateManager, GameAction mechanics |
| **screen-coder** | 2 | GameplayScreen UI layout, button wiring |
| **ui-builder** | 2 | Event handlers, feedback animation, resource display |
| **mode-verifier** | 3 | Per-mode testing, game-over detection, results screen |
| **test-writer** | 4 | Unit + integration tests, coverage verification |
| **production-validator** | 5 | Final playthrough, polish verification, zero-error confirmation |

---

## Key Differences from Project 3

| Feature | Project 3 (React) | WordPuzzleGame (Unity) |
|---------|-------------------|----------------------|
| Hint System | HintRevealButtons.tsx | GameAction.HandleUseHint + GameplayScreen buttons |
| Reveal System | ConsumableButton.tsx | GameAction.HandleUseReveal + economy integration |
| Puzzle Generation | generatePuzzle.ts BFS | PuzzleGenerator.cs BFS validation |
| Economy | Hard-coded costs | EconomyManager dispatch framework |
| Tier Progression | All modes | Only PuzzleShowMode |
| Random Generation | Not in Project 3 | Classic/Time Attack modes (new feature) |

---

**Status**: Ready for user approval and agent dispatch
