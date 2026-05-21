# Word Puzzle Game — Unity UI Setup
## Complete Step-by-Step Guide for Beginners

---

## Before You Start — What These Words Mean

| Term | Plain English |
|---|---|
| **GameObject** | Any object that exists in your scene. Like a folder — it holds components. |
| **Component** | A script or Unity feature attached to a GameObject. A Button is a component. Your scripts are components. |
| **Inspector** | The panel on the **right side** of Unity. Shows you everything about whatever object you have selected. |
| **Hierarchy** | The panel on the **left side**. Lists every GameObject in the scene as a tree (with children indented under parents). |
| **Project panel** | The panel at the **bottom**. Shows your files on disk — scripts, images, prefabs. |
| **Canvas** | A special container that all UI must live inside. Think of it as your phone screen. Nothing appears on screen unless it is inside a Canvas. |
| **Prefab** | A saved, reusable GameObject stored as a file in your Assets folder. You build one, save it, then the game creates copies of it at runtime. |
| **Rect Transform** | The position and size settings for any UI element. Always appears at the very top of the Inspector when a UI object is selected. |
| **Anchor** | Tells an element where in its parent to measure its position from. You set this using the Anchor Preset grid. |
| **SerializeField / Inspector field** | A variable in a script that shows up as a drag-target box in the Inspector. You fill these by dragging GameObjects into them. |

---

## How Positioning Works — Read This Once

Every UI element has a **Rect Transform** at the top of its Inspector. You set two things:

**1. The Anchor** — click the small square icon in the top-left of Rect Transform to open the Anchor Preset grid.
- For almost everything: click the **center dot** (Middle Center). Position is then measured from the center of the parent.
- For panels that should fill their parent: hold **Alt** and click the **bottom-right corner** of the grid (has 4 outward arrows). This stretches the element to fill its parent completely.

**2. The Numbers** — after setting the anchor, type these four values:

| Value | What it does |
|---|---|
| **Pos X** | Left/right from anchor point. Negative = left, positive = right, 0 = center. |
| **Pos Y** | Up/down from anchor point. Negative = down, positive = up, 0 = center. |
| **Width** | How wide the element is in pixels. |
| **Height** | How tall the element is in pixels. |

All positions in this guide are for a **1080 × 1920 canvas** (portrait phone). The center of the canvas is 0, 0. The top edge is around Y=960 and the bottom edge is around Y=−960.

---

## The Final Hierarchy You Are Building

By the end of this guide your Hierarchy will look exactly like this:

```
Scene
├── EventSystem          ← auto-created, never touch it
├── Bootstrap            ← runs the game, no visuals
└── Canvas               ← holds all UI
    ├── MainMenuScreen   ← first screen player sees
    │   ├── TitleText
    │   ├── ClassicModeButton
    │   ├── PuzzleShowButton
    │   └── TimeAttackButton
    ├── GameplayScreen   ← the puzzle screen
    │   ├── LivesText
    │   ├── ScoreText
    │   ├── WordChainDisplay
    │   │   └── Container
    │   ├── CurrentWordInput
    │   │   ├── InputText
    │   │   └── TargetText
    │   ├── HintButton
    │   ├── RevealButton
    │   ├── UndoButton
    │   ├── KeyboardContainer
    │   ├── SubmitButton
    │   ├── DeleteButton
    │   ├── WinOverlay
    │   │   └── WinText
    │   └── LossOverlay
    │       └── LossText
    ├── ResultsScreen    ← shows stats after game ends
    │   ├── ModeNameText
    │   ├── ScoreText
    │   ├── CoinsEarnedText
    │   ├── TimeText
    │   ├── NextButton
    │   └── MenuButton
    └── TimerDisplay     ← countdown timer (Time Attack only)
        └── TimerText

Assets/Prefabs/
├── LetterTile.prefab    ← one keyboard button (26 created at runtime)
└── WordLabel.prefab     ← one word in the chain display
```

---

---

# PART 1 — FOUNDATION

---

## Step 1 — Open Unity and the Scene

