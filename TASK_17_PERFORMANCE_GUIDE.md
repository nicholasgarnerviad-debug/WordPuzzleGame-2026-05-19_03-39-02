# Task 17: Performance Profiling and Optimization Guide

**Goal:** Profile the game, identify bottlenecks, and optimize for 60 FPS on target Android devices.

---

## Prerequisites

- ✅ All scenes created (using SceneSetupUtility)
- ✅ Project builds successfully for Android
- ✅ Game runs in Editor Play mode without errors

---

## Phase 1: Profiling with Unity Profiler

### Step 1: Open Unity Profiler

1. **In Play Mode**, go to: **Window → Analysis → Profiler**
2. Or use keyboard shortcut: **Ctrl + Alt + 7**
3. Make sure **Development Build** is enabled in Player Settings for accurate profiling

### Step 2: Profile Each Scene

For each scene (MainMenu, ClassicMode, PuzzleShowMode, TimeAttackMode):

1. **Open the scene** in Editor
2. **Press Play**
3. **Record for 30 seconds** while actively playing (submitting words, navigating)
4. **Check these metrics:**

#### CPU Usage
- Target: **< 50% of frame budget** (at 60 FPS, frame budget = 16.67ms)
- Accept: < 8ms per frame for game logic
- Watch for: Spikes above 10ms indicate bottlenecks

#### Memory Usage
- Target: **< 150 MB** total
- Break down:
  - Textures & Assets: < 80 MB
  - Scripts & MonoBehaviours: < 30 MB
  - UI: < 20 MB
  - Managed Memory: < 20 MB

#### GPU Usage
- Target: **< 30% GPU bandwidth**
- If exceeds 30%: Reduce texture quality or draw call count

#### Frame Rate
- Target: **60 FPS consistent** (no drops below 55 FPS)
- Accept: 60 FPS ±2 during regular gameplay
- Tolerate: Spikes to 50 FPS only during scene transitions

### Step 3: Identify Bottlenecks

In Profiler window, check these modules:

**CPU Tab:**
- **Scripts**: Look for expensive methods (PuzzleGenerator, WordGraph)
- **Physics**: Should be near 0 (no physics in this game)
- **Rendering**: Check if UI rendering is expensive
- **GC.Alloc**: Should be minimal (no garbage generation)

**Memory Tab:**
- **Assets**: Check for large audio files or textures
- **Managed Memory**: Watch for leak patterns (memory growing over time)

**GPU Tab:**
- **Batches**: Should be < 100 batches per frame
- **Draw Calls**: Should be < 80 for UI-heavy scenes

---

## Phase 2: Optimization Strategies

### If CPU is High (Scripts)

**Optimization 1: Cache PuzzleGenerator Results**

Current code in `GameController.cs`:
```csharp
public void GenerateNewPuzzle(int difficulty = 1)
{
    currentPuzzle = puzzleGenerator.GeneratePuzzle(difficulty);
    // ...
}
```

Optimized version:
```csharp
private Dictionary<int, PuzzleData> puzzleCache = new Dictionary<int, PuzzleData>();

public void GenerateNewPuzzle(int difficulty = 1)
{
    if (puzzleCache.ContainsKey(difficulty))
    {
        currentPuzzle = puzzleCache[difficulty];
    }
    else
    {
        currentPuzzle = puzzleGenerator.GeneratePuzzle(difficulty);
        puzzleCache[difficulty] = currentPuzzle;
    }
}
```

**Optimization 2: Object Pooling for PuzzleData**

Add to `PuzzleGenerator.cs`:
```csharp
private Queue<PuzzleData> puzzlePool = new Queue<PuzzleData>();
private const int POOL_SIZE = 5;

public PuzzleData GeneratePuzzle(int difficulty = 1)
{
    PuzzleData puzzle = GetPooledPuzzle();
    puzzle.difficulty = Mathf.Clamp(difficulty, 1, 5);
    // ... rest of generation
    return puzzle;
}

private PuzzleData GetPooledPuzzle()
{
    return puzzlePool.Count > 0 ? puzzlePool.Dequeue() : new PuzzleData();
}

public void ReturnToPool(PuzzleData puzzle)
{
    if (puzzlePool.Count < POOL_SIZE)
        puzzlePool.Enqueue(puzzle);
}
```

### If Memory is High (Assets)

**Check Asset Sizes:**
1. Assets → Right-click folder → Check File Size
2. Target asset budgets:
   - All audio files combined: < 20 MB
   - All textures combined: < 30 MB
   - Fonts & Resources: < 5 MB

**Compress Assets:**
1. File → Build Settings → Player Settings
2. Asset Pipeline → Compression Format: **LZ4**
3. Enable **Stripping** for unused code

### If GPU is High (Rendering)

