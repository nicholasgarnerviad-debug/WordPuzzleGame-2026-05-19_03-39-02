# Task 18: Mobile UI Refinement Guide

**Goal:** Ensure UI is optimized for mobile, accessible, and responsive across different screen sizes.

---

## Phase 1: Button Size & Touch Target Audit

### Standards

Mobile UI standards recommend:
- **Minimum touch target**: 48dp (Density-independent pixels)
- **Recommended size**: 64dp x 64dp
- **Spacing between targets**: 8dp minimum

In Unity with 1080x1920 reference resolution:
- 48dp ≈ **96 pixels**
- 64dp ≈ **128 pixels**
- 8dp ≈ **16 pixels**

### Audit All Buttons

For each button in all scenes, verify:

**Measurement Tool:**
1. Select button in Hierarchy
2. Look at RectTransform component
3. Check **Size Delta** values

**Button Size Checklist:**

- [ ] **MainMenu Buttons**
  - Classic Mode: Width ≥ 200px, Height ≥ 80px ✓
  - Puzzle Show: Width ≥ 200px, Height ≥ 80px ✓
  - Time Attack: Width ≥ 200px, Height ≥ 80px ✓
  - Shop: Width ≥ 150px, Height ≥ 60px ✓
  - Settings: Width ≥ 150px, Height ≥ 60px ✓

- [ ] **ClassicMode Buttons**
  - Submit: Width ≥ 150px, Height ≥ 60px ✓
  - Hint: Width ≥ 120px, Height ≥ 50px ✓
  - Undo: Width ≥ 120px, Height ≥ 50px ✓

- [ ] **PuzzleShowMode Buttons**
  - (Same as ClassicMode)

- [ ] **TimeAttackMode Buttons**
  - (Same as ClassicMode)

### Fix Undersized Buttons

If any button is too small:

1. Select button in Hierarchy
2. In Inspector, modify RectTransform:
   ```
   Size Delta:
   - X (Width): [increase to ≥150]
   - Y (Height): [increase to ≥60]
   ```
3. Adjust position if needed to prevent overlap

### Spacing Between Elements

For any buttons within 100px of each other, verify spacing:

1. Select first button → note position
2. Select second button → note position
3. Calculate distance: `abs(pos1 - pos2)`
4. **If distance < 16px, increase spacing**

---

## Phase 2: Text Readability

### Font Size Audit

Check all text elements for readability:

**Text Size Standards:**
- **Title**: 40-48pt ✓
- **Labels/Headers**: 32-36pt ✓
- **Body text**: 24-28pt ✓
- **Small text**: 18-20pt minimum

### Text Contrast & Accessibility

**WCAG AA Standard:** Contrast ratio ≥ 4.5:1 for normal text

**Test contrast:**
1. For each text element, note:
   - Text color (RGB)
   - Background color (RGB)
2. Use WCAG contrast checker: https://webaim.org/resources/contrastchecker/
3. Verify ratio ≥ 4.5:1

**Example checklist:**
- [ ] White text on blue background: ✓ (4.5:1+)
- [ ] Black text on white background: ✓ (21:1)
- [ ] Light gray text: ⚠️ Check contrast

**Common Problems:**
- Gray text on light backgrounds (< 3:1) ❌
- Yellow text on white (< 2:1) ❌
- Light blue text on light backgrounds ❌

**Fix:** Use high-contrast combinations:
- White on dark blue
- Black or dark gray on white/light gray
- Light colors on dark backgrounds

### Text Rendering

1. Select any TextMeshPro text element
2. In Inspector, verify:
   - Font Size: 24-48
   - Alignment: **Center** (for buttons/labels)
   - Word Wrap: **ON** for long text

---

## Phase 3: Screen Orientation Testing

### Portrait Mode (Primary)

All screens should work perfectly in portrait (1080x1920).

**Test:**
1. Open each scene in Editor
2. Game View aspect: **9:16 (Portrait)**
3. Verify:
   - All UI visible and not cut off
   - Buttons not overlapping
   - Text not cut off at edges
   - Input fields usable

### Landscape Mode (Secondary)

For v1.0, landscape is **not required**, but app must **not crash**.

**Test:**
1. Game View aspect: **16:9 (Landscape)**
2. Check:
   - App doesn't crash ✓
   - UI is partially off-screen (acceptable for v1.0)
   - No error messages

### Different Screen Sizes

Test on multiple aspect ratios:

1. Game View aspect dropdown → Custom (1080, 1080) → Test
2. Game View aspect dropdown → Custom (1440, 2560) → Test
3. Game View aspect dropdown → Custom (800, 1280) → Test

**Acceptance:** UI adapts to width, text wraps, buttons remain usable

---

## Phase 4: Input Field Usability

### Keyboard Visibility

For each InputField (word input):

1. In Play mode, click word input field
2. Verify:
   - Mobile keyboard appears
   - Input field doesn't get hidden behind keyboard
   - User can see text being typed

**If keyboard hides input:**
- Increase canvas height or reposition field higher
- Use ScrollRect to scroll field into view

