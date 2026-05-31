# Scene Setup Guide for Word Puzzle Game

Complete step-by-step instructions for creating and configuring all 4 required Unity scenes.

---

## Prerequisites

Before starting, ensure:
- ✅ Project is open in Unity Editor
- ✅ All C# scripts are imported (no compilation errors)
- ✅ Assets/ directory structure exists with Scripts, Tests, Resources subdirectories

---

## Scene 1: MainMenu.unity

**Purpose:** Entry point to the game. Displays mode selection and access to shop/settings.

### Step 1: Create the Scene

1. In Project window, navigate to `Assets/Scenes/`
2. Right-click → Create → Scene
3. Name it `MainMenu`
4. Double-click to open it

### Step 2: Create Canvas and UI

1. Right-click in Hierarchy → UI → Canvas
   - This creates a Canvas with CanvasScaler
2. Set Canvas Scaler settings:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920` (mobile portrait)

### Step 3: Add Buttons to Canvas

Under Canvas, create 5 buttons:

**Button 1: Classic Mode**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `ClassicModeButton`
3. Set Text to "Classic Mode"
4. Set Position: (0, 100, 0)
5. Set Size: (300, 80)

**Button 2: Puzzle Show**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `PuzzleShowButton`
3. Set Text to "Puzzle Show"
4. Set Position: (0, 0, 0)
5. Set Size: (300, 80)

**Button 3: Time Attack**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `TimeAttackButton`
3. Set Text to "Time Attack"
4. Set Position: (0, -100, 0)
5. Set Size: (300, 80)

**Button 4: Shop**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `ShopButton`
3. Set Text to "Shop"
4. Set Position: (-300, -250, 0)
5. Set Size: (200, 60)

**Button 5: Settings**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `SettingsButton`
3. Set Text to "Settings"
4. Set Position: (300, -250, 0)
5. Set Size: (200, 60)

### Step 4: Add Title Text

1. Right-click Canvas → TextMeshPro - Text
2. Name: `TitleText`
3. Set Text to "Word Puzzle Game"
4. Set Position: (0, 300, 0)
5. Set Font Size: 48

### Step 5: Add MainMenuScreen Script

1. Select Canvas
2. Drag `Assets/Scripts/UI/Screens/MainMenuScreen.cs` into Inspector (or use Add Component)
3. In Inspector, assign button references:
   - Classic Mode Button: (drag ClassicModeButton from Hierarchy)
   - Puzzle Show Button: (drag PuzzleShowButton from Hierarchy)
   - Time Attack Button: (drag TimeAttackButton from Hierarchy)
   - Shop Button: (drag ShopButton from Hierarchy)
   - Settings Button: (drag SettingsButton from Hierarchy)

### Step 6: Add UIManager

1. Create empty GameObject: Right-click Hierarchy → Create Empty
2. Name: `UIManager`
3. Add Component → Script → Drag `Assets/Scripts/UI/UIManager.cs`

### Step 7: Add Managers (for testing)

1. Create empty GameObject: `CoinSystemObject`
   - Add Component: CoinSystem
2. Create empty GameObject: `AdManagerObject`
   - Add Component: AdManager
3. Create empty GameObject: `IAPManagerObject`
   - Add Component: IAPManager
4. Create empty GameObject: `PlayerDataManagerObject`
   - Add Component: PlayerDataManager

### Step 8: Save and Configure Build Settings

1. File → Save Scene (or Ctrl+S)
2. File → Build Settings
3. Drag MainMenu.unity from Assets/Scenes/ into the Scenes In Build list
4. Set as **Scene 0**

---

## Scene 2: ClassicMode.unity

**Purpose:** Classic game mode with unlimited puzzles and automatic difficulty scaling.

### Step 1: Create the Scene

1. In Assets/Scenes/, create new scene
2. Name it `ClassicMode`
3. Open it

### Step 2: Create Game Structure

1. Create empty GameObject: `GameManager`
2. Add Components to GameManager:
   - GameController
   - ClassicMode

### Step 3: Create Canvas and UI

1. Create Canvas (right-click Hierarchy → UI → Canvas)
2. Set Canvas Scaler:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1080 x 1920`

### Step 4: Add UI Elements to Canvas

**Score Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `ScoreText`
3. Set Text to "Score: 0"
4. Position: (0, 400, 0)
5. Font Size: 36

**Words Found Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `WordsText`
3. Set Text to "Words: "
4. Position: (0, 300, 0)
5. Font Size: 28

**Word Input Field**
1. Right-click Canvas → Input Field - TextMeshPro
2. Name: `WordInput`
3. Position: (0, 100, 0)
4. Size: (400, 60)
5. Placeholder Text: "Enter word..."

**Submit Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `SubmitButton`
3. Text: "Submit"
4. Position: (0, -20, 0)
5. Size: (200, 60)

**Hint Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `HintButton`
3. Text: "Hint"
4. Position: (-150, -100, 0)
5. Size: (150, 50)

