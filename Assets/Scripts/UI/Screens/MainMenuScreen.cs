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
        private static readonly Color C_BG_BASE      = Hex("#0F1217"); // screen background (matches gameplay)
        private static readonly Color C_BG_SURFACE    = Hex("#1B1F27"); // tertiary fill
        private static readonly Color C_SURFACE_2     = Hex("#242936"); // mode + hero fill
        private static readonly Color C_GOLD          = Hex("#C9B458"); // hero edge/label + title
        private static readonly Color C_TEXT_PRIMARY  = Hex("#E7E1C4"); // mode labels
        private static readonly Color C_TEXT_MUTED    = Hex("#8A93A1"); // tertiary labels

        // Layout constants (named — no magic numbers).
        private const float MENU_TITLE_H   = 180f;
        private const float MENU_HERO_H    = 150f;
        private const float MENU_MODE_H    = 128f;
        private const float MENU_TERT_H    = 84f;
        private const float MENU_GAP       = 30f;   // gap between rows
        private const float MENU_GROUP_GAP = 54f;   // extra gap before the tertiary group
        private const float MENU_TITLE_W   = 940f;
        private const float MENU_BTN_W     = 720f;
        private const float MENU_TERT_W    = 600f;
        private const float MENU_UP_BIAS   = 40f;   // slight upper-weighting

        private enum MenuTier { Hero, Mode, Tertiary }

        /// <summary>Style + arrange the whole menu. Called from OnEnable.</summary>
        private void ApplyMenuPolish()
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = C_BG_BASE;

            StyleTitle();
            StyleTierButton(dailyButton,      MenuTier.Hero);
            StyleTierButton(classicModeButton, MenuTier.Mode);
            StyleTierButton(puzzleShowButton,  MenuTier.Mode);
            StyleTierButton(timeAttackButton,  MenuTier.Mode);
            StyleTierButton(resumeButton,      MenuTier.Mode);
            StyleTierButton(libraryButton,     MenuTier.Tertiary);
            StyleTierButton(statsButton,       MenuTier.Tertiary);
            StyleTierButton(settingsButton,    MenuTier.Tertiary);

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
            title.color = C_GOLD;
            title.fontStyle = FontStyles.Bold;
            title.fontSize = 96f;
            title.characterSpacing = 6f;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = false;
        }

        private static void StyleTierButton(Button btn, MenuTier tier)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            UIThemeManager.ApplyRoundedButton(img); // Task 21B — consistent rounded corners
            var outline = btn.GetComponent<Outline>();
            var label = btn.GetComponentInChildren<TMP_Text>(true);

            switch (tier)
            {
                case MenuTier.Hero: // DAILY — gold-edged primary CTA
                    if (img != null) img.color = C_SURFACE_2;
                    EnsureOutline(btn, C_GOLD, 4f);
                    if (label != null) { label.color = C_GOLD; label.fontStyle = FontStyles.Bold; label.fontSize = 46f; }
                    break;
                case MenuTier.Mode: // game modes — raised surface, primary text
                    if (img != null) img.color = C_SURFACE_2;
                    if (outline != null) outline.enabled = false;
                    if (label != null) { label.color = C_TEXT_PRIMARY; label.fontStyle = FontStyles.Bold; label.fontSize = 38f; }
                    break;
                case MenuTier.Tertiary: // library/stats/settings — demoted chrome
                    if (img != null) img.color = C_BG_SURFACE;
                    if (outline != null) outline.enabled = false;
                    if (label != null) { label.color = C_TEXT_MUTED; label.fontStyle = FontStyles.Normal; label.fontSize = 30f; }
                    break;
            }
        }

        private static void EnsureOutline(Button btn, Color color, float distance)
        {
            if (btn == null) return;
            var o = btn.GetComponent<Outline>();
            if (o == null) o = btn.gameObject.AddComponent<Outline>();
            o.enabled = true;
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, distance);
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
            var items = new List<(RectTransform rt, float h, float w, bool brk)>();
            void Add(Component c, float h, float w, bool brk = false)
            {
                if (c == null) return;
                var rt = c.transform as RectTransform;
                if (rt == null || !rt.gameObject.activeSelf) return;
                items.Add((rt, h, w, brk));
            }

            Add(transform.Find("TitleText"), MENU_TITLE_H, MENU_TITLE_W);
            Add(resumeButton,      MENU_MODE_H, MENU_BTN_W);
            Add(dailyButton,       MENU_HERO_H, MENU_BTN_W);
            Add(classicModeButton, MENU_MODE_H, MENU_BTN_W);
            Add(puzzleShowButton,  MENU_MODE_H, MENU_BTN_W);
            Add(timeAttackButton,  MENU_MODE_H, MENU_BTN_W);
            Add(libraryButton,     MENU_TERT_H, MENU_TERT_W, brk: true); // group break before tertiary
            Add(statsButton,       MENU_TERT_H, MENU_TERT_W);
            Add(settingsButton,    MENU_TERT_H, MENU_TERT_W);

            if (items.Count == 0) return;

            float total = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                total += items[i].h;
                if (i < items.Count - 1) total += items[i + 1].brk ? MENU_GROUP_GAP : MENU_GAP;
            }

            float cursor = total * 0.5f + MENU_UP_BIAS; // top of block, centred + slight up-bias
            for (int i = 0; i < items.Count; i++)
            {
                PlaceMenuRow(items[i].rt, cursor, items[i].w, items[i].h);
                cursor -= items[i].h;
                if (i < items.Count - 1) cursor -= items[i + 1].brk ? MENU_GROUP_GAP : MENU_GAP;
            }
        }

        private static void PlaceMenuRow(RectTransform rt, float topY, float width, float height)
        {
            if (rt == null) return;
            rt.localScale = Vector3.one;                 // undo scene scale hacks (2.0 / 0.9)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);            // pivot top → anchoredPosition.y is the top edge
            rt.anchoredPosition = new Vector2(0f, topY);
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