### Placeholder Text

Check that placeholder text appears when field is empty:

1. Select WordInput GameObject
2. In TMP_InputField component:
   - **Placeholder**: [Should show "Enter word..."]
   - Color: Light gray for visibility

---

## Phase 5: Visual Polish

### Button States

Buttons should have visual feedback for:

1. **Normal state** (default)
2. **Hover state** (pointer over, if mouse)
3. **Pressed state** (actively clicking)
4. **Disabled state** (not interactive)

**Configure in Unity:**

For each button:
1. Select button → Inspector → Button component
2. Under **Transition**, select **Color Tint**
3. Set colors:
   ```
   Normal Color: Light Blue (0.2, 0.2, 0.8, 1.0)
   Highlighted Color: Medium Blue (0.1, 0.1, 0.9, 1.0)
   Pressed Color: Dark Blue (0.05, 0.05, 0.7, 1.0)
   Disabled Color: Gray (0.5, 0.5, 0.5, 0.5)
   ```

### Color Scheme Consistency

Verify consistent color usage:

- **Primary buttons** (Submit, Start): Blue
- **Secondary buttons** (Hint, Undo): Green
- **Danger buttons** (if any): Red
- **Disabled state**: Gray with 50% alpha

### Safe Area (Notch Support)

For devices with notches/safe areas:

1. In Canvas Scaler:
   - Check if **Safe Area** option exists
   - If yes, enable it
   - This prevents UI from extending into notch area

---

## Phase 6: Device Testing

### Test on Real Devices

If possible, test on:
- ✅ Small phone (5.5" screen)
- ✅ Standard phone (6.1" screen)
- ✅ Tablet (if available)

**What to verify on each:**
1. All buttons reachable with thumb
2. No text cut off
3. No UI elements hidden
4. Keyboard doesn't obscure input field
5. Tap accuracy (buttons easy to hit)

### Android Device Testing

**On connected Android device:**

1. Build Development APK: `File → Build Settings → Build And Run`
2. Once installed, open app and test:
   - Tap each button → responsive?
   - Type word → input works?
   - Scroll if any elements overlap?
   - Portrait rotation → layout stable?

---

## Phase 7: Accessibility Checklist

- [ ] **Text contrast**: All text ≥ 4.5:1 ratio
- [ ] **Font sizes**: Title ≥ 40pt, Body ≥ 24pt
- [ ] **Button sizes**: All ≥ 48dp (96px)
- [ ] **Spacing**: Minimum 8dp (16px) between targets
- [ ] **Input fields**: Visible, placeholder text clear
- [ ] **Color independence**: Don't rely on color alone for meaning
- [ ] **Keyboard support**: All interactive elements keyboard accessible
- [ ] **Touch feedback**: Buttons show visual feedback on tap

---

## Phase 8: UI Refinement Acceptance Criteria

✅ **Task 18 Complete when:**

1. **Button sizing**
   - All buttons ≥ 48dp
   - Proper spacing verified
   - No overlapping elements

2. **Text readability**
   - All text ≥ 24pt
   - Contrast ≥ 4.5:1
   - No cut-off text

3. **Responsive design**
   - Portrait mode: Perfect
   - Landscape: No crashes
   - Multiple aspect ratios: Functional

4. **Touch usability**
   - All buttons responsive
   - Input fields work
   - No hidden UI

5. **Visual polish**
   - Button states configured
   - Color scheme consistent
   - Safe area respected

6. **Device testing**
   - Tested on real device (if available)
   - APK installs and runs
   - No crashes or visual issues

7. **Accessibility**
   - WCAG AA compliant
   - All accessibility checks passed

---

## Known Gaps (v1.1)

Document any gaps found:

- **Landscape orientation**: Not supported in v1.0
- **High contrast mode**: Not implemented
- **Screen reader support**: Not implemented
- **Haptic feedback**: Not implemented

These can be addressed in v1.1 updates.

---

## Checklist Template

Use this to track UI refinement:

```markdown
# UI Refinement Checklist

## Button Sizing
- [ ] MainMenu buttons: all ≥ 80px height
- [ ] ClassicMode buttons: all ≥ 60px height
- [ ] PuzzleShowMode buttons: all ≥ 60px height
- [ ] TimeAttackMode buttons: all ≥ 60px height

## Text Readability
- [ ] Title text: 40-48pt
- [ ] Label text: 24-36pt
- [ ] All contrast: ≥ 4.5:1

## Responsive Design
- [ ] Portrait mode: works perfectly
- [ ] Landscape mode: no crash
- [ ] Multiple aspect ratios: functional

## Device Testing
- [ ] APK builds successfully
- [ ] App installs on device
- [ ] All scenes work
- [ ] Input responsive
- [ ] No crashes

## Accessibility
- [ ] WCAG AA compliant
- [ ] Touch targets proper size
- [ ] High contrast colors
```

---

## Next: Task 19 (Final Build & Testing)

Once UI refinement is complete, proceed to Task 19 for final APK build, QA testing, and release preparation.