**Undo Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `UndoButton`
3. Text: "Undo"
4. Position: (150, -100, 0)
5. Size: (150, 50)

### Step 5: Add GameplayScreen Script

1. Select Canvas
2. Add Component → Script → MainMenuScreen.cs (WAIT - this is wrong!)
3. **Actually, drag GameplayScreen.cs** from Assets/Scripts/UI/Screens/
4. Assign references in Inspector:
   - Game Controller: (drag GameManager)
   - Submit Button: (drag SubmitButton)
   - Hint Button: (drag HintButton)
   - Undo Button: (drag UndoButton)
   - Score Text: (drag ScoreText)
   - Words Text: (drag WordsText)
   - Word Input: (drag WordInput)

### Step 6: Add Persistent Managers

1. Create: `UIManager` GameObject → Add UIManager script
2. Create: `CoinSystemObject` GameObject → Add CoinSystem script
3. Create: `AdManagerObject` GameObject → Add AdManager script
4. Create: `IAPManagerObject` GameObject → Add IAPManager script
5. Create: `PlayerDataManagerObject` GameObject → Add PlayerDataManager script

### Step 7: Configure GameManager

1. Select GameManager in Hierarchy
2. In Inspector for GameController component:
   - No setup needed (uses default word list)
3. In Inspector for ClassicMode component:
   - Call Initialize() in Start()

### Step 8: Save and Add to Build Settings

1. File → Save Scene
2. File → Build Settings
3. Add ClassicMode.unity to Scenes In Build
4. Set as **Scene 1**

---

## Scene 3: PuzzleShowMode.unity

**Purpose:** Tier-based progression system where players unlock new tiers by completing puzzles.

### Step 1: Create the Scene

1. In Assets/Scenes/, create new scene
2. Name it `PuzzleShowMode`
3. Open it

### Step 2: Create Game Structure

1. Create empty GameObject: `GameManager`
2. Add Components:
   - GameController
   - PuzzleShowMode

### Step 3: Create Canvas and UI

1. Create Canvas (UI → Canvas)
2. Set Canvas Scaler to Scale With Screen Size (1080 x 1920)

### Step 4: Add UI Elements

**Tier Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `TierText`
3. Set Text to "Tier: 1"
4. Position: (0, 450, 0)
5. Font Size: 40

**Score Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `ScoreText`
3. Set Text to "Score: 0"
4. Position: (0, 350, 0)
5. Font Size: 36

**Words Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `WordsText`
3. Set Text to "Words: "
4. Position: (0, 250, 0)
5. Font Size: 28

**Word Input**
1. Right-click Canvas → Input Field - TextMeshPro
2. Name: `WordInput`
3. Position: (0, 100, 0)
4. Size: (400, 60)

**Submit Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `SubmitButton`
3. Text: "Submit"
4. Position: (0, -20, 0)
5. Size: (200, 60)

**Hint Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `HintButton`
3. Text: "Hint"
4. Position: (-150, -100, 0)
5. Size: (150, 50)

**Undo Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `UndoButton`
3. Text: "Undo"
4. Position: (150, -100, 0)
5. Size: (150, 50)

### Step 5: Add GameplayScreen Script

1. Select Canvas
2. Add Component: GameplayScreen (from Assets/Scripts/UI/Screens/)
3. Assign references:
   - Game Controller: GameManager
   - Submit Button: SubmitButton
   - Hint Button: HintButton
   - Undo Button: UndoButton
   - Score Text: ScoreText
   - Words Text: WordsText
   - Word Input: WordInput

### Step 6: Add Persistent Managers

1. Create UIManager GameObject → Add UIManager script
2. Create CoinSystemObject → Add CoinSystem script
3. Create AdManagerObject → Add AdManager script
4. Create IAPManagerObject → Add IAPManager script
5. Create PlayerDataManagerObject → Add PlayerDataManager script

### Step 7: Configure GameManager

1. Select GameManager
2. GameController component: no changes
3. PuzzleShowMode component: Initialize() called in Start()

### Step 8: Save and Add to Build Settings

1. File → Save Scene
2. Build Settings → Add PuzzleShowMode.unity
3. Set as **Scene 2**

---

## Scene 4: TimeAttackMode.unity

**Purpose:** Time-based mode where puzzles get progressively harder (time limit decreases each round).

### Step 1: Create the Scene

1. In Assets/Scenes/, create new scene
2. Name it `TimeAttackMode`
3. Open it

### Step 2: Create Game Structure

1. Create empty GameObject: `GameManager`
2. Add Components:
   - GameController
   - TimeAttackMode

### Step 3: Create Canvas and UI

1. Create Canvas (UI → Canvas)
2. Canvas Scaler: Scale With Screen Size (1080 x 1920)

### Step 4: Add UI Elements

**Timer Display** ⏱️ (CRITICAL for Time Attack)
1. Right-click Canvas → TextMeshPro - Text
2. Name: `TimerText`
3. Set Text to "Time: 90s"
4. Position: (0, 450, 0)
5. Font Size: 48
6. Text Color: Red (for urgency)
7. **IMPORTANT:** You'll need to write a script to update this with TimeAttackMode.GetTimeRemaining()

