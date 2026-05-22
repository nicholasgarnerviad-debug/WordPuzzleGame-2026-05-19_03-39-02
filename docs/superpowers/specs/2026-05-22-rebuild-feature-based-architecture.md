# Word Puzzle Game Rebuild: Feature-Based Assembly Architecture

> **Status:** Design approved. Ready for implementation planning.
>
> **Lead Developer Role:** This document establishes the architectural foundation for a rock-solid, scalable, and maintainable Word Puzzle Game. Every decision prioritizes clarity, testability, and the ability to build features on top of a proven foundation.

**Goal:** Rebuild the Word Puzzle Game with a feature-based assembly structure, implementing only essential gameplay (three core modes with real-time feedback and basic stats), removing all non-essential systems, and establishing a clean, proven foundation for future growth.

**Architecture:** Feature-based assembly hierarchy with five independent, single-purpose modules (Game.Puzzle, Game.State, Game.Modes, Game.UI, Game.Bootstrap) plus isolated test assembly. Zero circular dependencies, clear layering, proven components reused where they work.

**Scope:** Classic Mode, Puzzle Show Mode, Time Attack Mode; essential UI (mode selection, gameplay, results); basic stats (words found, accuracy, time); word validation and puzzle generation only.

**Removed:** Animations, Economy/Coin system, Persistence/save data, Monetization, advanced features, all non-playable complexity.

**Tech Stack:** Unity 6000.4.6f1, C# 10, NUnit for testing, feature-based assembly definitions.

---

## Part 1: Assembly Hierarchy and Dependencies

### Assembly Definitions

Each assembly is defined with a single `.asmdef` file, no circular references.

**Layer 1 (Foundation):**
- **Game.Puzzle.asmdef**
  - Name: "Game.Puzzle"
  - References: (none) — purely self-contained
  - Purpose: Word engine, puzzle generation, word validation
  - Files: PuzzleGenerator.cs, WordValidator.cs, WordGraph.cs, Word.cs, PuzzleData.cs, Constants.cs
  - Key Exports: `IPuzzleGenerator`, `IWordValidator`, `WordGraph`, `Word`

**Layer 2 (State):**
- **Game.State.asmdef**
  - Name: "Game.State"
  - References: ["Game.Puzzle"]
  - Purpose: Game state management, state mutations, action handling
  - Files: GameStateManager.cs, GameState.cs, GameAction.cs
  - Key Exports: `IGameStateManager`, `GameState`

**Layer 3 (Gameplay):**
- **Game.Modes.asmdef**
  - Name: "Game.Modes"
  - References: ["Game.Puzzle", "Game.State"]
  - Purpose: Three game mode implementations
  - Files: IGameMode.cs, ClassicMode.cs, PuzzleShowMode.cs, TimeAttackMode.cs, ModeController.cs
  - Key Exports: `IGameMode`, `ModeController`

**Layer 4 (UI):**
- **Game.UI.asmdef**
  - Name: "Game.UI"
  - References: ["Game.Puzzle", "Game.State", "Game.Modes"]
  - Purpose: User interface screens and basic UI helpers
  - Files: MainMenuScreen.cs, GameplayScreen.cs, ResultsScreen.cs, UIManager.cs
  - Key Exports: `MainMenuScreen`, `GameplayScreen`, `ResultsScreen`, `UIManager`

**Layer 5 (Bootstrap):**
- **Game.Bootstrap.asmdef**
  - Name: "Game.Bootstrap"
  - References: ["Game.Puzzle", "Game.State", "Game.Modes", "Game.UI"]
  - Purpose: Application startup and dependency wiring only
  - Files: GameBootstrap.cs
  - Key Exports: None (entry point only)

**Test Assembly:**
- **Game.Tests.asmdef**
  - Name: "Game.Tests"
  - References: ["Game.Puzzle", "Game.State", "Game.Modes"]
  - Purpose: Unit and integration tests for game logic
  - Files: All test files
  - defineConstraints: ["UNITY_INCLUDE_TESTS"]

### Dependency Graph

```
GameBootstrap (Layer 5)
  ├── Game.UI (Layer 4)
  │   ├── Game.Modes (Layer 3)
  │   │   ├── Game.State (Layer 2)
  │   │   │   └── Game.Puzzle (Layer 1)
  │   │   └── Game.Puzzle
  │   └── Game.State
  │       └── Game.Puzzle
  ├── Game.Modes
  │   ├── Game.State
  │   │   └── Game.Puzzle
  │   └── Game.Puzzle
  └── (all layers transitively)

Game.Tests
  ├── Game.Modes
  │   ├── Game.State
  │   │   └── Game.Puzzle
  │   └── Game.Puzzle
  └── Game.Puzzle
```

