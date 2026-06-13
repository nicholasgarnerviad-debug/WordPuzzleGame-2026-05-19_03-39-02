using System;
using System.Collections;
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

        // Task 28 — subtle menu motion state (coroutine/Mathf-based; all gated by UIAnimations.ReduceMotion).
        private RectTransform _titleRt;
        private float _titleBaseY;
        private Coroutine _titleCo, _cascadeCo;

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

            StopMenuMotion(); // Task 28 — stop floats/cascade; coroutines also auto-stop on disable
        }

        // ============================================================
        //  Task 11 — menu polish (layout + tiered styling, code-driven).
        //  Overrides the hand-placed/mixed-scale scene rects at runtime, the same way
        //  GameplayScreen drives its own layout (SeatPowerUpBar/StylePowerUpButton).
        //  Design tokens are centralized here — no scattered inline hex.
        // ============================================================
        // Task 23A — per-button menu colours now live in WordPuzzle.UI.MenuPalette (UITheme.cs);
        // no gold on this screen. Task 42 — every button label is TypeScale Label (one role,
        // no per-tier sizes); Daily reads as the hero by GLOW, not by a larger label.

        // Layout constants (named — no magic numbers). Task 23B — even composed rhythm:
        // uniform primary height, uniform gap, a tighter title row to close the void under it.
        private const float MENU_TITLE_H   = 130f;  // was 180 — close the gap under the title
        private const float MENU_HERO_H    = 130f;  // uniform primary height
        private const float MENU_MODE_H    = 130f;  // uniform primary height
        private const float MENU_TERT_H    = 96f;   // Task 43 — ghost tier keeps a ≥96px hit target
        private const float MENU_GAP       = 26f;   // uniform gap between rows
        private const float MENU_GROUP_GAP = 44f;   // modest extra gap before the tertiary group
        private const float MENU_TITLE_W   = 940f;
        private const float MENU_BTN_W     = 720f;
        private const float MENU_PAIR_GAP  = 24f;   // gap between Library & Stats in the two-up row
        private const float MENU_UP_BIAS   = 30f;   // slight upper-weighting
        private const float MENU_TITLE_LIFT = 16f;  // Task 28 — nudge just the header up a touch

        /// <summary>Style + arrange the whole menu. Called from OnEnable.</summary>
        private void ApplyMenuPolish()
        {
            // Task 26 — render over the shared full-screen BackgroundLayer (root goes transparent so the
            // black layer — and a future space image — shows through). Fixes the leftover teal cast too.
            UIThemeManager.ApplyScreenBackground(gameObject);

            StyleTitle();
            // Task 43 — the three-tier hierarchy: DAILY is the ONE filled gradient hero ("where do
            // I tap" answered by FILL, not just glow); the modes stay coloured outlines; Library /
            // Stats recede to ghost (tinted text on an invisible hit target). Mode buttons carry
            // their icon so identity survives grayscale/colorblind viewing (hue is not the only cue).
            StyleMenuButton(resumeButton,      MenuPalette.ResumeFill,     MenuPalette.ResumeLabel,
                icon: UIThemeManager.LoadIconSprite("IconResume"));
            StyleMenuButton(dailyButton,       MenuPalette.DailyFill,      MenuPalette.DailyLabel,      hero: true,
                icon: UIThemeManager.LoadIconSprite("IconDaily"));
            StyleMenuButton(classicModeButton, MenuPalette.ClassicFill,    MenuPalette.ClassicLabel,
                icon: UIThemeManager.LoadIconSprite("IconClassic"));
            // The scene authored this button "Classic Mode", but the mode is called just "Classic"
            // everywhere else (results title, Stats card, share card) — relabel so it's consistent
            // on every screen. Runtime relabel (no scene edit); runs in OnEnable before first paint.
            SetButtonLabel(classicModeButton, "Classic");
            StyleMenuButton(puzzleShowButton,  MenuPalette.PuzzleShowFill, MenuPalette.PuzzleShowLabel,
                icon: UIThemeManager.LoadIconSprite("IconPuzzleShow"));
            // Scene authored this "Puzzle Show"; the mode is "Puzzle Library" now (tiers are
            // "shelves"). Relabel at runtime, like Classic/Timed.
            SetButtonLabel(puzzleShowButton, "Puzzle Library");
            StyleMenuButton(timeAttackButton,  MenuPalette.TimeAttackFill, MenuPalette.TimeAttackLabel,
                icon: UIThemeManager.LoadIconSprite("IconTimeAttack"));
            // Scene authored this "Time Attack"; the mode is just "Timed" now (results say
            // "Timed" / "Timed Survival", stats card "TIMED"). Relabel at runtime, like Classic.
            SetButtonLabel(timeAttackButton, "Timed");
            StyleMenuButton(libraryButton,     MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel, primary: false);
            StyleMenuButton(statsButton,       MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel, primary: false);
            StyleMenuButton(settingsButton,    MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel, primary: false);

            // Settings now lives in the shared top-right gear (UIManager.CreateGlobalSettingsButton),
            // so remove the bottom-row Settings from the menu — the tertiary row is Library + Stats.
            // (ArrangeMenu skips inactive children, so deactivating drops it from the layout.)
            if (settingsButton != null) settingsButton.gameObject.SetActive(false);

            ArrangeMenu();
            PlayMenuMotion(); // Task 28 — title float + button cascade + Daily glow + press feedback
        }

        private void StyleTitle()
        {
            var t = transform.Find("TitleText");
            var title = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (title == null) return;

            // Task 43 — the masthead is the LOGOTYPE sprite when it resolves; the Task-42 Display
            // TMP text is the fallback, so a missing asset can never strand the menu. The entrance
            // + float animation moves the TitleText row itself, so both render paths inherit it.
            var logo = UIThemeManager.LoadIconSprite("StarLadderLogotype");
            var logoTf = t.Find("LogotypeImage");
            if (logo != null)
            {
                title.enabled = false; // the sprite carries the masthead
                Image img;
                if (logoTf == null)
                {
                    var go = new GameObject("LogotypeImage", typeof(RectTransform));
                    go.transform.SetParent(t, false);
                    img = go.AddComponent<Image>();
                }
                else
                {
                    img = logoTf.GetComponent<Image>();
                    if (img == null) img = logoTf.gameObject.AddComponent<Image>();
                    logoTf.gameObject.SetActive(true);
                }
                var rt = img.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                img.sprite = logo;
                img.color = Color.white;          // the gradient is baked into the art
                img.preserveAspect = true;
                img.raycastTarget = false;
                return;
            }

            if (logoTf != null) logoTf.gameObject.SetActive(false);
            title.enabled = true;
            title.text = "STAR LADDER";
            TypeScale.Apply(title, TypeRole.Display); // Task 42 — Rungo Bold 96 masthead (Display role)
            title.color = MenuPalette.TitleColor; // Task 23C — clean flat title, bright + non-gold
            title.characterSpacing = 8f;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = false;
        }

        // Task 43 — render a button on its tier: `hero` = the ONE filled gradient hero (Daily),
        // `primary` = coloured rounded outline (the modes), otherwise GHOST (tinted text only,
        // invisible hit target — Library/Stats recede). Visual only; never touches onClick/routing.
        private static void StyleMenuButton(Button btn, Color color, Color labelColor,
            bool hero = false, bool primary = true, Sprite icon = null)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();

            if (!primary)
            {
                UIThemeManager.ApplyGhostButton(btn, color); // tier 3 — tinted Label text, no ring/glow
            }
            else if (hero)
            {
                // Tier 1 — Daily: solid orchid→deep-violet gradient fill; sets its own ~8% press dim.
                UIThemeManager.ApplyFilledHeroButton(btn, Palette.ModeDaily, Palette.ModePuzzleShow);
            }
            else
            {
                // Tier 2 — the ONE shared outline geometry for the mode stack (unchanged).
                UIThemeManager.ApplyPrimaryMenuButton(img, color, heroGlow: false);

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
            }

            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            var label = btn.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                // Hero/ghost label colour is owned by the tier helper; outline keeps its bright label.
                if (primary && !hero) { TypeScale.Apply(label, TypeRole.Label); label.color = labelColor; }
                // Root cause of the tiny STATS label: its Label child carried a stray localScale (~0.53) that
                // shrank the rendered text by ~half even though its fontSize matched its Library pair (both 30).
                // ArrangeMenu/PlaceMenuRowAtX only normalizes the BUTTON's scale, not the label child — so
                // normalize the label here too (project rule: menu labels/panels sit at scale 1.0).
                label.rectTransform.localScale = Vector3.one;
                // Guard: a scene label with TMP auto-sizing ON would ignore fontSize and use its own min/max.
                label.enableAutoSizing = false;
            }

            // Phase 3 — mode identity carried redundantly by an icon (ghosts stay text-only).
            // On the filled hero the token would vanish into its own fill, so the icon goes bright.
            if (primary && icon != null)
                UIThemeManager.ApplyButtonIcon(btn, icon, hero ? Palette.TextPrimary : color);
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
            iconImg.color = tint;             // menu muted token
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

            // Task 28 — remember the title's layout Y so the float oscillates around it (and follows a
            // Resume show/hide re-layout). Captured here, right after placement, before any float offset.
            var titleNow = transform.Find("TitleText") as RectTransform;
            if (titleNow != null) _titleBaseY = titleNow.anchoredPosition.y + MENU_TITLE_LIFT;
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
            if (dailyButtonLabel != null)
                dailyButtonLabel.text = completedToday
                    ? $"DAILY  ·  {currentStreak}"   // streak count (· — renders in every theme weight)
                    : "DAILY";
            // Daily 2.0 (Task 38) — the button stays ENABLED when played; tapping RE-SHOWS today's result.
            // One-and-done is enforced in GameBootstrap.StartDailyMode (shows the stored result, not a fresh
            // scored run), so there's no replay/farm. The streak in the label signals "played today".
            if (dailyButton != null)
                dailyButton.interactable = true;
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

        // ============================================================
        //  Task 28 — subtle menu motion: floating title (one-time entrance + slow sine float),
        //  staggered button cascade, a faint breathing Daily halo, and tap press-feedback.
        //  Everything routes through UIAnimations.ReduceMotion (ON ⇒ fully static).
        //  Coroutine/Mathf-based, no per-frame allocations, no shaders — mobile-safe.
        // ============================================================
        private void PlayMenuMotion()
        {
            StopMenuMotion();
            _titleRt = transform.Find("TitleText") as RectTransform;
            RemoveDailyHalo(); // user feedback — drop the wider orange halo on Daily
            HookPressFeedback();

            if (UIAnimations.ReduceMotion)
            {
                // Fully static: title at its layout Y, all buttons opaque.
                if (_titleRt != null)
                {
                    _titleRt.anchoredPosition = new Vector2(_titleRt.anchoredPosition.x, _titleBaseY);
                    var tcg = EnsureCanvasGroup(_titleRt.gameObject);
                    if (tcg != null) tcg.alpha = 1f;
                }
                SetButtonsOpaque();
                return;
            }

            _titleCo   = StartCoroutine(TitleEntranceAndFloat());
            _cascadeCo = StartCoroutine(ButtonCascade());
        }

        private void StopMenuMotion()
        {
            if (_titleCo != null)   { StopCoroutine(_titleCo);   _titleCo = null; }
            if (_cascadeCo != null) { StopCoroutine(_cascadeCo); _cascadeCo = null; }
        }

        // Title: gentle one-shot fade + settle from slightly above, then a small slow vertical float.
        private IEnumerator TitleEntranceAndFloat()
        {
            if (_titleRt == null) yield break;
            var cg = EnsureCanvasGroup(_titleRt.gameObject);
            float baseX = _titleRt.anchoredPosition.x;

            const float entDur = 0.45f, fromOffset = 16f;
            float t = 0f;
            cg.alpha = 0f;
            while (t < entDur)
            {
                t += Dt();
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / entDur));
                cg.alpha = p;
                _titleRt.anchoredPosition = new Vector2(baseX, _titleBaseY + Mathf.Lerp(fromOffset, 0f, p));
                yield return null;
            }
            cg.alpha = 1f;

            const float amp = 8f, period = 3.6f; // small + slow — weightless
            float w = Mathf.PI * 2f / period;
            float phase = 0f;
            while (true)
            {
                phase += Dt() * w; // clamped dt → a transition hitch never jumps the float; it stays smooth
                _titleRt.anchoredPosition = new Vector2(baseX, _titleBaseY + Mathf.Sin(phase) * amp);
                yield return null;
            }
        }

        // Buttons: a quick polished cascade — fade the visible buttons in, top-to-bottom.
        private IEnumerator ButtonCascade()
        {
            var order = new[] { resumeButton, dailyButton, classicModeButton,
                                puzzleShowButton, timeAttackButton, libraryButton, statsButton };
            var groups = new List<CanvasGroup>();
            foreach (var b in order)
            {
                if (b == null || !b.gameObject.activeSelf) continue;
                var cg = EnsureCanvasGroup(b.gameObject);
                cg.alpha = 0f;
                groups.Add(cg);
            }
            // user feedback — a touch slower + smoother (ease-in-out) so buttons drift in gently.
            const float stepDelay = 0.07f, dur = 0.45f;
            for (int i = 0; i < groups.Count; i++)
            {
                StartCoroutine(FadeIn(groups[i], dur));
                yield return new WaitForSecondsRealtime(stepDelay);
            }
        }

        private static IEnumerator FadeIn(CanvasGroup cg, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt();
                cg.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur)); // S-curve — silky ease-in-out
                yield return null;
            }
            cg.alpha = 1f;
        }

        // user feedback — the Daily halo was removed; clean up any runtime-created child defensively.
        private void RemoveDailyHalo()
        {
            if (dailyButton == null) return;
            var t = dailyButton.transform.Find("__DailyHalo");
            if (t != null) Destroy(t.gameObject);
        }

        // Press feedback: a quick scale punch on tap. Added alongside the routing listener (which OnDisable
        // clears); ApplyMenuPolish runs once per enable, so exactly one is added per active session.
        private void HookPressFeedback()
        {
            AddPress(resumeButton); AddPress(dailyButton); AddPress(classicModeButton);
            AddPress(puzzleShowButton); AddPress(timeAttackButton);
            AddPress(libraryButton); AddPress(statsButton);
        }

        private void AddPress(Button b)
        {
            if (b == null) return;
            var rt = b.transform as RectTransform;
            b.onClick.AddListener(() =>
            {
                if (!UIAnimations.ReduceMotion && isActiveAndEnabled && rt != null)
                    StartCoroutine(UIAnimations.ScaleButtonTap(rt));
            });
        }

        private void SetButtonsOpaque()
        {
            foreach (var b in new[] { resumeButton, dailyButton, classicModeButton,
                                      puzzleShowButton, timeAttackButton, libraryButton, statsButton })
            {
                if (b == null) continue;
                var cg = b.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        // Clamped unscaled delta — keeps the menu motion smooth THROUGH a transition frame-hitch
        // (heading into / out of a game mode) instead of jumping/stuttering. Caps a single frame at 50ms.
        private static float Dt() => Mathf.Min(Time.unscaledDeltaTime, 0.05f);
    }
}
