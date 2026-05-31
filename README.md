# Word Ladder

A modern, mobile-portrait word-ladder puzzle game built in Unity 6000.4.6f1. Transform a start word into an end word one letter at a time — every intermediate step must be a real English word that differs from the previous word by exactly one letter.

```
FROM  C  A  T            FROM  S  T  O  N  E
                                ↓
       ↓                         S  T  O  R  E
                                ↓
      B  A  T  ← changed 1       S  T  A  R  E
                                ↓
      B  A  G  ← changed 1       S  H  A  R  E
                                ↓
 TO   B  A  G            TO     S  H  A  R  P
```

---

## Game modes

| Mode | Timer | Puzzles | Win condition |
|---|---|---|---|
| **Classic** | None | Random 3–7 letter words, BFS-generated | Reach the end word; next puzzle auto-loads |
| **Puzzle Show** | None | Curated tier library (15 puzzles per tier × 6 tiers = 90) | Reach end word; tap a card to play any unlocked puzzle |
| **Time Attack** | 60s or 120s, Timed or Survival | Random 3–7 letter words back-to-back | Solve as many as you can before the timer runs out |

### Time Attack sub-modes
- **Timed**: Fixed countdown from 60s or 120s. No time rewards.
- **Survival**: Each completed puzzle adds reward seconds (+10s for the 60s base, +15s for the 120s base) so a skilled player can play forever.

---

## Power-ups

| Power-up | Effect | Available in |
|---|---|---|
| **Hint** | Gold-highlights the position in your current word that needs to change to reach the next solution step | All modes |
| **Reveal** | Shows the next word in the solution path as a ghosted preview row | All modes |
| **Undo** | Pops the last accepted chain word | All modes |
| **+Time** | Adds seconds to the clock: +5s (60s base, 3 charges) or +10s (120s base, 2 charges) | Time Attack only |

Each puzzle starts with a fixed budget per power-up. Submitting a valid word or using Undo clears any active Hint/Reveal preview to keep the UI honest.

---

## Puzzle Show tier progression

The library is organized into 6 tiers of increasing difficulty:

| Tier | Word length | Optimal steps | Puzzle count |
|---|---|---|---|
| 1 | 3 letters | 2 | 15 |
| 2 | 3 letters | 3 | 15 |
| 3 | 4 letters | 2–3 | 15 |
| 4 | 4 letters | 4 | 15 |
| 5 | 5 letters | 2 | 15 |
| 6 | 5 letters | 3–4 | 15 |

Tier 1 is unlocked by default. Complete **10 puzzles in the current tier** to unlock the next tier. Progress (completed puzzle IDs, in-progress IDs, current tier) is persisted to `PlayerPrefs` under the key `puzzle_progress_v1`.

The Puzzle Library screen renders each puzzle as a level card with state:
- **Locked** — grey card, padlock affordance (higher tier)
- **Unlocked / Unplayed** — neutral card with `FROM → TO` preview and step count
- **In Progress** — gold border
- **Completed** — green border with checkmark

---

## Word validation

A submitted word is accepted onto the chain only if all of these hold:

1. Exists in the 8,399-word curated dictionary (`Assets/Resources/Data/word_library.json`).
2. Differs from the previous chain word by **exactly one letter at the same position** (Hamming-1).
3. Is the same length as the previous chain word.
4. Has not already been used in the current chain.

When a word is rejected, the screen surfaces a specific reason via `OnWordSubmissionResult`:

- "Word not in dictionary" — fails dictionary check
- "Must change exactly one letter" — fails Hamming-1
- "Word must be N letters" — wrong length (from `GameStateManager` wrapper)
- "Word already used"
- "Type a word" — empty (from `GameStateManager` wrapper)

Rejected submissions never end the puzzle. There are no "lives" — players keep typing until they reach the end word or quit.

---

## Settings

Reachable from the main menu. Persisted to `PlayerPrefs` key `settings_v1`.

- **Master volume** slider (0–100) — drives `AudioListener.volume`
- **SFX volume** slider (persisted; AudioMixer integration planned)
- **Music volume** slider (persisted)
- **Mute All** toggle — forces `AudioListener.volume` to 0
- **Reset Progress** — wipes `puzzle_progress_v1` and player progress with a confirmation modal. Settings themselves are preserved.

---

## Architecture

### Core layout
```
Assets/
├── Resources/
│   └── Data/
│       ├── word_library.json        # 8,399 curated 3–5 letter words
│       └── tier_definitions.json    # 90 puzzles × 6 tiers, validated ladders
├── Scenes/
│   └── GameUI.unity                  # Single-scene UI app
├── Scripts/
│   ├── Core/
│   │   ├── Engine/                   # GameState, GameStateManager, GameAction
│   │   └── Persistence/              # IDataManager, DataManager (PlayerPrefs)
│   ├── Game/
│   │   ├── GameBootstrap.cs          # Wires the whole graph; mode routing
│   │   └── Modes/                    # ClassicMode, PuzzleShowMode, TimeAttackMode + TimeAttackConfig
│   ├── Puzzle/
│   │   ├── WordGraph.cs              # HashSet-backed dictionary + neighbor lookup
│   │   ├── WordValidator.cs          # Hamming-1 + dictionary check + repeat check
│   │   └── PuzzleGenerator.cs        # BFS over the word graph
│   └── UI/
│       ├── UIManager.cs              # Screen orchestration
│       └── Screens/
│           ├── MainMenuScreen.cs
│           ├── GameplayScreen.cs     # The ladder view (start/chain/preview/end)
│           ├── PuzzleLibraryScreen.cs
│           ├── TimeAttackSetupScreen.cs
│           ├── SettingsScreen.cs
│           └── ResultsScreen.cs
├── Tests/
│   ├── Unit/                         # GameStateManagerTests, LetterTileTests, etc.
│   └── Integration/
└── Editor/
    ├── VerifyWordLibrary.cs          # Tools/Verify Library/Run — validates all 90 puzzles
    ├── VerifyPuzzles.cs              # Tier-gate + UI probes
    ├── VerifyLadder.cs               # Hint/Reveal/Undo semantics probes
    ├── VerifyRedesign.cs             # Badge/chain/HOME button probes
    ├── VerifyPolish.cs               # Visual polish probes
    └── SceneBuilder7.cs              # Idempotent rebuild of TimeAttackSetupScreen + AddTime
```