**No circular dependencies. No cross-layer references except downward.**

---

## Part 2: Module Specifications

### Game.Puzzle Assembly — The Word Engine

**Responsibility:** Generate puzzles, validate words, maintain word graph. No knowledge of game modes or UI.

**Core Classes:**

1. **WordGraph** (existing, proven)
   - Maintains adjacency relationships between words
   - `AddWord(string word)` — add word to graph
   - `BuildAdjacencies()` — compute all one-letter-change connections
   - `GetAdjacent(string word)` — return list of adjacent words

2. **WordValidator** (existing, proven)
   - Validates if a word is in the word graph
   - `IsValid(string word, WordGraph graph)` — true if word exists and is connected

3. **PuzzleGenerator** (existing, proven, minor cleanup)
   - Generates puzzle instances with start word, end word, and solution path
   - `GenerateRandomPuzzle(Difficulty difficulty)` — returns Puzzle with solution
   - `GeneratePuzzle(string startWord, string endWord)` — returns specific puzzle

4. **WordPuzzle** (data class)
   - Immutable puzzle data: puzzleId, startWord, endWord, optimalSteps, solution (array), seedValue, difficulty
   - Read-only properties, no business logic

5. **Word** (data class)
   - Represents a single word: text, frequency (optional)
   - Simple value object

**Constraints:**
- Zero dependencies on Game.State, Game.Modes, Game.UI, or UnityEngine
- Self-contained, testable in pure C#
- Word list loaded at startup via Resources.Load

---

### Game.State Assembly — State Management

**Responsibility:** Track game state, apply game actions, compute new state. Single source of truth for what's happening.

**Core Classes:**