1. Open **Unity Hub**
2. Click your **WordPuzzleGame** project to open it. Wait for it to fully load (can take 1–2 minutes the first time).
3. In the **Project panel** at the bottom, navigate to `Assets → Scenes`
4. **Double-click `MainMenu.unity`** to open it
5. Look at the **Hierarchy** panel on the left
6. If you see any old GameObjects from a previous setup (things like `GameController`, `Canvas` with old content, etc.) — right-click each one → **Delete**. You want a clean scene.

---

## Step 2 — Create the Canvas

The Canvas is the screen everything lives inside. Do this first.

1. In the Hierarchy, **right-click on empty space → UI → Canvas**
2. Two objects appear automatically: `Canvas` and `EventSystem`
   > **Never delete EventSystem.** It handles mouse clicks and touch input. The game stops working without it.
3. Click `Canvas` in the Hierarchy
4. In the Inspector, find the **Canvas Scaler** component (scroll down if needed)
5. Change **UI Scale Mode** to `Scale With Screen Size`
6. Set **Reference Resolution** to **X: 1080** and **Y: 1920**
7. Set the **Match** slider to **0.5**

> This makes all your UI scale proportionally on any screen size.

---

## Step 3 — Create the Bootstrap (Manager) Object

Bootstrap is an invisible object that starts the game, creates all the services, and connects all the screens. It has no visual appearance.

1. In the Hierarchy, **right-click on empty space** (not inside Canvas) → **Create Empty**
2. Rename it `Bootstrap`
   > To rename: double-click the name in the Hierarchy, or single-click it in the Inspector's name field at the top.
3. With `Bootstrap` selected, look at the Inspector on the right
4. Click **Add Component** at the bottom of the Inspector
5. Type `GameBootstrap` in the search box → click it to add it
6. Click **Add Component** again → type `ModeController` → add it
7. Click **Add Component** again → type `UIManager` → add it

> Bootstrap does not need any position — it has no visuals. Leave its Transform at 0, 0, 0.

---

---

# PART 2 — THE THREE SCREENS

---

## Step 4 — Create the Main Menu Screen

This is the first screen the player sees when the game starts.

### 4-1. The background panel

1. Right-click **`Canvas`** in the Hierarchy → **UI → Panel**
2. Rename it `MainMenuScreen`
3. Select it, open the **Anchor Preset** grid (click the small square in the top-left of Rect Transform)
4. Hold **Alt** and click the **bottom-right corner** of the grid (4 outward arrows) — this stretches the panel to fill the whole canvas
5. In the Inspector find the **Image** component
6. Click the **Color** field → set: **R: 20, G: 30, B: 60, A: 255** (dark blue background)
7. Click **Add Component** → type `MainMenuScreen` → add it

### 4-2. Title text

1. Right-click `MainMenuScreen` → **UI → Text - TextMeshPro**
   > If a dialog pops up saying "Import TMP Essentials", click that button, wait for it to finish, then close the dialog. Only happens once.
2. Rename it `TitleText`
3. In the **TextMeshPro - Text (UI)** component, set the text to: `Word Puzzle Game`
4. Set **Font Size** to `80`
5. Click the center-horizontal and center-vertical alignment icons so text is centered
6. **Rect Transform:**
   - Anchor: **Middle Center** (click center dot)
   - Pos X: `0` | Pos Y: `700` | Width: `800` | Height: `120`

### 4-3. Classic Mode button

1. Right-click `MainMenuScreen` → **UI → Button - TextMeshPro**
2. Rename it `ClassicModeButton`
3. Click the **arrow** next to it in the Hierarchy to expand it → click the child named `Text (TMP)`
4. Set the text to: `Classic Mode` | Font Size: `50` | Alignment: centered
5. Click back on `ClassicModeButton` (the parent)
6. In the Inspector find the **Image** component → set Color to: **R: 70, G: 130, B: 200, A: 255** (blue button)
7. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `250` | Width: `500` | Height: `100`

### 4-4. Puzzle Show button

1. Right-click `MainMenuScreen` → **UI → Button - TextMeshPro**
2. Rename it `PuzzleShowButton`
3. Expand → set child text to: `Puzzle Show` | Font Size: `50` | centered
4. Click parent `PuzzleShowButton` → Image Color: **R: 130, G: 80, B: 200, A: 255** (purple button)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `110` | Width: `500` | Height: `100`

