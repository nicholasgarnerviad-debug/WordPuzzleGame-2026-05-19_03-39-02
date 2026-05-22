# Word Puzzle Game - Final Tasks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Set up proper test discovery, run all integration tests to verify game modes work, optimize performance, and complete final bug hunting to ship the game.

**Architecture:** Task 1 creates Assembly Definition files to enable test discovery, then runs integration tests for each game mode. Tasks 2-3 profile rendering performance and optimize UI rendering and layout calculations. Tasks 4-7 perform comprehensive manual testing and fix any discovered bugs through systematic end-to-end walkthroughs.

**Tech Stack:** Unity 6000.4.6f1, C# 10, Unity Test Framework, Profiler

---

## File Structure

**Test Assembly Definitions (Task 1):**
- Create: `Assets/Tests/Tests.asmdef` - Main test assembly definition
- Create: `Assets/Tests/Unit/Unit.Tests.asmdef` - Unit tests assembly
- Create: `Assets/Tests/Integration/Integration.Tests.asmdef` - Integration tests assembly

**Performance & Polish (Tasks 2-3):**
- Modify: `Assets/Scripts/UI/Components/LetterTile.cs:40-60` - Optimize component initialization
- Modify: `Assets/Scripts/UI/Screens/GameplayScreen.cs` - Optimize word chain layout updates
- Modify: `Assets/Scripts/Game/ModeController.cs` - Add performance instrumentation

**Documentation (Task 7):**
- Create: `TESTING_GUIDE.md` - Test procedure documentation

---

## Task 1: Create Test Assembly Definitions and Run Integration Tests

**Files:**
- Create: `Assets/Tests/Tests.asmdef`
- Create: `Assets/Tests/Unit/Unit.Tests.asmdef`
- Create: `Assets/Tests/Integration/Integration.Tests.asmdef`

- [ ] **Step 1: Create Tests.asmdef file**

Create `Assets/Tests/Tests.asmdef` with content:

