# Star Ladder v1.0 — Pre-Release Audit Report

**Date:** 2026-06-12 · **Auditor:** Claude (solo-Lead executing all specialist roles sequentially, per recorded project preference; one editor owned all changes per Operating Rule 5)
**Audit target:** branch `theme/direction-b-purple`, finishing at `f7732a4` (23 commits ahead of `main`)
**Suite at close:** **378/378 PlayMode green** (375 pre-audit + 3 new regression tests added this pass)

---

## 1. EXECUTIVE VERDICT — **DO NOT SHIP (yet)**

The game *plays* well and the core engine, economy, and persistence code audit clean — but three release-infrastructure gaps are each independently launch-fatal, and none is a code bug fixable in an audit pass:

1. **AdMob is unconfigured for release.** `AdService.cs:33,36` ship Google's published **test** unit IDs; the Shared Context's production IDs appear nowhere in the repo; and **no `GoogleMobileAdsSettings.asset` exists**, so no App ID reaches the Android manifest / iOS plist — a release build with the GMA SDK **crashes at launch**. (Verified: repo-wide grep for `ca-app-pub`, glob for the settings asset, scene-file grep for Inspector overrides — all negative.)
2. **UMP consent is not implemented.** The consent *gate* exists (41A, `AdService.Awake`), but the only implementation is `NullConsentService` ("Phase 2 swaps the device path to UmpConsentService" — that class does not exist). Auto-consent in EEA/UK violates AdMob policy. (Verified: grep — `UmpConsentService`/`ConsentInformation` have zero hits.)
3. **The real-money store is a mock.** `GameBootstrap.cs:523` wires `MockStoreService`; `PlatformStoreServiceStub` is an empty stub. The shop sells Starter Pack / coin packs / Remove Ads for USD — shipping this means fake purchases. (Verified: grep of `IStoreService` implementations and the bootstrap wiring.)