### 4-5. Time Attack button

1. Right-click `MainMenuScreen` → **UI → Button - TextMeshPro**
2. Rename it `TimeAttackButton`
3. Expand → set child text to: `Time Attack` | Font Size: `50` | centered
4. Click parent `TimeAttackButton` → Image Color: **R: 200, G: 80, B: 60, A: 255** (red button)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `-30` | Width: `500` | Height: `100`

---

## Step 5 — Create the Gameplay Screen

This is where the puzzle is played. It has the word chain, keyboard, and all the action buttons.

### 5-1. The background panel

1. Right-click **`Canvas`** → **UI → Panel**
2. Rename it `GameplayScreen`
3. Anchor: hold **Alt** + click **bottom-right** (stretch fill)
4. Image Color: **R: 15, G: 15, B: 25, A: 255** (near-black)
5. Click **Add Component** → `GameplayScreen`

### 5-2. Lives text

1. Right-click `GameplayScreen` → **UI → Text - TextMeshPro**
2. Rename it `LivesText`
3. Set text to: `Lives: 3` | Font Size: `45` | left-aligned
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `-350` | Pos Y: `850` | Width: `250` | Height: `60`

### 5-3. Score text

1. Right-click `GameplayScreen` → **UI → Text - TextMeshPro**
2. Rename it `ScoreText`
3. Set text to: `Steps: 0` | Font Size: `45` | right-aligned
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `350` | Pos Y: `850` | Width: `250` | Height: `60`

### 5-4. Word chain display

This shows the growing list of words the player has entered (e.g. CAT → BAT → HAT).

1. Right-click `GameplayScreen` → **Create Empty**
2. Rename it `WordChainDisplay`
3. Click **Add Component** → `WordChainDisplay`
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `600` | Width: `900` | Height: `200`

**Create the Container inside it:**

5. Right-click `WordChainDisplay` → **Create Empty**
6. Rename it `Container`
7. Select `Container` → Anchor: hold **Alt** + click **bottom-right** (stretch fill inside its parent)
8. Click **Add Component** → type `Vertical Layout Group` → add it
9. In the Vertical Layout Group component:
   - **Child Alignment**: `Upper Center`
   - Leave everything else at default

**Wire the WordChainDisplay component now** (while it is fresh):

10. Click `WordChainDisplay` in the Hierarchy
11. Find the **WordChainDisplay** component in the Inspector
12. Drag `Container` from the Hierarchy into the **Container** field
    > Leave the **Word Prefab** field empty for now — you will fill it in Step 9.

### 5-5. Current word input display

This shows what the player is currently typing and the target word they are aiming for.

1. Right-click `GameplayScreen` → **Create Empty**
2. Rename it `CurrentWordInput`
3. Click **Add Component** → `CurrentWordInput`
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `350` | Width: `900` | Height: `150`

**Input text** (what the player typed so far):

5. Right-click `CurrentWordInput` → **UI → Text - TextMeshPro**
6. Rename it `InputText`
7. Set text to: *(leave blank)* | Font Size: `65` | centered
8. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `35` | Width: `880` | Height: `75`

**Target text** (the goal word):

9. Right-click `CurrentWordInput` → **UI → Text - TextMeshPro**
10. Rename it `TargetText`
11. Set text to: `Target: ?` | Font Size: `40` | centered
12. **Rect Transform:**
    - Anchor: **Middle Center**
    - Pos X: `0` | Pos Y: `-45` | Width: `880` | Height: `60`

**Wire the CurrentWordInput component now:**

13. Click `CurrentWordInput` in the Hierarchy
14. Find the **CurrentWordInput** component in the Inspector
15. Drag `InputText` from the Hierarchy into the **Input Text** field
16. Drag `TargetText` from the Hierarchy into the **Target Text** field

### 5-6. Hint button

