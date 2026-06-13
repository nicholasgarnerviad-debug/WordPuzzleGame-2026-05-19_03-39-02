# Star Ladder

A modern, mobile-portrait word-ladder puzzle game built in **Unity 6000.4.6f1** (Unity 6 LTS), portrait **1080×1920**. Transform a start word into an end word one letter at a time — every intermediate step must be a real English word that differs from the previous word by exactly one letter.

```
FROM  C  A  T            FROM  S  T  O  N  E
       |                        S  T  O  R  E   ← changed 1
      B  A  T  ← changed 1      S  T  A  R  E   ← changed 1
      B  A  G  ← changed 1      S  H  A  R  E   ← changed 1
 TO   B  A  G            TO     S  H  A  R  P   ← changed 1
```

> **This README is also the canonical context document for AI-assisted development.** It is written so an LLM (e.g. Claude Opus) can read it and author precise, surgical task prompts ("meta prompts") for this repo. See **[§14 Writing a master prompt](#14-writing-a-master-prompt-for-this-repo)** and the **[Shared Context Block](#shared-context-block-paste-into-every-task-prompt)** at the end.

<!-- Screenshots intentionally removed — fresh captures pending. Drop new PNGs into docs/screenshots/ and re-embed. -->

---

## Table of contents
**📱 [Screens](#screens)** — a visual tour of every screen

- [Star Ladder](#star-ladder)
  - [Table of contents](#table-of-contents)
  - [Screens](#screens)
  - [1. Game modes](#1-game-modes)
  - [2. Power-ups](#2-power-ups)
  - [3. Economy \& monetization](#3-economy--monetization)
  - [4. Juice: motion, haptics, sound](#4-juice-motion-haptics-sound)
  - [5. Visual identity](#5-visual-identity)
  - [6. First-launch tutorial](#6-first-launch-tutorial)
  - [7. Puzzle Library shelves](#7-puzzle-library-shelves)
  - [8. Word validation](#8-word-validation)
  - [9. Balance config — the single source of truth](#9-balance-config--the-single-source-of-truth)
  - [10. Architecture](#10-architecture)
    - [Module / namespace map](#module--namespace-map)
    - [State flow (immutable + Dispatch — DO NOT change this shape)](#state-flow-immutable--dispatch--do-not-change-this-shape)
    - [Public interfaces to preserve (method names/signatures)](#public-interfaces-to-preserve-method-namessignatures)
    - [Mode routing (`GameBootstrap`)](#mode-routing-gamebootstrap)
  - [11. Persistence keys](#11-persistence-keys)
  - [12. Testing \& tooling](#12-testing--tooling)
    - [Reproducible data pipeline (`Tools/` — Python, NOT shipped in the build)](#reproducible-data-pipeline-tools--python-not-shipped-in-the-build)
  - [13. Known tech debt / candidate tasks](#13-known-tech-debt--candidate-tasks)
  - [14. Writing a master prompt for this repo](#14-writing-a-master-prompt-for-this-repo)
    - [Shared Context Block (paste into every task prompt)](#shared-context-block-paste-into-every-task-prompt)
  - [15. Design tokens](#15-design-tokens)
  - [16. Building \& running](#16-building--running)
  - [17. Notes for AI agents working in this repo](#17-notes-for-ai-agents-working-in-this-repo)
  - [Project history](#project-history)

---

## Screens

> **Screenshots removed — fresh captures pending.** The shots in this section are being re-taken against the current "Direction B" build (which renamed several modes — see below); new PNGs go in `docs/screenshots/` and get re-embedded. The descriptions below are current.
>
> Naming note (2026-06 rename pass): the modes/labels the player sees are **Daily**, **Classic**, **Puzzle Library** (was "Puzzle Show"; its tiers are now **Shelves**), and **Timed** (was "Time Attack"; its two variants surface in results as **Timed** / **Timed Survival**). The in-game currency is **stars** (was "coins") and the shop is the **Star Shop**. These are *display-string* renames — internal types keep their names (`PuzzleShowMode`, `TimeAttackMode`, `tierId`, `totalCoins`, …).

- **Main Menu** — the **Star Ladder logotype** masthead (aqua→periwinkle gradient sprite; Rungo `Display` text fallback; the old "ladder-rung A" motif was retired — it read as a glitch — so a single four-point star in the word gap carries the identity). **DAILY** is the ONE **filled gradient hero** (orchid→deep-violet over the shared bubbly shape, the glow reading as rim light; shows the streak once today is solved). The modes are **colored rounded outlines**, each carrying its purple-family token **and a stroke icon** so identity survives grayscale — **Classic** blue-violet ∞, **Puzzle Library** deep-violet trophy, **Timed** magenta stopwatch, **Resume** (only with an in-progress save) periwinkle play. **Puzzle Library link / Stats** recede to **ghost text** on invisible hit targets (Settings lives in the shared top-right gear). A **star pill** (top-left — a gold five-point star + the balance) opens the **Star Shop**; the **Daily Rewards** overlay surfaces the daily login claim + streak repair ([§3](#3-economy--monetization)).
- **Classic** — the core word ladder on black. The **start** word is a row of **white see-through outline tiles** (the origin — aqua is reserved for success), the **target** is **orchid** outline tiles (the goal), and the played **chain** rows are **periwinkle** see-through outlines; only the **active input row stays solid** (the "fill here" zone). Tiles carry **bold ~7px rings** and **ladder-feel motion** (letters drop in, accepted rows climb), over a subtle bottom-weighted **readability scrim**. Below: the **icon-carrying** Hint / Undo / Reveal **power-up bar** (stroke icons × the chip token, dimming with the ring when unusable) over a **rounded** QWERTY keyboard (`Alert`-red `DEL`, aqua `GO`) **floating on a transparent panel** — the space backdrop runs edge-to-edge. An icon **HOME** (top-left) and the shared **Settings** gear (top-right) flank a calm score header. Random 3–7-letter puzzles; on a solve a **compact win panel** ("Next Puzzle" / "Home") keeps you in the loop ([§1](#1-game-modes)).
- **Daily 2.0** (Task 36) — one shared puzzle a day, now **par-scored with stakes** (every daily is **≥ 4 steps**). Same start (white) → input → target (orchid) board as Classic, plus a **"Par N · Mistakes left M"** HUD. **Typos can't kill you:** a legal-shaped guess that just isn't a dictionary word **bounces free** ("Not a word — free try", no mistake) — only a **rule-breaking** guess (not a one-letter change) spends one of your **3 mistakes** (running out **fails** the run). **Detours** (valid but non-progress moves) cost your **grade**, not the run. Finishing scores **Perfect / Good / Solved / Failed** (★★★–☆) + par-scaled stars, advances your **played-streak** (a *failed* day still counts — only a missed calendar day breaks it), and logs a trailing-365 **W/L record + win%**. Results add a spoiler-free **path-shape share card** and a **watch-to-double** reward ([§1](#1-game-modes)).
- **Puzzle Library** (mode; was "Puzzle Show") — shelf-progression play on the same gameplay screen, with a `Shelf X / Y` indicator under the score. **700 curated ladders (7 shelves × 100)** on a length/difficulty curve (Shelf 1 easy 3-letter → Shelf 7 hard 7-letter, up to 8-step ladders), every one with **≥ 2 distinct optimal routes** (multiple ways to solve, guaranteed). Solving shows a stat screen offering **Next Puzzle / Shelf N ▸ / Home** ([§7](#7-puzzle-library-shelves)).
- **Timed** (was "Time Attack") — a countdown timer + the **+Time** power-up; chosen as 60s/120s × Timed/Survival on the **TIMED** setup screen. Ladders **auto-advance** as you solve them; the full results screen (puzzles solved + **Play Again** → new run, titled **Timed Results** or **Timed Survival Results** so the variant is obvious) appears only when the **timer hits 0**.
- **Puzzle Library → Shelf Select** (level 1) — **7 shelves** as tall, rounded **glowing-outline rows** (full-height — the rect height is set explicitly because the root layout group runs `childControlHeight=false`, which ignores `LayoutElement`) under the runtime **PUZZLE LIBRARY / Pick a shelf** header (the unthemed scene masthead is retired in both views; **HOME is a ghost**). Each row shows its theme (e.g. "3-letter words"), progress (`X/100`) and lock state — the **active shelf glows hero-bright**, locked shelves are dim with a **"Clear N on Shelf M to unlock"** hint (the old `□` padlock glyph was tofu in Rungo and is gone). Tap an unlocked shelf to open its grid.
- **Puzzle Library → Shelf Grid** (level 2, Task 48) — the selected shelf's **100** puzzle cards under a header with an **animated progress bar**, a ghost **‹ Back** to shelf-select, and the **"up next"** card carrying the ONE hero glow + a ▸ play cue (only the active shelf renders, for performance). Cards reflect saved progress: **Completed** (**aqua** + ✓ — Direction B retired green), **Unplayed** (surface grey), **Locked** (dim). Tapping an **unbeaten** card launches that exact puzzle; tapping a **beaten** one opens the spoiler-free **Path View** — your best solve + the optimal path uncovered so far ([§7](#7-puzzle-library-shelves)).
- **Stats** (redesigned) — tight, **hero-led runtime cards**, **solid** (`ApplySolidCard`) so the numbers never collide with backdrop art, each ring keyed to its **menu mode token** (DAILY orchid / CLASSIC blue-violet / TIMED magenta — one colour language across screens): a **DAILY** headline card with the **streak as the hero** (big gold number) and **Longest / Win % / W–L** beside it, a matched **CLASSIC** + **TIMED** pair of caption-over-value cells, and a slim Overall footer — with the **star pill** (gold star + number) and a **ghost HOME** (Task 43 tier 3).
- **Settings** (rebuilt) — clean **grouped sections built at runtime** (no scene authoring) in a scroll view: **Audio** (Master / Music / SFX sliders + Mute) · **Accessibility** (Reduce Motion · Haptics · Colorblind Mode) · **Data** (**Reset Progress** → a proper runtime confirm modal: SurfaceVoid scrim + a danger-ringed solid card with CANCEL / RESET, preserves settings + tutorial flag; **Replay Tutorial**) · **Privacy** (UMP consent re-prompt, shown only where consent options are required). Modernized to the solid-card identity — rounded slate-groove + **cyan**-fill sliders and **cyan/slate pill toggles** with a sliding, colorblind-safe knob (ReduceMotion-gated glide; the 9-slice corner-art was re-scaled so the knobs/segments are clean circles, not blobs); **no gold**. SFX/Music are flagged on-screen as silent until an audio bus exists. Build version at the foot.
- **Star Shop** (Tasks 33, 36; modernized + v1.0-gated) — opened by the **star pill**, which also renders **in the shop header**. Every row is a **solid card** (`ApplySolidCard`), **Back/Restore are ghosts**, and rows have real internal padding. Content for **v1.0**: **Power-Ups** (Hint / Undo / Reveal / Time in **×5 / ×15 / ×40** tiered **star** prices) and a **Free Stars · watch an ad** row (capped 3/day, with a disabled **"Loading…"** state while the rewarded ad loads). The **real-money sections — Starter Pack (gold-ring hero row + filled-hero price), star packs, Remove Ads, Restore — are HIDDEN behind `ShopScreen.RealMoneyEnabled = false`** until real billing lands (planned 1.1); the UI for them is fully built and returns with a one-line flip. Rebuilds from live state after each buy; unaffordable bundles disable ([§3](#3-economy--monetization)).
- **Results** (shared, per-mode `ResultsScreen`) — one **code-driven** results page reused by every mode, titled `{mode} Results` (**Classic Results**, **Puzzle Library Results**, **Timed Results** / **Timed Survival Results**). **Non-daily** modes show a clean **Final Score / Words / Accuracy / Time** block (hero score via the `GameAccents.Gold` token). **Daily** shows its **own, distinct** screen (titled **"Daily Results"**): a **drawn 3-star grade hero** (real star meshes — gold earned / dim unearned — because the bundled font has no ★ glyph) over a gold grade word + `Par N · You got X`, the **+N stars** payout, and a **streak · best · one-and-done** line (`Already counted today` / `Come back tomorrow`), framed in a subtle **ghost result card** — plus the spoiler-free **path-shape share card**. Buttons stay per-context: **Share Result** + **Home**, with **Next Puzzle / Shelf N ▸** (Puzzle Library) or **Play Again** (Timed).

> **Global chrome:** one shared **Settings** gear (icon-only, top-right, ~HOME-sized) shows on every screen *except* Settings itself and opens it — `UIManager.CreateGlobalSettingsButton` → `OnGlobalSettingsRequested` → `GameBootstrap.ShowSettings` (which populates then shows). On the gameplay screen a house **HOME** (top-left) and the gear (top-right) flank the header. Icon assets: `Assets/UI/Icons/*.svg` (Vector Graphics) + `Assets/Resources/Icons/*.png`.

---

## 1. Game modes

| Mode | Timer | Puzzles | Win condition |
|---|---|---|---|
| **Classic** | None | Random, BFS-generated; start/end restricted to a common-words subset | Reach the end word → **compact win panel** ("Next Puzzle" stays in Classic). First-ever launch routes into the tutorial. |
| **Daily 2.0** | None | One puzzle/day (**≥4 steps**), identical for everyone (no server) | **Par-scored with stakes** — typos bounce free, a 3-mistake budget on rule-breaks, detours cost grade; finishing (solve OR fail) → full results: grade/★, par-scaled stars, played-streak, W/L record, share card, watch-to-double (**Home** only) |
| **Puzzle Library** | None | **700 curated ladders (100 × 7 shelves, each ≥2 optimal routes)**, two-level library | Reach end word → stat screen (Next Puzzle / Shelf N ▸ / Home); tap any unlocked card to play it |
| **Timed** | 60s or 120s, Timed or Survival | Random words back-to-back | Solve as many as possible before time runs out → full results + **Play Again** (new run) |

**Daily 2.0 — par scoring, stakes, played-streak, W/L record, repair, share (Task 36).** Today's puzzle is derived from the **local date**, no network: `index = (Today − 2025-01-01).Days mod N` (`N` = pool size in `daily_puzzles.json`, all pre-validated Hamming-1 + dictionary). Daily runs reuse Classic mechanics but arm a **two-resource scored run** via `GameStateManager.ConfigureDailyRun(mistakeBudget, par)` (called after `StartNewPuzzle`; par = the puzzle's validated `optimalSteps`):
- **Mistakes (the stake) — typos can't kill you (2026-06 rework):** a **rule-breaking** guess (not a one-letter change from the chain tail) spends one of `DailyMistakeBudget` (3); running out **fails** the run. A **legal-shaped dictionary miss** (exactly one letter changed, just not a real word) **bounces free** — no mistake, no detour, message "Not a word — free try". *Precedence:* the validator checks the dictionary BEFORE the one-letter rule, so the free pass needs an explicit `WordOps.HaveOneLetterDifference` re-check; gibberish that *also* breaks the rule pays a mistake (and shows the rule copy). A wrong-length/empty entry is malformed (not a mistake).
- **Detours (the score):** an accepted move that isn't *progress* (`!validation.isProgress` — not strictly closer to the target) is a **detour**; detours set the grade but never end the run. Undo decrements the detour count (floored; no mistake refund).
- **Grade** — pure `PathScoring.Score(par, steps, detours, mistakesUsed, ranOutOfMistakes, usedPowerUp)` → `PathGrade` where `(int)grade == stars`: **Perfect** (★★★, 0 detours) / **Good** (★★, ≤ `GoodMaxDetours` = 2) / **Solved** (★) / **Failed** (☆, out of mistakes). **Grade integrity (Task 40):** a power-up-assisted solve is **capped at Good** (`BalanceConfig.PowerUpMaxGrade`) — **Perfect is reserved for unassisted runs** — and the assistance is disclosed honestly: the results screen shows a plain-text **"assisted"** note (text, not an emoji — the bundled font can't render ⚡) and the share card carries a ⚡ disclosure. Surfaced on `ResultsScreen.ShowDailyResult` as a row of **drawn star meshes** (`StarGraphic` — the bundled TMP font has no ★ glyph, so the rating is rendered as geometry, not text) above a gold grade word + "Par N · You got X", inside a subtle ghost result card.

**Played-streak** (`DailyStreakRules.ApplyPlayed`, pure/testable — the streak authority, replacing completion-only `ApplyCompletion`; never call both): a **played** day (solve OR fail) advances `currentStreak` iff yesterday was played; only a **missed calendar day** resets it; same-day replay never double-counts. A **trailing-365-day W/L record** (`outcomes` ledger of `DayOutcome{dateIso,won}`; `Wins`/`Losses`/`WinRatePct`, `RecordWindowDays = 365`) tracks skill alongside the habit streak. **Streak repair** (`CanRepair`/`ApplyRepair`): if *only yesterday* was missed, bridge the gap for `StreakRepairCoinCost` (150) **stars** **or** a rewarded ad, once per `StreakRepairCooldownDays` (7) — a bridge only (does **not** auto-play today). One-and-done (Task 38): once today is played, the menu **DAILY** button shows the streak and **re-tapping re-shows today's stored result** (grade/stars + streak) instead of starting a fresh scored run — no replay, no reward re-grant. Persisted under `daily_v1`.

**Path-shape share card** (`ShareCardBuilder.BuildDailyShapeCard`) — a **spoiler-free** daily result: a header (`Star Ladder Daily #n · Par p · X/p · ★★☆`), one glyph row per step (`🟩` progress / `🟨` detour / `⬛` mistake-step), and the streak line — **no words**. A power-up-assisted run appends a **⚡ "power-up used" disclosure** (Task 40) so a shared Good never masquerades as a clean run. Copied via `ClipboardShareService`.

**Timed sub-modes** (internal type stays `TimeAttackMode`) — **Timed**: fixed countdown (60s/120s), no rewards → results titled **"Timed Results"**. **Survival**: each solve grants `BalanceConfig.SurvivalRewardSeconds` (15s) so a skilled player can sustain → results titled **"Timed Survival Results"** so the variant is unmistakable. Configured via `TimeAttackConfig` (factories `Default60`/`Default120`/`DefaultSurvival`, all read `BalanceConfig`).

**Post-win flow** (which surface shows on a solve) is decided by one pure function, `PostWinRouter.Decide(ModeKind, isDaily, puzzleComplete, timeUp)`, called by `GameBootstrap.CheckGameOver` — the single source of truth:
- **Classic** → a **compact inline win panel** overlaid on the board (`GameplayScreen.ShowWinPanel`); "Next Puzzle" starts a fresh Classic puzzle in the same mode, "Home" exits.
- **Timed** → solving a ladder **auto-advances** to the next (the run's clock keeps running via a one-shot `timerSeeded`); the full `ResultsScreen` (ladders solved + "Play Again" → new run) shows only when the timer expires.
- **Puzzle Library** → the full `ResultsScreen` configured with **Next Puzzle** (another on the current shelf), an optional **Shelf N ▸** (when the next shelf just unlocked → opens the library), and **Home**.
- **Daily** → its **own** `ResultsScreen` view (titled **"Daily Results"**, *not* a mode's): a **grade/par/stars hero** + a **streak · best · one-and-done** line + the share card, with the score/accuracy/time metrics hidden and **no "Play Again"** (never re-run the daily as a scored game) — just **Home**.
"Play Again" / "Next Puzzle" always **re-route into the active mode**, never the main menu (the old bug). `ResultsScreen.ConfigureForDaily/ForEndless/ForPuzzleShow` set button visibility/labels per context **and flip an `_isDaily` flag that routes the layout** — Daily gets the grade-hero/streak block (its title is set here so a previously-shown view's title can't leak through), the others the score/accuracy/time block. The whole page is **code-driven** on palette tokens (an older, unwired duplicate stat list is hidden), and the Daily streak line renders into a **dedicated label that is created once and SET each view** — fixing a status line that used to **append to the title on every Daily open**, stacking unbounded.

**Share result** — `ResultsScreen` "Share" copies a Wordle-style emoji grid to the clipboard (`ClipboardShareService`, zero third-party deps). One row per accepted step, `🟩` at the changed position, `⬛` elsewhere; mode-specific label/footer. Native image share is seam-ready (`IShareService` + `ShareCardBuilder`) but requires an approved plugin.

---

## 2. Power-ups

| Power-up | Effect | Owned inventory | Get more | Available in |
|---|---|---|---|---|
| **Hint** | Gold-highlights the position in the current word to change next | persisted; **start 5**, **+2/day** | stars (Star Shop) / rewarded ad | All modes |
| **Reveal** | Shows the next solution word as a ghost preview row | persisted; **start 5**, **+2/day** | stars (Star Shop) | All modes |
| **Undo** | Pops the last accepted chain word | tracked* | stars (Star Shop) | All modes |
| **+Time** | Adds `AddTimeGrantSeconds` (10s) to the clock | persisted; **start 5**, **+2/day** | stars (Star Shop) | Timed only* |

**Full real economy (Task 33):** power-ups are now a **persisted owned inventory** (`PlayerProgress.total{Hints,Reveals,Undos,Time}Earned` via `EconomyManager`). Hint/Reveal charges **seed each puzzle from that inventory** (`GameStateManager.SetOwnedPowerUpProvider`, wired by `GameBootstrap`; null in unit tests so they fall back to the `BalanceConfig` defaults), and using one in-game **spends from your saved stock** (`Use*Async`). Every player starts with **5 each** and gets **+2 each per local day**; the Star Shop tops them up — see [§3](#3-economy--monetization). Reveal stays the premium power-up. *\*Undo's count + the +TIME→Timed hookup are tracked in the economy but their gameplay wiring is still in progress (see [§13](#13-known-tech-debt--candidate-tasks)).* Submitting a valid word or using Undo clears any active hint/reveal preview.

---

## 3. Economy & monetization

**Stars → power-ups (Tasks 33, 36).** The in-game currency is **stars** (renamed 2026-06 from "coins"; the gold token is a five-point star). *Display-only* — the internal economy keeps its names (`EconomyManager`, `totalCoins`, `SpendCoinsAsync`/`AddCoinsAsync`, `coin_shop.json`). One `EconomyManager : IEconomyManager` (constructed + initialized in `GameBootstrap`) persists everything through `DataManager` → `PlayerProgress`: the star balance **and** the owned power-up inventory (hint/undo/reveal/time), the `removeAds` flag + starting/daily-grant bookkeeping, and the **Task 36** faucet/sink state — the one-time **Starter Pack** flag, a temporary **ad-free window**, the **login-reward** cycle position, the **watch-for-stars** daily counter, and the highest **streak milestone** paid. All amounts/prices live in `BalanceConfig`. (A legacy `CoinSystem` MonoBehaviour also exists but is orphaned — see [§13](#13-known-tech-debt--candidate-tasks).)

**Two currencies, one direction — real money buys stars; stars buy power-ups:**

| Layer | What | Bought with |
|---|---|---|
| 🎁 **Starter Pack** (36J) | one-time **$1.99** → 1000 stars + 5 of each power-up + a 3-day ad-free window (`StoreProductType.StarterPack`) | **real money** via `IStoreService` |
| 💎 Star packs | **Pouch** 150/$0.99 · **Stack** 500/$2.49 · **Chest** 1200/$4.99 *(MOST POPULAR)* · **Vault** 3000/$9.99 · **Hoard** 7000/$19.99 *(BEST VALUE)* — `coin_shop.json` (names + badges) | **real money** via `IStoreService` |
| 🎟️ Power-up bundles | Hint/Undo **50·135·320** · Reveal **120·320·800** · Time **60·160·400**, each as **×5 / ×15 / ×40** (tiered, bulk-discounted) | **stars** (`SpendCoinsAsync` → `Add*Async`) |
| 🚫 Remove Ads | one-time **$4.99**, sets the persisted `removeAds` flag | **real money** via `IStoreService` |

**Free grants:** every new player starts with **5 each** power-up (`ApplyStartingInventoryIfNeeded` — idempotent, *tops up*, never reduces a richer save) and gets **+2 each per local day** (`GrantDailyIfDue` — idempotent, no missed-day stacking, reuses the `DailyPuzzleService` clock).

**Star faucets & sinks (Task 36 Phase 5 — numbers in `BalanceConfig`, all clock-free + idempotent per local day):**

| Faucet / sink | Amount | Where it surfaces |
|---|---|---|
| **Par-scaled daily reward** | Perfect **60** / Good **40** / Solved **25** / Failed **10** (`DailyCoinReward(stars, failed)`) | granted on daily finish (replaces the old flat +25) |
| **Login reward** (7-day cycle) | `{25, 25, 50, 50, 75, 75, 150}` then wraps (`ClaimLoginRewardAsync`) | the **Daily Rewards** popup on the menu |
| **Watch for stars** | **35** stars, cap **3/day** (`GrantWatchCoinsAsync`) | a row in the Star Shop's faucet section |
| **Streak milestones** | **+100** at a **7 / 30 / 100**-day streak, each once ever (`AwardStreakMilestonesAsync`) | a toast on the daily results |
| **Reward doubler** | watch an ad → **2×** today's daily reward, once per result | a button on the daily results |
| **Streak repair** (sink) | spend **150** stars *or* watch an ad | the **Daily Rewards** popup |
| Classic / Puzzle-Library completion | **+10** (`PuzzleCompletionReward`); rewarded video = +1 Hint | gameplay |

**The Star Shop** (`ShopScreen` — a runtime overlay: the shared **star pill** in the header, **solid-card rows** via `ApplySolidCard`, ghost Back/Restore) is opened by the **star pill** (`UIManager` → `OnShopRequested` → `GameBootstrap`). **v1.0 ships with real-money commerce HIDDEN** behind `ShopScreen.RealMoneyEnabled = false` (the only `IStoreService` is a mock — selling would be fake purchases): the Starter Pack, star packs, Remove Ads, and Restore sections are fully built but gated, returning with a one-line flip when real billing lands. What shows in v1.0: the star-priced **Power-Up** bundles (the price is a bare number — the bundled font has no ★ glyph, so the "buy with stars" header + star pill carry the unit) and the **Free Stars · watch an ad** row (disabled **"Loading…"** while the rewarded ad loads, with a bounded readiness poll). It rebuilds from live state after each purchase; unaffordable bundles disable; when enabled, Remove-Ads / Starter-Pack flip to **"Owned."** The **Daily Rewards** popup (`DailyRewardPopup`, same runtime-overlay idiom — solid gold-ringed card) is a separate menu surface for the login claim + streak repair, shown when either is available (and never stacked over the first-launch tutorial offer).

**Mockable store — real billing is stubbed, not faked:** `IStoreService` abstracts real-money purchases (star packs, Remove-Ads, Starter-Pack) + **`RestorePurchasesAsync`**. The Editor/tests use `MockStoreService` (grants immediately so the flow is testable); the real platform impl is `PlatformStoreServiceStub` — a clearly-marked TODO that **always returns `Failed`** until Unity IAP + store-console products + a device are wired. Granting happens **only on `Success`**; `Cancelled`/`Failed` grant nothing. **Non-consumables** (Remove-Ads, Starter-Pack) are owned once and **idempotent** — `GrantStarterPackAsync` no-ops if already owned, and **Restore re-applies entitlements without re-granting the consumable stars**. The 3-day ad-free window is a Unix timestamp (`adFreeUntilUnix`); `GameBootstrap` recomputes `AdPolicyService.AdsRemoved = removeAds || ad-free-active` at boot and on purchase.

**Anti-deadlock:** no fail/lives gate + the free starting + daily grants mean a broke player can always finish; power-ups accelerate, never gate — no pay-to-win.

**Ads (Google Mobile Ads, integrated + release-wired by the v1.0 audit):** `IAdService` (low-dep `Puzzle` assembly so tests mock it) → `AdService` (real AdMob) + `NullAdService` (fallback). **`BootstrapInitializer` now ensures the `AdService` component at boot** — the v1.0 audit found it was attached to *nothing*, so the production ad stack had never actually run. **Production unit IDs load from `Assets/Resources/Config/ad_units.json`** (the component is runtime-added, so there's no scene Inspector; the rule stands — real IDs never hardcoded in source; a missing config keeps the Google **test-ID** `[SerializeField]` defaults), and the **GMA App ID lives in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`** (without it a release build crashes at launch). **The editor stays ad-INERT** (`enableAdsInEditor = false`) so GMA v11 placeholder ads can't pop mid-PlayMode-test. **Rewarded video is opt-in only**, granted exactly once on the SDK reward callback, never on dismiss/failure. `AdPolicyService` enforces the **interstitial frequency cap** (time cooldown `InterstitialCooldownSeconds` = 300 **and** `InterstitialPuzzleCap` = 5 puzzles, between-session only) — and **`AdsRemoved` is wired to `removeAds` *and* the Starter-Pack 3-day ad-free window** (recomputed at boot + on purchase), so the IAPs genuinely suppress interstitials. **Task 36's rewarded-ad faucets** (watch-for-stars, the daily reward doubler, ad-based streak repair) are fully wired to `IAdService` and the production stack is now attached + configured — but **the editor is deliberately ad-inert** (`IsRewardedReady == false`, exactly like before), so in-editor their UI shows the graceful "ads not available yet" / "Loading…" states; **live rewarded ads need the device pass** (AUDIT_REPORT.md). **Login reward and star-based repair work today**, no ad needed.

**Ad-stack hardening + money durability (Task 39):** `AdService` is production-safe — **(39A)** `MobileAds.RaiseAdEventsOnUnityMainThread = true` so SDK callbacks can't touch Unity state off-thread; **(39B)** failed loads retry on a pure, unit-tested **exponential backoff** (`AdRetryPolicy`, `Puzzle` asm; `AdRetryBaseDelaySeconds` = 1 doubling to `AdRetryMaxDelaySeconds` = 64); **(39C)** the **reward is granted only on the SDK reward callback and applied after the ad closes** (never on dismiss/failure), with stored-delegate unsubscribes so handlers can't double-fire; **(39D)** every **money-bearing save** (stars/inventory/entitlements through `UpdatePlayerProgressAsync`) is flushed durably to `PlayerPrefs.Save()`, belt-and-suspenders via `OnApplicationPause`/`OnApplicationQuit` hooks on `GameBootstrap`; **(39E)** Classic's win-panel **"Next Puzzle" runs through `TryShowInterstitial`** so the grinder loop actually monetizes — still under the `AdPolicyService` frequency cap and the Remove-Ads / ad-free-window suppressions. *(39A/39C are device-only verifiable — `NullAdService` never exercises them; flag on the next Android build.)*

**Consent gate (Task 41A; UMP implemented by the v1.0 audit):** ad initialization is **consent-gated** behind `IConsentService` (Puzzle asm, mirrors the `IAdService` seam idiom) + the pure, unit-tested `ConsentGate` — `MobileAds.Initialize` is unreachable until `Gather` completes **and** `CanRequestAds` is true. `NullConsentService` (Game asm) is the Editor/test default; **device builds run the real `UmpConsentService`** (Google's UMP `Update` → `LoadAndShowConsentFormIfRequired` flow over the bundled `GoogleMobileAds.Ump.dll`; any failure completes the gather with ads gated OFF, so the game never blocks on consent). Settings gains a **PRIVACY band** (the UMP re-prompt), visible only when the consent state reports options **Required** (EEA/UK), refreshed each open. *Residual: the EEA form needs a debug-geography device test.*

**Analytics (Task 41B):** a flat, Firebase-shaped, **no-PII** event spine — `IAnalytics` (Puzzle asm) ← `LogAnalytics` (live `Debug.Log` default; `FirebaseAnalytics` is the documented swap-in once `google-services.json` lands) / `NullAnalytics` (tests). **`AnalyticsReporter` is the ONE place taxonomy events are assembled** (constructor-injected, owned by `GameBootstrap`; UI seams raise plain C# events and the bootstrap forwards, since the UI assembly can't see Puzzle). The whole taxonomy: `session_start · tutorial_step{step} · tutorial_done{skipped} · mode_start{mode} · puzzle_complete{mode,steps,win} · daily_result{grade,stars,par,steps,detours,mistakes,used_powerup,streak} · share_tapped{mode} · shop_open · purchase_attempt{product} · purchase_result{product,status} · powerup_bundle_bought{kind,size} · ad_rewarded{placement} · ad_interstitial` — no typed words, no constructed IDs. Contracts pinned by tests: **`daily_result` fires exactly once per completed run** (the one-and-done re-tap routes through `DailyReShow()`, which emits **nothing**), and `puzzle_complete` has one emission point per surface (Classic's win panel never reaches `EndGame`).

**Daily reminder notification (Task 41C):** pure, Unity-free scheduling rules in `NotificationRules` — the settings toggle (`SettingsData.notificationsEnabled`, default ON) gates **whether**; `todayPlayed` shifts **when** (already played → tomorrow-at-hour, otherwise today-at-hour if it hasn't passed); the only scheduling pattern is **cancel-then-reschedule** (idempotent, never stacks). Fire hour = `BalanceConfig.ReminderHourLocal` (19:00); body copy omits the streak suffix for streak-0 players. The platform scheduler (`LocalNotificationService` over Unity Mobile Notifications) is the pending consumer — see [§13](#13-known-tech-debt--candidate-tasks).

---

## 4. Juice: motion, haptics, sound

All three feedback channels fire on the same four moments and all respect a **reduce-motion** accessibility flag.

| Moment | Animation (≤200ms ease-out) | Haptic | Sound |
|---|---|---|---|
| Letter placed | tile punch + glyph **drop-in settle** (`LetterTile.PunchScale` / `DropInSettle`); the input row **reuses persistent tiles** (`ReconcileInputTiles`) so **every** typed letter pops, not just the last | light tap | key-press |
| Word accepted | newest row **climbs** up into place (`UIAnimations.RowClimbSettle`); changed tile → green | medium tap | accept |
| Word rejected | input-row shake (skipped if reduce-motion; reason still shows) | buzz | reject |
| Puzzle won | `GameplayScreen.WinAscentBeat` (TO row gold→green, upward rise+settle, ~500ms) | buzz | win sting |

- **reduce-motion:** `SettingsData.reduceMotion` → `UIAnimations.ReduceMotion` (static, set from `GameBootstrap` on settings load/save). Every animation coroutine in `UIAnimations` and `LetterTile` snaps to the end-state and `yield break`s when true.
- **Haptics:** `IHaptics` → `HandheldHaptics(Func<bool> enabled, Action vibrate = Handheld.Vibrate)` + `NullHaptics`. `Handheld.Vibrate` is a coarse full-buzz; fine-grained haptics need a plugin (TODO, not added). Gated on `SettingsData.hapticsEnabled`. The injectable `vibrate` action makes it unit-testable.
- **Sound:** `SfxManager` (pooled `AudioSource`; clip slots assigned in-scene). No `AudioMixer`/clips in the repo yet → AudioListener-level. Pure static gate `SfxManager.EffectiveSfxVolume(SettingsData)` returns 0 when muted (testable).

---

## 5. Visual identity

True-black, **outline ("ghost")** identity with a vertical ladder/ascent metaphor.
- **Three button tiers + the logotype/icon identity (Task 43):** hierarchy is carried by FILL, not just glow. **Tier 1 — ONE filled gradient hero per surface**: menu **DAILY** fills orchid→deep-violet over the shared bubbly 9-slice (`ApplyFilledHeroButton` + the `UIVerticalGradient` mesh effect; the neon glow now reads as rim light on a solid). **Tier 2 — outlined secondaries**: the modes and context actions stay **colored rounded outlines with transparent centers** (`ApplyOutlineButton` / `ApplyPrimaryMenuButton` — one shared geometry), each carrying a **tight, static neon-tube glow** tinted to its own token (`UITheme.ApplyNeonGlow` — 8 faint `Shadow` samples over a ~2px radius; a luminous *line*, not a halo). **Tier 3 — ghosts**: navigation/utility recedes to **tinted Label text on an invisible ≥96px hit target** (`ApplyGhostButton` — no ring, no glow: menu Library/Stats, library grid Back, shop Restore, results Home). Mode tokens — **Daily** orchid (hero), **Classic** blue-violet, **Puzzle Library** deep-violet, **Timed** magenta, **Resume / HOME** periwinkle; **bright labels** on every button. Mode buttons carry **stroke icons** (white SVG art × token tint, `Tools/logo_icons_build.py` — identity survives grayscale/colorblind viewing), and the masthead is the **Star Ladder logotype sprite** (aqua→periwinkle, a single four-point star in the word gap — the old "ladder-rung A" overlay was retired because it read as a glitch on the A at masthead size; TMP `Display` text fallback so a missing asset can't strand the menu). **The solid-card seam:** any card/row/modal whose content sits over the backdrop uses `UIThemeManager.ApplySolidCard(img, accent)` — a rounded near-opaque `SurfaceVoid` fill with the accent ring (+ tight glow) as a **layout-ignored overlay child** (tutorial welcome modal, stats cards, shop rows). Never use `ApplyOutlineButton` as a card *fill* — its centre is transparent and content collides with backdrop art; and note `ApplyNeonGlow`'s halo child carries `LayoutElement.ignoreLayout` so a glow inside a layout group can never become a phantom cell.
- **Purple-black background + swappable space layer (video or still):** the app renders on a purple-black `#0D0A1F` (`Palette.SurfaceVoid`), painted by a single full-screen **Background layer** behind every screen (`UIThemeManager.ApplyScreenBackground` / `EnsureBackgroundLayer`). Backdrop priority is **looping video → still sprite → flat black**, all swappable by just dropping a file in (no scene edit): a muted, looping `Assets/Resources/UI/SpaceBackground.mp4` (driven by a `VideoPlayer` → `RenderTexture` on a full-screen `RawImage`, `EnsureVideoBackground`) wins; else `SpaceBackground.png`; else flat black. A pixel-art space loop ships now. **Task 44:** the video is **gated** — `ReduceMotion` ON or OS **low-power** (`IPowerModeService`; the always-false `NullPowerModeService` ships) resolves the layer to the **still** on the next screen transition (pure, unit-tested `UIThemeManager.ResolveBackdrop`), and the player **pauses on app background / focus loss** (`VideoBackdropPauser`).
- **Gameplay tiles:** the **start** row is **white** (`TextPrimary`) see-through outline tiles (Task 44 — the neutral "where you began"; **aqua is reserved for success/affirmative**), the **target** row is **orchid**, and the played **chain** rows are **periwinkle** see-through outlines — all with a **bold ~7px ring**. The **active input row stays solid** (the obvious "current row," gold-edged as you type); the **aqua** correct-letter highlight shows inside the chain rows; the win beat turns the target solid aqua. The chain `VerticalLayoutGroup` honors the rung gap (`childControlHeight = true`) so rows read as **separate rungs**, not a touching block.
- **Gameplay scrim + safe area (Task 44):** the gameplay screen (all four modes) gets a static **vertical-gradient scrim** on the shared BackgroundLayer — `SurfaceVoid` at **0.00 (top 15%) → 0.28 (board) → 0.38 (keyboard)** (`Scrim` constants in `UITheme`, bands built from `UIVerticalGradient`, raycast-transparent), so the brightest video frame can never drop tile/key contrast; menu/stats/library keep the unveiled backdrop spectacle, and overlays above gameplay (tutorial) keep the scrim. Every screen that requests its background is safe-area'd by **`SafeAreaPanel`** (`Screen.safeArea` → root anchors; the backdrop + scrim intentionally stay full-bleed outside the panel); the old hand-baked 130px gameplay header offset is retired for `HudLayout.HeaderTopPadding` below the safe edge.
- **Keyboard (Tasks 29, 32, 44):** **rounded** keys (the shared bubbly 9-slice) — **solid `Panel` fills** (≥ 0.88 alpha, pinned by tests), `DEL` = `Alert`, `GO` = `AccentAqua`, letters `Label`/`TextPrimary` — floating on a **transparent panel**, so the space background fills the whole lower screen (no grey brick behind the keys).
- **Gameplay motion (Task 29):** subtle **ladder-feel** animations — a letter **drops into** its tile as you type, a valid word's row **climbs** up into place, the win beat pulses, an invalid word shakes — all `ReduceMotion`-gated and clamped-`dt` smoothed.
- **Star Shop (Task 33):** the same identity — purple-black, **aqua** title (**STAR SHOP**), **gold** balance, solid-card rows — reached via a tappable **star pill** on the menu.
- **The currency is STARS (gold):** the currency mark is a gold five-point star — `UIThemeManager.CreateStarToken` draws a `StarGraphic` in the `Palette.Coins` gold token (kept; gold stars suit the Star Ladder identity), wired into all three pills (menu / Stats / Star Shop). **Warm gold `Coins #E9C98C` is in-game only** (the colour token name is unchanged): hint / active-input tiles, the win "Next Puzzle", in-progress & current-shelf rings, the streak headline, and the star icon/number. (The star pill's *background ring* is periwinkle; only the star + number are gold.) The bundled font has no ★ glyph, so the star is always drawn geometry, never text.
- **Menu motion (Task 28):** the aqua **STAR LADDER** title does a one-time entrance then a slow, subtle vertical float; the buttons **cascade** in on open and give a tactile **press-punch** on tap. All coroutine/`Mathf`-based, **clamped-`dt` smoothed** so it rides through screen-transition hitches, and **fully gated by `UIAnimations.ReduceMotion`** (ON ⇒ static).
- **Ascent:** the chain climbs toward the anchored TO row at the bottom; the win beat reinforces upward motion.
- **Motion vocabulary** (one place: `UIAnimations`): `MICRO = 0.16s` (micro-interactions), `STANDARD = 0.22s` (transitions), `EaseOutCubic`. Deliberate and weighted — no cartoon bounce. All restyles are static and honor ReduceMotion.

---

## 6. First-launch tutorial

On first launch (flag `onboarding_v1` absent/incomplete) the tutorial is **OFFERED over the menu at boot** (`GameBootstrap.MaybeOfferTutorial` → `TutorialOverlay.ShowWelcome`) — no longer **forced** when Classic is tapped. The welcome is a proper **modal**: a 0.86 `SurfaceVoid` scrim + a **solid card** (`ApplySolidCard` — the old outline-only card let the menu bleed through the copy), with **PLAY TUTORIAL as a filled hero** (the DAILY gradient) and **SKIP as a ghost** (marked done, stay on the menu, never re-nags); Classic always just plays.
- Fixed ladder **CAT → BAT → BAG** (`TutorialPuzzle.Create()`), injected like the daily puzzle.
- `TutorialOverlay` — non-modal step-gated coach marks (accent-gold emphasis) with a **Skip** button at every step; advances only on intended actions; rejection reuses the existing `OnWordSubmissionResult` feedback; a short success beat then drops into the first real puzzle.
- Gating logic is pure/testable: `OnboardingRules.ShouldRouteToTutorial / MarkCompleted / Reset`. Persisted as `OnboardingData { completed, skipped }` under `onboarding_v1`.
- **Replay tutorial** in Settings clears the flag and drops **straight into the lesson** (no welcome prompt), repeatable cleanly. The flag **survives Reset Progress** (only Replay clears it). If the overlay isn't wired, the offer no-ops so a player is never stranded.

---

## 7. Puzzle Library shelves

> The mode is **Puzzle Library** and its 7 difficulty bands are **Shelves** to the player (renamed 2026-06 from "Puzzle Show" / "tiers"). *Display-only* — the code keeps `PuzzleShowMode`, `tierId`, `TierData`, `tier_definitions.json`, `MaxTier`, `PuzzlesPerTier`, etc. So below, "shelf" = what the player sees; the cited identifiers are the unchanged internals.

**7 shelves × 100 = 700 curated ladders** (`MaxTier = 7`, `PuzzlesPerTier = 100`), on a length/difficulty curve. Every puzzle clears the [§8](#8-word-validation) **minimum-move floor** (true full-dictionary shortest ≥ 2), has **≥ 2 distinct optimal-length routes** (multiple ways to solve, guaranteed — single-route candidates are flagged and replaced at build time), and rises across shelves:

| Shelf | Word length | Moves (steps) |
|---|---|---|
| 1 | 3 | 2–3 |
| 2 | 4 | 2–3 |
| 3 | 5 | 3–4 |
| 4 | 5–6 | 3–4 |
| 5 | 6 | 4–5 |
| 6 | 6–7 | 4–6 |
| 7 | 7 | 4–8 (hardest) |

**Two-level navigation** (`PuzzleLibraryScreen`): **Shelf Select** (7 shelves with theme + `X/100` + lock state) → tap an unlocked shelf → **Shelf Grid** (that shelf's 100 cards + Back). Only the active shelf's cards render (performance).

**Progressive unlock:** Shelf 1 is unlocked by default; clearing `BalanceConfig.PuzzlesRequiredToAdvance(tier)` puzzles unlocks the next — **10 / 15 / 20 / 25 / 30 / 35** out of shelves 1–6 (rises with depth; Shelf 1 stays 10 for the wiring test). Unlocked shelves stay open, so a player can return to any shelf for completion.

**Completion coloring** is driven by saved progress, resolved by the pure `PuzzleShowMode.ResolveState(puzzleId, tierUnlocked, completed, inProgress)` (shared by the live mode and the library so card state matches gameplay exactly): **Completed** → **aqua** + ✓ (Direction B retired green; the ✓ is a non-color cue for colorblind mode), **In Progress** → hero-accent border, **Unlocked/Unplayed** → surface grey, **Locked** → dim. `GameBootstrap.ShowLibrary` injects the saved `PuzzleProgressData` into the screen via `PuzzleLibraryScreen.SetProgress(completed, inProgress, highestUnlockedTier)` before it populates.

Progress (`PuzzleProgressData`: completed IDs, in-progress IDs, current shelf) persists under `puzzle_progress_v1`. The authoritative shelf→puzzleId map comes from `tier_definitions.json` (never hardcoded math). **`tier_definitions.json` is machine-generated** by `Tools/puzzleshow_build.py` — see [§12](#12-testing--tooling). Tapping an **unbeaten** card → `OnLibraryPuzzleSelected(int puzzleId)` → `StartSpecificPuzzle`.

**Library Path View** (per-puzzle solve record). Tapping a **beaten** card opens a spoiler-free detail panel instead of replaying: **(A)** the player's **best solve** (the full word route, "best" = fewest steps, only ever improving), and **(B)** the canonical **optimal path** as word-slots — matched slots revealed (green), the rest still **blank** — so the optimal route uncovers progressively across replays (a perfect/optimal-length solve auto-reveals the whole path). A **REPLAY** button reuses the normal launch path. All update rules live in one pure, Unity-free, fully-tested place — `PuzzlePathProgress.ApplySolve` (best only improves; the revealed set unions and never shrinks) — folded on every solve/replay by `PuzzleShowMode.OnPuzzleSolutionReached` *before* `ExportProgress`, so the record persists. Records are kept in `PuzzleProgressData.puzzlePaths` (`List<PuzzlePathRecord>{ puzzleId, bestSolvePath, bestSolveSteps, revealedOptimalIndices }`); `GameBootstrap.ShowLibrary` injects them via `PuzzleLibraryScreen.SetPathRecords`. The detail panel is **content-sized + centered** (hugs its content, no dead space). Old saves default to an empty list (no migration needed — a previously-beaten puzzle just shows nothing until next played).

---

## 8. Word validation

`WordValidator : IWordValidator` accepts a word onto the chain only if **all** hold:
1. Exists in the 17,326-word curated dictionary (`word_library.json`; 3-letter 890 / 4-letter 2,429 / 5-letter 3,716 / 6-letter 4,882 / 7-letter 5,409).
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
Tiers:       MaxTier=7  PuzzlesPerTier=100  PuzzlesRequiredToAdvanceTier=10 (base)
             PuzzlesRequiredToAdvance(tier): 10/15/20/25/30/35/40 (rises with depth)
Economy:     PuzzleCompletionReward=10  RewardedAdHintGrant=1
             StartingPowerUpGrant=5  DailyPowerUpGrant=2
Daily 2.0:   DailyMistakeBudget=3  PerfectMaxDetours=0  GoodMaxDetours=2  PowerUpMaxGrade=Good
             DailyCoinReward(stars,failed): Perfect=60 Good=40 Solved=25 Failed=10
             StreakRepairCoinCost=150  StreakRepairCooldownDays=7
Faucets:     LoginRewardCycle={25,25,50,50,75,75,150}  WatchCoinsReward=35 (WatchCoinsDailyCap=3)
             StreakMilestones={7,30,100} @ StreakMilestoneReward=100
Shop:        PowerUpBundleSizes={5,15,40} + tiered price arrays:
             Hint/UndoBundlePrices={50,135,320}  RevealBundlePrices={120,320,800}  TimeBundlePrices={60,160,400}
             (coin packs + Remove-Ads + Starter-Pack are defined in coin_shop.json, NOT BalanceConfig)
Ads:         InterstitialCooldownSeconds=300  InterstitialPuzzleCap=5
             AdRetryBaseDelaySeconds=1  AdRetryMaxDelaySeconds=64 (load-retry doubling curve)
Reminders:   ReminderHourLocal=19 (daily streak-reminder local notification)
```

`Constants.cs` forwards its legacy power-up/tier fields to `BalanceConfig` to avoid drift.

**Generation quality filter:** `common_words.json` (6,875 verified words — every ENABLE word inside a tighter Norvig frequency gate, dense across all lengths 3–7) restricts generated START/END (and intermediates) to fair words. Fallback chain: strict-common → relaxed-common-endpoints → known-good fallback (`cat→cot→cog→dog`). Curated tier/daily puzzles bypass the generator and are exempt.

---

## 10. Architecture

### Module / namespace map
```
Assets/Scripts/
├── Core/
│   ├── Engine/        WordPuzzle.State    GameState (immutable), GameStateManager (reducer/Dispatch),
│   │                                      GameAction, Constants, EconomyManager, IEconomyManager,
│   │                                      IStoreService/MockStoreService/PlatformStoreServiceStub, ShopCatalog
│   └── Persistence/   WordPuzzle.Persistence  IDataManager, DataManager, PlayerProgress, SaveData,
│                                          SettingsData, DailyProgress, OnboardingData, TierDataLoader
├── Game/             WordPuzzle.Game      GameBootstrap (DI wiring), BootstrapInitializer,
│                                          DailyPuzzleService, DailyStreakRules, OnboardingRules,
│                                          TutorialPuzzle, ShareCardBuilder, IShareService,
│                                          IAdService impls (AdService, AdPolicyService, NullAdService),
│                                          AnalyticsReporter, LogAnalytics, NullConsentService,
│                                          UmpConsentService (device UMP), GameLog (release-stripped),
│                                          NotificationRules
│   └── Modes/         WordPuzzle.Modes    ClassicMode, PuzzleShowMode, TimeAttackMode(+Config),
│                                          IGameMode, ModeController
├── Puzzle/           WordPuzzle.Puzzle    WordGraph, WordValidator (IWordValidator), PuzzleGenerator,
│                                          WordOps, BalanceConfig, PathScoring (daily grade/stars), IAdService,
│                                          AdRetryPolicy, IAnalytics, IConsentService (+ ConsentGate),
│                                          WordPuzzle (model), PuzzleDefinition, TierData, Difficulty, ValidationResult
└── UI/               WordPuzzle.UI        UIManager, UIAnimations, TimerDisplay, Themes/UITheme,
                                           Audio/SfxManager, Haptics/(IHaptics,HandheldHaptics,NullHaptics),
                                           TutorialOverlay, Components/(LetterTile, OnScreenKeyboard, …),
                                           Screens/(MainMenu, Gameplay, PuzzleLibrary, Results,
                                           Settings, TimeAttackSetup, Shop, Stats, DailyRewardPopup)

Assets/Resources/Data/  word_library.json (17,326), tier_definitions.json (700 = 7×100), daily_puzzles.json (600), common_words.json (6,875), coin_shop.json (coin packs + Remove-Ads + Starter-Pack)
Assets/Resources/Config/ ad_units.json (PRODUCTION AdMob unit IDs — loaded by AdService.Awake; missing ⇒ test-ID defaults). GMA App ID: Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
Assets/Scenes/          GameUI.unity  ← the ONLY live scene. MainMenu/ClassicMode/PuzzleShowMode/
                                        TimeAttackMode/SampleScene are legacy and never LoadScene'd.
Assets/Tests/           Unit/ + Integration/  (NUnit; TestMocks.cs has Mock* doubles)
Assets/Editor/          SceneBuilder*.cs + Verify* menu-item tools
```
Assembly dependency direction: `Puzzle` (lowest) ← `Persistence`/`State` ← `Modes` ← `Game`/`UI`. **`Puzzle` must never reference `State`/`UI`** (circular). Put shared low-level types in `Puzzle`.

### State flow (immutable + Dispatch — DO NOT change this shape)
`GameStateManager` owns an immutable `GameState` snapshot plus a private `MutableGameState`. UI subscribes to state; `GameAction` instances go through `Dispatch()`, which routes to handlers: `HandlePressLetter`, `HandleDeleteLetter`, `HandleSubmitWord`, `HandleUseHint`, `HandleUseReveal`, `HandleUseAddTime`, `HandleUndo`. Each handler mutates the working state, then notifies subscribers and persists. Events: `OnWordSubmissionResult` (accept/reject + reason), `OnTimeAdded` (AddTime/Survival seconds). **Daily 2.0 (Task 36)** adds daily fields on the *internal* `MutableGameState` (mistakes/detours/par/per-step classes), armed by `ConfigureDailyRun(mistakeBudget, par)` after `StartNewPuzzle` and read via getters (`GetDailyResult`/`GetMistakesRemaining`/`GetDetourCount`/`GetDailyStepClasses`/`IsDailyRun`) — the immutable `GameState` shape is unchanged.

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
| `puzzle_progress_v1` | `PuzzleProgressData` (tiers, completed IDs, in-progress IDs; **Library Path View:** `puzzlePaths` — per-puzzle best-solve + revealed-optimal slots) | ✅ yes |
| `wordpuzzle_progress` | `PlayerProgress` (coins, **owned power-up inventory**, `removeAds`, starting/daily-grant flags, stats; **Task 36:** `starterPackOwned`, `adFreeUntilUnix`, login-reward cycle pos, watch-for-coins counter, milestone-paid marker) | ✅ yes |
| `wordpuzzle_save` | in-flight `GameStateSnapshot` | ✅ yes |
| `daily_v1` | `DailyProgress` — streak + **Daily 2.0:** `lastPlayedDateIso`, `lastRepairDateIso`, `todayPlayed`, and the trailing-365 `outcomes` ledger (`DayOutcome{dateIso,won}`) | ✅ yes |
| `settings_v1` | `SettingsData` (volumes, mute, reduceMotion, hapticsEnabled, **notificationsEnabled** — Task 41C, default ON) | ❌ preserved |
| `onboarding_v1` | `OnboardingData` (tutorial done/skipped) | ❌ preserved (only Replay clears) |

`DataManager.ResetAllAsync` clears the four "yes" keys and preserves settings + onboarding. (`"Coins"` is a legacy key written only by the orphaned `CoinSystem`/`PlayerDataManager` — see [§13](#13-known-tech-debt--candidate-tasks).)

**Migration (Tasks 33, 36):** `PlayerProgress`/`PlayerProgressData` gained `totalTimeEarned`, `removeAds`, `startingGrantApplied`, `lastDailyGrantDate` (33), then `starterPackOwned`, `adFreeUntilUnix`, `lastLoginRewardDate`, `loginRewardIndex`, `lastWatchCoinsDate`, `watchCoinsCountToday`, `highestStreakMilestoneAwarded` (36). `DailyProgress` gained `lastPlayedDateIso`, `lastRepairDateIso`, `todayPlayed`, `outcomes` + a `Normalize()` (the Q6 seed `lastPlayedDate ← lastCompletedDate`, run in the **Persistence** assembly by `DataManager.LoadDailyProgressAsync` since it can't reference the Game asm). Everything serializes through the DTO + `DataManager` converters, and **JsonUtility auto-defaults missing fields**, so pre-update saves load cleanly (new fields = 0/false/"") and one-time grants (e.g. `startingGrantApplied = false`) fire once on the next boot. **Task 40** added `todayResultUsedPowerUp` to `DailyProgress` (so the one-and-done re-show keeps the "assisted" disclosure); **Task 41C** added `notificationsEnabled` to `SettingsData` (field-initialized `true`, so old saves default ON). **Durability (Task 39D):** money-bearing progress writes (`UpdatePlayerProgressAsync`) call `PlayerPrefs.Save()` immediately, plus `OnApplicationPause`/`OnApplicationQuit` flushes on `GameBootstrap` — a force-killed app can't lose a purchase or ad reward.

---

## 12. Testing & tooling

- **NUnit EditMode** tests under `Assets/Tests/Unit/{Engine,Persistence,UI}` and `Assets/Tests/Integration`. The `Unit.Tests` asmdef references the `Game.*` assemblies (incl. `Game.Puzzle`, `Game.UI`, `Game.Persistence`); UI-folder tests use a separate `Tests` asmdef. Most new tests need **no asmdef change**.
- **Mocks** in `Assets/Tests/TestMocks.cs`: `MockDataManager`, `MockWordValidator`, `MockEconomyManager`, `MockAdService`, `MockAnalytics` (records `(name, params)` events; `CountOf(name)`). Extend these rather than inventing new doubles.
- **Conventions:** pure-logic classes (e.g. `DailyStreakRules`, `OnboardingRules`, `WordOps`, `BalanceConfig`, `SfxManager.EffectiveSfxVolume`) are tested standalone; `GameStateManager` tests build it with the mocks; persistence tests use `new DataManager()` against PlayerPrefs with `[SetUp]/[TearDown]` key cleanup.
- **Run (human):** Window → General → Test Runner → **PlayMode** → Run All. **MCP agents:** `run_tests(mode="PlayMode")` — it works (full suite **384/384**: the v1.0 audit added 3 corrupt-save regression tests, then the daily failure rework added the free-bounce / precedence / typos-never-fail tests + a 4-step-floor data test); EditMode returns 0 (see [§17](#17-notes-for-ai-agents-working-in-this-repo)).
- **Editor tools** (`Tools/` menu): `Verify*` probes (library/ladder/polish), `SceneBuilder*` idempotent scene builders.
- **Key data-integrity tests:** `MinMovesFloorTests` (no sub-2-move puzzle anywhere; generated puzzles meet the length curve, by *true* BFS distance), `PuzzleShowTierTests` (7×100 structure, Hamming-1 ladders, non-decreasing min steps, ≥2-optimal-route guarantee, `ResolveState` mapping, progressive unlock), `GenerationQualityTests` (junk-blocklist absence, curated-word presence, min long-word counts), `PostWinRouterTests` (per-mode surface routing), `BalanceConfigWiringTests`; **Tasks 39–41:** `AdRetryPolicyTests` (backoff curve), `PathScoringTests` (incl. the power-up grade cap), `AnalyticsReporterTests` (emission contracts — `daily_result` once, re-show silent), `ConsentGateTests` (no ad init before consent), `NotificationRulesTests` (played/unplayed × before/after hour × toggle).
- **Canary convention:** a `// CANARY-INVERTED` / `// CANARY-VIOLATION` comment marks a *deliberate* must-fail bug left in to prove the test runner surfaces failures — run the suite, confirm the failure appears, then fix it; don't debug it as if it were accidental.

### Reproducible data pipeline (`Tools/` — Python, NOT shipped in the build)
The word data is **machine-generated and validated**, not hand-edited — re-run the tools, never edit the JSON by hand (it would drift and can silently break solvability/floors). All live outside `Assets/`, fetch/cache reference lists in the OS temp dir (never committed), and **fail loudly** on any violation. **License (verified):** shipped word content comes only from **ENABLE** (PUBLIC DOMAIN — not TWL/SOWPODS), so it is commercial-safe; the Norvig frequency list is build-time-only ranking and is not redistributed. **Run order:** `dictionary_build → puzzleshow_build → daily_floor_fix → daily_expand`, then `verify_data` (all support `--dry-run`).
- **`dictionary_build.py`** → rebuilds `word_library.json` + `common_words.json` as a PURE function of the cited sources: `library = {ENABLE words len 3–7 with Norvig freq-rank < 60000} ∪ {permanent original-daily solution words}` MINUS a 124-term offensive/slur **blocklist**; `common = {library words with rank < 15000}`. This simultaneously **cleans** obscure-but-valid junk (e.g. `abaka`, `abmho`, `abos`) and offensive terms, and **adds** fair words across ALL lengths 3–7. Re-validates all curated puzzles stay solvable + reports multi-route counts.
- **`puzzleshow_build.py`** → regenerates `tier_definitions.json` (7×100) on the difficulty curve; every ladder is a BFS shortest path drawn from the common subset, validated for the min-move floor by **true full-dictionary** distance, unique within tier, in-band, and proven to have **≥ 2 distinct optimal routes** (single-route candidates are flagged & replaced).
- **`daily_floor_fix.py`** → enforces the **daily-only 4-step floor + honest par** (2026-06 rework, `FLOOR = 4`): replaces any Daily whose true full-dictionary shortest is `< 4` **OR** whose stored `optimalSteps` ≠ the true shortest (a dishonest par lets players beat par via non-common words), with fresh same-length ladders whose true distance EXACTLY equals the target, **preserving puzzleId + array order** (so `DailyPuzzleService` indexing is unchanged). `verify_data.py` re-checks the floor + honest par independently. (Tier/Classic/Timed floors are untouched.)
- **`daily_expand.py`** → **additively** grows the Daily pool (currently 450 → 600) by appending validated puzzles in a reserved id block (≥ 20001); idempotent (re-runs rebuild the same set), preserves the original 450 byte-stable. Save-safe: daily index is `day % poolCount` and progress is keyed by ISO date.
- **`verify_data.py`** → independent integrity verifier (mirrors `VerifyWordLibrary.cs` rules + multi-route + min-move floor + offensive-absence + counts). `--canary` injects a broken Hamming-1 edge and asserts it is **caught** (proves the checks fail when they should).
> **Byte-reproducible:** `build_graph` returns SORTED adjacency so BFS is deterministic regardless of `PYTHONHASHSEED`; re-running the whole pipeline yields **byte-identical** JSON (SHA-256 verified). Run via the Bash tool (`python Tools/<tool>.py`); the output is the committed JSON.

---

## 13. Known tech debt / candidate tasks

> **v1.0 release audit (2026-06-12):** see **`AUDIT_REPORT.md`** at repo root — a full coherence/debug/polish/lifecycle/release pass. Fixed in that pass: **B7** corrupt-save recovery (3 unguarded `JsonUtility.FromJson` loads, incl. the money path, now catch-and-default + regression-tested), the **ad stack actually attached + release-configured**, **UMP consent implemented**, **real-money commerce gated off** for v1.0, iOS bundle ID set, release log hygiene. Remaining before ship: the owner's visual gates, merge to `main`, and the device-verification list in the report's Honesty section.
>
> **Post-audit polish (2026-06-13):** the daily failure rule was softened (typos bounce free; only rule-breaks spend a mistake) and the daily pool re-floored to **≥ 4 steps with honest par**; a sweep of modal/screen surfaces moved every card onto `ApplySolidCard` (tutorial welcome, Stats, Star Shop, Settings sections, daily-rewards popup, and a new runtime **Reset Progress** confirm modal — the scene-authored one rendered transparent/behind). And a **terminology rename pass** (display-only): **Time Attack → Timed** (results disambiguate **Timed** / **Timed Survival**), **Puzzle Show → Puzzle Library** with **tiers → shelves**, **Classic Mode → Classic**, **Shop → Star Shop**, and the **coins → stars** currency (gold five-point-star token). Internal types/fields/files are unchanged.

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
9. **Real IAP billing** — `PlatformStoreServiceStub` always returns `Failed`, so **v1.0 hides all real-money commerce** behind `ShopScreen.RealMoneyEnabled = false` (the v1.0 audit's Track 3). Activating the Starter Pack / coin packs / Remove-Ads / **Restore** for real money needs Unity IAP + store-console products (`starter_pack`, `coins_150…coins_7000`, `premium_no_ads`) + receipt validation + a device build, then the one-line flag flip. The shop UI/flow are already written against `IStoreService`.
10. **Device verification of the now-wired ad stack** — the v1.0 audit attached `AdService` (it had never been on anything), wired production unit IDs (`Resources/Config/ad_units.json`) + the GMA App ID asset, and implemented `UmpConsentService`. The faucets (watch-for-coins, reward doubler, ad-repair) are fully wired and the editor is deliberately ad-inert — what remains is **device-only**: real ads loading/showing, 39A (main-thread events) / 39C (reward-after-close) under the live SDK, and the UMP form under EEA debug geography. Login reward + coin-based repair already work.
    - **Analytics device swap-in (Task 41):** `LogAnalytics` (Debug.Log) stands in for `FirebaseAnalytics` until `google-services.json` lands. Swaps behind the existing seam, no call-site changes.
    - **Notification platform scheduler (Task 41C):** `NotificationRules` (pure, fully tested) has no consumer yet — a `LocalNotificationService` over Unity Mobile Notifications needs wiring (cancel-then-reschedule on boot/settings-change/daily-finish) plus the Settings toggle row.
11. ✅ **Done (Task 38):** the daily-HUD leak into Classic is fixed (`GameplayScreen.SetDailyPar` always writes/clears the slot via the pure `ComposeDailyHud`); the Results doubler + streak lines no longer leak onto non-daily results; the **Stats screen was rebuilt** into grouped runtime cards; **one-and-done now truly locks** (re-shows the stored result on re-tap, no replay/coin-farm); and the pre-existing **missing-script** scene errors were cleaned (`Tools/Cleanup/Remove Missing Scripts In Open Scenes`).
12. **Typography follow-ups (Task 42):** the Rungo TMP assets are **dynamic** SDF atlases built at runtime — fine for a word game's glyph set, but **bake static atlases in-Editor before the production build** (first-frame cost + determinism). Once the ★/✓ symbols fallback proves stable on device: `StarGraphic` (drawn star meshes) is retirable in favour of real ★ glyphs, and the tile ✓/✗ state cues + the Results "⌂ HOME" glyph — disabled when the bundled font lacked them — can be re-enabled (deliberate visual calls, not bugs).
13. **Native power-mode reader (Task 44):** `IPowerModeService` ships as the always-false `NullPowerModeService`. iOS (`NSProcessInfo.lowPowerModeEnabled`) / Android (`PowerManager.isPowerSaveMode`) implementations — wired in `GameBootstrap` exactly like the ad/store stubs — would let the video backdrop drop to the still automatically in battery saver.
14. **Balance-audit flags (Task 38 — surfaced, NOT applied; these shift feel/monetization, so they're deliberate calls):** *par-relative* daily detour grading (cutoffs are currently absolute, so long dailies grade proportionally stricter); Starter Pack generosity (~502 coins/$ one-time). Also: the per-use `HintCost`/`RevealCost`/`UndoCost` constants are **vestigial** (the owned-inventory model consumes a *charge*, not coins) — deletable once `Constants.cs` stops forwarding them.

> **Verification note (important for any agent):** the unityMCP **`run_tests(mode="PlayMode")` path works** and reports real pass/fail — the full suite runs green at **384/384** and genuine failures surface correctly. Two caveats: **(a)** `mode="EditMode"` with a filter returns `total:0` (a false `"Passed"`) because this project's test assembly is **PlayMode-registered** — always run PlayMode; **(b)** PlayMode occasionally "fails to initialize (timeout)" — retry, and if a timed-out run left the editor in Play Mode, `manage_editor(stop)` first. See [§17](#17-notes-for-ai-agents-working-in-this-repo) for the full workflow.

---

## 14. Writing a master prompt for this repo

Tasks here are driven by a consistent **meta-prompt** format — paste the whole document into Opus (often with `USE SWARM`) and it self-organizes, plans, implements, and verifies. The shape that's proven out across 30+ tasks (the modern template):

**1. OPERATING RULES (read first).** A short preamble that sets the bar for *every* task:
- **Definition of done** — concrete and outcome-based; *"a tool reported success" is NOT done.* Spell out exactly what must be true (every screen, all tests green, editor left **OUT of Play mode**).
- **Ask before assuming** — STOP and surface assumptions in the PLAN rather than guessing.
- **Scope discipline** — do ONLY this task; list everything else at the end as *"Observations for later."*
- **Verification honesty** — run the suite via `run_tests(mode="PlayMode")` (it works; EditMode returns 0 — [§17](#17-notes-for-ai-agents-working-in-this-repo)), confirm a **must-fail canary** actually fails, and hand the **portrait Simulator eyeball** to the user (MCP can't screenshot it). "A tool reported success" is still not done.
- **Single-editor reality** — only one process writes the Unity project at once; specialists PLAN in parallel, a **named Lead** integrates, resolves shared-file conflicts, and verifies editor state.

**2. [Shared Context Block](#shared-context-block-paste-into-every-task-prompt)** — paste it; tell the agent to **verify it against the live tree** (this README drifts).

**3. REFERENCE** — if a screenshot is attached, *describe it in prose* for the agent that can't see images.

**4. GOAL** — one concern per prompt.

**5. Lettered sub-tasks** (`TASK NA / NB …`) — each with **Targets** (exact files/seams), **Do** (steps), and **ACCEPTANCE** (what a test asserts / the manual check).

**6. DO NOT (guardrails)** — the easy-to-trip mistakes: don't change `onClick`/routing/scoring, don't break tap/raycast/badge/label, honor ReduceMotion, don't save `GameUI.unity` with non-default screen visibility, delete the `.meta` with any asset.

**7. SWARM ORCHESTRATION** — name a **Lead Developer (coordinator)** plus specialist agents (UI/Theme, Layout, UI/Feel, QA, …) that coordinate **through the Lead** (SendMessage-first, not polling). The process is always **PLAN FIRST → Lead approves → implement → QA + Lead watch the Simulator → Lead integrates.** Give an explicit **dependency order**.

**8. FINAL VERIFICATION + FINAL DELIVERABLE** — a checklist the Lead must *honestly* tick (or flag as incomplete), then a SUMMARY: what changed, the perf/ReduceMotion confirmation, before/after Simulator notes, EditMode assertions read by hand, and confirmation the editor is OUT of Play mode with a clean tree.

**For BIG features, phase it.** Land the **foundation first** (logic + persistence + tests, EditMode-verifiable) → get a confirm → then the **UI + the human's playtest**. And when a task has a genuine fork the code can't settle (how deep to wire an economy, a missed-day policy, a visual-weight choice), **ask the user that one question, then proceed on sensible defaults for the rest** — don't stall the whole task on choices that have an obvious default. Commit/push each verified, self-contained increment so the working tree never carries a half-built feature.

**Repo conventions to bake into every prompt:** colors live in `UITheme` (`MenuPalette` for the menu) — **no inline hex**; tunables live in `BalanceConfig`; the menu/screens **style themselves at runtime**, so most visual tasks need **no scene edit** (restyle/animate in code and `GameUI.unity` stays clean); all motion routes through `UIAnimations.ReduceMotion`; name the exact `BalanceConfig` constant to read, the `GameBootstrap` wire point, and the existing mock to extend; inject a `Func<>`/`Action` for anything time/SDK-driven so it's testable; and for anything visual the final check is a manual portrait eyeball.

### Shared Context Block (paste into every task prompt)
```
Repo: Unity 6000.4.6f1 mobile word-ladder game ("Star Ladder"). Portrait 1080x1920.
Single live scene: Assets/Scenes/GameUI.unity. Architecture: immutable GameState + Dispatch
(GameStateManager; handlers HandlePressLetter/HandleDeleteLetter/HandleSubmitWord/HandleUseHint/
HandleUseReveal/HandleUseAddTime/HandleUndo; events OnWordSubmissionResult, OnTimeAdded).
Tunable numbers live in Assets/Scripts/Puzzle/BalanceConfig.cs (single source of truth;
incl. MinMovesForLength curve 3->2 4->2 5->3 6->3 7->4, AbsoluteMinMoves=2, MaxTier=7,
PuzzlesPerTier=100, progressive PuzzlesRequiredToAdvance(tier) 10..40).
DISPLAY-NAME RENAMES (2026-06, display strings only — internal types/fields/files UNCHANGED):
the player sees Classic, Daily, "Puzzle Library" (=PuzzleShowMode; its tiers are "Shelves"),
and "Timed" (=TimeAttackMode; results read "Timed" / "Timed Survival"). Currency = "stars"
(gold five-point-star token via UIThemeManager.CreateStarToken; internal field still totalCoins,
methods SpendCoinsAsync etc). Shop = "Star Shop". Keep using the old code identifiers.
Word data (Assets/Resources/Data/, all MACHINE-GENERATED by Tools/*.py — re-run, never hand-edit;
content from PUBLIC-DOMAIN ENABLE, Norvig freq = build-time ranking only => commercial-safe):
  word_library.json (17,326) + common_words.json (6,875) <- dictionary_build.py
    (library = ENABLE len 3-7 with Norvig rank<60000 + permanent orig-daily words, MINUS offensive
     blocklist; common = library subset rank<15000);
  tier_definitions.json (7 tiers x 100 = 700, each >=2 optimal routes) <- puzzleshow_build.py;
  daily_puzzles.json (600 = 450 hand-curated/floor-fixed + 150 appended id>=20001) <- daily_floor_fix.py
    then daily_expand.py (additive, idempotent; daily index = day % poolCount, save-safe).
  Every puzzle's TRUE full-dictionary shortest path is >= MinMovesForLength (no 1-move puzzles).
  Pipeline is BYTE-REPRODUCIBLE (sorted-adjacency BFS); verify with Tools/verify_data.py (+ --canary).
Post-win surface routing: pure PostWinRouter.Decide(...) called by GameBootstrap.CheckGameOver.
Persistence: PlayerPrefs JSON via DataManager (keys: puzzle_progress_v1, wordpuzzle_progress,
wordpuzzle_save, daily_v1, settings_v1, onboarding_v1). New persisted PlayerProgress fields serialize
through the PlayerProgressData DTO + DataManager converters; JsonUtility auto-defaults missing fields.
Daily 2.0 (Task 36): par-scored stakes. GameStateManager.ConfigureDailyRun(mistakeBudget, par) AFTER
StartNewPuzzle (par = puzzleDefinition.optimalSteps). Mistake rule (2026-06 rework, DAILY only): a
RULE-BREAK (not Hamming-1 from the chain tail) spends a mistake (DailyMistakeBudget=3; 0 => FAIL); a
LEGAL-SHAPED dictionary miss (Hamming-1 but not a word) BOUNCES FREE (no mistake/detour) — precedence
needs an explicit WordOps.HaveOneLetterDifference re-check since the validator checks the dictionary
FIRST. An accepted !validation.isProgress move is a DETOUR (sets grade, never fails). Every daily is
>=4 steps with honest par (Tools/daily_floor_fix.py FLOOR=4). Pure
PathScoring.Score(...) -> PathGrade (Perfect/Good/Solved/Failed; (int)grade==stars); usedPowerUp CAPS the
grade at BalanceConfig.PowerUpMaxGrade=Good (Task 40 — Perfect = unassisted only; results show a plain-text
"assisted" note, the share card a ⚡ disclosure; persisted as DailyProgress.todayResultUsedPowerUp so the
one-and-done re-show keeps it). Streak authority =
DailyStreakRules.ApplyPlayed (a PLAYED day, solve OR fail, advances the streak; only a missed calendar day
resets) — NOT ApplyCompletion (never call both). Trailing-365 W/L = outcomes ledger (Wins/Losses/WinRatePct,
RecordWindowDays=365). Repair = CanRepair/ApplyRepair (yesterday-only, once/StreakRepairCooldownDays=7; coins
StreakRepairCoinCost=150 or rewarded ad; bridge only, does NOT auto-play today). Share =
ShareCardBuilder.BuildDailyShapeCard (spoiler-free: header + glyph rows + streak, NO words).
Economy (Tasks 33+36): EconomyManager : IEconomyManager owns persisted coins + the OWNED power-up inventory
(PlayerProgress.total{Hints,Reveals,Undos,Time}Earned) + removeAds + starting/daily-grant flags + Task-36
faucet state (starterPackOwned, adFreeUntilUnix, login cycle pos, watch counter, milestone marker). Hint/Reveal
SEED each puzzle from owned (GameStateManager.SetOwnedPowerUpProvider, wired in GameBootstrap; null in unit
tests => BalanceConfig defaults stand). New players get 5 each; +2/day (GrantDailyIfDue). Faucets/sinks
(numbers in BalanceConfig, clock-free, idempotent/local-day): DailyCoinReward(stars,failed) 60/40/25/10;
ClaimLoginRewardAsync cycle {25,25,50,50,75,75,150}; GrantWatchCoinsAsync 35 cap 3/day;
AwardStreakMilestonesAsync 100 @ 7/30/100; reward doubler (ad => 2x); repair sink (150 stars or ad). Star
Shop = ShopScreen runtime overlay (star pill -> OnShopRequested): pinned Starter Pack + Restore, tiered
power-up bundles (Hint/Undo 50/135/320, Reveal 120/320/800, Time 60/160/400 for x5/x15/x40 — INJECTED since
UI can't ref Puzzle; price shown as a bare number, no glyph), a Free-Stars watch row, named star packs
(coin_shop.json). All displayed currency text says "stars". DailyRewardPopup = a SEPARATE menu
overlay (login claim + repair). Real-money buys + RestorePurchasesAsync go through IStoreService
(MockStoreService in editor; PlatformStoreServiceStub = real billing, NOT implemented; StoreProductType
{Coins, RemoveAds, StarterPack}). v1.0 HIDES all real-money shop sections behind
ShopScreen.RealMoneyEnabled=false (flip + real IStoreService = 1.1). removeAds || ad-free-window wires
AdPolicyService.AdsRemoved. v1.0 audit wired the ad stack for release: BootstrapInitializer ensures the
AdService component (it was attached to NOTHING before — ads had never run); production unit IDs load from
Resources/Config/ad_units.json; the GMA App ID lives in GoogleMobileAdsSettings.asset; the editor stays
ad-INERT (enableAdsInEditor=false) so PlayMode tests stay deterministic; UmpConsentService (real UMP) runs
on device with a conditional PRIVACY band in Settings. Login reward + coin-repair work everywhere; the
rewarded faucets need the device pass to confirm live ads.
Ads hardening (Task 39): main-thread SDK events (39A); load-retry exponential backoff via pure AdRetryPolicy
(Puzzle asm; AdRetryBase/MaxDelaySeconds=1/64) (39B); reward granted ONLY on the SDK reward callback, applied
after close, stored-delegate unsubscribes (39C); money-bearing saves flush PlayerPrefs.Save() + pause/quit
hooks (39D); Classic win-panel "Next Puzzle" -> TryShowInterstitial under AdPolicyService caps (39E).
Analytics + consent (Task 41): IAnalytics (Puzzle asm) <- AnalyticsReporter (Game; the ONE taxonomy assembly
point — daily_result fires EXACTLY once per run, the re-show path emits NOTHING; puzzle_complete one emission
point per surface). LogAnalytics = live default; MockAnalytics in TestMocks. Ad init is consent-gated:
IConsentService + pure ConsentGate (no MobileAds.Initialize until Gather completes AND CanRequestAds);
NullConsentService = editor default. Daily reminder rules = pure NotificationRules (toggle gates WHETHER,
todayPlayed shifts WHEN — played => tomorrow-at-hour; cancel-then-reschedule only;
ReminderHourLocal=19; SettingsData.notificationsEnabled default true). Platform scheduler NOT wired yet.
Tests live in Assets/Tests/Unit/ (NOT Assets/Scripts/Tests).
Assemblies (dep direction): Puzzle (lowest; BalanceConfig, WordGraph, WordValidator, IAdService) <-
Persistence/State <- Modes <- Game/UI. Puzzle must NOT reference State/UI.
Design tokens ("Direction B" purple palette; one source of truth = UITheme.Palette, everything forwards to
it — see §15): foundation SurfaceVoid #0D0A1F (app background base) / Surface #1C1640 / Panel #2E2560 /
Amethyst #473A7E. Buttons + start/target tiles are colored OUTLINES; modes are one cool family, hue-spaced
(Daily orchid #BE84E2 hero, Classic blue-violet #6E84D6, Puzzle Library deep-violet #8160D2, Timed
magenta #B25EB8), Resume/HOME/Library/Stats use periwinkle #8E78C8. Aqua-spark #54A8B4 is the one cool
highlight (title, success/correct tile, GO key). Coins #E9C98C (warm gold — the STAR currency token name
is unchanged; hints, active input, win/shelf accents, streak) and Alert #E08A8A (warm red — errors/destructive) are the only warm notes. text-primary
#EFEAF8, text-muted #ABA0CE (Task 42; was #9A8FBE).
Typography (Task 42): Rungo (Poppins-derived, OFL) x4 weights as runtime dynamic TMP SDF assets + a
symbols fallback (star/check/triangle glyphs render tofu-free). ONE seam: WordPuzzle.UI.TypeScale roles
(Display 96/Headline 64/Title 44/Label 38/TileLetter 56 responsive/Body 32/Caption 26) via
TypeScale.Apply(text, role) (= UITheme.ApplyType). NEVER set a raw fontSize or font asset in a screen.

Hard constraints (ALL prompts):
- Preserve the immutable GameState + Dispatch architecture and the public interfaces
  IWordValidator, IDataManager, IGameMode, IEconomyManager (extended additively in Task 33),
  IStoreService unless a task says otherwise.
- All tests stay green — run via run_tests(mode="PlayMode") (PlayMode-registered suite, 384 tests;
  EditMode returns total:0). If two Unity instances are connected, set_active_instance to
  WordPuzzleGame FIRST or runs hit the wrong project. Delete the .meta when you delete a Unity
  asset, and GUID-check scenes/prefabs before deleting any MonoBehaviour script.
- Never commit Library/Temp/obj. Minimal, surgical diffs.
- PLAN FIRST: confirm exact method seams against the real files before editing; state assumptions
  where ambiguous. Tunables go in BalanceConfig, never as new magic-number literals.
```

---

## 15. Design tokens

**"Direction B" purple palette** — the whole app reads as one painterly purple world. The single source of
truth is the static `UITheme.Palette` class (the ONLY place raw theme hex lives); the older `MenuPalette` /
`GameAccents` / `KeyboardPalette` / `AccessiblePalette` classes and every screen now **forward to it** by role
(no scattered hex). Modes differ by **hue-spacing + brightness, not temperature**; **Coins** + **Alert** are
the only warm notes; **Aqua-spark** is the single cool highlight.

**Foundation (surfaces)**

| Token | Hex | Use |
|---|---|---|
| `SurfaceVoid` | `#0D0A1F` | App background base (purple-black) + near-black ghost-card centres |
| `Surface` | `#1C1640` | Input / keyboard surfaces, dark fills |
| `Panel` | `#2E2560` | Cards, keyboard keys, tile fills |
| `Amethyst` | `#473A7E` | Card / tile borders, subtle rings |

**Accents** (one cool family + the single aqua highlight)

| Token | Hex | Use |
|---|---|---|
| `AccentLavender` | `#9F7ED6` | Accent (e.g. Timed **SURVIVAL**) |
| `AccentPeriwinkle` | `#8E78C8` | Secondary chrome rings (Library / Stats / HOME / star pill), played-chain outline |
| `AccentOrchid` | `#B072BC` | Accent |
| `AccentAqua` | `#54A8B4` | The one cool highlight — title, success/correct tile, `GO` key, completed shelves |

**Mode buttons** — one cool family, hue-spaced; **Daily is the hero by brightness + a heavier ring** (not by size or a warm color).

| Token | Hex | Use |
|---|---|---|
| `ModeDaily` | `#BE84E2` | Daily (orchid hero) |
| `ModeClassic` | `#6E84D6` | Classic (blue-violet) |
| `ModePuzzleShow` | `#8160D2` | Puzzle Library (deep violet) — *token name unchanged* |
| `ModeTimeAttack` | `#B25EB8` | Timed (magenta-violet) + **TIMED** setup card — *token name unchanged* |

**Text & semantic** (Coins / Alert are the only warm notes — kept)

| Token | Hex | Use |
|---|---|---|
| `TextPrimary` | `#EFEAF8` | Body, button / tile labels, start-row outline *(Task 44 — aqua reserved for success)* |
| `TextMuted` | `#ABA0CE` | Captions, subtitles *(Task 42 — raised from `#9A8FBE` to clear WCAG AA at Caption sizes on `Panel`)* |
| `Coins` | `#E9C98C` | **Warm gold** — the **star currency** mark (token name kept), hints, active-input tiles, win "Next Puzzle", in-progress & current-shelf rings, streak headline (`GameAccents.Gold` forwards here; `UIThemeManager.CreateStarToken` draws the gold star in this colour) |
| `Alert` | `#E08A8A` | **Warm red** — errors, destructive actions, invalid flash (`GameAccents.Danger` forwards here) |

**Enforcement (Task 46)** — the design system is **mechanical**, not curated: `Palette.All` + `Palette.IsToken(c)` (any token at reduced alpha matches; colorblind hues count) and `TypeScale.All` feed **`DesignSystemTests`**, which sweeps every runtime-built screen — every active text a TypeScale font, every active `Graphic` token-resolving, every screen root safe-area'd, exactly **one** filled hero on the menu (ghosts glow-free), every button ≥ **88×88** — plus a **source lint** over `Assets/Scripts/UI` (no raw hex / `Color32` / `fontSize` outside `UITheme.cs` + `UITypeScale.cs`; `fontSize` only from `TypeScale.`). Acceptance for any future UI task is simply **suite green + eyeball the one changed screen**.

**Typography (Task 42)** — the app typeface is **Rungo** (the Poppins-derived four-weight OFL 1.1 family; TTFs + license under `Assets/Fonts/Rungo/`, runtime copies under `Assets/Resources/Fonts/Rungo/`), built at runtime as **dynamic TMP SDF assets** with an OFL symbols fallback (`Resources/Fonts/Symbols.ttf` — ★ ☆ ✓ ▸ · render tofu-free through every weight). One source of truth: `WordPuzzle.UI.TypeScale` (roles `Display 96·Bold` / `Headline 64·Bold` / `Title 44·SemiBold` / `Label 38·SemiBold` / `TileLetter 56·SemiBold`, responsive via `ApplyTileLetter` / `Body 32·Medium` / `Caption 26·Regular`) applied via `TypeScale.Apply(text, role)` (forwarder: `UITheme.ApplyType`). **No screen sets a raw `fontSize` or font asset** — pinned by `TypeScaleTests`.

---

## 16. Building & running

**Requirements:** Unity 6000.4.6f1, TextMeshPro (bundled), Google Mobile Ads (integrated). Portrait 1080×1920; CanvasScaler matches height.

1. Clone, open the root folder via Unity Hub → *Add project from disk*.
2. Open `Assets/Scenes/GameUI.unity` and press **Play**.
3. Tests: Window → General → Test Runner → **PlayMode** → Run All (the suite is PlayMode-registered, **384 tests**).

---

## 17. Notes for AI agents working in this repo

Environment quirks learned the hard way — relevant when an agent verifies its own work:
- **The unityMCP `run_tests` runner works in PlayMode** (verified: full suite **384/384**, real failures surface). Gotchas: **(1)** the suite is **PlayMode-registered**, so `mode="EditMode"` with a filter returns `summary.total:0` — a *false* `"Passed"`; always use `mode="PlayMode"`. **(2)** PlayMode sometimes returns *"failed to initialize (tests did not start within timeout)"* — **retry**; a timed-out init can leave the editor IN Play Mode, after which the next run errors *"Cannot start … in Play Mode"* → `manage_editor(action="stop")`, then re-run. **(3)** `get_test_job.progress.completed` can exceed `total` (double-counted) — trust `result.summary.{total,passed,failed}`. **(4)** new `.cs` files written outside Unity need `refresh_unity(scope="all", mode="force")` to import before they compile (a `scope="scripts"` refresh can miss a brand-new file → `CS0234`). **(5)** running PlayMode tests leaves a temp `InitTestScene<guid>` loaded (Game view then shows *"No camera rendering"*) → reload `Assets/Scenes/GameUI.unity` afterward.
- **`execute_code` (in-editor C#) is broken here** (mono "filename or extension is too long"; Roslyn not installed). You cannot script Play-mode drives or screenshots — visual/feel acceptance is a human-in-Editor check. **Workaround for one-shot editor operations** (e.g. creating the GMA settings asset): write a temporary script with a `[MenuItem]`, `refresh_unity`, run it via `execute_menu_item`, verify via `read_console`, then delete the script + its `.meta`.
- **Two Unity instances can be connected to MCP at once** (this machine often has a second project open). Test runs and edits **silently route to the wrong editor** — symptoms: "failed to initialize" twice, then a foreign suite (e.g. 57 tests, `Riptide.*`). Before any compile/test session, read `mcpforunity://instances` and `set_active_instance` to `WordPuzzleGame@<hash>` first.
- **Never bulk-rewrite source files with PowerShell 5.1** (`Get-Content`/`Set-Content` round-trips read UTF-8 as Windows-1252 and mojibake every non-ASCII character — em-dashes throughout this repo's comments). Use the Edit tool; if a file gets corrupted, `git checkout --` it and redo the edits.
- **`manage_camera` screenshots can't see the portrait game.** The capture returns a blank ~2:1 landscape Game-view rendered via the Main Camera (which **excludes** the Screen Space - Overlay UI canvas); the real frame is the portrait Device Simulator on display 0, which MCP can't read (`scene_view` capture needs an open Scene View). Verify UI **numerically** instead — `manage_scene get_hierarchy` with `include_transform` for positions, `ReadMcpResourceTool` on `mcpforunity://scene/gameobject/{id}/component/{name}` for rects/colors/refs — and hand the portrait eyeball to a human. Also: Play mode boots straight to **MainMenu** (you can't script into a specific mode), `manage_gameobject`/`set_property` edits are **blocked during Play**, and **instance IDs churn on every domain reload** — re-query, never cache them.
- **Confirm scene context before/after agent work.** Loading a scene in the editor replaces the open one; agents have left the editor on a non-`GameUI` scene and/or in Play mode. Re-open `GameUI.unity` and stop Play mode to restore the expected view.
- **`git status` before planning/committing.** Background agents occasionally drop shell-misfire junk files at repo root (e.g. `nul`, `{`, `0`) and can even pick up a *later* task autonomously — clean junk and check the tree before each commit.
- **Icons — SVG via Vector Graphics, or PNG.** `com.unity.vectorgraphics` provides the SVG importer; set the importer's **SVG Type = Textured Sprite** (the default "UI Toolkit" type yields *no* uGUI sprite — empty `SpriteRect`), and give artwork a **concrete stroke/fill colour, not `currentColor`** (which rasterizes to black and can't be tinted via `Image.color`). UI chrome (HOME, the global Settings gear) is built in code as tinted `Image` children. A `[SerializeField] Sprite` ref can point anywhere under `Assets/`; sprites loaded at runtime (`Resources.Load`) must sit under a `Resources/` folder.
- **Design-system enforcement lives in `DesignSystemTests`** (Task 46): fonts, token colours, safe-area, button hierarchy, hit-target geometry, and a source lint over `Assets/Scripts/UI`. **If your UI change fails it, fix the change, not the test** — a legitimate exception requires a reviewed entry in the test's registries, which ship **empty**. Scan rules to know: tokens match at any alpha; a `UIVerticalGradient` surface is judged by its gradient stops; white is allowed only on backdrop art / the logotype; a fill-driven button axis (flexible/stretch in a layout) is exempt from the 88px check only when it reads 0 headlessly.
- Untracked tooling dirs (`.claude/`, `.swarm/`, `.claude-flow/`, `agentdb.*`, `_Recovery/`) are not part of the game — never commit them. Shell-misfire junk (`nul`, `{`, `0`, `560)`, `statsScreen`, …) sometimes lands at repo root — delete before committing.

---

## Project history

Built iteratively through AI-orchestrated swarms, one concern each: word library & ladder semantics → modern tile/keyboard polish → library cards & tier gate → HOME/settings → hint/reveal semantics → per-mode behaviors & AddTime → TimeAttack UI → share result → daily + streak → **balance config & common-words generation** → **economy & rewarded ads** → **tactile juice (motion/haptics/sound)** → **premium visual identity (gold focus, ascent, motion vocabulary)** → **UI polish pass** (main-menu hierarchy with a gold DAILY hero, gameplay spacing, a keyboard-anchored power-up bar, a reliable visible HOME, and a properly clipping/scrolling word-chain) → **icon chrome** (SVG-via-Vector-Graphics + PNG icons: a house HOME and one shared, icon-only top-right Settings gear on every screen) → **Time Attack setup polish** (fit/styling/header, HOME aligned to the shared gear) → **dictionary expansion & cleanup** (reproducible ENABLE+Norvig tool: junk removed, 8,626→12,183 words, dense common 6/7-letter coverage) → **Puzzle Show 7×50** (350 curated ladders, two-level tier-select→grid navigation, completion coloring, progressive unlock) → **post-win flow** (compact win panel for endless Classic, auto-advancing Time Attack with results on timeout, Puzzle Show stat screen, Daily Home-only; "Play Again" re-routes into the mode) → **minimum-move floor** (no 1-move puzzles anywhere; min scales with word length, enforced by true full-dictionary shortest path in the generator and across all curated data) → **Classic-mode polish** (bolder tile outlines, rounded keyboard keys, subtle ladder-feel drop-in/climb animations) → **see-through cyan chain rows** with even rung spacing → **Reveal flicker fix** (idempotent per-frame render guards; Reveal/Hint decoupled) → **transparent keyboard panel** (space background edge-to-edge) → **shop & coins economy** (real-money coin bundles + coins-for-power-ups, a persisted owned inventory that seeds gameplay, 5-each starting + +2/day grants, a remove-ads IAP, a mockable store with real billing stubbed, and a `ShopScreen` reached from a live coin pill) → **Daily 2.0** (a par-scored daily with stakes: a 3-mistake budget + detour-based grade via pure `PathScoring`, a *played*-streak that survives a loss, a trailing-365 W/L record + win%, coins-or-ad streak repair, and a spoiler-free path-shape share card) → **Phase 5 economy** (tiered power-up bundles + reloaded/named coin packs with badges, a one-time **Starter Pack** + **Restore Purchases** + a 3-day ad-free window, par-scaled daily coin rewards, and the faucet/sink surfaces — a 7-day **login-reward** popup, **watch-for-coins**, **streak-milestone** pops, a **reward doubler**, and the Stats W/L line — all wired to `IAdService`/`IStoreService` ahead of a real ad SDK & billing) → **Stats rebuild + debug/balance pass (Task 38)** (the Stats screen rebuilt into clean grouped runtime cards; a debug pass that fixed three daily/results UI-state leaks — the daily HUD bleeding into Classic, the reward-doubler/streak lines leaking onto non-daily results, and a one-and-done that was cosmetic-only, now re-showing the stored result with no replay/farm; a missing-script scene cleanup with a reusable editor tool; and a balance audit that confirmed the economy is sane) → **Classic typing/feel polish** (the keyboard re-themed to the deep-indigo `UITheme.KeyboardPalette`, Reveal-rarity hardening, and clearly-felt press motion on keys, power-up buttons, and tiles; the active input row now **reuses persistent tiles** so **every** typed letter pops — not just the last — and **typing is capped at the puzzle's word length**, so a 5-letter puzzle accepts at most 5 letters) → **Settings rebuild** (the scene-authored, off-left-clipped page rebuilt at runtime like Stats — a scroll view of grouped **Audio / Accessibility / Data** cards, modern rounded **cyan** sliders + sliding **pill toggles** from `UITheme` tokens (no gold), HOME re-anchored; the gameplay-tile-only **High-Contrast / Large-Text** toggles removed, SFX/Music flagged silent until an audio bus) → **"Direction B" purple palette** (the whole app retokenized onto one `UITheme.Palette` source of truth — a soft purple-dominant world; modes recolored to one cool family with **Daily the hero by glow, not color**; an all-screen audit that caught the `new Color32(0x..)` stragglers on Time Attack / Results / Library; a soft **glow on every menu button** + the primary buttons **unified onto one shared geometry**; and a **hero-led Stats redesign** that kills the dead space and restyles the coin pill + HOME) → **Library + Shop polish** (the **Puzzle Library** tier-select dead dark panel removed — the 7 tiers now breathe as tall rounded **glowing rows** on the shared backdrop, the active tier hero-glowed and locked tiers dim; and the **Shop** overlay switched from a stale still image to the same animated app backdrop every other screen shows) → **Results screen overhaul** (the shared `ResultsScreen` rebuilt **code-driven** on the palette: an unwired **duplicate stat list** — empty `WORDS FOUND / TIME / ACCURACY / BEST WORD` dashes overlapping the real, bound values — removed, the oversized **title** that clipped into the clouds shrunk to fit, the stat block given even rhythm, and the gold **Score** routed to a token; plus **two Daily bugs** squashed — Daily was showing **Puzzle Show's** results because the one-and-done **re-show path never reset the title**, and the streak status line **stacked another copy on every Daily open** because it appended to the title instead of clearing — Daily now renders its **own** "Daily Results" grade-hero/streak screen, and the streak line shows **exactly once per view** from a dedicated SET-not-append label; the grade **★ stars — which the bundled font can't render (every glyph + runtime-font-fallback attempt tofu'd to □) — are drawn as real star meshes** (`StarGraphic`), and the cluster is framed in a subtle **ghost result card** with a gold grade word) → **ad-stack hardening + money durability (Task 39)** (main-thread SDK events, a pure exponential load-retry policy, reward granted only on the SDK callback and applied after close, money-bearing saves flushed durably to `PlayerPrefs` + pause/quit hooks, and Classic's "Next Puzzle" wired through the capped interstitial) → **Daily grade integrity (Task 40)** (a power-up-assisted daily caps at **Good** — Perfect is unassisted-only — with an honest "assisted" note on results and a ⚡ disclosure on the share card, persisted through the one-and-done re-show) → **analytics, consent & reminder rules (Task 41)** (a flat no-PII analytics taxonomy assembled in one place — `AnalyticsReporter` over the `IAnalytics` seam, `daily_result` firing exactly once per run; ad init consent-gated behind `IConsentService`/`ConsentGate`; and pure `NotificationRules` for the daily streak reminder — toggle gates whether, played-today shifts when, cancel-then-reschedule only) → **Rungo typography + TypeScale (Task 42)** (one type seam — `TypeScale.Apply(text, role)`, the Poppins-derived Rungo family as runtime SDF assets, no raw `fontSize` anywhere) → **button tiers + the Star Ladder identity (Task 43)** (ONE filled gradient hero per surface, outlined secondaries, ghost utilities; the logotype masthead + stroke mode icons) → **gameplay legibility + safe-area (Task 44)** (white start row, gradient gameplay scrim, `SafeAreaPanel` systematized, backdrop video gated on ReduceMotion/low-power) → **celebration juice (Task 45)** (buffered payout choreography on results, tier/streak celebration modals, `StaggeredPop`/count-up vocabulary) → **design-system enforcement (Task 46)** (`DesignSystemTests` — mechanical token/typography/safe-area/hit-target/hierarchy scans + a source lint; acceptance = suite green + eyeball the changed screen) → **Classic board overhaul (Task 47)** (centered play zone, solid input slabs, aqua caret, keyboard spacing) → **library tier-grid overhaul (Task 48)** (animated tier progress bar, "up next" hero card, decluttered cells) → **Time Attack setup overhaul (Task 49)** (column headers over the grid, the 60s TIMED hero, card entrance choreography) → **the modern-surface sweep** (tutorial welcome modal, Stats, Shop, and the Puzzle Show tier-select all rebuilt on the shared **`ApplySolidCard`** seam; the `ApplyNeonGlow` phantom-cell layout bug and the library's `childControlHeight=false` row-squash fixed at the root) → **the v1.0 release audit** (`AUDIT_REPORT.md`: full coherence/debug/polish/lifecycle/release pass — **B7** corrupt-save recovery fixed + regression-tested, the **ad stack attached and release-configured** (it had never actually run), **UMP consent implemented**, **real-money commerce gated off for v1.0**, iOS bundle ID set; suite at **378/378**; remaining: visual gates, merge, and the device-verification pass) → **post-audit polish + rename pass (2026-06-13)** (the daily failure rule softened so **typos bounce free** — only rule-breaks spend a mistake — and the daily pool re-floored to **≥4 steps with honest par**; the modal/screen sweep finished onto `ApplySolidCard`, incl. the daily-rewards popup and a new runtime **Reset Progress** confirm modal that replaced a transparent scene-authored one, plus a slider/toggle 9-slice radius fix; the logotype's glitchy "ladder-rung A" overlay retired; and a **display-only terminology rename** — **Classic Mode → Classic**, **Time Attack → Timed** (results read Timed / Timed Survival), **Puzzle Show → Puzzle Library** with **tiers → Shelves**, **Shop → Star Shop**, and **coins → stars** with a gold five-point-star token — internal types/fields/files untouched; suite at **384/384**). The git log captures the progression.
