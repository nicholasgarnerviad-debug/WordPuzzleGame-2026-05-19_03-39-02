# Word Puzzle Game UI Overhaul Design
**Date:** 2026-05-21  
**Scope:** Complete redesign of game UI to match Word Connect aesthetic  
**Target Platform:** Mobile Portrait Only  
**Status:** Design Approved

---

## Design Overview

Complete redesign of the game UI to match Word Connect's satisfying, colorful aesthetic while preserving all existing game logic. The UI overhaul affects only the visual presentation layer; game mechanics remain unchanged.

### Approach: UI Overhaul Only
- **Keep:** Existing game logic (ClassicMode, TimeAttackMode, PuzzleShowMode)
- **Keep:** Game state management and services
- **Redesign:** All UI screens and visual components
- **Rationale:** Game logic is tested and working; UI redesign is purely a visual layer change with minimal risk

---

## Screen Designs

### 1. Main Menu Screen

**Purpose:** Entry point where players select which game mode to play.

**Layout & Components:**
- Game title at top
- 3 large mode selection buttons stacked vertically (or grid layout based on space):
  - **Classic Mode** button (bright teal/cyan #00bcd4)
  - **Puzzle Show Mode** button (vibrant purple/magenta #e91e63)
  - **Time Attack Mode** button (warm orange/coral #ff6b6b)
- Dark navy background (#1a1a2e)
- Buttons are large (~80-100px height) for easy mobile tapping

**Interactions:**
- Buttons scale and shift color slightly on tap (feedback animation)
- Clicking any button immediately calls `ModeController.SwitchMode(modeType)`
- Smooth fade transition to Gameplay Screen

**Data Requirements:**
- None (stateless screen)

---

### 2. Gameplay Screen

**Purpose:** Main game play area where players interact with word tiles and build words.

**Layout & Components (top to bottom):**

1. **Header Bar**
   - Game mode name (e.g., "Classic Mode")
   - Score display (large, prominent, right-aligned)
   - Timer display (only visible in Time Attack mode)
   - Back/Menu button (small, top-right corner)

2. **Current Word Input Section**
   - Text input field showing word being typed
   - Clear button (resets current word)
   - Submit button (validates and submits word)

3. **Letter Tiles Grid (Main Content Area)**
   - Colorful tiles displaying available letters or word suggestions
   - Large, vibrant, rounded square tiles (Word Connect style)
   - Tap tiles to build/modify current word
   - Visual feedback: scale animation when tapping
   - Tiles use game mode colors (teal for Classic, purple for Puzzle Show, orange for Time Attack)

4. **Word Chain Display**
   - Shows all previously found valid words in scrollable list
   - Each word shows points earned
   - Updates in real-time as new words are found

5. **Game Status Area (Bottom)**
   - Current streak display
   - Words remaining (if applicable to mode)
   - End Game button (for Time Attack early exit)

**Interactions:**
- Tap letter tiles to build words
- Tap Submit to validate word
- Invalid words show brief error message
- Valid words animate (scale/fade) as they add to chain
- Score updates with satisfying animation
- All animations smooth (300-500ms duration)

**Data Flow:**
- GameStateManager provides current game state
- UI updates reactively to state changes
- Word validation happens in game logic
- Score updates propagate immediately to UI

---

### 3. Results Screen

**Purpose:** Display game completion stats and allow replaying or returning to menu.

**Layout & Components (top to bottom):**

1. **Header**
   - "Game Complete!" title
   - Final score (large, bold, prominent display)

2. **Detailed Statistics (scrollable if needed)**
   - Final Score: Total points earned
   - Game Duration: Time elapsed
   - Words Found: Count of valid words
   - Accuracy: Percentage of valid attempts
   - Best Word: Highest-scoring single word
   - Current Streak: Streak at game end
   - Longest Streak: Best streak in this game

3. **Action Buttons (Bottom)**
   - **Play Again**: Restart same mode (new game)
   - **Main Menu**: Return to mode selection

**Design Details:**
- Dark background (consistent with game)
- Stats in clean, readable list format
- Each stat clearly labeled with icon or color accent
- Button colors match current game mode

**Interactions:**
- Smooth fade-in animation when screen appears
- Button tap animations (scale/color shift)
- Play Again → immediately starts new game in same mode
- Main Menu → transitions back to Main Menu

**Data Requirements:**
- Game completion stats from GameStateManager
- Accuracy calculation: (valid_words / total_attempts) × 100
- Best word identification from word list

---

## Visual Design System

### Color Palette

**Background:**
- Primary Dark: #1a1a2e (dark navy) - used for all screen backgrounds

**Game Mode Colors (Primary):**
- Classic Mode: #00bcd4 (bright teal/cyan)
- Puzzle Show: #e91e63 (vibrant purple/magenta)
- Time Attack: #ff6b6b (warm orange/coral)

**Secondary Colors:**
- Light Text: #ffffff (white)
- Subtle Text: #cccccc (light gray)
- Accent Gold: #ffd700 (gold/yellow) - for highlights and animations
- Error/Invalid: #ff5252 (red) - for invalid word feedback

### Typography

- **Title/Score:** Bold sans-serif, 32-48px
- **Section Headers:** Bold sans-serif, 20-24px
- **Body Text:** Regular sans-serif, 14-16px
- **Button Text:** Bold sans-serif, 16-18px
- Font Family: System sans-serif (Roboto, Segoe UI, or equivalent)

### UI Elements

**Buttons:**
- Rounded corners (12px border radius)
- Minimum touch size: 48x48dp
- Solid color fills matching theme
- Tap animation: 0.3s scale (1.0 → 0.95 → 1.0) + color shift
- Feedback: haptic pulse on tap (if device supports)

**Tiles:**
- Rounded squares (8px border radius)
- Size: ~60-80px depending on grid layout
- Vibrant colors matching current game mode
- Tap animation: scale 1.0 → 1.1 → 1.0 over 0.3s
- Text: bold white, centered, large font

**Text Input:**
- Light border (subtle, ~2px)
- Dark background
- Light text color
- Clear visual hierarchy

**Animations:**
- All transitions: 300-500ms duration
- Easing: ease-out (smooth deceleration)
- Effects: scale, fade, color shift
- Word adding to chain: bounce scale (1.0 → 1.2 → 1.0) + fade in

### Mobile Optimization

- **Orientation:** Portrait only (no landscape)
- **Safe Areas:** Respect notches/safe areas on modern phones
- **Touch Targets:** Minimum 48x48dp tappable areas
- **Spacing:** 16px padding between major sections
- **Layout:** Vertical stack (top to bottom) for all screens
- **Font Sizes:** Scaled for legibility on small screens
- **Aspect Ratio:** Optimized for 9:16 to 9:20 (common mobile ratios)

---

## Data Flow

### Application Initialization
1. GameBootstrap.Awake() creates all services (DataManager, EconomyManager, GameStateManager, etc.)
2. UIManager registers all screens (MainMenuScreen, GameplayScreen, ResultsScreen)
3. Dependencies injected into screens via InjectDependencies()
4. MainMenuScreen displayed first

### Game Mode Selection Flow
1. User taps game mode button on MainMenuScreen
2. MainMenuScreen.StartMode(modeType) called
3. ModeController.SwitchMode(modeType) executed
4. Appropriate game mode initialized
5. UIManager.ShowScreen<GameplayScreen>() called
6. GameplayScreen displayed with active game

### Gameplay Flow
1. GameplayScreen receives updated game state
2. UI components update reactively:
   - Score display
   - Letter tiles
   - Word chain
   - Timer (Time Attack only)
3. Player interactions:
   - Tap tiles → current word updates
   - Tap Submit → word validation
   - Valid word → chain updates, score updates, new tiles
   - Invalid word → error feedback
4. Game continues until mode-specific end condition
5. ModeController fires ModeCompleted event with final stats

### Game Completion Flow
1. Game mode ends (time expired, puzzle solved, etc.)
2. ModeController fires ModeCompleted event
3. ResultsScreen.ShowResults(stats) called with final stats
4. UIManager.ShowScreen<ResultsScreen>() called
5. ResultsScreen displays detailed statistics
6. Player chooses:
   - **Play Again:** New game same mode (loop back to step 1)
   - **Main Menu:** Return to MainMenuScreen (reset and start over)

### State Management

**GameStateManager:**
- Holds current game state
- Tracks words found, score, streaks
- Validates word submissions
- Calculates accuracy statistics

**ModeController:**
- Manages game mode lifecycle
- Coordinates between game logic and UI
- Fires events on mode completion

**UIManager:**
- Registers and manages screen visibility
- Ensures only one screen active at time
- Coordinates screen transitions

---

## Testing Strategy

### Unit Tests (Game Logic)
- Word validation logic
- Score calculation accuracy
- Streak tracking
- Mode switching logic

### Integration Tests (Game + UI)
- Game mode initialization with UI
- Word submission and UI updates
- Score updates reflect in UI
- Results screen displays correct stats
- Mode switching and screen transitions
- All three modes work independently

### UI/Interaction Tests
- Button clicks route to correct handlers
- Screen transitions are smooth
- Tile tapping builds words correctly
- Invalid words show error feedback
- Valid words animate into chain
- Results screen stats are accurate
- Play Again starts new game
- Main Menu button returns correctly

### Cross-Mode Tests
- Economy/points consistency across modes
- Game state isolation between modes
- No data leakage between game sessions

### Visual/Polish Tests
- All animations are smooth (60fps on target devices)
- Text is readable and properly sized
- Colors are vibrant and consistent
- Touch targets are adequate
- No visual clipping or layout issues
- Mobile portrait layout works at multiple screen sizes

### End-to-End Tests
- Start game → play → finish → results → play again ✓
- Start game → play → finish → results → main menu → new mode ✓
- All three modes playable and completable ✓
- Score calculation correct across all modes ✓
- Stats displayed accurately ✓

---

## Implementation Phases

### Phase 1: UI Components & Screens
- Create/redesign all screen prefabs (MainMenuScreen, GameplayScreen, ResultsScreen)
- Build UI elements (buttons, tiles, displays) with Word Connect styling
- Wire up button interactions
- Implement screen transitions

### Phase 2: Visual Polish & Animations
- Add smooth animations to all interactions (button taps, tile placement, score updates)
- Implement satisfying feedback (scale animations, color shifts, haptic feedback)
- Fine-tune colors and typography
- Test animations on target devices

### Phase 3: Integration & Testing
- Wire UI to game logic completely
- Implement reactive state updates
- Test all three game modes end-to-end
- Verify stats calculations and display
- Run full test suite

### Phase 4: Polish & Debug
- Fix any remaining issues
- Optimize performance
- Ensure all tests pass
- Final visual review and adjustments
- Build to near-perfect state

---

## Success Criteria

✓ All three game modes fully playable and completable  
✓ UI matches Word Connect aesthetic (colorful, satisfying, clean)  
✓ Mobile portrait layout works on various screen sizes  
✓ All screens display correctly and transition smoothly  
✓ Game logic produces correct scores and statistics  
✓ Results screen displays all required stats accurately  
✓ All user interactions have satisfying visual/haptic feedback  
✓ 100% of unit tests passing  
✓ 100% of integration tests passing  
✓ No console errors or warnings  
✓ Playable and near-perfect state  

---

## Out of Scope (Future)

- Sound effects/music
- Animations beyond basic polish
- Landscape orientation
- Settings/preferences screen
- Shop/economy UI
- Multiplayer features
- Cloud save/sync
- Analytics tracking