1. Right-click `GameplayScreen` → **UI → Button - TextMeshPro**
2. Rename it `HintButton`
3. Expand → set child text to: `Hint` | Font Size: `40` | centered
4. Click parent `HintButton` → Image Color: **R: 200, G: 160, B: 30, A: 255** (gold)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `-290` | Pos Y: `150` | Width: `200` | Height: `80`

### 5-7. Reveal button

1. Right-click `GameplayScreen` → **UI → Button - TextMeshPro**
2. Rename it `RevealButton`
3. Expand → set child text to: `Reveal` | Font Size: `40` | centered
4. Click parent `RevealButton` → Image Color: **R: 60, G: 160, B: 60, A: 255** (green)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `150` | Width: `200` | Height: `80`

### 5-8. Undo button

1. Right-click `GameplayScreen` → **UI → Button - TextMeshPro**
2. Rename it `UndoButton`
3. Expand → set child text to: `Undo` | Font Size: `40` | centered
4. Click parent `UndoButton` → Image Color: **R: 100, G: 100, B: 100, A: 255** (grey)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `290` | Pos Y: `150` | Width: `200` | Height: `80`

### 5-9. Keyboard container

The game will automatically fill this with 26 letter buttons at runtime. You just need the container with the right layout settings.

1. Right-click `GameplayScreen` → **Create Empty**
2. Rename it `KeyboardContainer`
3. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `-170` | Width: `1000` | Height: `320`
4. Click **Add Component** → type `Grid Layout Group` → add it
5. Set these values in the Grid Layout Group:
   - **Cell Size**: X = `100`, Y = `100`
   - **Spacing**: X = `2`, Y = `2`
   - **Start Corner**: `Upper Left`
   - **Constraint**: `Fixed Column Count`
   - **Constraint Count**: `10`

> This makes the 26 letter buttons flow into 3 rows (10 / 10 / 6) automatically.

### 5-10. Submit button

1. Right-click `GameplayScreen` → **UI → Button - TextMeshPro**
2. Rename it `SubmitButton`
3. Expand → set child text to: `Submit` | Font Size: `48` | centered
4. Click parent `SubmitButton` → Image Color: **R: 50, G: 180, B: 100, A: 255** (green)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `-200` | Pos Y: `-560` | Width: `340` | Height: `100`

### 5-11. Delete button

1. Right-click `GameplayScreen` → **UI → Button - TextMeshPro**
2. Rename it `DeleteButton`
3. Expand → set child text to: `Delete ←` | Font Size: `48` | centered
4. Click parent `DeleteButton` → Image Color: **R: 180, G: 60, B: 60, A: 255** (red)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `200` | Pos Y: `-560` | Width: `340` | Height: `100`

### 5-12. Win overlay

This covers the whole screen in green when the player wins.

1. Right-click `GameplayScreen` → **UI → Panel**
2. Rename it `WinOverlay`
3. Anchor: hold **Alt** + click **bottom-right** (stretch fill — covers the entire screen)
4. Image Color: **R: 0, G: 180, B: 80, A: 210** (semi-transparent green)
5. Right-click `WinOverlay` → **UI → Text - TextMeshPro**
6. Rename it `WinText`
7. Set text to: `YOU WIN!` | Font Size: `90` | centered | Color: white
8. **Rect Transform on WinText:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `100` | Width: `800` | Height: `150`

### 5-13. Loss overlay

This covers the whole screen in red when the player runs out of lives.

1. Right-click `GameplayScreen` → **UI → Panel**
2. Rename it `LossOverlay`
3. Anchor: hold **Alt** + click **bottom-right** (stretch fill)
4. Image Color: **R: 180, G: 30, B: 30, A: 210** (semi-transparent red)
5. Right-click `LossOverlay` → **UI → Text - TextMeshPro**
6. Rename it `LossText`
7. Set text to: `GAME OVER` | Font Size: `90` | centered | Color: white
8. **Rect Transform on LossText:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `100` | Width: `800` | Height: `150`

---

## Step 6 — Create the Results Screen

This appears after a game ends showing your score, coins, and time.

### 6-1. The background panel

1. Right-click **`Canvas`** → **UI → Panel**
2. Rename it `ResultsScreen`
3. Anchor: hold **Alt** + click **bottom-right** (stretch fill)
4. Image Color: **R: 20, G: 20, B: 40, A: 255** (dark purple-black)
5. Click **Add Component** → `ResultsScreen`