**Round Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `RoundText`
3. Set Text to "Round: 1"
4. Position: (0, 350, 0)
5. Font Size: 36

**Score Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `ScoreText`
3. Set Text to "Score: 0"
4. Position: (0, 250, 0)
5. Font Size: 36

**Words Display**
1. Right-click Canvas → TextMeshPro - Text
2. Name: `WordsText`
3. Set Text to "Words: "
4. Position: (-200, 150, 0)
5. Font Size: 24

**Word Input**
1. Right-click Canvas → Input Field - TextMeshPro
2. Name: `WordInput`
3. Position: (0, 100, 0)
4. Size: (400, 60)

**Submit Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `SubmitButton`
3. Text: "Submit"
4. Position: (0, -20, 0)
5. Size: (200, 60)

**Hint Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `HintButton`
3. Text: "Hint"
4. Position: (-150, -100, 0)
5. Size: (150, 50)

**Undo Button**
1. Right-click Canvas → Button - TextMeshPro
2. Name: `UndoButton`
3. Text: "Undo"
4. Position: (150, -100, 0)
5. Size: (150, 50)

### Step 5: Add GameplayScreen Script

1. Select Canvas
2. Add Component: GameplayScreen
3. Assign references:
   - Game Controller: GameManager
   - Submit Button: SubmitButton
   - Hint Button: HintButton
   - Undo Button: UndoButton
   - Score Text: ScoreText
   - Words Text: WordsText
   - Word Input: WordInput

### Step 6: Add Timer Update Script (OPTIONAL)

For the TimerText to update in real-time, you could optionally create a small script:

```csharp
// Assets/Scripts/UI/TimerDisplay.cs
using UnityEngine;
using TMPro;

public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private TimeAttackMode timeAttackMode;

    private void Start()
    {
        timeAttackMode = FindObjectOfType<TimeAttackMode>();
    }

    private void Update()
    {
        if (timeAttackMode != null)
        {
            float timeRemaining = timeAttackMode.GetTimeRemaining();
            timerText.text = $"Time: {timeRemaining:F1}s";
            
            // Change color based on time
            if (timeRemaining < 10f)
                timerText.color = Color.red;
            else if (timeRemaining < 30f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.green;
        }
    }
}
```

Then attach this to TimerText GameObject.

### Step 7: Add Persistent Managers

1. Create UIManager → Add UIManager script
2. Create CoinSystemObject → Add CoinSystem script
3. Create AdManagerObject → Add AdManager script
4. Create IAPManagerObject → Add IAPManager script
5. Create PlayerDataManagerObject → Add PlayerDataManager script

### Step 8: Save and Add to Build Settings

1. File → Save Scene
2. Build Settings → Add TimeAttackMode.unity
3. Set as **Scene 3**

---

## Build Settings Configuration

After creating all 4 scenes, your Build Settings should look like:

```
File → Build Settings

Scenes In Build:
  0: Assets/Scenes/MainMenu.unity
  1: Assets/Scenes/ClassicMode.unity
  2: Assets/Scenes/PuzzleShowMode.unity
  3: Assets/Scenes/TimeAttackMode.unity

Platform: Android
```

### Platform Settings

1. Switch to Android platform if not already
2. Player Settings (Edit → Project Settings → Player):
   - Company Name: Nicholas Garner
   - Product Name: WordPuzzle
   - Bundle Identifier: com.nicholasgarner.wordpuzzle
   - Target API Level: 34
   - Minimum API Level: 24

---

## Testing Your Scenes

### In Editor (Play Mode)

1. Open MainMenu scene
2. Press Play
3. Click Classic Mode button
4. Verify:
   - ClassicMode scene loads
   - GameController initializes
   - You can type words in input field
   - Submit button works

### Run All Tests

1. Window → General → Test Runner
2. Run Editor tests (all 25+ tests should pass)
3. Run PlayMode tests (integration tests should pass)

---

## Troubleshooting

**Problem:** Scripts show as missing in Inspector
- **Solution:** Ensure Assets/Scripts/ directory has correct structure and no compilation errors (Console tab)

**Problem:** Buttons don't respond
- **Solution:** Check that button references are assigned in MainMenuScreen/GameplayScreen Inspector

**Problem:** No text displaying
- **Solution:** Ensure TextMeshPro assets are imported (Window → TextMesh Pro → Import TMP Essential Resources)

**Problem:** CoinSystem not found at runtime
- **Solution:** Verify CoinSystemObject exists in scene and has CoinSystem component

---

## Next Steps

Once all scenes are created:

1. **Test in Editor:** Open MainMenu, play through each mode
2. **Build for Android:** File → Build Settings → Build
3. **Test on Device:** Install APK on Android phone, verify all features work
4. **Performance Profile:** Window → Analysis → Profiler (while in Play mode)