### State flow
`GameStateManager` owns an immutable `GameState` snapshot plus a private `MutableGameState`. UI subscribes to state changes; `GameAction` instances are dispatched through `Dispatch()` which routes to handlers (`HandleSubmitWord`, `HandleUseHint`, `HandleUseReveal`, `HandleUndo`, `HandleUseAddTime`). Each handler mutates the working state, then notifies subscribers and persists.

### Ladder UI semantics
`GameplayScreen` renders five layered row types inside the chain view:
1. **Start word row** — anchored top, FROM label
2. **Chain history rows** — each accepted word; the tile at the position that *changed from the previous row* is highlighted green (#6AAA64) using a diff-highlight algorithm
3. **Current input row** — what the player is typing. If a hint is active, the indicated tile is gold (#C9B458)
4. **Reveal preview row** — only visible when reveal is active. Outline-only tiles, except the changed-letter index in gold
5. **End word row** — anchored bottom, TO label. Turns green when the chain reaches it

Auto-scrolls 180ms ease-out after every chain mutation so the current-input row sits 8px above the end-word row.

### Mode routing
- `MainMenu → Classic` → `StartClassicMode()` → random length 3–7 puzzle
- `MainMenu → Puzzle Show` → opens `PuzzleLibraryScreen`; tapping a card calls `OnLibraryPuzzleSelected(int puzzleId)` → starts that specific puzzle
- `MainMenu → Time Attack` → opens `TimeAttackSetupScreen` (2×2 grid) → on confirm calls `StartTimeAttackModeWithConfig(TimeAttackConfig)` → starts a session
- **HOME** button on any in-game screen returns to MainMenu and tears down the active mode cleanly

---

## Building and running

### Requirements
- Unity 6000.4.6f1 (Unity 6 LTS)
- TextMeshPro (included in Unity)
- Designed for portrait 1080×1920; CanvasScaler matches height

### Open the project
1. Clone this repo
2. Open the root folder in Unity Hub → **Add project from disk**
3. Open `Assets/Scenes/GameUI.unity`
4. Press **Play**

### Running tests
- Edit-mode unit tests: **Window → General → Test Runner → EditMode → Run All**
- Tests live in `Assets/Tests/Unit/` and `Assets/Tests/Integration/`

### Editor verification utilities
The `Tools/` menu contains agent-built menu items for in-editor probes:
- `Tools/Verify Library/Run` — confirms all 90 puzzles pass Hamming-1 + dictionary validation
- `Tools/Verify Puzzles/*`, `Tools/Verify Ladder/*`, `Tools/Verify Polish/*`, `Tools/Verify Redesign/*` — per-feature probes
- `Tools/SceneBuilder7/Run All` — idempotent scene rebuild for `TimeAttackSetupScreen` + AddTime button
- `Tools/Tester7/*` — quick navigation between screens for visual QA

---

## Design tokens

| Token | Hex | Use |
|---|---|---|
| `bg-base` | `#0F1217` | Screen backgrounds |
| `bg-surface` | `#1B1F27` | Buttons, panels, tiles |
| `surface-2` | `#242936` | Filled letter tiles |
| `accent-gold` | `#C9B458` | Targets, hints, titles |
| `accent-green` | `#6AAA64` | Correct chain, success, +TIME |
| `accent-red` | `#C9215C` | Destructive actions, reveal accent |
| `text-primary` | `#E7E1C4` / `#F5F7FA` | Body, button labels |
| `text-muted` | `#8A93A1` | Subtitles, FROM label |
| `text-dim` | `#5A6270` | Locked card text, version |

---

## Project history

The project was built iteratively through seven AI-orchestrated swarms, each focused on a single concern. The git log captures the progression:

- `swarm-1` Word library, settings, redesign, ladder semantics
- `swarm-2/3` Modern polish: letter tiles, on-screen keyboard, hint/undo/reveal feedback
- `swarm-3` Library cards, tier gate, FROM/TO labels
- `swarm-4` Home button, badge reparenting, settings feature
- `swarm-5` Ladder hint/reveal semantics (next-word, changed-position)
- `swarm-6` Word submit bug fix, per-mode behaviors, AddTime power-up
- `swarm-7` UI completion: TimeAttackSetupScreen + AddTime button in scene

See `Assets/Screenshots/` for chronological visual snapshots.

---

## Status

Shippable v1.0. Polish backlog tracked for v1.1:
- Library card badge anchor unification
- HOME button visual treatment consistency
- Locked-card affordance (alpha/glyph)
- Results stats layout merge
- PuzzleShow tier indicator emphasis
- 4-power-up row spacing balance
- Empty placeholder tile labeling