### 6-2. Mode name text

1. Right-click `ResultsScreen` → **UI → Text - TextMeshPro**
2. Rename it `ModeNameText`
3. Set text to: `Classic Mode` | Font Size: `70` | centered
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `600` | Width: `800` | Height: `100`

### 6-3. Score text

1. Right-click `ResultsScreen` → **UI → Text - TextMeshPro**
2. Rename it `ScoreText`
3. Set text to: `Puzzles: 0` | Font Size: `55` | centered
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `400` | Width: `700` | Height: `80`

### 6-4. Coins earned text

1. Right-click `ResultsScreen` → **UI → Text - TextMeshPro**
2. Rename it `CoinsEarnedText`
3. Set text to: `Coins: +0` | Font Size: `55` | centered
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `300` | Width: `700` | Height: `80`

### 6-5. Time text

1. Right-click `ResultsScreen` → **UI → Text - TextMeshPro**
2. Rename it `TimeText`
3. Set text to: `Time: 0s` | Font Size: `55` | centered
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `200` | Width: `700` | Height: `80`

### 6-6. Play Again button

1. Right-click `ResultsScreen` → **UI → Button - TextMeshPro**
2. Rename it `NextButton`
3. Expand → set child text to: `Play Again` | Font Size: `50` | centered
4. Click parent `NextButton` → Image Color: **R: 50, G: 180, B: 100, A: 255** (green)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `-210` | Pos Y: `-500` | Width: `380` | Height: `100`

### 6-7. Main Menu button

1. Right-click `ResultsScreen` → **UI → Button - TextMeshPro**
2. Rename it `MenuButton`
3. Expand → set child text to: `Main Menu` | Font Size: `50` | centered
4. Click parent `MenuButton` → Image Color: **R: 70, G: 130, B: 200, A: 255** (blue)
5. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `210` | Pos Y: `-500` | Width: `380` | Height: `100`

---

## Step 7 — Create the Timer Display

This shows a live countdown in Time Attack mode. It hides itself automatically in Classic and Puzzle Show modes.

1. Right-click **`Canvas`** → **Create Empty**
2. Rename it `TimerDisplay`
3. Click **Add Component** → `TimerDisplay`
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `850` | Width: `350` | Height: `70`
5. Right-click `TimerDisplay` → **UI → Text - TextMeshPro**
6. Rename it `TimerText`
7. Set text to: `Time: 60.0s` | Font Size: `55` | centered | Color: green (R:100, G:220, B:100)
8. Select `TimerText` → Anchor: hold **Alt** + click **bottom-right** (stretch fill inside TimerDisplay)

**Wire the TimerDisplay component now:**

9. Click `TimerDisplay` in the Hierarchy
10. Find the **TimerDisplay** component
11. Drag `TimerText` from the Hierarchy into the **Timer Text** field

---

---

# PART 3 — PREFABS

---

## Step 8 — Create the LetterTile Prefab

The keyboard is built automatically at runtime — 26 copies of this prefab are created, one per letter. You build one, save it as a prefab, then delete the scene copy.

### 8-1. Create a Prefabs folder

1. In the **Project panel** at the bottom, click on `Assets` to go to the root
2. Right-click in empty space → **Create → Folder**
3. Name it `Prefabs`

### 8-2. Build the tile

1. Right-click **`Canvas`** → **UI → Button - TextMeshPro**
2. Rename it `LetterTile`
3. **Rect Transform:**
   - Anchor: **Middle Center**
   - Pos X: `0` | Pos Y: `0` | Width: `96` | Height: `96`
4. Click **Add Component** → `LetterTile`
5. The Button already has a child object called `Text (TMP)` — click it and rename it `LetterText`
6. Set its text to: `A` | Font Size: `50` | centered | Color: black (so it shows on white button)

### 8-3. Wire the LetterTile component

1. Click `LetterTile` (the parent, not LetterText) in the Hierarchy
2. In the Inspector find the **LetterTile** component — it has three empty fields:
   - **Button**
   - **Letter Text**
   - **Background**