1. **GameState** (rewritten, simplified)
   - **Read-only data class:**
     - `WordPuzzle puzzle` — current puzzle
     - `List<string> wordChain` — words submitted so far (includes start word)
     - `int score` — current score (0-based)
     - `int wordsFound` — count of valid submissions
     - `float elapsedTime` — seconds elapsed (0.0 for modes that don't track time)
   - **No methods.** Pure data container. Immutable externally.

2. **GameStateManager** (rewritten, ~100 lines)
   - **Responsibility:** Apply actions, compute new state, hold current state
   - **Key Methods:**
     - `Initialize(WordPuzzle puzzle)` — start new puzzle
     - `SubmitWord(string word)` — validate and add word, return success/fail
     - `GetCurrentState()` — returns current GameState (read-only)
     - `GetFinalStats()` — returns ModeStats after puzzle complete
     - `Tick(float deltaTime)` — update elapsed time (called by Time Attack mode)
   - **No events, no observers.** Modes poll state via `GetCurrentState()`.
   - **Scoring:** Simple formula: (words found) × (10 + difficulty multiplier)

3. **GameAction** (data class)
   - Base class for all game actions (SubmitWordAction, etc.)
   - Used internally by modes to communicate with state manager

4. **ModeStats** (data class)
   - **Return value for mode completion:**
     - `string modeName` — "Classic", "Puzzle Show", "Time Attack"
     - `int wordsFound`
     - `int totalSteps` — steps in solution path
     - `float accuracy` — (words found / total steps) × 100
     - `float timeElapsed` — seconds (0 for modes without timer)
     - `int score`

**Constraints:**
- Stateless game logic. State manager is a pure state machine: `CurrentState + Action → NewState`
- No side effects in state methods
- Modes do not mutate state directly; they call `SubmitWord()` or `Tick()`

---

### Game.Modes Assembly — Game Mode Implementations

**Responsibility:** Implement the three playable game modes. Each knows how to play itself.

**Core Classes:**

1. **IGameMode** (interface, unchanged)
   - `Initialize(GameModeContext context)` — set up mode
   - `StartGame()` — begin a new game
   - `HandleInput(GameAction action)` — process user action
   - `Tick(float deltaTime)` — called every frame
   - `GetStats()` — return ModeStats when complete
   - `event Action<ModeStats> ModeCompleted` — fired when mode ends

2. **ClassicMode** (rewritten, ~150 lines)
   - Standard word chain puzzle
   - No time limit
   - On every valid word: increase score, update UI
   - Complete when player finds solution path OR gives up (5 invalid attempts)
   - **Tick():** No-op (no timer)
   - **GetStats():** Return words found, accuracy, solution length, score

3. **PuzzleShowMode** (rewritten, ~150 lines)
   - Shows complete solution path upfront
   - Player must follow exact path
   - No deviation allowed
   - Faster completion, teaches solution
   - **Tick():** No-op
   - **GetStats():** Return completion accuracy, time taken, score

4. **TimeAttackMode** (rewritten, ~200 lines)
   - 60-second timer (hardcoded for MVP)
   - Find as many valid words as possible
   - No solution requirement; any valid word counts
   - **Tick():** Decrement timer, end when time = 0
   - **GetStats():** Return words found in time, time used, score

5. **ModeController** (rewritten, ~100 lines)
   - Manages mode switching
   - Holds current active mode
   - `SwitchMode(ModeType modeType)` — clean up old mode, start new one
   - `event Action<ModeStats> ModeCompleted` — forward from current mode

**Constraints:**
- Each mode is 150-250 lines. If longer, split it.
- Modes do NOT directly mutate UI. They call state manager and UI manager.
- Modes do NOT manage puzzle data. State manager provides the puzzle.
- Each mode is independently testable with a mock state manager.

---

### Game.UI Assembly — User Interface

**Responsibility:** Display game state, accept user input, show results. No game logic.

**Core Classes:**

1. **MainMenuScreen** (rewritten, ~80 lines)
   - Three buttons: Classic, Puzzle Show, Time Attack
   - On click: `ModeController.SwitchMode()` → mode initializes → mode calls `GameplayScreen.Show()`

2. **GameplayScreen** (rewritten, ~150 lines)
   - Display: current puzzle (start word, end word), word chain submitted so far, score
   - Input field: accept player word submission
   - On submit: `GameStateManager.SubmitWord(input)` → display feedback (valid ✓ / invalid ✗) → update chain display
   - Button: "Give Up" → end mode → show results

3. **ResultsScreen** (rewritten, ~120 lines)
   - Display: mode name, words found, accuracy %, time (if applicable), score
   - Button: "Play Again" → return to main menu
   - On click: `ModeController.SwitchMode(ModeType.MainMenu)` (or equivalent)

4. **UIManager** (new, ~60 lines)
   - Centralized screen management
   - `ShowScreen<T>()` — display screen by type, hide others
   - `HideScreen<T>()` — hide specific screen
   - Manages Canvas, screen GameObjects

**Constraints:**
- Screens do NOT contain game logic. They display state and forward input.
- Screens do NOT directly reference modes or state manager. They go through UIManager.
- No animations. Text updates, button clicks, that's it.
- No tween systems, no fade effects, no complex transitions.

---

### Game.Bootstrap Assembly — Startup

**Responsibility:** Wire everything together. One file, ~80 lines.

**GameBootstrap.cs:**
1. In `Awake()`:
   - Create word graph from word list (hardcoded for MVP)
   - Instantiate Game.Puzzle components (PuzzleGenerator, WordValidator)
   - Instantiate Game.State components (GameStateManager)
   - Instantiate Game.Modes components (ModeController)
   - Instantiate Game.UI components (UIManager)
   - Wire mode completion → show results
   - Wire main menu button → mode switch
   - Show main menu

**Constraints:**
- No game logic in GameBootstrap
- Every dependency is explicitly constructed and wired
- If a constructor breaks, it fails here, visibly

---

## Part 3: File Structure

```
Assets/
├── Scripts/
│   ├── Game.Puzzle.asmdef
│   ├── Puzzle/
│   │   ├── PuzzleGenerator.cs
│   │   ├── WordValidator.cs
│   │   ├── WordGraph.cs
│   │   ├── Word.cs
│   │   ├── PuzzleData.cs
│   │   └── Constants.cs
│   │
│   ├── Game.State.asmdef
│   ├── Core/
│   │   ├── Engine/
│   │   │   └── GameStateManager.cs
│   │   └── Models/
│   │       ├── GameState.cs
│   │       ├── GameAction.cs
│   │       └── ModeStats.cs
│   │
│   ├── Game.Modes.asmdef
│   ├── Game/
│   │   ├── IGameMode.cs
│   │   ├── ModeController.cs
│   │   └── Modes/
│   │       ├── ClassicMode.cs
│   │       ├── PuzzleShowMode.cs
│   │       └── TimeAttackMode.cs
│   │
│   ├── Game.UI.asmdef
│   ├── UI/
│   │   ├── UIManager.cs
│   │   ├── Screens/
│   │   │   ├── MainMenuScreen.cs
│   │   │   ├── GameplayScreen.cs
│   │   │   └── ResultsScreen.cs
│   │   ├── Components/
│   │   │   ├── LetterTile.cs
│   │   │   ├── WordChainDisplay.cs
│   │   │   └── CurrentWordInput.cs
│   │
│   ├── Game.Bootstrap.asmdef
│   └── Game/
│       └── GameBootstrap.cs
│
├── Tests/
│   ├── Game.Tests.asmdef
│   ├── Integration/
│   │   ├── ClassicModeIntegrationTests.cs
│   │   ├── PuzzleShowModeIntegrationTests.cs
│   │   └── TimeAttackModeIntegrationTests.cs
│   ├── Unit/
│   │   ├── Engine/
│   │   │   ├── GameStateManagerTests.cs
│   │   │   └── PuzzleGeneratorTests.cs
│   │   └── Modes/
│   │       ├── ClassicModeTests.cs
│   │       ├── PuzzleShowModeTests.cs
│   │       └── TimeAttackModeTests.cs
│
├── Scenes/
│   └── GameUI.unity (single scene, all screens in Canvas)
│
└── Resources/
    └── Data/
        └── wordlist.txt (word data)
```

**Deleted entirely:**
- All animation files (UIAnimations.cs, *.anim)
- Economy system (CoinSystem.cs, EconomyManager.cs if simplified to nothing)
- Persistence system (PlayerDataManager.cs, SaveData.cs if not needed for MVP)
- Monetization folder

---

## Part 4: Data Flow Example — User Submits a Word

1. **User types "bat" and presses Submit** in GameplayScreen
2. **GameplayScreen** calls `GameStateManager.SubmitWord("bat")`
3. **GameStateManager** calls `WordValidator.IsValid("bat", wordGraph)`
4. **WordValidator** checks if "bat" is in the graph → returns `true`
5. **GameStateManager**:
   - Adds "bat" to wordChain
   - Recomputes score: (words found) × (10 + difficulty)
   - Returns `true` (success)
6. **GameplayScreen** receives `true`
   - Displays "✓ Valid!" feedback
   - Calls `WordChainDisplay.AddWord("bat")`
   - Calls `UIManager.UpdateScore(newScore)`
7. **GameplayScreen** calls `GameStateManager.GetCurrentState()`
8. **Checks for puzzle completion:**
   - If wordChain[-1] == endWord: mode is complete
   - Call `ModeController.EndMode(stats)`
9. **ModeController** fires `ModeCompleted` event
10. **GameBootstrap** listener shows ResultsScreen

---

## Part 5: Testing Strategy

### Unit Tests (Game.Tests assembly)

**GameStateManager Tests:**
- Initialize with puzzle
- Submit valid word → state updates, score increases
- Submit invalid word → state unchanged, returns false
- Submit duplicate → rejected
- Completion detection → correct endWord triggers done

**Mode Tests (each mode independently):**
- Initialize with mock state manager
- Verify mode calls correct state manager methods
- Verify stats returned correctly
- Verify Tick() updates correctly (for Time Attack)

**Integration Tests:**
- Full flow: start Classic Mode → submit 3 valid words → complete → results correct
- Full flow: start Time Attack → wait 60 seconds → mode ends
- Full flow: switch modes → old mode cleaned up, new mode initialized

### Manual Testing Script

Before declaring a mode "done":
1. Play through to completion
2. Verify every UI element updates correctly
3. Verify score calculation is correct
4. Verify invalid words are rejected with feedback
5. Verify completion is detected correctly
6. Verify mode can be replayed without errors
7. Verify mode switch works cleanly

---

## Part 6: Success Criteria

**When is the rebuild done?**

1. ✅ All five assemblies compile without errors or warnings
2. ✅ No circular dependencies (verify via assembly structure)
3. ✅ GameBootstrap component can be added to Bootstrap GameObject
4. ✅ Game starts, shows main menu
5. ✅ Each mode can be started from menu
6. ✅ Words can be submitted, validated, and displayed
7. ✅ Each mode can be completed and shows results
8. ✅ Results screen shows correct stats (words found, accuracy, score)
9. ✅ Mode switching works cleanly (no memory leaks, no stale UI)
10. ✅ Each mode is playable end-to-end with zero console errors
11. ✅ Full game flow (menu → mode → results → menu → different mode) works
12. ✅ All tests pass (unit + integration)
13. ✅ Code is simple, focused, understandable (no line of code does two things)

---

## Implementation Order

1. **Phase 1: Foundation** — Build Game.Puzzle and Game.State assemblies first (they have no dependencies)
2. **Phase 2: Gameplay** — Build Game.Modes on top of State
3. **Phase 3: UI** — Build Game.UI to display Modes
4. **Phase 4: Bootstrap** — Wire everything in GameBootstrap
5. **Phase 5: Tests** — Write comprehensive tests
6. **Phase 6: Integration & Polish** — Full playthrough testing, bug fixes, final verification

---

## Notes for Implementation

- **Every assembly must compile independently.** If Game.Modes won't compile because Game.State has an error, we know exactly where to look.
- **Every class should be < 300 lines.** If a class exceeds that, split it.
- **Every method should do one thing.** If a method is > 20 lines, it probably does two things.
- **No magic numbers.** All constants go in Constants.cs (Game.Puzzle) or mode-specific constants at the top of each mode file.
- **Fail loudly.** If assembly loading fails, throw an error immediately. Don't silently skip dependencies.

