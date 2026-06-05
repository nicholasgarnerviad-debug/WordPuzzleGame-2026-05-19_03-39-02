# Word Ladder

A modern, mobile-portrait word-ladder puzzle game built in **Unity 6000.4.6f1** (Unity 6 LTS), portrait **1080×1920**. Transform a start word into an end word one letter at a time — every intermediate step must be a real English word that differs from the previous word by exactly one letter.

```
FROM  C  A  T            FROM  S  T  O  N  E
       |                        S  T  O  R  E   ← changed 1
      B  A  T  ← changed 1      S  T  A  R  E   ← changed 1
      B  A  G  ← changed 1      S  H  A  R  E   ← changed 1
 TO   B  A  G            TO     S  H  A  R  P   ← changed 1
```

> **This README is also the canonical context document for AI-assisted development.** It is written so an LLM (e.g. Claude Opus) can read it and author precise, surgical task prompts ("meta prompts") for this repo. See **[§14 Writing a master prompt](#14-writing-a-master-prompt-for-this-repo)** and the **[Shared Context Block](#shared-context-block-paste-into-every-task-prompt)** at the end.

<p align="center">
  <img src="docs/screenshots/menu-hero.png" width="300" alt="Word Ladder main menu with the Daily Rewards overlay — black pixel-space backdrop, cyan WORD LADDER header, a gold coin pill, and a 'Day 1 reward · +25 coins' claim popup">
  <br><em>Main menu with the new <strong>Daily Rewards</strong> overlay — a true-black pixel-space backdrop, a softly-floating cyan <strong>WORD LADDER</strong> title, a gold <strong>coin pill</strong> (top-left), colored outline (“ghost”) buttons, and the daily login-reward claim (Tasks 22–28, 36).</em>
</p>

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

> Live captures (iPhone 13 Pro Max portrait). The UI follows the **true-black, outline ("ghost")** identity in [§5](#5-visual-identity) / [§15](#15-design-tokens) — colored rounded outlines over a black pixel-space backdrop (a swappable full-screen layer), each action in its own color, with subtle **ReduceMotion-gated** menu motion (floating cyan title, button cascade, press feedback).
>
> ⚠️ **Note:** the **Main Menu, Classic, Daily, and Shop** thumbnails are current; **Puzzle Show / Time Attack / Puzzle Library** still show the earlier dark, gold-accented design. **Stats** and **Settings** have since been rebuilt, but their thumbnails here are pre-rebuild — all will be recaptured in a later pass.

| Screen | |
|---|---|
| **Main Menu** — white `WORD LADDER` masthead on black. Buttons are **colored rounded outlines** (transparent centers): **DAILY** is the hero — a thicker, brighter **orange** ring (primary call-to-action; shows the streak once today is solved). The modes each carry their own color — **Classic** green, **Puzzle Show** violet, **Time Attack** red, **Resume** (only with an in-progress save) teal. **Puzzle Library / Stats** are a muted side-by-side row (Settings lives in the shared top-right gear). A gold **coin pill** (top-left) shows the balance and opens the Shop; the **Daily Rewards** overlay (shown here) surfaces the daily login claim + streak repair ([§3](#3-economy--monetization)). | <img src="docs/screenshots/main-menu.png" width="250"> |
| **Classic Mode** — the core word ladder on black. The **start** word is a row of **teal see-through outline tiles** (the origin), the **target** is **orange** outline tiles (the goal), and the played **chain** rows are **cyan** see-through outlines; only the **active input row stays solid** (the "fill here" zone, gold-edged as you type). Tiles carry **bold ~7px rings** and **ladder-feel motion** (letters drop in, accepted rows climb). Below: the Hint / Undo / Reveal **power-up bar** over a **rounded** QWERTY keyboard (red `DEL`, green `GO`) **floating on a transparent panel** — the space backdrop runs edge-to-edge. An icon **HOME** (top-left) and the shared **Settings** gear (top-right) flank a calm score header. Random 3–7-letter puzzles; on a solve a **compact win panel** ("Next Puzzle" / "Home") keeps you in the loop ([§1](#1-game-modes)). | <img src="docs/screenshots/classic-mode.png" width="250"> |
| **Daily 2.0** (Task 36) — one shared puzzle a day, now **par-scored with stakes**. Same start (teal) → input → target (orange) board as Classic, plus a **"Par N · Mistakes left M"** HUD. You get a **3-mistake budget** (an invalid guess spends one; running out **fails** the run) while **detours** (valid but non-progress moves) cost your **grade**, not the run. Finishing scores **Perfect / Good / Solved / Failed** (★★★–☆) + par-scaled coins, advances your **played-streak** (a *failed* day still counts — only a missed calendar day breaks it), and logs a trailing-365 **W/L record + win%**. Results add a spoiler-free **path-shape share card** and a **watch-to-double** reward ([§1](#1-game-modes)). | <img src="docs/screenshots/daily.png" width="250"> |
| **Puzzle Show** — tier-progression play on the same gameplay screen, with a `Tier X / Y` indicator under the score. **350 curated ladders (7 tiers × 50)** on a length/difficulty curve (Tier 1 easy 3-letter → Tier 7 hard 7-letter, up to 8-step ladders). Solving shows a stat screen offering **Next Puzzle / Tier N ▸ / Home** ([§7](#7-puzzle-show-tiers)). | <img src="docs/screenshots/puzzle-show.png" width="250"> |
| **Time Attack** — a countdown timer + the **+Time** power-up; chosen as 60s/120s × Timed/Survival on a setup screen. Ladders **auto-advance** as you solve them; the full results screen (puzzles solved + **Play Again** → new run) appears only when the **timer hits 0**. | <img src="docs/screenshots/time-attack.png" width="250"> |
| **Puzzle Library → Tier Select** (level 1) — the entry to Puzzle Show: a list of **7 tiers**, each with its theme (e.g. "3-letter words"), progress (`X/50`) and lock state (**gold** = current tier; **padlock + "Clear N in Tier M"** = locked). Tap an unlocked tier to open its grid. | <img src="docs/screenshots/puzzle-library.png" width="250"> |
| **Puzzle Library → Tier Grid** (level 2) — the selected tier's **50** puzzle cards with a **Back** to tier-select (only the active tier renders, for performance). Cards reflect saved progress: **Completed** (green + ✓), **Unplayed** (surface grey), **Locked** (padlock). Tapping a card launches that exact puzzle. | <img src="docs/screenshots/puzzle-library-tier.png" width="250"> |
| **Stats** (rebuilt Task 38) — clean **grouped cards** built at runtime (no scene authoring): a **DAILY** card (hero current-streak + a 3-up **Longest / Win % / W–L** row), **CLASSIC** + **TIME ATTACK** cards side-by-side, a gold **coin pill** top-right, and an Overall footer. Replaces the old flat, colliding label grid (thumbnail is pre-rebuild). | <img src="docs/screenshots/stats.png" width="250"> |
| **Settings** (rebuilt) — clean **grouped sections built at runtime** (no scene authoring) in a scroll view: **Audio** (Master / Music / SFX sliders + Mute) · **Accessibility** (Reduce Motion · Haptics · Colorblind Mode) · **Data** (**Reset Progress** confirm-gated, preserves settings + tutorial flag; **Replay Tutorial**). Modernized to the outline/rounded identity — rounded slate-groove + **cyan**-fill sliders and **cyan/slate pill toggles** with a sliding, colorblind-safe knob (ReduceMotion-gated glide); **no gold**. SFX/Music are flagged on-screen as silent until an audio bus exists; **High-Contrast/Large-Text were removed** (they only affected gameplay tiles). Build version at the foot (thumbnail is pre-rebuild). | <img src="docs/screenshots/settings.png" width="250"> |
| **Shop** (Tasks 33, 36) — opened by the **gold coin pill**. A pinned **Starter Pack** ($1.99 one-time → 1000 coins + 5 of each power-up + 3 ad-free days) and a **Restore Purchases** button sit up top, then **Power-Ups** (Hint / Undo / Reveal / Time in **×5 / ×15 / ×40** tiered coin prices), a **Free Coins · watch an ad** row (capped 3/day), real-money **coin packs** with names + merchandising badges (**Pouch / Stack / Chest "MOST POPULAR" / Vault / Hoard "BEST VALUE"**), and **Remove Ads**. Rebuilds from live state after each buy; unaffordable bundles disable; real billing is stubbed ([§3](#3-economy--monetization)). | <img src="docs/screenshots/shop.png" width="250"> |

> **Global chrome:** one shared **Settings** gear (icon-only, top-right, ~HOME-sized) shows on every screen *except* Settings itself and opens it — `UIManager.CreateGlobalSettingsButton` → `OnGlobalSettingsRequested` → `GameBootstrap.ShowSettings` (which populates then shows). On the gameplay screen a house **HOME** (top-left) and the gear (top-right) flank the header. Icon assets: `Assets/UI/Icons/*.svg` (Vector Graphics) + `Assets/Resources/Icons/*.png`.

---

## 1. Game modes

| Mode | Timer | Puzzles | Win condition |
|---|---|---|---|
| **Classic** | None | Random, BFS-generated; start/end restricted to a common-words subset | Reach the end word → **compact win panel** ("Next Puzzle" stays in Classic). First-ever launch routes into the tutorial. |
| **Daily 2.0** | None | One puzzle/day, identical for everyone (no server) | **Par-scored with stakes** — a 3-mistake budget, detours cost grade; finishing (solve OR fail) → full results: grade/★, par-scaled coins, played-streak, W/L record, share card, watch-to-double (**Home** only) |
| **Puzzle Show** | None | **350 curated ladders (50 × 7 tiers)**, two-level library | Reach end word → stat screen (Next Puzzle / Tier N ▸ / Home); tap any unlocked card to play it |
| **Time Attack** | 60s or 120s, Timed or Survival | Random words back-to-back | Solve as many as possible before time runs out → full results + **Play Again** (new run) |

**Daily 2.0 — par scoring, stakes, played-streak, W/L record, repair, share (Task 36).** Today's puzzle is derived from the **local date**, no network: `index = (Today − 2025-01-01).Days mod N` (`N` = pool size in `daily_puzzles.json`, all pre-validated Hamming-1 + dictionary). Daily runs reuse Classic mechanics but arm a **two-resource scored run** via `GameStateManager.ConfigureDailyRun(mistakeBudget, par)` (called after `StartNewPuzzle`; par = the puzzle's validated `optimalSteps`):
- **Mistakes (the stake):** an invalid guess spends one of `DailyMistakeBudget` (3); running out **fails** the run. A wrong-length/empty entry is malformed (not a mistake).
- **Detours (the score):** an accepted move that isn't *progress* (`!validation.isProgress` — not strictly closer to the target) is a **detour**; detours set the grade but never end the run. Undo decrements the detour count (floored; no mistake refund).
- **Grade** — pure `PathScoring.Score(par, steps, detours, mistakesUsed, ranOutOfMistakes, usedPowerUp)` → `PathGrade` where `(int)grade == stars`: **Perfect** (★★★, 0 detours) / **Good** (★★, ≤ `GoodMaxDetours` = 2) / **Solved** (★) / **Failed** (☆, out of mistakes). Surfaced on `ResultsScreen.ShowDailyResult` as "grade ★ · Par N · You got X".

**Played-streak** (`DailyStreakRules.ApplyPlayed`, pure/testable — the streak authority, replacing completion-only `ApplyCompletion`; never call both): a **played** day (solve OR fail) advances `currentStreak` iff yesterday was played; only a **missed calendar day** resets it; same-day replay never double-counts. A **trailing-365-day W/L record** (`outcomes` ledger of `DayOutcome{dateIso,won}`; `Wins`/`Losses`/`WinRatePct`, `RecordWindowDays = 365`) tracks skill alongside the habit streak. **Streak repair** (`CanRepair`/`ApplyRepair`): if *only yesterday* was missed, bridge the gap for `StreakRepairCoinCost` (150) coins **or** a rewarded ad, once per `StreakRepairCooldownDays` (7) — a bridge only (does **not** auto-play today). One-and-done (Task 38): once today is played, the menu **DAILY** button shows the streak and **re-tapping re-shows today's stored result** (grade/stars + streak) instead of starting a fresh scored run — no replay, no reward re-grant. Persisted under `daily_v1`.

**Path-shape share card** (`ShareCardBuilder.BuildDailyShapeCard`) — a **spoiler-free** daily result: a header (`Word Ladder Daily #n · Par p · X/p · ★★☆`), one glyph row per step (`🟩` progress / `🟨` detour / `⬛` mistake-step), and the streak line — **no words**. Copied via `ClipboardShareService`.

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

| Power-up | Effect | Owned inventory | Get more | Available in |
|---|---|---|---|---|
| **Hint** | Gold-highlights the position in the current word to change next | persisted; **start 5**, **+2/day** | coins (shop) / rewarded ad | All modes |
| **Reveal** | Shows the next solution word as a ghost preview row | persisted; **start 5**, **+2/day** | coins (shop) | All modes |
| **Undo** | Pops the last accepted chain word | tracked* | coins (shop) | All modes |
| **+Time** | Adds `AddTimeGrantSeconds` (10s) to the clock | persisted; **start 5**, **+2/day** | coins (shop) | Time Attack only* |

**Full real economy (Task 33):** power-ups are now a **persisted owned inventory** (`PlayerProgress.total{Hints,Reveals,Undos,Time}Earned` via `EconomyManager`). Hint/Reveal charges **seed each puzzle from that inventory** (`GameStateManager.SetOwnedPowerUpProvider`, wired by `GameBootstrap`; null in unit tests so they fall back to the `BalanceConfig` defaults), and using one in-game **spends from your saved stock** (`Use*Async`). Every player starts with **5 each** and gets **+2 each per local day**; the shop tops them up — see [§3](#3-economy--monetization). Reveal stays the premium power-up. *\*Undo's count + the +TIME→Time-Attack hookup are tracked in the economy but their gameplay wiring is still in progress (see [§13](#13-known-tech-debt--candidate-tasks)).* Submitting a valid word or using Undo clears any active hint/reveal preview.

---

## 3. Economy & monetization

**Coins → power-ups (Tasks 33, 36).** One `EconomyManager : IEconomyManager` (constructed + initialized in `GameBootstrap`) persists everything through `DataManager` → `PlayerProgress`: the coin balance **and** the owned power-up inventory (hint/undo/reveal/time), the `removeAds` flag + starting/daily-grant bookkeeping, and the **Task 36** faucet/sink state — the one-time **Starter Pack** flag, a temporary **ad-free window**, the **login-reward** cycle position, the **watch-for-coins** daily counter, and the highest **streak milestone** paid. All amounts/prices live in `BalanceConfig`. (A legacy `CoinSystem` MonoBehaviour also exists but is orphaned — see [§13](#13-known-tech-debt--candidate-tasks).)

**Two currencies, one direction — real money buys coins; coins buy power-ups:**

| Layer | What | Bought with |
|---|---|---|
| 🎁 **Starter Pack** (36J) | one-time **$1.99** → 1000 coins + 5 of each power-up + a 3-day ad-free window (`StoreProductType.StarterPack`) | **real money** via `IStoreService` |
| 💎 Coin packs | **Pouch** 150/$0.99 · **Stack** 500/$2.49 · **Chest** 1200/$4.99 *(MOST POPULAR)* · **Vault** 3000/$9.99 · **Hoard** 7000/$19.99 *(BEST VALUE)* — `coin_shop.json` (names + badges) | **real money** via `IStoreService` |
| 🎟️ Power-up bundles | Hint/Undo **50·135·320** · Reveal **120·320·800** · Time **60·160·400**, each as **×5 / ×15 / ×40** (tiered, bulk-discounted) | **coins** (`SpendCoinsAsync` → `Add*Async`) |
| 🚫 Remove Ads | one-time **$4.99**, sets the persisted `removeAds` flag | **real money** via `IStoreService` |

**Free grants:** every new player starts with **5 each** power-up (`ApplyStartingInventoryIfNeeded` — idempotent, *tops up*, never reduces a richer save) and gets **+2 each per local day** (`GrantDailyIfDue` — idempotent, no missed-day stacking, reuses the `DailyPuzzleService` clock).

**Coin faucets & sinks (Task 36 Phase 5 — numbers in `BalanceConfig`, all clock-free + idempotent per local day):**

| Faucet / sink | Amount | Where it surfaces |
|---|---|---|
| **Par-scaled daily reward** | Perfect **60** / Good **40** / Solved **25** / Failed **10** (`DailyCoinReward(stars, failed)`) | granted on daily finish (replaces the old flat +25) |
| **Login reward** (7-day cycle) | `{25, 25, 50, 50, 75, 75, 150}` then wraps (`ClaimLoginRewardAsync`) | the **Daily Rewards** popup on the menu |
| **Watch for coins** | **35** coins, cap **3/day** (`GrantWatchCoinsAsync`) | a row in the Shop's coin section |
| **Streak milestones** | **+100** at a **7 / 30 / 100**-day streak, each once ever (`AwardStreakMilestonesAsync`) | a toast on the daily results |
| **Reward doubler** | watch an ad → **2×** today's daily reward, once per result | a button on the daily results |
| **Streak repair** (sink) | spend **150** coins *or* watch an ad | the **Daily Rewards** popup |
| Classic / Puzzle-Show completion | **+10** (`PuzzleCompletionReward`); rewarded video = +1 Hint | gameplay |

**The Shop** (`ShopScreen` — a runtime overlay: black bg, cyan title, gold balance, colored rounded-outline buttons) is opened by the **gold coin pill** (`UIManager` → `OnShopRequested` → `GameBootstrap`). Top to bottom: a **pinned Starter Pack** + **Restore Purchases**, the coins-priced **Power-Up** bundles, a **Free Coins · watch an ad** row, the real-money **coin packs** (names + MOST POPULAR / BEST VALUE badges), and **Remove Ads**. It rebuilds from live state after each purchase; unaffordable bundles disable; Remove-Ads / Starter-Pack flip to **"Owned."** The **Daily Rewards** popup (`DailyRewardPopup`, same runtime-overlay idiom) is a separate menu surface for the login claim + streak repair, shown when either is available.

**Mockable store — real billing is stubbed, not faked:** `IStoreService` abstracts real-money purchases (coin packs, Remove-Ads, Starter-Pack) + **`RestorePurchasesAsync`**. The Editor/tests use `MockStoreService` (grants immediately so the flow is testable); the real platform impl is `PlatformStoreServiceStub` — a clearly-marked TODO that **always returns `Failed`** until Unity IAP + store-console products + a device are wired. Granting happens **only on `Success`**; `Cancelled`/`Failed` grant nothing. **Non-consumables** (Remove-Ads, Starter-Pack) are owned once and **idempotent** — `GrantStarterPackAsync` no-ops if already owned, and **Restore re-applies entitlements without re-granting the consumable coins**. The 3-day ad-free window is a Unix timestamp (`adFreeUntilUnix`); `GameBootstrap` recomputes `AdPolicyService.AdsRemoved = removeAds || ad-free-active` at boot and on purchase.

**Anti-deadlock:** no fail/lives gate + the free starting + daily grants mean a broke player can always finish; power-ups accelerate, never gate — no pay-to-win.

**Ads (Google Mobile Ads, integrated):** `IAdService` (low-dep `Puzzle` assembly so tests mock it) → `AdService` (real AdMob) + `NullAdService` (Editor). Unit IDs are AdMob **TEST IDs** as `[SerializeField]` placeholders — never real IDs in source. **Rewarded video is opt-in only**, granted exactly once on the SDK reward callback, never on dismiss/failure. `AdPolicyService` enforces the **interstitial frequency cap** (time cooldown `InterstitialCooldownSeconds` = 300 **and** `InterstitialPuzzleCap` = 5 puzzles, between-session only) — and **`AdsRemoved` is wired to `removeAds` *and* the Starter-Pack 3-day ad-free window** (recomputed at boot + on purchase), so the IAPs genuinely suppress interstitials. **Task 36's rewarded-ad faucets** (watch-for-coins, the daily reward doubler, ad-based streak repair) are fully wired to `IAdService` but **dormant under `NullAdService`** (the Editor default, `IsRewardedReady == false`) — their UI shows "ads not available yet" until a real rewarded SDK is present, exactly like the billing stub. **Login reward and coin-based repair work today**, no ad needed.

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
- **Outline buttons:** every button is a **colored rounded outline with a transparent center** (not a fill), so the black backdrop shows through. Each action owns a color from the menu set — **Resume** teal, **Daily** orange (hero), **Classic** green, **Puzzle Show** violet, **Time Attack** red, **Library/Stats** muted slate. The hero (Daily) gets a **thicker, brighter** orange ring; light/white labels sit on every button. The ring + black treatment is centralized in `UITheme` (`ApplyOutlineButton` / `ApplyHeroOutlineButton`, 9-slice ring sprites under `Assets/Resources/UI/`).
- **Black background + swappable space layer:** the app renders on a neutral near-black `#0A0A0A`, painted by a single full-screen **Background layer** behind every screen (`UIThemeManager.ApplyScreenBackground` / `EnsureBackgroundLayer`). It auto-loads `Assets/Resources/UI/SpaceBackground.png` if present — drop a sprite there to swap in a space backdrop with no restructuring (a pixel-art starfield ships now).
- **Gameplay tiles (Tasks 29–30):** the **start** row is **teal** see-through outline tiles, the **target** row is **orange**, and the played **chain** rows are **cyan** see-through outlines — all with a **bold ~7px ring** (thickened from the old hairline). The **active input row stays solid** (the obvious "current row," gold-edged as you type); the **green** correct-letter highlight still shows inside the cyan rows; the win beat turns the target solid green. The chain `VerticalLayoutGroup` honors the rung gap (`childControlHeight = true`) so rows read as **separate rungs**, not a touching block.
- **Keyboard (Tasks 29, 32):** **rounded** keys (the shared bubbly 9-slice) — DEL red, GO green — floating on a **transparent panel**, so the space background fills the whole lower screen (no grey brick behind the keys).
- **Gameplay motion (Task 29):** subtle **ladder-feel** animations — a letter **drops into** its tile as you type, a valid word's row **climbs** up into place, the win beat pulses, an invalid word shakes — all `ReduceMotion`-gated and clamped-`dt` smoothed.
- **Shop (Task 33):** the same identity — black, **cyan** title, **gold** balance, colored rounded-outline buttons — reached via a tappable **gold coin pill** on the menu.
- **Gold is now in-game only:** `accent-gold #C9B458` is no longer a menu color — it's reserved for in-game focus (hint / active-input tiles, the win "Next Puzzle", in-progress & current-tier rings, the streak headline).
- **Menu motion (Task 28):** the cyan **WORD LADDER** title does a one-time entrance then a slow, subtle vertical float; the buttons **cascade** in on open and give a tactile **press-punch** on tap. All coroutine/`Mathf`-based, **clamped-`dt` smoothed** so it rides through screen-transition hitches, and **fully gated by `UIAnimations.ReduceMotion`** (ON ⇒ static).
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
Economy:     PuzzleCompletionReward=10  RewardedAdHintGrant=1
             StartingPowerUpGrant=5  DailyPowerUpGrant=2
Daily 2.0:   DailyMistakeBudget=3  PerfectMaxDetours=0  GoodMaxDetours=2
             DailyCoinReward(stars,failed): Perfect=60 Good=40 Solved=25 Failed=10
             StreakRepairCoinCost=150  StreakRepairCooldownDays=7
Faucets:     LoginRewardCycle={25,25,50,50,75,75,150}  WatchCoinsReward=35 (WatchCoinsDailyCap=3)
             StreakMilestones={7,30,100} @ StreakMilestoneReward=100
Shop:        PowerUpBundleSizes={5,15,40} + tiered price arrays:
             Hint/UndoBundlePrices={50,135,320}  RevealBundlePrices={120,320,800}  TimeBundlePrices={60,160,400}
             (coin packs + Remove-Ads + Starter-Pack are defined in coin_shop.json, NOT BalanceConfig)
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
│   │                                      GameAction, Constants, EconomyManager, IEconomyManager,
│   │                                      IStoreService/MockStoreService/PlatformStoreServiceStub, ShopCatalog
│   └── Persistence/   WordPuzzle.Persistence  IDataManager, DataManager, PlayerProgress, SaveData,
│                                          SettingsData, DailyProgress, OnboardingData, TierDataLoader
├── Game/             WordPuzzle.Game      GameBootstrap (DI wiring), BootstrapInitializer,
│                                          DailyPuzzleService, DailyStreakRules, OnboardingRules,
│                                          TutorialPuzzle, ShareCardBuilder, IShareService,
│                                          IAdService impls (AdService, AdPolicyService, NullAdService)
│   └── Modes/         WordPuzzle.Modes    ClassicMode, PuzzleShowMode, TimeAttackMode(+Config),
│                                          IGameMode, ModeController
├── Puzzle/           WordPuzzle.Puzzle    WordGraph, WordValidator (IWordValidator), PuzzleGenerator,
│                                          WordOps, BalanceConfig, PathScoring (daily grade/stars), IAdService,
│                                          WordPuzzle (model), PuzzleDefinition, TierData, Difficulty, ValidationResult
└── UI/               WordPuzzle.UI        UIManager, UIAnimations, TimerDisplay, Themes/UITheme,
                                           Audio/SfxManager, Haptics/(IHaptics,HandheldHaptics,NullHaptics),
                                           TutorialOverlay, Components/(LetterTile, OnScreenKeyboard, …),
                                           Screens/(MainMenu, Gameplay, PuzzleLibrary, Results,
                                           Settings, TimeAttackSetup, Shop, Stats, DailyRewardPopup)

Assets/Resources/Data/  word_library.json (12,183), tier_definitions.json (350 = 7×50), daily_puzzles.json (450), common_words.json (6,582), coin_shop.json (coin packs + Remove-Ads + Starter-Pack)
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
| `puzzle_progress_v1` | `PuzzleProgressData` (tiers, completed IDs) | ✅ yes |
| `wordpuzzle_progress` | `PlayerProgress` (coins, **owned power-up inventory**, `removeAds`, starting/daily-grant flags, stats; **Task 36:** `starterPackOwned`, `adFreeUntilUnix`, login-reward cycle pos, watch-for-coins counter, milestone-paid marker) | ✅ yes |
| `wordpuzzle_save` | in-flight `GameStateSnapshot` | ✅ yes |
| `daily_v1` | `DailyProgress` — streak + **Daily 2.0:** `lastPlayedDateIso`, `lastRepairDateIso`, `todayPlayed`, and the trailing-365 `outcomes` ledger (`DayOutcome{dateIso,won}`) | ✅ yes |
| `settings_v1` | `SettingsData` (volumes, mute, reduceMotion, hapticsEnabled) | ❌ preserved |
| `onboarding_v1` | `OnboardingData` (tutorial done/skipped) | ❌ preserved (only Replay clears) |

`DataManager.ResetAllAsync` clears the four "yes" keys and preserves settings + onboarding. (`"Coins"` is a legacy key written only by the orphaned `CoinSystem`/`PlayerDataManager` — see [§13](#13-known-tech-debt--candidate-tasks).)

**Migration (Tasks 33, 36):** `PlayerProgress`/`PlayerProgressData` gained `totalTimeEarned`, `removeAds`, `startingGrantApplied`, `lastDailyGrantDate` (33), then `starterPackOwned`, `adFreeUntilUnix`, `lastLoginRewardDate`, `loginRewardIndex`, `lastWatchCoinsDate`, `watchCoinsCountToday`, `highestStreakMilestoneAwarded` (36). `DailyProgress` gained `lastPlayedDateIso`, `lastRepairDateIso`, `todayPlayed`, `outcomes` + a `Normalize()` (the Q6 seed `lastPlayedDate ← lastCompletedDate`, run in the **Persistence** assembly by `DataManager.LoadDailyProgressAsync` since it can't reference the Game asm). Everything serializes through the DTO + `DataManager` converters, and **JsonUtility auto-defaults missing fields**, so pre-update saves load cleanly (new fields = 0/false/"") and one-time grants (e.g. `startingGrantApplied = false`) fire once on the next boot.

---

## 12. Testing & tooling

- **NUnit EditMode** tests under `Assets/Tests/Unit/{Engine,Persistence,UI}` and `Assets/Tests/Integration`. The `Unit.Tests` asmdef references the `Game.*` assemblies (incl. `Game.Puzzle`, `Game.UI`, `Game.Persistence`); UI-folder tests use a separate `Tests` asmdef. Most new tests need **no asmdef change**.
- **Mocks** in `Assets/Tests/TestMocks.cs`: `MockDataManager`, `MockWordValidator`, `MockEconomyManager`, `MockAdService`. Extend these rather than inventing new doubles.
- **Conventions:** pure-logic classes (e.g. `DailyStreakRules`, `OnboardingRules`, `WordOps`, `BalanceConfig`, `SfxManager.EffectiveSfxVolume`) are tested standalone; `GameStateManager` tests build it with the mocks; persistence tests use `new DataManager()` against PlayerPrefs with `[SetUp]/[TearDown]` key cleanup.
- **Run (human):** Window → General → Test Runner → **PlayMode** → Run All. **MCP agents:** `run_tests(mode="PlayMode")` — it works (full suite 274/274); EditMode returns 0 (see [§17](#17-notes-for-ai-agents-working-in-this-repo)).
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
9. **Real IAP billing** — `PlatformStoreServiceStub` always returns `Failed`. Activating the Starter Pack / coin packs / Remove-Ads / **Restore** for real money needs Unity IAP + store-console products (`starter_pack`, `coins_150…coins_7000`, `premium_no_ads`) + receipt validation + a device build. The shop UI/flow are already written against `IStoreService`, so this is a swap-in with no UI changes.
10. **Rewarded-ad SDK for the Task 36 faucets** — watch-for-coins, the daily reward doubler, and ad-based streak repair are wired to `IAdService` but no-op under `NullAdService` (they show "ads not available yet"). A real rewarded provider (AdMob is already integrated for the hint ad) activates them; login reward + coin-based repair already work.
11. ✅ **Done (Task 38):** the daily-HUD leak into Classic is fixed (`GameplayScreen.SetDailyPar` always writes/clears the slot via the pure `ComposeDailyHud`); the Results doubler + streak lines no longer leak onto non-daily results; the **Stats screen was rebuilt** into grouped runtime cards; **one-and-done now truly locks** (re-shows the stored result on re-tap, no replay/coin-farm); and the pre-existing **missing-script** scene errors were cleaned (`Tools/Cleanup/Remove Missing Scripts In Open Scenes`).
12. **Balance-audit flags (Task 38 — surfaced, NOT applied; these shift feel/monetization, so they're deliberate calls):** *par-relative* daily detour grading (cutoffs are currently absolute, so long dailies grade proportionally stricter); Starter Pack generosity (~502 coins/$ one-time). Also: the per-use `HintCost`/`RevealCost`/`UndoCost` constants are **vestigial** (the owned-inventory model consumes a *charge*, not coins) — deletable once `Constants.cs` stops forwarding them.

> **Verification note (important for any agent):** the unityMCP **`run_tests(mode="PlayMode")` path works** and reports real pass/fail — the full suite runs green at **274/274** and genuine failures surface correctly. Two caveats: **(a)** `mode="EditMode"` with a filter returns `total:0` (a false `"Passed"`) because this project's test assembly is **PlayMode-registered** — always run PlayMode; **(b)** PlayMode occasionally "fails to initialize (timeout)" — retry, and if a timed-out run left the editor in Play Mode, `manage_editor(stop)` first. See [§17](#17-notes-for-ai-agents-working-in-this-repo) for the full workflow.

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
wordpuzzle_save, daily_v1, settings_v1, onboarding_v1). New persisted PlayerProgress fields serialize
through the PlayerProgressData DTO + DataManager converters; JsonUtility auto-defaults missing fields.
Daily 2.0 (Task 36): par-scored stakes. GameStateManager.ConfigureDailyRun(mistakeBudget, par) AFTER
StartNewPuzzle (par = puzzleDefinition.optimalSteps). An invalid guess spends a mistake (DailyMistakeBudget=3;
0 => FAIL); an accepted !validation.isProgress move is a DETOUR (sets grade, never fails). Pure
PathScoring.Score(...) -> PathGrade (Perfect/Good/Solved/Failed; (int)grade==stars). Streak authority =
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
AwardStreakMilestonesAsync 100 @ 7/30/100; reward doubler (ad => 2x); repair sink (150 coins or ad). Shop =
ShopScreen runtime overlay (coin pill -> OnShopRequested): pinned Starter Pack + Restore, tiered power-up
bundles (Hint/Undo 50/135/320, Reveal 120/320/800, Time 60/160/400 for x5/x15/x40 — INJECTED since UI can't
ref Puzzle), a Free-Coins watch row, named coin packs (coin_shop.json). DailyRewardPopup = a SEPARATE menu
overlay (login claim + repair). Real-money buys + RestorePurchasesAsync go through IStoreService
(MockStoreService in editor; PlatformStoreServiceStub = real billing, NOT implemented; StoreProductType
{Coins, RemoveAds, StarterPack}). removeAds || ad-free-window wires AdPolicyService.AdsRemoved. Rewarded-ad
faucets (watch-coins, doubler, ad-repair) are DORMANT under NullAdService (IsRewardedReady=false) until a real
ad SDK; login reward + coin-repair work now.
Tests live in Assets/Tests/Unit/ (NOT Assets/Scripts/Tests).
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
  IWordValidator, IDataManager, IGameMode, IEconomyManager (extended additively in Task 33),
  IStoreService unless a task says otherwise.
- All tests stay green — run via run_tests(mode="PlayMode") (PlayMode-registered suite, ~274 tests;
  EditMode returns total:0). Delete the .meta when you delete a Unity asset, and GUID-check
  scenes/prefabs before deleting any MonoBehaviour script.
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
3. Tests: Window → General → Test Runner → **PlayMode** → Run All (the suite is PlayMode-registered, ~274 tests).

---

## 17. Notes for AI agents working in this repo

Environment quirks learned the hard way — relevant when an agent verifies its own work:
- **The unityMCP `run_tests` runner works in PlayMode** (verified: full suite **274/274**, real failures surface). Gotchas: **(1)** the suite is **PlayMode-registered**, so `mode="EditMode"` with a filter returns `summary.total:0` — a *false* `"Passed"`; always use `mode="PlayMode"`. **(2)** PlayMode sometimes returns *"failed to initialize (tests did not start within timeout)"* — **retry**; a timed-out init can leave the editor IN Play Mode, after which the next run errors *"Cannot start … in Play Mode"* → `manage_editor(action="stop")`, then re-run. **(3)** `get_test_job.progress.completed` can exceed `total` (double-counted) — trust `result.summary.{total,passed,failed}`. **(4)** new `.cs` files written outside Unity need `refresh_unity(scope="all", mode="force")` to import before they compile (a `scope="scripts"` refresh can miss a brand-new file → `CS0234`). **(5)** running PlayMode tests leaves a temp `InitTestScene<guid>` loaded (Game view then shows *"No camera rendering"*) → reload `Assets/Scenes/GameUI.unity` afterward.
- **`execute_code` (in-editor C#) is broken here** (mono "filename or extension is too long"; Roslyn not installed). You cannot script Play-mode drives or screenshots — visual/feel acceptance is a human-in-Editor check.
- **`manage_camera` screenshots can't see the portrait game.** The capture returns a blank ~2:1 landscape Game-view rendered via the Main Camera (which **excludes** the Screen Space - Overlay UI canvas); the real frame is the portrait Device Simulator on display 0, which MCP can't read (`scene_view` capture needs an open Scene View). Verify UI **numerically** instead — `manage_scene get_hierarchy` with `include_transform` for positions, `ReadMcpResourceTool` on `mcpforunity://scene/gameobject/{id}/component/{name}` for rects/colors/refs — and hand the portrait eyeball to a human. Also: Play mode boots straight to **MainMenu** (you can't script into a specific mode), `manage_gameobject`/`set_property` edits are **blocked during Play**, and **instance IDs churn on every domain reload** — re-query, never cache them.
- **Confirm scene context before/after agent work.** Loading a scene in the editor replaces the open one; agents have left the editor on a non-`GameUI` scene and/or in Play mode. Re-open `GameUI.unity` and stop Play mode to restore the expected view.
- **`git status` before planning/committing.** Background agents occasionally drop shell-misfire junk files at repo root (e.g. `nul`, `{`, `0`) and can even pick up a *later* task autonomously — clean junk and check the tree before each commit.
- **Icons — SVG via Vector Graphics, or PNG.** `com.unity.vectorgraphics` provides the SVG importer; set the importer's **SVG Type = Textured Sprite** (the default "UI Toolkit" type yields *no* uGUI sprite — empty `SpriteRect`), and give artwork a **concrete stroke/fill colour, not `currentColor`** (which rasterizes to black and can't be tinted via `Image.color`). UI chrome (HOME, the global Settings gear) is built in code as tinted `Image` children. A `[SerializeField] Sprite` ref can point anywhere under `Assets/`; sprites loaded at runtime (`Resources.Load`) must sit under a `Resources/` folder.
- Untracked tooling dirs (`.claude/`, `.swarm/`, `.claude-flow/`, `agentdb.*`, `_Recovery/`) are not part of the game — never commit them. Shell-misfire junk (`nul`, `{`, `0`, `560)`, `statsScreen`, …) sometimes lands at repo root — delete before committing.

---

## Project history

Built iteratively through AI-orchestrated swarms, one concern each: word library & ladder semantics → modern tile/keyboard polish → library cards & tier gate → HOME/settings → hint/reveal semantics → per-mode behaviors & AddTime → TimeAttack UI → share result → daily + streak → **balance config & common-words generation** → **economy & rewarded ads** → **tactile juice (motion/haptics/sound)** → **premium visual identity (gold focus, ascent, motion vocabulary)** → **UI polish pass** (main-menu hierarchy with a gold DAILY hero, gameplay spacing, a keyboard-anchored power-up bar, a reliable visible HOME, and a properly clipping/scrolling word-chain) → **icon chrome** (SVG-via-Vector-Graphics + PNG icons: a house HOME and one shared, icon-only top-right Settings gear on every screen) → **Time Attack setup polish** (fit/styling/header, HOME aligned to the shared gear) → **dictionary expansion & cleanup** (reproducible ENABLE+Norvig tool: junk removed, 8,626→12,183 words, dense common 6/7-letter coverage) → **Puzzle Show 7×50** (350 curated ladders, two-level tier-select→grid navigation, completion coloring, progressive unlock) → **post-win flow** (compact win panel for endless Classic, auto-advancing Time Attack with results on timeout, Puzzle Show stat screen, Daily Home-only; "Play Again" re-routes into the mode) → **minimum-move floor** (no 1-move puzzles anywhere; min scales with word length, enforced by true full-dictionary shortest path in the generator and across all curated data) → **Classic-mode polish** (bolder tile outlines, rounded keyboard keys, subtle ladder-feel drop-in/climb animations) → **see-through cyan chain rows** with even rung spacing → **Reveal flicker fix** (idempotent per-frame render guards; Reveal/Hint decoupled) → **transparent keyboard panel** (space background edge-to-edge) → **shop & coins economy** (real-money coin bundles + coins-for-power-ups, a persisted owned inventory that seeds gameplay, 5-each starting + +2/day grants, a remove-ads IAP, a mockable store with real billing stubbed, and a `ShopScreen` reached from a live coin pill) → **Daily 2.0** (a par-scored daily with stakes: a 3-mistake budget + detour-based grade via pure `PathScoring`, a *played*-streak that survives a loss, a trailing-365 W/L record + win%, coins-or-ad streak repair, and a spoiler-free path-shape share card) → **Phase 5 economy** (tiered power-up bundles + reloaded/named coin packs with badges, a one-time **Starter Pack** + **Restore Purchases** + a 3-day ad-free window, par-scaled daily coin rewards, and the faucet/sink surfaces — a 7-day **login-reward** popup, **watch-for-coins**, **streak-milestone** pops, a **reward doubler**, and the Stats W/L line — all wired to `IAdService`/`IStoreService` ahead of a real ad SDK & billing) → **Stats rebuild + debug/balance pass (Task 38)** (the Stats screen rebuilt into clean grouped runtime cards; a debug pass that fixed three daily/results UI-state leaks — the daily HUD bleeding into Classic, the reward-doubler/streak lines leaking onto non-daily results, and a one-and-done that was cosmetic-only, now re-showing the stored result with no replay/farm; a missing-script scene cleanup with a reusable editor tool; and a balance audit that confirmed the economy is sane) → **Classic typing/feel polish** (the keyboard re-themed to the deep-indigo `UITheme.KeyboardPalette`, Reveal-rarity hardening, and clearly-felt press motion on keys, power-up buttons, and tiles; the active input row now **reuses persistent tiles** so **every** typed letter pops — not just the last — and **typing is capped at the puzzle's word length**, so a 5-letter puzzle accepts at most 5 letters) → **Settings rebuild** (the scene-authored, off-left-clipped page rebuilt at runtime like Stats — a scroll view of grouped **Audio / Accessibility / Data** cards, modern rounded **cyan** sliders + sliding **pill toggles** from `UITheme` tokens (no gold), HOME re-anchored; the gameplay-tile-only **High-Contrast / Large-Text** toggles removed, SFX/Music flagged silent until an audio bus). The git log captures the progression.