3. **For the Button field:**
   Click the small circle icon to the right of the Button field → a picker window opens → find and double-click **Button** in the list (it is the Button component on this same GameObject, listed as `LetterTile`)

4. **For the Letter Text field:**
   Drag `LetterText` (the child object) from the Hierarchy into this field

5. **For the Background field:**
   Look at the Inspector — `LetterTile` has an **Image** component on it (this acts as the background color). Drag the `LetterTile` **GameObject itself** from the Hierarchy into the Background field.

### 8-4. Save as a prefab

1. In the **Project panel**, navigate to `Assets/Prefabs`
2. Drag `LetterTile` from the **Hierarchy** into the `Assets/Prefabs` folder in the Project panel
3. A dialog appears — click **Original Prefab**
4. The `LetterTile` in the Hierarchy now has a blue icon (blue = it is a prefab instance)
5. Right-click `LetterTile` in the Hierarchy → **Delete**

> The prefab is safely saved in Assets/Prefabs. Deleting the scene copy is correct.

---

## Step 9 — Create the Word Label Prefab

WordChainDisplay uses this to display each word in the chain as the player progresses.

1. Right-click **`WordChainDisplay`** in the Hierarchy → **UI → Text - TextMeshPro**
2. Rename it `WordLabel`
3. Set text to: `WORD` | Font Size: `55` | centered | Color: white
4. **Rect Transform:**
   - Anchor: **Middle Center**
   - Width: `800` | Height: `70`
5. Drag `WordLabel` from the **Hierarchy** into `Assets/Prefabs` in the Project panel → choose **Original Prefab**
6. Right-click `WordLabel` in the Hierarchy → **Delete**

---

---

# PART 4 — WIRING (CONNECTING EVERYTHING)

---

## Step 10 — Fill In All Inspector Fields

This is where everything connects. For each object below, click it in the Hierarchy, find the component in the Inspector, and drag the correct objects into the empty fields.

> **How to drag into a field:** grab the target object from the Hierarchy and drop it onto the empty field box. Alternatively, click the small circle icon next to any field to open a picker window, then double-click the object you want.

---

### 10-1. Bootstrap

Click `Bootstrap` in the Hierarchy. It has three components. Fill each:

**GameBootstrap component:**

| Field | Drag this in |
|---|---|
| Mode Controller | `Bootstrap` (drag Bootstrap itself — ModeController is a component on it) |
| Ui Manager | `Bootstrap` (drag Bootstrap itself — UIManager is a component on it) |
| Gameplay Screen | `GameplayScreen` (inside Canvas in Hierarchy) |
| Main Menu Screen | `MainMenuScreen` (inside Canvas) |
| Results Screen | `ResultsScreen` (inside Canvas) |

> Yes — you drag Bootstrap into its own fields. ModeController, UIManager, and GameBootstrap all live on the same Bootstrap object, so Bootstrap references itself.

**ModeController component:**

| Field | Drag this in |
|---|---|
| Timer Display | `TimerDisplay` (inside Canvas) |

**UIManager component:** no fields to fill.

---

### 10-2. MainMenuScreen

Click `MainMenuScreen`. Find the **MainMenuScreen** component:

| Field | Drag this in |
|---|---|
| Classic Mode Button | `ClassicModeButton` (child of MainMenuScreen) |
| Puzzle Show Button | `PuzzleShowButton` |
| Time Attack Button | `TimeAttackButton` |
| Shop Button | leave empty |
| Settings Button | leave empty |

---

### 10-3. GameplayScreen

Click `GameplayScreen`. Find the **GameplayScreen** component:

| Field | Drag this in |
|---|---|
| Word Chain Display | `WordChainDisplay` |
| Current Word Input | `CurrentWordInput` |
| Keyboard Container | `KeyboardContainer` |
| Letter Tile Prefab | The `LetterTile` file from `Assets/Prefabs` — **drag from the Project panel, not the Hierarchy** |
| Lives Text | `LivesText` |
| Score Text | `ScoreText` |
| Submit Button | `SubmitButton` |
| Hint Button | `HintButton` |
| Reveal Button | `RevealButton` |
| Undo Button | `UndoButton` |
| Delete Button | `DeleteButton` |
| Win Overlay | `WinOverlay` |
| Loss Overlay | `LossOverlay` |