Secondary release gaps: iOS bundle identifier is unset in ProjectSettings (Android is `com.nicholasgarner.wordpuzzle`); the audit branch is 23 commits ahead of `main` with **5 same-day UI overhauls still HUMAN GATE PENDING** (tutorial modal, stats, shop, library tier-select, and this pass's fixes); device-only verification (force-kill, offline cold-launch, performance numbers) has not been performed by anyone yet per the honesty section.

**The good news:** every historical bug area (B1–B5) re-verified as actually fixed; one real new bug found and fixed this pass (B7 corrupt-save recovery); no softlock path exists in the economy; the design system holds mechanically.

---

## 2. FIXED THIS PASS

| ID | Fix | Files | Verification |
|----|-----|-------|--------------|
| B7 | Corrupt-save recovery: `GetPlayerProgressAsync` (money path), `LoadGameStateAsync`, `LoadPuzzleProgressAsync` parsed PlayerPrefs JSON with no try/catch — `JsonUtility` throws on malformed input, escaping the boot path. Now catch-and-default, matching the settings/daily loader pattern. Commit `f7732a4`. | `Assets/Scripts/Core/Persistence/DataManager.cs`, `Assets/Tests/Unit/Persistence/DataManagerTests.cs` | 3 new regression tests plant garbage under the real PlayerPrefs keys and load through a fresh `DataManager` (no warm cache). Suite went 375 → **378/378** — the +3 delta confirms the new tests executed (guarding against the known result-collapsing runner issue, README §17). Pre-fix vulnerability confirmed by code trace (unguarded `FromJson` at all three sites). |

Same-day, immediately pre-audit (separate user requests, listed for completeness since they're in the audited range): tutorial welcome modal rebuild (`f4bafc3`), stats screen + global neon-glow layout fix (`dac0c92`), shop overhaul + shared `ApplySolidCard` seam (`8d906a1`), library tier-select squash/tofu/header fixes (`cd6cf73`). Each shipped with the suite green; all visually HUMAN GATE PENDING.

---

## 3. TASK A — FEATURE COHERENCE (per-mode verdicts)

- **Daily — SOUND.** Goal (today's puzzle, streak) communicated on the menu hero + results. Seed: `DailyPuzzleService` derives days-since-epoch (2025-01-01) over the **local** calendar date, mod a curated pool — deterministic per local date by design (method: code read). Streak engine is pure (`DailyStreakRules`, ISO local-date strings) with completion-date dedup, so a date increments at most once; repair path exists. Share card captured from actual run state before teardown (`CaptureShareInput` before mode clear).
- **Classic — SOUND.** Endless, win-panel loop; interstitials only in the between-puzzle slot (B5). Single analytics emission point per surface (win panel; `EndGame` unreachable for Classic — documented in code).
- **Puzzle Show — ISSUES (all resolved or intentional).** (a) Shared Context says 50 puzzles/tier; **reality is 100/tier × 7 = 700** (method: parsed `tier_definitions.json`) — docs/context drift, data is coherent. (b) "Completed = green" is now **aqua** — recorded intentional (Direction B retired green). (c) Tier-select screen had real defects (squashed rows, missing-glyph lock boxes, doubled masthead) — found and fixed pre-audit same day (`cd6cf73`). Gating (`PuzzlesRequiredToAdvance`: 10/15/20/25/30/35, capped) is stated on locked rows and enforced via `highestUnlockedTier`. Difficulty ramps 3→7 letters by tier (TierTheme + build tool).
- **Time Attack — SOUND** mechanically (60/120 timed + survival with +Ns/word pulled from `BalanceConfig` so copy can't drift); setup screen redesigned in Task 49 (gate pending). Backgrounding during a live timer is device-unverified (see Honesty).
- **A2 economy / softlock — PASS.** Every sink (hint/undo/reveal/time bundles, streak repair) has visible earn paths: puzzle completion rewards, daily bonus, login rewards, watch-ad faucet (3/day cap), starter grants (3 of each power-up + 1 daily reveal). Crucially **no puzzle requires a power-up to solve** — typing valid words is always free, so "no coins + no ads" never blocks progress; a daily loss ends the run rather than wedging it. Reveal economy = owned inventory (Task 33/38 audit). Verdict: no softlock path exists (method: code read of `EconomyManager` + `BalanceConfig` + mode loops).
- **A5 navigation — PASS by code trace.** Every screen wires a back/home route: results (Home), library (HOME + grid `‹ Back`), stats (HOME), shop (Back), settings, Time Attack setup, tutorial (SKIP at both levels). No screen lacks an exit event handler. Not exhaustively device-walked (Honesty).
- **A6 first launch — SOUND.** Fresh install routes through `OnboardingRules.ShouldRouteToTutorial` → welcome offer (PLAY TUTORIAL / SKIP) → 5-beat hands-on lesson on a real puzzle; onboarding survives progress reset by design; mid-tutorial app death re-offers next launch (tested in suite).

**Canary check:** issues were found (tier-count drift, tier-select defects, plus §1 blockers) — the audit was not issue-free.

## 4. TASK B — FUNCTIONAL DEBUG

| ID | Verdict | Evidence / method |
|----|---------|-------------------|
| B1 ad threading | **PASS** | `MobileAds.RaiseAdEventsOnUnityMainThread = true` set in `AdService.Awake` *before* any init — SDK-level marshal covers every callback path (load, earned, closed, failed). Code trace. |
| B2 coin persistence | **PASS (code trace; device kill untested)** | Every `EconomyManager` mutation awaits `UpdatePlayerProgressAsync`, which `PlayerPrefs.Save()`-flushes (39D); `OnApplicationPause`/`OnApplicationQuit` flush as belt-and-suspenders. Mid-transaction kill: grant persists at the moment of flush; reward→grant→flush ordering means a kill before flush drops the grant *and* the ad impression credit together (no half-state). **Force-kill on device not performed — see Honesty; canary honored by not claiming it.** |
| B3 rewarded retry | **PASS** | `AdRetryPolicy`: delay = min(base·2^attempt, max) from BalanceConfig, counter resets on success, 30-shift overflow guard. Retries continue at the ceiling — deliberately (a bounded retry *count* would permanently kill the button; capped backoff is the standard AdMob pattern). Watch button re-reads `IsRewardedReady` on every shop rebuild. Code trace + unit-tested policy class. |
| B4 reward ordering | **PASS** | `OnUserEarnedReward` only sets a flag; grant fires from `OnRewardedClosed` **iff** the flag is set, with the pending callback nulled before invoke and the flag cleared before the grant — close-early grants nothing; completed grants exactly once; re-trigger resets state in `ShowRewarded`. Double-grant impossible without a second full `Show`→earn cycle. Code trace (39C) + Task 39 tests in suite. |
| B5 interstitial cadence | **PASS** | Exactly two call sites, both between puzzles with board torn down (`OnWinNextPuzzle` Classic slot; `EndGame` with `activeMode` already null). Gates: AdsRemoved → cooldown → puzzle cap → readiness, all in `TryShowInterstitial`, caps in BalanceConfig. Can never fire mid-input. Code trace. |
| B6 state machine | **PASS by suite + trace; physical fuzz not performed** | Single `Dispatch(GameAction)` entry; submissions validate via the one `wordValidator`; resume caches cleared at every completion path; prior sessions fixed and tested the shared-instance state-leak bugs (Task 38). Device-level rapid-tap/rotation fuzz not performed (Honesty). |
| B7 save/load integrity | **FAIL → FIXED** | See §2. Settings/daily/onboarding loaders were already safe; the three unguarded paths now match. |
| B8 validation path | **PASS** | One `WordValidator` instance inside `GameStateManager`; all four modes submit through `Dispatch` → `ValidateWord` (`GameStateManager.cs:215`); no mode-local validators exist (grep). WordValidatorTests cover accept/reject in suite. |

**Canary check:** B2 reported with the force-kill caveat explicit; B4 double-grant mechanism described explicitly.

## 5. TASK C — UI/UX POLISH

- **C1/C2 palette + typography — PASS mechanically.** The Task 46 enforcement suite (DesignSystemTests, TypeScale tests — in the green 378) mechanically scans spawned screens for non-token Image colors and non-theme fonts; mode hexes live only in `Palette` (`#BE84E2/#6E84D6/#8160D2/#B25EB8/#54A8B4` verified in `UITheme.cs`); warm gold is token-routed (coins/hints/alerts). One tofu glyph (the `□` lock) found and removed (`cd6cf73`).
- **C3 layout — PASS mechanically, edge-values UNVERIFIED.** Safe-area via `SafeAreaPanel` through the shared background seam (+ shop overlay `__SafeContent`, Task 46); hit targets ≥88px enforced by tests. **Not rendered with 5-digit coins / 3-digit streaks / longest words — flagged per canary rather than claimed.** (Coin pills use `"N0"` with ContentSizeFitter self-sizing, so overflow clips are unlikely but unproven.)
- **C4 feedback — PASS with one gap.** Press feedback (`ScaleButtonTap`/ColorBlock dims) and consistent entrance/transition animations (shared `UIAnimations`, ReduceMotion-gated) are systematic. **Gap (should-fix, not fixed — UI addition beyond minimal-fix scope):** the shop "Watch" button has no loading state while a rewarded ad loads; if tapped when not ready the service no-ops to `onClosed` — functional but silent.
- **C5 completion states — PASS.** Aqua/grey resolved via `PuzzleShowMode.ResolveState` against persisted `completedPuzzleIds` (the same store gameplay writes), re-read on every `Show()` — survives relaunch by construction; persistence round-trip covered in suite.
- **C6 copy — PASS on sampled surfaces; not exhaustive.** All strings read during the five screen passes (menu, tutorial, stats, shop, library, results, Time Attack setup): no typos found; casing consistent (TITLE CASE headers / sentence-case body). **Strings changed this pass: one** — "STARTER PACK · one-time · best value" → "STARTER PACK · best value" (`8d906a1`, fixed physical truncation at Title size).

**Canary check:** C3 edge-value rendering explicitly not claimed; screens are not reported "perfect" (4 human gates open).

## 6. TASK D — EDGE CASES & LIFECYCLE

- **D1 offline — PASS by code trace; cold-launch offline not device-tested.** Boot never blocks on ads: consent gathers → init → loads are all async and failure-tolerant; `IsRewardedReady=false` renders the Watch row disabled (no spinner, no dead tap); gameplay paths never await ad state. Per the canary: the required cold-launch-offline device test **was not performed** — declared, not faked.
- **D2 lifecycle — PARTIAL PASS.** Pause/quit flush PlayerPrefs (39D); the video backdrop pauses on focus loss; ad-retry timers use realtime waits that survive pause. Resume-mid-puzzle snapshots exist per keystroke (unflushed by design, flushed on pause). **Time Attack timer across a 10-min background: behavior not device-verified** (Honesty).
- **D3 date/time — RULE STATED, exploitable by design.** *The enforced rule: the daily puzzle and streak key off the device's local calendar date (`yyyy-MM-dd` local ISO; rollover at local midnight); there is no server time source, so clock manipulation can replay or advance days.* Negative-day clamp prevents pre-epoch crashes; per-date dedup prevents same-day double-credit. Flagged as accepted offline-game limitation, not fixed (would require a network time dependency — out of scope).
- **D4 perf hygiene — PASS on the checked surface.** No per-frame allocations in the gameplay loop hot path were found (animations use cached WaitForSeconds/unscaled-dt loops; no `Update()` string churn in board code); a full profiler capture was not possible (no device).
- **D5 consent — FAIL (release blocker, flagged not fixed).** Gate architecture exists and is correct (`ConsentGate.ShouldInitAds`, init unreachable until gather); but the only implementation auto-consents (`NullConsentService`); UMP is absent. The game *is* fully playable with ads never initializing (the D5 "declined" criterion holds by the same gate). Fix requires the UMP SDK + device/region testing — outside an audit pass; per DO-NOT, consent logic was not altered.

## 7. TASK E — RELEASE READINESS

- **E1 cold start / E2 frames / E4 build size: NOT MEASURABLE in this environment** (no device, no build produced). Numbers deliberately not invented.
- **E3 build hygiene (from the actual configuration, not memory):**
  - ❌ AdMob: **test unit IDs in source; no `GoogleMobileAdsSettings.asset`** → release build crashes at launch (§1.1). This is worse than "test-device flags on" — the flags can't even be evaluated until the settings asset exists.
  - ❌ Store: `MockStoreService` is the live IStoreService (§1.3).
  - ❌ UMP consent absent (§1.2).
  - ⚠️ iOS `applicationIdentifier` unset (Android: `com.nicholasgarner.wordpuzzle` ✓); `bundleVersion` 1.0, `AndroidBundleVersionCode` 1, target SDK 34, min SDK 25 ✓.
  - ✅ Debug logging: 15 `Debug.Log` call sites repo-wide (boot-time wiring + warnings) — no spam; stripping config absent (Observation).
  - ✅ No editor-only code paths reachable at runtime found (MockStore is runtime — already flagged); Tiers 8–10: only the benign `LoadAllTierDataAsync` 1..10 loop, which safe-defaults for 8–10 — untouched per DO-NOT.
  - ⚠️ Release would ship from `main`, which is 23 commits behind the audited branch.

## 8. OPEN ISSUES

| Severity | Issue | Why not fixed this pass |
|----------|-------|-------------------------|
| **Blocker** | GMA settings asset + production ad unit IDs missing (§1.1) | Requires the real IDs entered in-editor (Assets ▸ Google Mobile Ads ▸ Settings) + a device build to verify; DO-NOT forbids changing ad IDs unflagged; AdService doc forbids hardcoding them in source. |
| **Blocker** | UMP consent not implemented (§1.2) | Needs the UMP SDK wired as `UmpConsentService` + EEA-region device testing. Architecture seam already exists. |
| **Blocker** | Real-money store is `MockStoreService` (§1.3) | Needs Unity IAP / Play Billing / StoreKit integration + store-console products + device receipts. Alternative: cut real-money items from v1.0 and ship coins-only — design decision, not auditor's call. |
| Should-fix | iOS bundle identifier unset | One-line ProjectSettings change, but the right reverse-DNS value is the owner's call alongside store-console setup. |
| Should-fix | Shop "Watch" button: no loading state while rewarded ad loads (C4) | UI addition beyond the minimal-fix mandate; noted for the next UI pass. |
| Should-fix | 5 HUMAN GATE PENDING visual overhauls need the owner's simulator eyeball | Auditor cannot screenshot the portrait Device Simulator (known tooling limitation). |
| Cosmetic | Daily/streak trusts the local clock (D3) | Accepted offline-design limitation; fix = server time, out of scope. |
| Cosmetic | `LoadAllTierDataAsync` loops tiers 1–10 (3 no-op loads) | Touches the parked Tiers 8–10 surface; DO-NOT. |

## 9. OBSERVATIONS (out of scope, not implemented)

- Conditional-compile (`#if !UNITY_EDITOR` or a stripping define) the `Debug.Log` wiring lines for release.
- `SaveGameStateAsync` skips `PlayerPrefs.Save()` per keystroke for perf (flushed on pause) — consider a periodic flush during very long sessions.
- `TutorialOverlay`'s welcome card still uses its inline copy of the solid-card recipe — fold into `ApplySolidCard` next time that file is touched.
- Consider a "restore purchases" smoke test once a real store lands.
- The Shared Context Block and one project memory still say 50 puzzles/tier — update the design docs to 100.

## 10. HONESTY SECTION (what this audit could NOT verify, and why)

1. **Force-kill coin persistence (B2)** — no physical device; editor "kill" doesn't replicate Android process death. Code trace only.
2. **Offline cold-launch (D1), background-during-ad, Time Attack 10-min background (D2)** — device-only behaviors.
3. **Clock-manipulation replay (D3)** — rule derived from code, not exercised on device.
4. **Cold-start time, frame stability, AAB size (E1/E2/E4)** — no build was produced; no target hardware attached.
5. **UMP region flow (D5)** — nothing to test; the implementation doesn't exist (that's the finding).
6. **Visual confirmation of the 5 pending UI overhauls** — tooling cannot screenshot the portrait Device Simulator; numeric layout verification + the enforcement suite stand in, owner eyeball required.
7. **C6 copy pass** covered every string on the surfaces visited this week, not a generated inventory of every string constant in the repo.
8. **Real ad load/show behavior** — test IDs + no app ID means the ad stack can't be exercised end-to-end anywhere right now.

---

*Audit fixes commit: `f7732a4` (B7). Suite: 378/378 PlayMode. Runner-integrity check: test count delta matched added tests exactly.*