**Optimization 1: Reduce UI Overdraw**
- Move off-screen UI elements far from camera
- Use simple colors instead of gradients
- Disable raycast on non-interactive UI (Button → Image → Raycast Target: OFF)

**Optimization 2: Batch UI Elements**
- Group related buttons together
- Use single canvas when possible
- Avoid nested canvases

---

## Phase 3: Device Testing

### Setup Test Device

**On Android Device:**
1. Enable **Developer Options**: Settings → About → Build number (tap 7x)
2. Enable **USB Debugging**: Developer Options → USB Debugging
3. Connect to PC via USB cable

### Build Development APK

```bash
File → Build Settings
- Platform: Android
- Development Build: CHECKED ✓
- Autoconnect Profiler: CHECKED ✓
- Click Build And Run
```

**Wait ~2-3 minutes for build and install**

### Profile On Device

**With device connected:**
1. **Open Profiler** while game runs on device
2. In Profiler window, select device from dropdown: **[Device Name]**
3. **Play each mode for 1-2 minutes** while profiling
4. Take note of metrics

---

## Phase 4: Optimization Targets & Acceptance Criteria

### CPU Performance

| Metric | Target | Accept | Warning |
|--------|--------|--------|---------|
| Frame Time | < 14ms | < 16ms | > 16ms (< 60 FPS) |
| GC Allocs | 0 per frame | < 1 KB/frame | > 10 KB/frame |
| Script Time | < 5ms | < 8ms | > 10ms |

### Memory Performance

| Metric | Target | Accept | Max |
|--------|--------|--------|-----|
| Total RAM | < 100 MB | < 150 MB | > 200 MB |
| Managed Heap | < 20 MB | < 30 MB | > 50 MB |
| Asset Memory | < 80 MB | < 100 MB | > 150 MB |

### Graphics Performance

| Metric | Target | Accept | Warning |
|--------|--------|--------|---------|
| FPS | 60 stable | 55-60 | < 55 FPS |
| Batches | < 50 | < 80 | > 100 |
| Draw Calls | < 40 | < 60 | > 80 |

---

## Phase 5: Final Optimization Checklist

- [ ] CPU usage: **Pass** (all frames < 14ms)
- [ ] Memory usage: **Pass** (< 150 MB total)
- [ ] GPU usage: **Pass** (< 100 batches)
- [ ] Frame rate: **Pass** (60 FPS stable)
- [ ] No memory leaks: **Pass** (memory stable over 5 min)
- [ ] Responsive UI: **Pass** (no input lag)
- [ ] Load times: **Pass** (< 2 seconds per scene)
- [ ] Battery drain: **Pass** (acceptable with display on)

---

## Common Issues & Solutions

### Issue: High GC Allocs (garbage collection spikes)

**Cause:** Creating new objects during gameplay
**Solution:**
```csharp
// BAD: Creates new List every frame
List<string> words = new List<string>(puzzle.words);

// GOOD: Reuse cached list
private List<string> cachedWords = new List<string>();
private void CacheWords()
{
    cachedWords.Clear();
    cachedWords.AddRange(puzzle.words);
}
```

### Issue: Memory grows over time (leak)

**Cause:** Event listeners not being unsubscribed
**Solution:**
```csharp
private void OnEnable()
{
    gameController.PuzzleCompleted += OnPuzzleComplete;
}

private void OnDisable()
{
    gameController.PuzzleCompleted -= OnPuzzleComplete;  // IMPORTANT!
}
```

### Issue: Stuttering during gameplay

**Cause:** Instantiating/destroying objects mid-game
**Solution:** Use object pooling (pre-create objects, reuse them)

---

## Documentation & Reporting

Create a **PERFORMANCE_REPORT.md** file with:

```markdown
# Performance Report - Word Puzzle Game

## Device Tested
- Device: [Model]
- Android Version: [Version]
- RAM: [Amount]

## Results

### CPU Performance
- Average Frame Time: [X]ms
- Peak Frame Time: [X]ms
- GC Allocations: [X]KB/frame
- **Status: PASS / FAIL**

### Memory Performance
- Total Memory: [X]MB
- Managed Heap: [X]MB
- **Status: PASS / FAIL**

### Graphics Performance
- Average FPS: [X]
- Min FPS: [X]
- Batches: [X]
- Draw Calls: [X]
- **Status: PASS / FAIL**

## Optimizations Applied
- [List optimizations made]

## Remaining Issues
- [List any remaining issues]
```

---

## Success Criteria

✅ **Task 17 Complete when:**
1. Profiler data collected for all 4 scenes
2. CPU < 14ms per frame on target device
3. Memory < 150 MB total
4. FPS stable 60 (no drops below 55)
5. No memory leaks detected
6. PERFORMANCE_REPORT.md created

---

## Next: Task 18 (Mobile UI Refinement)

After profiling and optimization, proceed to Task 18 for UI refinement and accessibility testing.