---

### 10-4. WordChainDisplay

Click `WordChainDisplay`. Find the **WordChainDisplay** component:

| Field | Drag this in |
|---|---|
| Container | `Container` (child of WordChainDisplay) |
| Word Prefab | The `WordLabel` file from `Assets/Prefabs` — **drag from Project panel** |

---

### 10-5. ResultsScreen

Click `ResultsScreen`. Find the **ResultsScreen** component:

| Field | Drag this in |
|---|---|
| Mode Name Text | `ModeNameText` |
| Score Text | `ScoreText` |
| Coins Earned Text | `CoinsEarnedText` |
| Time Text | `TimeText` |
| Next Button | `NextButton` |
| Menu Button | `MenuButton` |

---

---

# PART 5 — TEST IT

---

## Step 11 — Save and Press Play

1. Press **Ctrl+S** to save the scene
2. Check the bottom of the screen — if it shows `Assembly-CSharp (compiling...)` wait for it to finish
3. Press the **Play button** (triangle at the top center of Unity)

**What you should see — check each item:**

| What happens | What it confirms |
|---|---|
| Main Menu screen appears immediately | GameBootstrap connected correctly |
| Three coloured buttons visible | MainMenuScreen wired correctly |
| Click **Classic Mode** → Gameplay Screen appears | ModeController and UIManager working |
| 26 letter buttons appear in 3 rows | LetterTile prefab wired, Grid Layout working |
| Click a letter → it appears in the input area at the top | GameplayScreen subscribed to state |
| Click **Delete** → last letter removed | DeleteLetterAction dispatching |
| Type `BAT` (after starting word `CAT`) → click **Submit** → BAT added to chain | Word validation working |
| Type an invalid word → click Submit → lose a life | Lives counter working |
| Reach the target word → green overlay appears | Win condition working |
| Lose all 3 lives → red overlay appears | Loss condition working |
| After win/loss → Results Screen appears | ModeCompleted event firing |
| Click **Play Again** → new game starts | ResultsScreen wired to ModeController |
| Click **Main Menu** → back to menu | UIManager ShowScreen working |
| Start **Time Attack** → timer counts down with color changes | TimerDisplay bound to mode |

---

## Step 12 — Troubleshooting

**Nothing appears / black screen:**
- Open `Window → General → Console`
- Look for red errors. They almost always say `NullReferenceException` and name a script
- Find that script's component in the Hierarchy and check every field — an empty field is the cause

**Keyboard does not appear:**
- The `Letter Tile Prefab` field on GameplayScreen must point to the **prefab file** in `Assets/Prefabs`
- If you accidentally dragged from the Hierarchy instead of the Project panel, clear the field (right-click → Clear) and drag again from the Project panel bottom

**Buttons do nothing when clicked:**
- Open the Console — you will likely see `[MainMenuScreen] ModeController not injected`
- Go back to Step 10-1 and make sure all 5 GameBootstrap fields are filled

**Timer shows during Classic Mode:**
- The TimerDisplay should be a direct child of Canvas, not inside GameplayScreen
- It hides itself automatically when not in Time Attack mode — but only if `Timer Display` is filled on the ModeController component (Step 10-1)

**"Screen X not registered" warning:**
- One of the screen drag-targets in GameBootstrap is empty
- Go to Bootstrap, check all 5 GameBootstrap fields

**Words not being accepted:**
- Open the Console — if you see `Word not in dictionary`, the word is not in the built-in test word list
- Try simple 3-letter words: `bat`, `hat`, `mat`, `can`, `tan`, `dog`, `fog`, `log`

**The scene goes black when you stop playing:**
- This is normal — Unity hides everything when you exit Play mode
- Your scene is still there, just press Play again

---

## Quick Layout Reference