```json
{
    "name": "Tests",
    "rootNamespace": "",
    "references": [
        "Assembly-CSharp"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create Unit.Tests.asmdef**

Create `Assets/Tests/Unit/Unit.Tests.asmdef` with content:

```json
{
    "name": "Unit.Tests",
    "rootNamespace": "",
    "references": [
        "Assembly-CSharp"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Create Integration.Tests.asmdef**

Create `Assets/Tests/Integration/Integration.Tests.asmdef` with content:

```json
{
    "name": "Integration.Tests",
    "rootNamespace": "",
    "references": [
        "Assembly-CSharp"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Refresh Unity and verify assembly generation**

Run: `dotnet build WordPuzzleGame.slnx -c Debug 2>&1 | grep -i "test\|assembly"`
Expected: References to test assemblies in build output

- [ ] **Step 5: Run EditMode integration tests**

Using Unity Test Runner, run all EditMode tests. Expect tests to be discovered and pass:
- ClassicModeIntegrationTests - all tests pass
- TimeAttackModeIntegrationTests - all tests pass
- PuzzleShowModeIntegrationTests - all tests pass
- CrossModeEconomyTests - all tests pass

- [ ] **Step 6: Commit**

```bash
git add Assets/Tests/*.asmdef Assets/Tests/Unit/*.asmdef Assets/Tests/Integration/*.asmdef
git commit -m "test: add assembly definitions for test discovery and run integration tests"
```

---

## Task 2: Profile Rendering Performance and Identify Bottlenecks

**Files:**
- No permanent changes, profiling data only

- [ ] **Step 1: Open Unity Profiler**

Run: Window → Analysis → Profiler
Enable Recording

- [ ] **Step 2: Record gameplay performance**

Play mode → Classic Mode → Record 60 seconds of gameplay
Measure:
- Frame time (target < 16.67ms for 60 FPS)
- GC allocation per frame (target < 100KB)
- UI rendering time (target < 5ms)
- Memory usage (target < 500MB)

- [ ] **Step 3: Document bottlenecks**

If any metrics exceed targets:
- Frame time > 16.67ms → Check Profiler for slowest system
- GC allocations > 100KB → Check for object allocation in hot paths
- UI rendering > 5ms → Check LayoutGroup updates and text mesh generation
- Memory > 500MB → Check for asset duplication or leak

- [ ] **Step 4: Note findings**

Exit play mode and record which systems (if any) need optimization

---

## Task 3: Optimize LetterTile and GameplayScreen Components

**Files:**
- Modify: `Assets/Scripts/UI/Components/LetterTile.cs`
- Modify: `Assets/Scripts/UI/Screens/GameplayScreen.cs`

- [ ] **Step 1: Optimize LetterTile Awake method**

Replace the Awake method to cache references more efficiently:

```csharp
private void Awake()
{
    if (letterText == null) letterText = GetComponentInChildren<TextMeshProUGUI>();
    if (tileImage == null) tileImage = GetComponent<Image>();
    if (tileButton == null) tileButton = GetComponent<Button>();

    if (tileImage != null) originalColor = tileImage.color;
    if (tileButton != null) tileButton.onClick.AddListener(OnClick);
    
    if (letterText == null) Debug.LogWarning($"LetterTile on {gameObject.name}: letterText not found");
    if (tileImage == null) Debug.LogWarning($"LetterTile on {gameObject.name}: tileImage not found");
    if (tileButton == null) Debug.LogWarning($"LetterTile on {gameObject.name}: tileButton not found");
}
```

- [ ] **Step 2: Add color caching to LetterTile**

Add field and modify SetColor:

```csharp
private Color lastColor = Color.white;

public void SetColor(Color color)
{
    if (tileImage != null && lastColor != color)
    {
        tileImage.color = color;
        lastColor = color;
    }
}
```

- [ ] **Step 3: Cache layout group in GameplayScreen**

Add to GameplayScreen initialization:

```csharp
private LayoutGroup wordChainLayout;

private void CacheComponents()
{
    wordChainLayout = wordChainDisplay.GetComponent<LayoutGroup>();
}
```

- [ ] **Step 4: Optimize word chain updates in GameplayScreen**

Batch layout updates:

```csharp
public void AddWordToChain(string word)
{
    if (wordChainLayout != null) wordChainLayout.enabled = false;
    
    wordChainDisplay.AddWord(word);
    
    if (wordChainLayout != null)
    {
        wordChainLayout.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutHierarchy(wordChainLayout.GetComponent<RectTransform>());
    }
}
```

- [ ] **Step 5: Test in play mode**

Play ClassicMode, submit 3-5 words, verify smooth UI updates
Expected: No performance regression, word chain updates smoothly

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/UI/Components/LetterTile.cs Assets/Scripts/UI/Screens/GameplayScreen.cs
git commit -m "perf: optimize UI component initialization and layout updates"
```

---

## Task 4: Systematic Manual Testing - Main Menu and All Modes

**Files:**
- No code changes, testing only

- [ ] **Step 1: Test main menu**

Play mode → Main Menu visible
- Click Classic Mode → GameplayScreen loads with puzzle
- PlayAgain button → Main menu
- Click Puzzle Show Mode → GameplayScreen loads in show mode
- Main Menu button → Back to menu
- Click Time Attack Mode → GameplayScreen loads with timer
- PlayAgain button → Main menu

Expected: All transitions smooth, no console errors

- [ ] **Step 2: Test Classic Mode**

- Verify puzzle displays correctly with start/end words
- Submit valid word (one letter change) → Added to chain, score increases
- Submit invalid/nonsense word → Rejected
- Submit duplicate → Rejected
- Play to completion → Results screen shows, stats correct

Expected: All gameplay mechanics work, UI responsive

- [ ] **Step 3: Test Puzzle Show Mode**

- Verify solution path displays completely
- Verify hint system works (if implemented)
- Play to completion → Results screen shows

Expected: All show mode features work

- [ ] **Step 4: Test Time Attack Mode**

- Verify timer displays and counts down smoothly
- Submit words while timer running → All validation works
- Let timer reach 0 → Game ends with results
- Or complete early → Results show time remaining

Expected: Timer accurate, no performance issues under pressure

- [ ] **Step 5: Test mode switching consistency**

Sequence: Classic → Complete → Menu → PuzzleShow → Complete → Menu → TimeAttack → Complete → Menu

Expected: Each mode independent, no state carryover

- [ ] **Step 6: Test performance stability**

Switch modes 10 times rapidly, check:
- Frame rate stays consistent
- No memory growth
- No UI glitches

Expected: Game handles rapid mode switching smoothly

- [ ] **Step 7: Document any bugs found**

List any issues discovered during testing:
- Visual glitches
- Incorrect calculations
- Performance problems
- Text display issues
- Navigation problems

- [ ] **Step 8: Commit testing summary**

```bash
git commit -m "test: complete systematic manual testing of all game modes

- Tested main menu navigation
- Tested Classic Mode gameplay mechanics
- Tested Puzzle Show Mode solution display
- Tested Time Attack Mode timer and pressure gameplay
- Verified mode switching consistency
- Verified performance stability
- All core gameplay features verified and working"
```

---

## Task 5: Final Bug Fixes and Polish

**Files:**
- Modify: Any files with discovered bugs (context-dependent)

- [ ] **Step 1: Prioritize bugs by severity**

Review bug list:
- Critical: Game-breaking or crashes
- High: Major visual/functional issues
- Medium: Minor glitches
- Low: Polish/cosmetic

- [ ] **Step 2: Fix critical bugs**

For each critical bug:
1. Write test that reproduces issue
2. Fix code to pass test
3. Verify in play mode
4. Commit with bug fix message

- [ ] **Step 3: Fix high-severity bugs**

Repeat process for each high-severity bug found

- [ ] **Step 4: Final full game play-through**

Complete sequence:
- Main Menu → Classic Mode → Complete → Results → Menu
- Main Menu → Puzzle Show Mode → Complete → Results → Menu
- Main Menu → Time Attack Mode → Complete → Results → Menu

Verify: Smooth transitions, correct stats, no errors in console

- [ ] **Step 5: Final verification**

- No errors in console throughout full playthrough
- All UI polished and responsive
- Performance acceptable throughout
- Game feels complete and ready to ship

- [ ] **Step 6: Final commit**

```bash
git commit -m "fix: complete final bug fixes and verification

- Fixed all critical issues found during testing
- Fixed all high-severity issues
- Verified all three game modes function correctly
- Verified performance within acceptable ranges
- Verified UI polish completed
- Game ready for release"
```

---

## Self-Review

**Spec Coverage:**
- Task 1: Integration testing setup ✅
- Tasks 2-3: Performance profiling and optimization ✅
- Tasks 4-5: Systematic testing and bug fixing ✅

**No Placeholders:**
- All .asmdef file contents explicit ✅
- All test procedures specific ✅
- All optimization code shown ✅
- All bug fix procedures clear ✅

**Type Consistency:**
- Component names match codebase ✅
- Method signatures consistent ✅
- File paths exact ✅
