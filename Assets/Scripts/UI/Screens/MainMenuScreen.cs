using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordPuzzle.UI
{
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private Button classicModeButton;
        [SerializeField] private Button puzzleShowButton;
        [SerializeField] private Button timeAttackButton;
        [SerializeField] private Button libraryButton;
        [SerializeField] private Button settingsButton;
        // Solo icon test — Lucide settings gear (SVG via Vector Graphics) shown in place of the SETTINGS label.
        [SerializeField] private Sprite settingsIcon;

        // Task 1C — Daily puzzle entry point.
        [SerializeField] private Button dailyButton;
        [SerializeField] private TextMeshProUGUI dailyButtonLabel;

        // Task 9F — Statistics screen.
        [SerializeField] private Button statsButton;

        // Task 9G — Resume in-progress puzzle.
        [SerializeField] private Button resumeButton;
        [SerializeField] private TextMeshProUGUI resumeButtonLabel;

        public event Action OnClassicModeSelected;
        public event Action OnPuzzleShowSelected;
        public event Action OnTimeAttackSelected;
        public event Action OnLibrarySelected;
        public event Action OnSettingsSelected;
        public event Action OnDailySelected;
        public event Action OnStatsSelected;
        public event Action OnResumeSelected;

        private void OnEnable()
        {
            if (classicModeButton != null)
                classicModeButton.onClick.AddListener(() => OnClassicModeSelected?.Invoke());
            if (puzzleShowButton != null)
                puzzleShowButton.onClick.AddListener(() => OnPuzzleShowSelected?.Invoke());
            if (timeAttackButton != null)
                timeAttackButton.onClick.AddListener(() => OnTimeAttackSelected?.Invoke());
            if (libraryButton != null)
                libraryButton.onClick.AddListener(() => OnLibrarySelected?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnSettingsSelected?.Invoke());
            if (dailyButton != null)
                dailyButton.onClick.AddListener(() => OnDailySelected?.Invoke());
            if (statsButton != null)
                statsButton.onClick.AddListener(() => OnStatsSelected?.Invoke());
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => OnResumeSelected?.Invoke());

            ApplyMenuPolish();
        }

        private void OnDisable()
        {
            if (classicModeButton != null)
                classicModeButton.onClick.RemoveAllListeners();
            if (puzzleShowButton != null)
                puzzleShowButton.onClick.RemoveAllListeners();
            if (timeAttackButton != null)
                timeAttackButton.onClick.RemoveAllListeners();
            if (libraryButton != null)
                libraryButton.onClick.RemoveAllListeners();
            if (settingsButton != null)
                settingsButton.onClick.RemoveAllListeners();
            if (dailyButton != null)
                dailyButton.onClick.RemoveAllListeners();
            if (statsButton != null)
                statsButton.onClick.RemoveAllListeners();
            if (resumeButton != null)
                resumeButton.onClick.RemoveAllListeners();
        }

        // ============================================================
        //  Task 11 — menu polish (layout + tiered styling, code-driven).
        //  Overrides the hand-placed/mixed-scale scene rects at runtime, the same way
        //  GameplayScreen drives its own layout (SeatPowerUpBar/StylePowerUpButton).
        //  Design tokens are centralized here — no scattered inline hex.
        // ============================================================
        private static readonly Color C_BG_BASE = Hex("#0F1217"); // screen background (matches gameplay)

        // Task 23A — per-button menu colours now live in WordPuzzle.UI.MenuPalette (UITheme.cs);
        // no gold on this screen. Font sizes per tier (named — no magic numbers).
        private const float FONT_HERO = 46f;  // Daily — slightly larger to read as the hero
        private const float FONT_MODE = 40f;  // Resume + game modes
        private const float FONT_TERT = 30f;  // Library / Stats (bumped — fix tiny Stats text)

        // Layout constants (named — no magic numbers). Task 23B — even composed rhythm:
        // uniform primary height, uniform gap, a tighter title row to close the void under it.
        private const float MENU_TITLE_H   = 130f;  // was 180 — close the gap under the title
        private const float MENU_HERO_H    = 130f;  // uniform primary height
        private const float MENU_MODE_H    = 130f;  // uniform primary height
        private const float MENU_TERT_H    = 80f;
        private const float MENU_GAP       = 26f;   // uniform gap between rows
        private const float MENU_GROUP_GAP = 44f;   // modest extra gap before the tertiary group
        private const float MENU_TITLE_W   = 940f;
        private const float MENU_BTN_W     = 720f;
        private const float MENU_PAIR_GAP  = 24f;   // gap between Library & Stats in the two-up row
        private const float MENU_UP_BIAS   = 30f;   // slight upper-weighting

        /// <summary>Style + arrange the whole menu. Called from OnEnable.</summary>
        private void ApplyMenuPolish()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = C_BG_BASE;

            StyleTitle();
            // Task 23A — each primary button gets its own distinct bright fill (no gold).
            // Daily = coral hero; Library/Stats stay muted slate (secondary chrome).
            StyleMenuButton(resumeButton,      MenuPalette.ResumeFill,     MenuPalette.ResumeLabel,     FONT_MODE, bold: true);
            StyleMenuButton(dailyButton,       MenuPalette.DailyFill,      MenuPalette.DailyLabel,      FONT_HERO, bold: true);
            StyleMenuButton(classicModeButton, MenuPalette.ClassicFill,    MenuPalette.ClassicLabel,    FONT_MODE, bold: true);
            StyleMenuButton(puzzleShowButton,  MenuPalette.PuzzleShowFill, MenuPalette.PuzzleShowLabel, FONT_MODE, bold: true);
            StyleMenuButton(timeAttackButton,  MenuPalette.TimeAttackFill, MenuPalette.TimeAttackLabel, FONT_MODE, bold: true);
            StyleMenuButton(libraryButton,     MenuPalette.SecondaryFill,  MenuPalette.SecondaryLabel,  FONT_TERT, bold: false);
            StyleMenuButton(statsButton,       MenuPalette.SecondaryFill,  MenuPalette.SecondaryLabel,  FONT_TERT, bold: false);
            StyleMenuButton(settingsButton,    MenuPalette.SecondaryFill,  MenuPalette.SecondaryLabel,  FONT_TERT, bold: false);

            // Settings now lives in the shared top-right gear (UIManager.CreateGlobalSettingsButton),
            // so remove the bottom-row Settings from the menu — the tertiary row is Library + Stats.
            // (ArrangeMenu skips inactive children, so deactivating drops it from the layout.)
            if (settingsButton != null) settingsButton.gameObject.SetActive(false);

            ArrangeMenu();
        }

        private void StyleTitle()
        {
            var t = transform.Find("TitleText");
            var title = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (title == null) return;
            title.text = "WORD LADDER";
            title.color = MenuPalette.TitleColor; // Task 23C — clean flat title, bright + non-gold
            title.fontStyle = FontStyles.Bold;
            title.fontSize = 92f;
            title.characterSpacing = 8f;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = false;
        }

        // Task 23A — apply a button's distinct fill + legible label colour. Visual only; never
        // touches onClick/routing. Preserves the Task 22 bubbly rounded background. Disables any
        // leftover Outline from the previous gold-hero treatment so no gold edge survives.
        private static void StyleMenuButton(Button btn, Color fill, Color labelColor, float fontSize, bool bold)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            UIThemeManager.ApplyRoundedButton(img); // keep Task 22 bubbly corners
            if (img != null) img.color = fill;

            // Task 24 — neutralise the scene's inconsistent per-button ColorBlock. Some menu buttons
            // were authored with a DARK normalColor which (via ColorTint) multiplied the fill down to
            // near-black — that's why Daily/Resume rendered maroon/teal-black and illegible. Force a
            // white base so the true fill shows; keep a subtle press-dim for tactile feedback.
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.pressedColor     = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor    = Color.white;
            cb.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;

            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = labelColor;
                label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                label.fontSize = fontSize;
            }
        }

        private static void SetButtonLabel(Button btn, string text)
        {
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }

        // Solo icon test — replace a button's text label with a centered, tinted sprite icon.
        private static void ShowButtonIcon(Button btn, Sprite icon, Color tint, float size)
        {
            if (btn == null || icon == null) return;

            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = string.Empty; label.raycastTarget = false; }

            var iconTf = btn.transform.Find("Icon");
            Image iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform));
                go.transform.SetParent(btn.transform, false);
                var irt = go.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(size, size);
                iconImg = go.AddComponent<Image>();
            }
            iconImg.sprite = icon;
            iconImg.color = tint;             // menu muted token (#8A93A1)
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
        }

        /// <summary>
        /// Positions the visible rows top-down, auto-centered (slightly upper-weighted).
        /// Proven LadderLayoutDriver pattern: anchor=centre, pivot=top, write anchoredPosition.y.
        /// Resets the scene's broken localScale hacks (Stats/Resume were 2.0, Library 0.9).
        /// Re-called when Resume visibility changes.
        /// </summary>
        private void ArrangeMenu()
        {
            // Primary stack — full-width rows, top-down. Skips inactive (e.g. a hidden Resume).
            var rows = new List<(RectTransform rt, float h, float w)>();
            void AddRow(Component c, float h, float w)
            {
                if (c == null) return;
                var rt = c.transform as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf) return;
                rows.Add((rt, h, w));
            }

            AddRow(transform.Find("TitleText"), MENU_TITLE_H, MENU_TITLE_W);
            AddRow(resumeButton,      MENU_MODE_H, MENU_BTN_W);
            AddRow(dailyButton,       MENU_HERO_H, MENU_BTN_W);
            AddRow(classicModeButton, MENU_MODE_H, MENU_BTN_W);
            AddRow(puzzleShowButton,  MENU_MODE_H, MENU_BTN_W);
            AddRow(timeAttackButton,  MENU_MODE_H, MENU_BTN_W);

            // Task 24B — Puzzle Library + Stats SIDE BY SIDE in one two-up row beneath the stack.
            // settingsButton is deactivated in ApplyMenuPolish, so it never joins the pair.
            var pair = new List<RectTransform>();
            void AddPair(Button b)
            {
                if (b == null) return;
                var rt = b.transform as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf) return;
                pair.Add(rt);
            }
            AddPair(libraryButton);
            AddPair(statsButton);
            bool hasPair = pair.Count > 0;

            if (rows.Count == 0 && !hasPair) return;

            // Total block height: primary rows + uniform gaps, then a group gap + one tertiary row.
            float total = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                total += rows[i].h;
                if (i < rows.Count - 1) total += MENU_GAP;
            }
            if (hasPair)
            {
                if (rows.Count > 0) total += MENU_GROUP_GAP;
                total += MENU_TERT_H;
            }

            float cursor = total * 0.5f + MENU_UP_BIAS; // top edge of the block (centred, slight up-bias)
            for (int i = 0; i < rows.Count; i++)
            {
                PlaceMenuRow(rows[i].rt, cursor, rows[i].w, rows[i].h);
                cursor -= rows[i].h;
                if (i < rows.Count - 1) cursor -= MENU_GAP;
            }
            if (hasPair)
            {
                if (rows.Count > 0) cursor -= MENU_GROUP_GAP;
                PlacePairRow(pair, cursor, MENU_TERT_H);
            }
        }

        // Two equal-width buttons side by side, centred on the stack's width with a small gap.
        private static void PlacePairRow(List<RectTransform> pair, float topY, float height)
        {
            if (pair.Count == 1) { PlaceMenuRowAtX(pair[0], 0f, topY, MENU_BTN_W, height); return; }
            float halfW = (MENU_BTN_W - MENU_PAIR_GAP) * 0.5f;
            float cx = (halfW + MENU_PAIR_GAP) * 0.5f;
            PlaceMenuRowAtX(pair[0], -cx, topY, halfW, height); // Library — left
            PlaceMenuRowAtX(pair[1],  cx, topY, halfW, height); // Stats — right
        }

        private static void PlaceMenuRow(RectTransform rt, float topY, float width, float height)
            => PlaceMenuRowAtX(rt, 0f, topY, width, height);

        private static void PlaceMenuRowAtX(RectTransform rt, float x, float topY, float width, float height)
        {
            if (rt == null) return;
            rt.localScale = Vector3.one;                 // undo scene scale hacks (2.0 / 0.9)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);            // pivot top → anchoredPosition.y is the top edge
            rt.anchoredPosition = new Vector2(x, topY);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static Color Hex(string h) =>
            ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void SelectClassicMode() => OnClassicModeSelected?.Invoke();
        public void SelectPuzzleShowMode() => OnPuzzleShowSelected?.Invoke();
        public void SelectTimeAttackMode() => OnTimeAttackSelected?.Invoke();
        public void SelectSettings() => OnSettingsSelected?.Invoke();
        public void SelectDaily() => OnDailySelected?.Invoke();

        /// <summary>
        /// Refresh the DAILY button label to reflect today's completion state.
        /// Called by GameBootstrap on app start and whenever returning to MainMenu.
        /// </summary>
        public void SetDailyState(bool completedToday, int currentStreak)
        {
            if (dailyButtonLabel == null) return;
            dailyButtonLabel.text = completedToday
                ? $"DAILY  ·  {currentStreak}"   // streak count (· is font-safe; ✓ U+2713 may be missing)
                : "DAILY";
        }

        /// <summary>
        /// Task 9G — show or hide the Resume button.
        /// Pass a non-empty description (e.g. start→end) to display on the button label.
        /// </summary>
        public void SetResumeVisible(bool visible, string description = "")
        {
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(visible);
            if (resumeButtonLabel != null && !string.IsNullOrEmpty(description))
                resumeButtonLabel.text = $"RESUME  {description}";

            ArrangeMenu(); // re-center now that the visible row count changed
        }
    }
}
