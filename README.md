# Word Ladder

A modern, mobile-portrait word-ladder puzzle game built in **Unity 6000.4.6f1** (Unity 6 LTS), portrait **1080×1920**. Transform a start word into an end word one letter at a time — every intermediate step must be a real English word that differs from the previous word by exactly one letter.

```
FROM  C  A  T            FROM  S  T  O  N  E
       |                        S  T  O  R  E   ← changed 1
      B  A  T  ← changed 1      S  T  A  R  E   ← changed 1
      B  A  G  ← changed 1      S  H  A  R  E   ← changed 1
 TO   B  A  G            TO     S  H  A  R  P   ← changed 1
```

> **This README is also the canonical context document for AI-assisted development.** It is written so an LLM (e.g. Claude Opus) can read it and author precise, surgical task prompts ("master prompts") for this repo. See **[§14 Writing a master prompt](#14-writing-a-master-prompt-for-this-repo)** and the **[Shared Context Block](#shared-context-block-paste-into-every-task-prompt)** at the end.

---

## Table of contents
**📱 [Screens](#screens)** — a visual tour of every screen

1. [Game modes](#1-game-modes)
2. [Power-ups](#2-power-ups)
3. [Economy & monetization](#3-economy--monetization)
4. [Juice: motion, haptics, sound](#4-juice-motion-haptics-sound)
5. [Visual identity](#5-visual-identity)
6. [First-launch tutorial](#6-first-launch-tutorial)
7. [Puzzle Show tiers](#7-puzzle-show-tiers)
8. [Word validation](#8-word-validation)
9. [Balance config — the single source of truth](#9-balance-config--the-single-source-of-truth)
10. [Architecture](#10-architecture)
11. [Persistence keys](#11-persistence-keys)
12. [Testing & tooling](#12-testing--tooling)
13. [Known tech debt / candidate tasks](#13-known-tech-debt--candidate-tasks)
14. [Writing a master prompt for this repo](#14-writing-a-master-prompt-for-this-repo)
15. [Design tokens](#15-design-tokens)
16. [Building & running](#16-building--running)
17. [Notes for AI agents working in this repo](#17-notes-for-ai-agents-working-in-this-repo)

---

## Screens

> Live captures (iPhone 13 Pro Max portrait). The UI follows the **true-black, outline ("ghost")** identity in [§5](#5-visual-identity) / [§15](#15-design-tokens) — colored rounded outlines over a black backdrop (a swappable full-screen space layer), each action in its own color.
>
> ⚠️ **Note:** the thumbnails below predate the black/outline overhaul (they show the earlier dark, gold-accented look) — they'll be refreshed in a later pass.

| Screen | |
|---|---|
| **Main Menu** — white `WORD LADDER` masthead on black. Buttons are **colored rounded outlines** (transparent centers): **DAILY** is the hero — a thicker, brighter **orange** ring (primary call-to-action; shows the streak once today is solved). The modes each carry their own color — **Classic** green, **Puzzle Show** violet, **Time Attack** red, **Resume** (only with an in-progress save) teal. **Puzzle Library / Stats** are a muted side-by-side row (Settings lives in the shared top-right gear). | <img src="docs/screenshots/main-menu.png" width="250"> |
| **Classic Mode** — the core word ladder on black. The **start** word is a row of **teal see-through outline tiles** (the origin); the **target** word is **orange outline tiles** (the goal); between them the played **chain** and the **active input row** stay solid/filled (the "fill here" zone, gold-edged as you type). Below: the Hint / Undo / Reveal **power-up bar** (outline buttons) above the QWERTY keyboard (red `DEL`, green `GO`). An icon **HOME** (top-left) and the shared **Settings** gear (top-right) flank a calm score header. Random 3–7-letter puzzles; on a solve a **compact win panel** ("Next Puzzle" / "Home") keeps you in the loop ([§1](#1-game-modes)). | <img src="docs/screenshots/classic-mode.png" width="250"> |
| **Puzzle Show** — tier-progression play on the same gameplay screen, with a `Tier X / Y` indicator under the score. **350 curated ladders (7 tiers × 50)** on a length/difficulty curve (Tier 1 easy 3-letter → Tier 7 hard 7-letter, up to 8-step ladders). Solving shows a stat screen offering **Next Puzzle / Tier N ▸ / Home** ([§7](#7-puzzle-show-tiers)). | <img src="docs/screenshots/puzzle-show.png" width="250"> |
| **Time Attack** — a countdown timer + the **+Time** power-up; chosen as 60s/120s × Timed/Survival on a setup screen. Ladders **auto-advance** as you solve them; the full results screen (puzzles solved + **Play Again** → new run) appears only when the **timer hits 0**. | <img src="docs/screenshots/time-attack.png" width="250"> |
| **Puzzle Library → Tier Select** (level 1) — the entry to Puzzle Show: a list of **7 tiers**, each with its theme (e.g. "3-letter words"), progress (`X/50`) and lock state (**gold** = current tier; **padlock + "Clear N in Tier M"** = locked). Tap an unlocked tier to open its grid. | <img src="docs/screenshots/puzzle-library.png" width="250"> |
| **Puzzle Library → Tier Grid** (level 2) — the selected tier's **50** puzzle cards with a **Back** to tier-select (only the active tier renders, for performance). Cards reflect saved progress: **Completed** (green + ✓), **Unplayed** (surface grey), **Locked** (padlock). Tapping a card launches that exact puzzle. | <img src="docs/screenshots/puzzle-library-tier.png" width="250"> |
| **Stats** — current & longest daily **streak**, dailies completed, total **coins**, total puzzles, and per-mode played/won (Classic, Time Attack best round). | <img src="docs/screenshots/stats.png" width="250"> |
| **Settings** — audio sliders (master / SFX / music), accessibility toggles (mute, reduce-motion, haptics, colorblind mode, high-contrast, large-text), **Reset Progress** (confirm-gated; preserves settings + tutorial flag), **Replay Tutorial**, and the build version. | <img src="docs/screenshots/settings.png" width="250"> |

> **Global chrome:** one shared **Settings** gear (icon-only, top-right, ~HOME-sized) shows on every screen *except* Settings itself and opens it — `UIManager.CreateGlobalSettingsButton` → `OnGlobalSettingsRequested` → `GameBootstrap.ShowSettings` (which populates then shows). On the gameplay screen a house **HOME** (top-left) and the gear (top-right) flank the header. Icon assets: `Assets/UI/Icons/*.svg` (Vector Graphics) + `Assets/Resources/Icons/*.png`.

---

## 1. Game modes

| Mode | Timer | Puzzles | Win condition |
|---|---|---|---|
| **Classic** | None | Random, BFS-generated; start/end restricted to a common-words subset | Reach the end word → **compact win panel** ("Next Puzzle" stays in Classic). First-ever launch routes into the tutorial. |
| **Daily** | None | One puzzle per day, identical for every player (no server) | Reach the end word → full results (streak + share, **Home** only) |
| **Puzzle Show** | None | **350 curated ladders (50 × 7 tiers)**, two-level library | Reach end word → stat screen (Next Puzzle / Tier N ▸ / Home); tap any unlocked card to play it |
| **Time Attack** | 60s or 120s, Timed or Survival | Random words back-to-back | Solve as many as possible before time runs out → full results + **Play Again** (new run) |

**Daily puzzle + streak** — Today's puzzle is derived from the player's **local date** with no network call: `index = (Today − 2025-01-01).Days mod N`, where `N` = pool size in `Assets/Resources/Data/daily_puzzles.json` (all entries pre-validated Hamming-1 + dictionary). Streak rules (`DailyStreakRules`, pure/testable): completing today increments `currentStreak` iff yesterday was completed; a missed day resets to 1; same-day re-completion never double-counts; `longestStreak = max(longestStreak, currentStreak)`. Persisted under `daily_v1`.

**Time Attack sub-modes** — **Timed**: fixed countdown (60s/120s), no rewards. **Survival**: each solve grants `BalanceConfig.SurvivalRewardSeconds` (15s) so a skilled player can sustain. Configured via `TimeAttackConfig` (factories `Default60`/`Default120`/`DefaultSurvival`, all read `BalanceConfig`).

**Post-win flow** (which surface shows on a solve) is decided by one pure function, `PostWinRouter.Decide(ModeKind, isDaily, puzzleComplete, timeUp)`, called by `GameBootstrap.CheckGameOver` — the single source of truth:
- **Classic** → a **compact inline win panel** overlaid on the board (`GameplayScreen.ShowWinPanel`); "Next Puzzle" starts a fresh Classic puzzle in the same mode, "Home" exits.
- **Time Attack** → solving a ladder **auto-advances** to the next (the run's clock keeps running via a one-shot `timerSeeded`); the full `ResultsScreen` (ladders solved + "Play Again" → new run) shows only when the timer expires.
- **Puzzle Show** → the full `ResultsScreen` configured with **Next Puzzle** (another in the current tier), an optional **Tier N ▸** (when the next tier just unlocked → opens the library), and **Home**.
- **Daily** → the full `ResultsScreen` (stats + streak + share) with **no "Play Again"** (never re-run the daily as a scored game) — just **Home**.
"Play Again" / "Next Puzzle" always **re-route into the active mode**, never the main menu (the old bug). `ResultsScreen.ConfigureForDaily/ForEndless/ForPuzzleShow` set button visibility/labels per context.

**Share result** — `ResultsScreen` "Share" copies a Wordle-style emoji grid to the clipboard (`ClipboardShareService`, zero third-party deps). One row per accepted step, `🟩` at the changed position, `⬛` elsewhere; mode-specific label/footer. Native image share is seam-ready (`IShareService` + `ShareCardBuilder`) but requires an approved plugin.

---

## 2. Power-ups

| Power-up | Effect | Default budget / puzzle | Coin cost | Available in |
|---|---|---|---|---|
| **Hint** | Gold-highlights the position in the current word to change next | `BalanceConfig.DefaultHintsPerPuzzle` = **3** | `HintCost` = 0 | All modes |
| **Reveal** | Shows the next solution word as a ghost preview row | `BalanceConfig.DefaultRevealsPerPuzzle` = **1** | `RevealCost` = 25 | All modes |
| **Undo** | Pops the last accepted chain word | n/a | `UndoCost` = 0 | All modes |
| **+Time** | Adds `AddTimeGrantSeconds` (10s); charges = 1 (60s base) / 2 (120s base) | from `TimeAttackConfig` | — | Time Attack only |

Reveal is deliberately **scarcer and pricier** than Hint (it's strictly stronger). Budgets seed in `GameStateManager.StartNewPuzzle` from `BalanceConfig`. Submitting a valid word or using Undo clears any active hint/reveal preview.

---

## 3. Economy & monetization

**Single economy:** `EconomyManager : IEconomyManager` (constructed + `InitializeAsync()` in `GameBootstrap`), persisting coins through `DataManager` → `PlayerProgress.totalCoins`. (A legacy `CoinSystem` MonoBehaviour also exists but is orphaned — see [§13](#13-known-tech-debt--candidate-tasks).)

**Faucet / sink model** (all amounts in `BalanceConfig`):

| Direction | Source / sink | Amount |
|---|---|---|
| 🟢 Faucet | Puzzle completion (`GrantPuzzleReward`) | `PuzzleCompletionReward` = +10 |
| 🟢 Faucet | Daily bonus (stacks) | `DailyBonusReward` = +25 |
| 🟢 Faucet | Rewarded video (opt-in) | `RewardedAdHintGrant` = +1 Hint |
| 🔴 Sink | Reveal (extra) | `RevealCost` = −25 |
| ⚪ Free baseline | Per puzzle | 3 hints + 1 reveal, regardless of balance |

**Anti-deadlock guarantee:** the free per-puzzle baseline + no fail/lives gate means a broke player can always finish; 3 completions (3×10) more than fund one Reveal (25). Power-ups accelerate, never gate — no pay-to-win.

**Ads (Google Mobile Ads, already integrated):**
- `IAdService` (in the low-dep `Puzzle` assembly so tests can mock it) → `AdService : MonoBehaviour` (real AdMob) + `NullAdService` (Editor/headless fallback).
- **Ad unit IDs are AdMob TEST IDs as `[SerializeField]` placeholders — never real IDs in source.**
- **Rewarded video is opt-in only** (`GameBootstrap.RequestRewardedHintAd` / `RequestRewardedContinue`); reward granted **exactly once** on the SDK's reward callback, **never** on dismiss/failure.
- `AdPolicyService` enforces the **interstitial frequency cap** — both a time cooldown (`InterstitialCooldownSeconds` = 300) **and** a puzzle count (`InterstitialPuzzleCap` = 5), between-session only, gated on a stubbed `AdsRemoved` flag (future "remove ads" IAP). Clock is injectable for tests.

---

## 4. Juice: motion, haptics, sound

All three feedback channels fire on the same four moments and all respect a **reduce-motion** accessibility flag.

| Moment | Animation (≤200ms ease-out) | Haptic | Sound |
|---|---|---|---|
| Letter placed | tile lock-in punch (`LetterTile.PunchScale`) | light tap | key-press |
| Word accepted | row settle + changed tile → green glow | medium tap | accept |
| Word rejected | input-row shake (skipped if reduce-motion; reason still shows) | buzz | reject |
| Puzzle won | `GameplayScreen.WinAscentBeat` (TO row gold→green, upward rise+settle, ~500ms) | buzz | win sting |

- **reduce-motion:** `SettingsData.reduceMotion` → `UIAnimations.ReduceMotion` (static, set from `GameBootstrap` on settings load/save). Every animation coroutine in `UIAnimations` and `LetterTile` snaps to the end-state and `yield break`s when true.
- **Haptics:** `IHaptics` → `HandheldHaptics(Func<bool> enabled, Action vibrate = Handheld.Vibrate)` + `NullHaptics`. `Handheld.Vibrate` is a coarse full-buzz; fine-grained haptics need a plugin (TODO, not added). Gated on `SettingsData.hapticsEnabled`. The injectable `vibrate` action makes it unit-testable.
- **Sound:** `SfxManager` (pooled `AudioSource`; clip slots assigned in-scene). No `AudioMixer`/clips in the repo yet → AudioListener-level. Pure static gate `SfxManager.EffectiveSfxVolume(SettingsData)` returns 0 when muted (testable).

---

## 5. Visual identity

True-black, **outline ("ghost")** identity with a vertical ladder/ascent metaphor.
- **Outline buttons:** every button is a **colored rounded outline with a transparent center** (not a fill), so the black backdrop shows through. Each action owns a color from the menu set — **Resume** teal, **Daily** orange (hero), **Classic** green, **Puzzle Show** violet, **Time Attack** red, **Library/Stats** muted slate. The hero (Daily) gets a **thicker, brighter** orange ring; light/white labels sit on every button. The ring + black treatment is centralized in `UITheme` (`ApplyOutlineButton` / `ApplyHeroOutlineButton`, 9-slice ring sprites under `Assets/Resources/UI/`).
- **Black background + swappable space layer:** the app renders on a neutral near-black `#0A0A0A`, painted by a single full-screen **Background layer** behind every screen (`UIThemeManager.ApplyScreenBackground` / `EnsureBackgroundLayer`). It auto-loads `Assets/Resources/UI/SpaceBackground.png` if present — drop a sprite there to swap in a space backdrop with no restructuring (a pixel-art starfield ships now).
- **Gameplay tiles:** the **start** word row is **teal** see-through outline tiles, the **target** row is **orange** see-through outline tiles, and the **active input row stays solid** (the obvious "current row," gold-edged as you type). Settled chain rows stay solid; the win beat turns the target solid green.
- **Gold is now in-game only:** `accent-gold #C9B458` is no longer a menu color — it's reserved for in-game focus (hint / active-input tiles, the win "Next Puzzle", in-progress & current-tier rings, the streak headline).
- **Ascent:** the chain climbs toward the anchored TO row at the bottom; the win beat reinforces upward motion.
- **Motion vocabulary** (one place: `UIAnimations`): `MICRO = 0.16s` (micro-interactions), `STANDARD = 0.22s` (transitions), `EaseOutCubic`. Deliberate and weighted — no cartoon bounce. All restyles are static and honor ReduceMotion.

---

## 6. First-launch tutorial

On first launch (flag `onboarding_v1` absent/incomplete), tapping **Classic** routes into a scripted tutorial instead of a random puzzle.
- Fixed ladder **CAT → BAT → BAG** (`TutorialPuzzle.Create()`), injected like the daily puzzle.
- `TutorialOverlay` — non-modal step-gated coach marks (accent-gold emphasis) with a **Skip** button at every step; advances only on intended actions; rejection reuses the existing `OnWordSubmissionResult` feedback; a short success beat then drops into the first real puzzle.
- Gating logic is pure/testable: `OnboardingRules.ShouldRouteToTutorial / MarkCompleted / Reset`. Persisted as `OnboardingData { completed, skipped }` under `onboarding_v1`.
- **Replay tutorial** in Settings clears the flag. The flag **survives Reset Progress** (only Replay clears it). If the overlay isn't wired, the gate no-ops so a player is never stranded.

---

## 7. Puzzle Show tiers

**7 tiers × 50 = 350 curated ladders** (`MaxTier = 7`, `PuzzlesPerTier = 50`), on a length/difficulty curve. Every puzzle clears the [§8](#8-word-validation) **minimum-move floor** (true full-dictionary shortest ≥ 2) and rises across tiers:

| Tier | Word length | Moves (steps) |
|---|---|---|
| 1 | 3 | 2–3 |
| 2 | 4 | 2–3 |
| 3 | 5 | 3–4 |
| 4 | 5–6 | 3–4 |
| 5 | 6 | 4–5 |
| 6 | 6–7 | 4–6 |
| 7 | 7 | 4–8 (hardest) |

**Two-level navigation** (`PuzzleLibraryScreen`): **Tier Select** (7 tiers with theme + `X/50` + lock state) → tap an unlocked tier → **Tier Grid** (that tier's 50 cards + Back). Only the active tier's cards render (performance).

**Progressive unlock:** Tier 1 is unlocked by default; clearing `BalanceConfig.PuzzlesRequiredToAdvance(tier)` puzzles unlocks the next — **10 / 15 / 20 / 25 / 30 / 35** out of tiers 1–6 (rises with depth; Tier-1 stays 10 for the wiring test). Unlocked tiers stay open, so a player can return to any tier for completion.

**Completion coloring** is driven by saved progress, resolved by the pure `PuzzleShowMode.ResolveState(puzzleId, tierUnlocked, completed, inProgress)` (shared by the live mode and the library so card state matches gameplay exactly): **Completed** → green + ✓ (the ✓ is a non-color cue for colorblind mode), **In Progress** → gold border, **Unlocked/Unplayed** → surface grey, **Locked** → padlock. `GameBootstrap.ShowLibrary` injects the saved `PuzzleProgressData` into the screen via `PuzzleLibraryScreen.SetProgress(completed, inProgress, highestUnlockedTier)` before it populates.

Progress (`PuzzleProgressData`: completed IDs, in-progress IDs, current tier) persists under `puzzle_progress_v1`. The authoritative tier→puzzleId map comes from `tier_definitions.json` (never hardcoded math). **`tier_definitions.json` is machine-generated** by `Tools/puzzleshow_build.py` — see [§12](#12-testing--tooling). Tapping a card → `OnLibraryPuzzleSelected(int puzzleId)` → `StartSpecificPuzzle`.

---

## 8. Word validation

`WordValidator : IWordValidator` accepts a word onto the chain only if **all** hold:
1. Exists in the 12,183-word curated dictionary (`word_library.json`; 3-letter 534 / 4-letter 2,234 / 5-letter 4,304 / 6-letter 2,535 / 7-letter 2,576).
2. Differs from the previous chain word by **exactly one letter at the same position** (Hamming-1, via `WordOps.HaveOneLetterDifference`).
3. Same length as the previous word.
4. Not already used in the current chain.

Distances are computed **once per puzzle**: `WordValidator.Initialize` caches `WordGraph.ComputeDistancesFrom(target)` so `ValidateWord` does **zero BFS per submission**. `isProgress` = strictly closer to the target than the previous word.

**Minimum-move floor (no 1-move puzzles).** Every puzzle — generated *and* curated — must require at least **2 moves**, scaled by word length via `BalanceConfig.MinMovesForLength(len)` (**3→2, 4→2, 5→3, 6→3, 7→4**; hard floor `AbsoluteMinMoves = 2`). The floor is measured by **true full-dictionary BFS shortest distance**, not the length of the common-word path a generator happened to walk — a pair that *looks* like 4 moves but is solvable in 1 via a non-common word is rejected. `PuzzleGenerator.MeetsFloor` enforces this at every generation stage (strict-common → relaxed → fallback), and the curated `tier_definitions.json`/`daily_puzzles.json` are validated the same way (`MinMovesFloorTests` + the build tools). The hardcoded ultimate fallback (`cat→cot→cog→dog`, 3 moves) also satisfies it.

On rejection, `GameStateManager` surfaces a user-facing reason via the `OnWordSubmissionResult` event (consumed by `GameBootstrap` → `GameplayScreen`). User strings: `"Not a real word"`, `"Already used"`, `"Change exactly one letter"`, `"Word must be N letters"`, `"Type a word"`. Rejected submissions never end the puzzle — there are **no lives**; players keep typing until they reach the end word or quit.

> Note: the validator currently returns English `Message` strings that `GameStateManager.MapValidationMessage` re-parses with `IndexOf` to pick the reason — brittle if reworded. A typed-enum refactor is a known candidate task ([§13](#13-known-tech-debt--candidate-tasks)).

---

## 9. Balance config — the single source of truth

`Assets/Scripts/Puzzle/BalanceConfig.cs` (global namespace, lowest-dependency `Puzzle` assembly so everything can read it). **All tunable numbers live here** — never reintroduce magic-number literals in consumers.

```
Power-ups:   DefaultHintsPerPuzzle=3  DefaultRevealsPerPuzzle=1
             HintCost=0  RevealCost=25  UndoCost=0
Time Attack: TimeAttackBaseSecondsShort=60  TimeAttackBaseSecondsLong=120
             AddTimeChargesShort=1  AddTimeChargesLong=2
             AddTimeGrantSeconds=10  SurvivalRewardSeconds=15
Generation:  MaxBfsDepth=10  MaxGenerationAttempts=20
             Easy/Medium/HardWordLength=3/4/5
             Easy/Medium/HardTargetDistance=2/4/6
             AbsoluteMinMoves=2   MinMovesForLength(len): 3→2 4→2 5→3 6→3 7→4
Tiers:       MaxTier=7  PuzzlesPerTier=50  PuzzlesRequiredToAdvanceTier=10 (base)
             PuzzlesRequiredToAdvance(tier): 10/15/20/25/30/35/40 (rises with depth)
Economy:     PuzzleCompletionReward=10  DailyBonusReward=25  RewardedAdHintGrant=1
Ads:         InterstitialCooldownSeconds=300  InterstitialPuzzleCap=5
```

`Constants.cs` forwards its legacy power-up/tier fields to `BalanceConfig` to avoid drift.

**Generation quality filter:** `common_words.json` (6,582 verified words = every tier/daily ladder word ∪ a curated common list, incl. ~2,500 common 6- and 7-letter words for dense long-ladder generation) restricts generated START/END (and intermediates) to fair words. Fallback chain: strict-common → relaxed-common-endpoints → known-good fallback (`cat→cot→cog→dog`). Curated tier/daily puzzles bypass the generator and are exempt.

---

## 10. Architecture

### Module / namespace map
```
Assets/Scripts/
├── Core/
│   ├── Engine/        WordPuzzle.State    GameState (immutable), GameStateManager (reducer/Dispatch),
│   │                                      GameAction, Constants, EconomyManager, IEconomyManager
│   └── Persistence/   WordPuzzle.Persistence  IDataManager, DataManager, PlayerProgress, SaveData,
│                                          SettingsData, DailyProgress, OnboardingData, TierDataLoader
├── Game/             WordPuzzle.Game      GameBootstrap (DI wiring), BootstrapInitializer,
│                                          DailyPuzzleService, DailyStreakRules, OnboardingRules,
│                                          TutorialPuzzle, ShareCardBuilder, IShareService,
│                                          IAdService impls (AdService, AdPolicyService, NullAdService)
│   └── Modes/         WordPuzzle.Modes    ClassicMode, PuzzleShowMode, TimeAttackMode(+Config),
│                                          IGameMode, ModeController
├── Puzzle/           WordPuzzle.Puzzle    WordGraph, WordValidator (IWordValidator), PuzzleGenerator,
│                                          WordOps, BalanceConfig, IAdService, WordPuzzle (model),
│                                          PuzzleDefinition, TierData, Difficulty, ValidationResult
└── UI/               WordPuzzle.UI        UIManager, UIAnimations, TimerDisplay, Themes/UITheme,
                                           Audio/SfxManager, Haptics/(IHaptics,HandheldHaptics,NullHaptics),
                                           TutorialOverlay, Components/(LetterTile, OnScreenKeyboard, …),
                                           Screens/(MainMenu, Gameplay, PuzzleLibrary, Results,
                                           Settings, TimeAttackSetup)

Assets/Resources/Data/  word_library.json (12,183), tier_definitions.json (350 = 7×50), daily_puzzles.json (450), common_words.json (6,582)
Assets/Scenes/          GameUI.unity  ← the ONLY live scene. MainMenu/ClassicMode/PuzzleShowMode/
                                        TimeAttackMode/SampleScene are legacy and never LoadScene'd.
Assets/Tests/           Unit/ + Integration/  (NUnit; TestMocks.cs has Mock* doubles)
Assets/Editor/          SceneBuilder*.cs + Verify* menu-item tools
```
Assembly dependency direction: `Puzzle` (lowest) ← `Persistence`/`State` ← `Modes` ← `Game`/`UI`. **`Puzzle` must never reference `State`/`UI`** (circular). Put shared low-level types in `Puzzle`.

### State flow (immutable + Dispatch — DO NOT change this shape)
`GameStateManager` owns an immutable `GameState` snapshot plus a private `MutableGameState`. UI subscribes to state; `GameAction` instances go through `Dispatch()`, which routes to handlers: `HandlePressLetter`, `HandleDeleteLetter`, `HandleSubmitWord`, `HandleUseHint`, `HandleUseReveal`, `HandleUseAddTime`, `HandleUndo`. Each handler mutates the working state, then notifies subscribers and persists. Events: `OnWordSubmissionResult` (accept/reject + reason), `OnTimeAdded` (AddTime/Survival seconds).

### Public interfaces to preserve (method names/signatures)
`IWordValidator`, `IDataManager`, `IGameMode`, `IEconomyManager`. (You may change a *return payload* if a task explicitly says so, but keep the method surface.)

### Mode routing (`GameBootstrap`)
- `Classic` → `StartClassicMode()` → tutorial gate, else random puzzle (common-words filtered).
- `Daily` → `StartDailyMode()` → today's deterministic puzzle.
- `Puzzle Show` → `PuzzleLibraryScreen`; tap → `OnLibraryPuzzleSelected(int puzzleId)`.
- `Time Attack` → `TimeAttackSetupScreen` → `StartTimeAttackModeWithConfig(TimeAttackConfig)`.
- **HOME** on any in-game screen returns to MainMenu and tears down the active mode + event subscriptions cleanly.

---

## 11. Persistence keys

All via `PlayerPrefs` (JSON values). `DataManager` owns them.

| Key | Holds | Cleared by Reset Progress? |
|---|---|---|
| `puzzle_progress_v1` | `PuzzleProgressData` (tiers, completed IDs) | ✅ yes |
| `wordpuzzle_progress` | `PlayerProgress` (coins, stats) | ✅ yes |
| `wordpuzzle_save` | in-flight `GameStateSnapshot` | ✅ yes |
| `daily_v1` | `DailyProgress` (streak) | ✅ yes |
| `settings_v1` | `SettingsData` (volumes, mute, reduceMotion, hapticsEnabled) | ❌ preserved |
| `onboarding_v1` | `OnboardingData` (tutorial done/skipped) | ❌ preserved (only Replay clears) |

`DataManager.ResetAllAsync` clears the four "yes" keys and preserves settings + onboarding. (`"Coins"` is a legacy key written only by the orphaned `CoinSystem`/`PlayerDataManager` — see [§13](#13-known-tech-debt--candidate-tasks).)

---

## 12. Testing & tooling

- **NUnit EditMode** tests under `Assets/Tests/Unit/{Engine,Persistence,UI}` and `Assets/Tests/Integration`. The `Unit.Tests` asmdef references the `Game.*` assemblies (incl. `Game.Puzzle`, `Game.UI`, `Game.Persistence`); UI-folder tests use a separate `Tests` asmdef. Most new tests need **no asmdef change**.
- **Mocks** in `Assets/Tests/TestMocks.cs`: `MockDataManager`, `MockWordValidator`, `MockEconomyManager`, `MockAdService`. Extend these rather than inventing new doubles.
- **Conventions:** pure-logic classes (e.g. `DailyStreakRules`, `OnboardingRules`, `WordOps`, `BalanceConfig`, `SfxManager.EffectiveSfxVolume`) are tested standalone; `GameStateManager` tests build it with the mocks; persistence tests use `new DataManager()` against PlayerPrefs with `[SetUp]/[TearDown]` key cleanup.
- **Run:** Window → General → Test Runner → EditMode → Run All. (See [§17](#17-notes-for-ai-agents-working-in-this-repo) for the MCP test-runner caveat.)
- **Editor tools** (`Tools/` menu): `Verify*` probes (library/ladder/polish), `SceneBuilder*` idempotent scene builders.
- **Key data-integrity tests:** `MinMovesFloorTests` (no sub-2-move puzzle anywhere; generated puzzles meet the length curve, by *true* BFS distance), `PuzzleShowTierTests` (7×50 structure, Hamming-1 ladders, non-decreasing min steps, `ResolveState` mapping, progressive unlock), `GenerationQualityTests` (junk-blocklist absence, curated-word presence, min long-word counts), `PostWinRouterTests` (per-mode surface routing), `BalanceConfigWiringTests`.

### Reproducible data pipeline (`Tools/` — Python, NOT shipped in the build)
The word data is **machine-generated and validated**, not hand-edited — re-run the tool, never edit the JSON by hand (it would drift and can silently break solvability/floors). All live outside `Assets/`, fetch/cache reference lists in the OS temp dir (never committed), and **fail loudly** on any violation:
- **`dictionary_build.py`** → rebuilds `word_library.json` + `common_words.json` from **ENABLE** (validity/cleanliness — excludes abbreviations/acronyms/proper nouns) ∩ **Norvig `count_1w.txt`** frequency (commonness). Drops junk, adds ~2,500 common 6- and 7-letter words each, re-validates all 540 curated puzzles stay solvable.
- **`puzzleshow_build.py`** → regenerates `tier_definitions.json` (7×50) on the difficulty curve; every ladder is a BFS shortest path drawn from the common subset, validated for the min-move floor by **true full-dictionary** distance, unique within tier, in-band.
- **`daily_floor_fix.py`** → replaces only the Daily puzzles whose true shortest path < 2 moves with fresh same-length ladders, **preserving puzzleId + array order** (so `DailyPuzzleService` indexing is unchanged).
> These use Python `set` iteration, so re-runs produce *valid, floor-compliant but not byte-identical* data. They are run via the Bash tool (`python Tools/<tool>.py`, `--dry-run` supported); their output is the committed JSON.

---

## 13. Known tech debt / candidate tasks

**Closed in the Task 9 consolidation (commit `17e6ab3`):**
1. ✅ **Orphaned managers removed** — `CoinSystem` + `PlayerDataManager` stripped from the 3 legacy scenes and deleted; one-time stale `"Coins"` key cleanup in `DataManager`.
2. ✅ **Undo collapsed to one path** — dead `GameSnapshot`/`undoHistory` stack deleted; `HandleUndo` is chain-rewind only (power-ups stay spent by design).
3. ✅ **Score + streak floored** at 0 on undo (`Mathf.Max(0, …)`).
4. ✅ **Typed reject reason** — `WordValidator` returns a `WordRejectReason` enum (in the `Puzzle` assembly); `GameStateManager` maps enum→text in one place; the `IndexOf` parser is gone. User strings unchanged.
5. ✅ **`HaveOneLetterDifference` unified** — `PuzzleGenerator` now calls `WordOps`.

**Still open:**
6. **Native image share** — `IShareService`/`ShareCardBuilder.RenderPng` seam is ready; wiring the OS share sheet needs an approved plugin (e.g. NativeShare).
7. **AudioMixer + real SFX clips** — `SfxManager` has slots but no clips/mixer in the repo yet.
8. **Full Classic resume** — Resume restores tier/daily puzzles (id-resolvable); random Classic isn't reconstructable from the current save snapshot (no end-word/solution stored), so it hides Resume. A snapshot-schema extension would enable full Classic resume.

> **Verification caveat (important for any agent):** the unityMCP EditMode test runner is currently **non-functional** — it collapses to the assembly root and never invokes NUnit, reporting `"Passed"` even for a deliberately-failing canary. Treat its summary as "not run." Verify changes by **clean compile + reading test assertions against the implementation** until the runner is fixed (see [§17](#17-notes-for-ai-agents-working-in-this-repo)).

---

## 14. Writing a master prompt for this repo

A good task prompt for this codebase has a consistent shape. Reuse it:

1. **Paste the [Shared Context Block](#shared-context-block-paste-into-every-task-prompt)** (repo layout, hard constraints, design tokens) at the top — it grounds the agent.
2. **State a single GOAL** (one concern per prompt; this repo was built one swarm per concern).
3. **`PLAN FIRST`** — require the agent to locate exact files + method seams against the real files, list assumptions where ambiguous, and *not* edit before confirming. (Critical: parts of this README may drift; the agent must verify against the tree.)
4. **Break the work into lettered sub-tasks** (e.g. `TASK 9A / 9B …`), each with concrete file/seam targets.
5. **Give explicit ACCEPTANCE criteria** — what an EditMode test must assert, what stays green, what the manual check is.
6. **Repeat the hard constraints** (immutable state + Dispatch; preserve `IWordValidator/IDataManager/IGameMode/IEconomyManager`; all tests green; delete `.meta` with assets; never commit `Library/Temp/obj`; surgical diffs).
7. **Optionally `USE SWARM`** for 3+ file / cross-module work; keep single-file fixes solo.

**Effective patterns observed:** name the exact constant in `BalanceConfig` to read; specify the testable seam (inject a `Func<>`/`Action` rather than calling `Time`/`Handheld`/SDK directly); say where in `GameBootstrap` to wire; tell the agent which existing mock to extend; and for anything visual, acknowledge the final check is a manual portrait eyeball.

### Shared Context Block (paste into every task prompt)
```
Repo: Unity 6000.4.6f1 mobile word-ladder game ("Word Ladder"). Portrait 1080x1920.
Single live scene: Assets/Scenes/GameUI.unity. Architecture: immutable GameState + Dispatch
(GameStateManager; handlers HandlePressLetter/HandleDeleteLetter/HandleSubmitWord/HandleUseHint/
HandleUseReveal/HandleUseAddTime/HandleUndo; events OnWordSubmissionResult, OnTimeAdded).
Tunable numbers live in Assets/Scripts/Puzzle/BalanceConfig.cs (single source of truth;
incl. MinMovesForLength curve 3->2 4->2 5->3 6->3 7->4, AbsoluteMinMoves=2, MaxTier=7,
PuzzlesPerTier=50, progressive PuzzlesRequiredToAdvance(tier) 10..40).
Word data (Assets/Resources/Data/, all MACHINE-GENERATED by Tools/*.py — re-run, never hand-edit):
  word_library.json (12,183) + common_words.json (6,582) <- dictionary_build.py;
  tier_definitions.json (7 tiers x 50 = 350) <- puzzleshow_build.py;
  daily_puzzles.json (450, hand-curated origin, floor-fixed) <- daily_floor_fix.py.
  Every puzzle's TRUE full-dictionary shortest path is >= MinMovesForLength (no 1-move puzzles).
Post-win surface routing: pure PostWinRouter.Decide(...) called by GameBootstrap.CheckGameOver.
Persistence: PlayerPrefs JSON via DataManager (keys: puzzle_progress_v1, wordpuzzle_progress,
wordpuzzle_save, daily_v1, settings_v1, onboarding_v1).
Assemblies (dep direction): Puzzle (lowest; BalanceConfig, WordGraph, WordValidator, IAdService) <-
Persistence/State <- Modes <- Game/UI. Puzzle must NOT reference State/UI.
Design tokens: bg-base #0A0A0A (true black; one full-screen Background layer behind every screen,
auto-loads Resources/UI/SpaceBackground.png), bg-surface #1B1F27, surface-2 #242936. Buttons + start/
target tiles are colored OUTLINES via UITheme (MenuPalette: Resume/start teal #1B9E8F, Daily/target
orange #FF8A2E, Classic green #3D9E54, Puzzle Show violet #7B5FD4, Time Attack red #D23F58). accent-gold
#C9B458 is IN-GAME ONLY now (hints, active input, win/tier accents — not a menu color). accent-green
#6AAA64, accent-red #C9215C, text-primary #E7E1C4/#F5F7FA, text-muted #8A93A1, text-dim #5A6270.

Hard constraints (ALL prompts):
- Preserve the immutable GameState + Dispatch architecture and the public interfaces
  IWordValidator, IDataManager, IGameMode, IEconomyManager unless a task says otherwise.
- All existing EditMode/PlayMode tests stay green. Delete the .meta when you delete a Unity asset,
  and GUID-check scenes/prefabs before deleting any MonoBehaviour script.
- Never commit Library/Temp/obj. Minimal, surgical diffs.
- PLAN FIRST: confirm exact method seams against the real files before editing; state assumptions
  where ambiguous. Tunables go in BalanceConfig, never as new magic-number literals.
```

---

## 15. Design tokens

**Base / surface**

| Token | Hex | Use |
|---|---|---|
| `bg-base` | `#0A0A0A` | App background — one full-screen layer behind every screen (holds the swappable space image) |
| `bg-surface` | `#1B1F27` | Keyboard panel, win-card surface |
| `surface-2` | `#242936` | Solid letter tiles (active input + settled chain rows), keys |
| `text-primary` | `#E7E1C4` / `#F5F7FA` | Body, button/tile labels |
| `text-muted` | `#8A93A1` | Subtitles, muted secondary outlines |
| `text-dim` | `#5A6270` | Locked card text, version |

**Menu / outline palette** (`UITheme.MenuPalette`) — each button's color is its ring/border; light labels.

| Token | Hex | Use |
|---|---|---|
| Resume (teal) | `#1B9E8F` | Resume button + **start-word** tile outline |
| Daily (orange) | `#FF8A2E` | Daily hero button (thicker ring) + **target-word** tile outline |
| Classic (green) | `#3D9E54` | Classic Mode button |
| Puzzle Show (violet) | `#7B5FD4` | Puzzle Show button |
| Time Attack (red) | `#D23F58` | Time Attack button |
| Secondary (slate) | `#39435A` / ring `#8A93A1` | Library / Stats / utility outlines |

**In-game accents**

| Token | Hex | Use |
|---|---|---|
| `accent-gold` | `#C9B458` | **In-game only** (no longer a menu color) — hints, active-input tiles, win "Next Puzzle", in-progress & current-tier rings, streak headline |
| `accent-green` | `#6AAA64` | Correct chain, success, +TIME, win beat |
| `accent-red` | `#C9215C` | Destructive actions, reveal accent |

---

## 16. Building & running

**Requirements:** Unity 6000.4.6f1, TextMeshPro (bundled), Google Mobile Ads (integrated). Portrait 1080×1920; CanvasScaler matches height.

1. Clone, open the root folder via Unity Hub → *Add project from disk*.
2. Open `Assets/Scenes/GameUI.unity` and press **Play**.
3. Tests: Window → General → Test Runner → EditMode → Run All.

---

## 17. Notes for AI agents working in this repo

Environment quirks learned the hard way — relevant when an agent verifies its own work:
- **The unityMCP `run_tests` runner is unreliable for pass/fail.** It reports `summary.total=0`, collapses results to a single root node, and has reported `"Passed"` for a suite containing a must-fail test. Treat `"Passed"` as *compiles + discovered*, **not** runtime-green — verify by reading test-source assertions against the implementation. EditMode runs require the editor **not** in Play Mode (`manage_editor stop` first).
- **`execute_code` (in-editor C#) is broken here** (mono "filename or extension is too long"; Roslyn not installed). You cannot script Play-mode drives or screenshots — visual/feel acceptance is a human-in-Editor check.
- **`manage_camera` screenshots can't see the portrait game.** The capture returns a blank ~2:1 landscape Game-view rendered via the Main Camera (which **excludes** the Screen Space - Overlay UI canvas); the real frame is the portrait Device Simulator on display 0, which MCP can't read (`scene_view` capture needs an open Scene View). Verify UI **numerically** instead — `manage_scene get_hierarchy` with `include_transform` for positions, `ReadMcpResourceTool` on `mcpforunity://scene/gameobject/{id}/component/{name}` for rects/colors/refs — and hand the portrait eyeball to a human. Also: Play mode boots straight to **MainMenu** (you can't script into a specific mode), `manage_gameobject`/`set_property` edits are **blocked during Play**, and **instance IDs churn on every domain reload** — re-query, never cache them.
- **Confirm scene context before/after agent work.** Loading a scene in the editor replaces the open one; agents have left the editor on a non-`GameUI` scene and/or in Play mode. Re-open `GameUI.unity` and stop Play mode to restore the expected view.
- **`git status` before planning/committing.** Background agents occasionally drop shell-misfire junk files at repo root (e.g. `nul`, `{`, `0`) and can even pick up a *later* task autonomously — clean junk and check the tree before each commit.
- **Icons — SVG via Vector Graphics, or PNG.** `com.unity.vectorgraphics` provides the SVG importer; set the importer's **SVG Type = Textured Sprite** (the default "UI Toolkit" type yields *no* uGUI sprite — empty `SpriteRect`), and give artwork a **concrete stroke/fill colour, not `currentColor`** (which rasterizes to black and can't be tinted via `Image.color`). UI chrome (HOME, the global Settings gear) is built in code as tinted `Image` children. A `[SerializeField] Sprite` ref can point anywhere under `Assets/`; sprites loaded at runtime (`Resources.Load`) must sit under a `Resources/` folder.
- Untracked tooling dirs (`.claude/`, `.swarm/`, `.claude-flow/`, `agentdb.*`, `_Recovery/`) are not part of the game — never commit them. Shell-misfire junk (`nul`, `{`, `0`, `560)`, `statsScreen`, …) sometimes lands at repo root — delete before committing.

---

## Project history

Built iteratively through AI-orchestrated swarms, one concern each: word library & ladder semantics → modern tile/keyboard polish → library cards & tier gate → HOME/settings → hint/reveal semantics → per-mode behaviors & AddTime → TimeAttack UI → share result → daily + streak → **balance config & common-words generation** → **economy & rewarded ads** → **tactile juice (motion/haptics/sound)** → **premium visual identity (gold focus, ascent, motion vocabulary)** → **UI polish pass** (main-menu hierarchy with a gold DAILY hero, gameplay spacing, a keyboard-anchored power-up bar, a reliable visible HOME, and a properly clipping/scrolling word-chain) → **icon chrome** (SVG-via-Vector-Graphics + PNG icons: a house HOME and one shared, icon-only top-right Settings gear on every screen) → **Time Attack setup polish** (fit/styling/header, HOME aligned to the shared gear) → **dictionary expansion & cleanup** (reproducible ENABLE+Norvig tool: junk removed, 8,626→12,183 words, dense common 6/7-letter coverage) → **Puzzle Show 7×50** (350 curated ladders, two-level tier-select→grid navigation, completion coloring, progressive unlock) → **post-win flow** (compact win panel for endless Classic, auto-advancing Time Attack with results on timeout, Puzzle Show stat screen, Daily Home-only; "Play Again" re-routes into the mode) → **minimum-move floor** (no 1-move puzzles anywhere; min scales with word length, enforced by true full-dictionary shortest path in the generator and across all curated data). The git log captures the progression.