```
┌──────────────── Canvas 1080 × 1920 ─────────────────┐
│                                                       │
│ Lives:3 (-350,850) [Timer: 60s] (0,850) Steps:0(350) │
│                                                       │
│      ┌──────── WordChainDisplay (0, 600) ──────┐     │
│      │         CAT  →  BAT  →  HAT             │     │
│      └────────────────────────────────────────┘     │
│                                                       │
│      ┌──────── CurrentWordInput  (0, 350) ──────┐    │
│      │              H A T                        │    │
│      │           Target: DOG                     │    │
│      └────────────────────────────────────────┘     │
│                                                       │
│        [Hint]      [Reveal]      [Undo]  (y=150)     │
│                                                       │
│   ┌──────── KeyboardContainer   (0, -170) ────────┐  │
│   │  [A][B][C][D][E][F][G][H][I][J]   ← row 1    │  │
│   │  [K][L][M][N][O][P][Q][R][S][T]   ← row 2    │  │
│   │  [U][V][W][X][Y][Z]               ← row 3    │  │
│   └────────────────────────────────────────────────┘  │
│                                                       │
│         [ Submit ]          [ Delete ← ]  (y=-560)   │
│                                                       │
└───────────────────────────────────────────────────────┘
```

---

## Complete Position Reference Card

| Screen | Element | Anchor | Pos X | Pos Y | Width | Height |
|---|---|---|---|---|---|---|
| MainMenu | Panel | Stretch fill | — | — | — | — |
| MainMenu | TitleText | Middle Center | 0 | 700 | 800 | 120 |
| MainMenu | ClassicModeButton | Middle Center | 0 | 250 | 500 | 100 |
| MainMenu | PuzzleShowButton | Middle Center | 0 | 110 | 500 | 100 |
| MainMenu | TimeAttackButton | Middle Center | 0 | -30 | 500 | 100 |
| Gameplay | Panel | Stretch fill | — | — | — | — |
| Gameplay | LivesText | Middle Center | -350 | 850 | 250 | 60 |
| Gameplay | ScoreText | Middle Center | 350 | 850 | 250 | 60 |
| Gameplay | WordChainDisplay | Middle Center | 0 | 600 | 900 | 200 |
| Gameplay | Container (inside WordChainDisplay) | Stretch fill | — | — | — | — |
| Gameplay | CurrentWordInput | Middle Center | 0 | 350 | 900 | 150 |
| Gameplay | InputText (inside CurrentWordInput) | Middle Center | 0 | 35 | 880 | 75 |
| Gameplay | TargetText (inside CurrentWordInput) | Middle Center | 0 | -45 | 880 | 60 |
| Gameplay | HintButton | Middle Center | -290 | 150 | 200 | 80 |
| Gameplay | RevealButton | Middle Center | 0 | 150 | 200 | 80 |
| Gameplay | UndoButton | Middle Center | 290 | 150 | 200 | 80 |
| Gameplay | KeyboardContainer | Middle Center | 0 | -170 | 1000 | 320 |
| Gameplay | SubmitButton | Middle Center | -200 | -560 | 340 | 100 |
| Gameplay | DeleteButton | Middle Center | 200 | -560 | 340 | 100 |
| Gameplay | WinOverlay | Stretch fill | — | — | — | — |
| Gameplay | WinText (inside WinOverlay) | Middle Center | 0 | 100 | 800 | 150 |
| Gameplay | LossOverlay | Stretch fill | — | — | — | — |
| Gameplay | LossText (inside LossOverlay) | Middle Center | 0 | 100 | 800 | 150 |
| Results | Panel | Stretch fill | — | — | — | — |
| Results | ModeNameText | Middle Center | 0 | 600 | 800 | 100 |
| Results | ScoreText | Middle Center | 0 | 400 | 700 | 80 |
| Results | CoinsEarnedText | Middle Center | 0 | 300 | 700 | 80 |
| Results | TimeText | Middle Center | 0 | 200 | 700 | 80 |
| Results | NextButton | Middle Center | -210 | -500 | 380 | 100 |
| Results | MenuButton | Middle Center | 210 | -500 | 380 | 100 |
| Canvas | TimerDisplay | Middle Center | 0 | 850 | 350 | 70 |
| Canvas | TimerText (inside TimerDisplay) | Stretch fill | — | — | — | — |
