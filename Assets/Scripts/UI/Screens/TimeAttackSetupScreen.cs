using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordPuzzle.Modes;

namespace WordPuzzle.UI
{
    /// <summary>
    /// §5.3 — Time Attack setup screen. 2x2 button grid lets the player pick
    /// a (baseTimeSeconds, subMode) tuple before starting a Time Attack run.
    /// Fires OnConfigConfirmed with the resulting TimeAttackConfig.
    ///
    /// Task 13 — premium polish pass. The scene-authored layout was rendered at a
    /// stray 2.01x root scale (siblings are 1.0), which clipped the card labels,
    /// oversized the pure-black cards, and flung the title/subtitle/Home off-screen.
    /// LayoutAndStyle() normalizes the scale and applies a deterministic layout +
    /// styling pass at runtime — the same in-code approach GameplayScreen uses
    /// (LayoutHeader / StyleHomeButton). No Time Attack logic is touched.
    /// </summary>
    public class TimeAttackSetupScreen : MonoBehaviour
    {
        [SerializeField] private Button btn60Timed;
        [SerializeField] private Button btn60Survival;
        [SerializeField] private Button btn120Timed;
        [SerializeField] private Button btn120Survival;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI explainerText;

        public event Action<TimeAttackConfig> OnConfigConfirmed;
        public event Action OnBackToMenu;

        // ── Design tokens (README §14). UI layout/colour constants live here, mirroring
        //    GameplayScreen's in-screen literals; gameplay tunables stay in BalanceConfig. ──
        // Direction B — forward to the canonical Palette (no local colour literals). TIMED/SURVIVAL get two
        // distinct on-palette accents (was gold/green); the title uses the shared app title token (aqua).
        private static readonly Color C_ACCENT_TIMED    = Palette.ModeTimeAttack; // TIMED card/label (magenta-violet)
        private static readonly Color C_ACCENT_SURVIVAL = Palette.AccentLavender; // SURVIVAL card/label (lavender)
        private static readonly Color C_TITLE           = MenuPalette.TitleColor; // cool title, matches the menu
        private static readonly Color C_TEXT_PRIMARY    = Palette.TextPrimary;    // duration / card top
        private static readonly Color C_TEXT_MUTED      = Palette.TextMuted;      // subtitle / explainer

        // Vertical rhythm in the screen-root's local space (centre pivot; visible ≈ ±1059y).
        // Task 49 — the floating footer legend is gone (the modes are column headers now), so the
        // column composes: title → subtitle → headers → the 2×2 grid, with the bottom left to the
        // backdrop art.
        private const float TITLE_Y     =  660f;
        private const float SUBTITLE_Y  =  548f;
        private const float GRID_Y      =  -40f;
        private const float COLHEAD_LIFT = 84f;  // column-header centre above the grid's top edge

        // Task 49 — the cards own the screen: 2×380 + 24 gap = 784 of the 1080 canvas (the scene's
        // smaller cells floated in dead space).
        private static readonly Vector2 CARD_CELL    = new Vector2(380f, 280f);
        private static readonly Vector2 CARD_SPACING = new Vector2(24f, 24f);

        private void OnEnable()
        {
            if (titleText != null) titleText.text = "TIMED"; // the mode is "Timed" now; the TIMED/SURVIVAL columns are the variant picker

            if (btn60Timed != null)
                btn60Timed.onClick.AddListener(() => ConfirmWithPunch(btn60Timed, 60f, TimeAttackSubMode.Timed));
            if (btn60Survival != null)
                btn60Survival.onClick.AddListener(() => ConfirmWithPunch(btn60Survival, 60f, TimeAttackSubMode.Survival));
            if (btn120Timed != null)
                btn120Timed.onClick.AddListener(() => ConfirmWithPunch(btn120Timed, 120f, TimeAttackSubMode.Timed));
            if (btn120Survival != null)
                btn120Survival.onClick.AddListener(() => ConfirmWithPunch(btn120Survival, 120f, TimeAttackSubMode.Survival));

            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackToMenu?.Invoke());

            LayoutAndStyle();
        }

        private void OnDisable()
        {
            if (btn60Timed != null) btn60Timed.onClick.RemoveAllListeners();
            if (btn60Survival != null) btn60Survival.onClick.RemoveAllListeners();
            if (btn120Timed != null) btn120Timed.onClick.RemoveAllListeners();
            if (btn120Survival != null) btn120Survival.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }

        // ── Task 13: deterministic layout + styling. Idempotent; safe to re-run on every enable. ──
        private void LayoutAndStyle()
        {
            // 13A — kill the 2.01x zoom that clipped labels and flung header chrome off-screen.
            transform.localScale = Vector3.one;

            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background

            // 13C — header: cool title + muted subtitle, repositioned on-screen.
            PlaceCentered(titleText != null ? titleText.rectTransform : null, TITLE_Y, 860f, 110f);
            if (titleText != null)
            {
                titleText.text = "TIMED";
                TypeScale.Apply(titleText, TypeRole.Headline); // Task 42 — screen title
                titleText.color = C_TITLE;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.raycastTarget = false;
            }

            PlaceCentered(subtitleText != null ? subtitleText.rectTransform : null, SUBTITLE_Y, 860f, 60f);
            if (subtitleText != null)
            {
                subtitleText.text = "Choose your duration & mode";
                TypeScale.Apply(subtitleText, TypeRole.Body); // Task 42
                subtitleText.color = C_TEXT_MUTED;
                subtitleText.alignment = TextAlignmentOptions.Center;
                subtitleText.raycastTarget = false;
            }

            // 13A — centre the grid in its slot and re-seat it below the subtitle. Task 49 —
            // the cells grow to CARD_CELL so the cards own the screen instead of floating.
            var grid = GetComponentInChildren<GridLayoutGroup>(true);
            if (grid != null)
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                grid.cellSize = CARD_CELL;
                grid.spacing = CARD_SPACING;
                var grt = grid.GetComponent<RectTransform>();
                if (grt != null)
                {
                    grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 0.5f);
                    grt.anchoredPosition = new Vector2(0f, GRID_Y);
                }
            }

            // 13B — card styling + the TIMED/SURVIVAL meaning scheme (two distinct on-palette
            // accents). Task 49 — "60s TIMED" is the recommended first run: it carries the ONE
            // hero glow, answering "where do I tap" the way DAILY does on the menu.
            StyleCard(btn60Timed,     C_ACCENT_TIMED, hero: true);
            StyleCard(btn120Timed,    C_ACCENT_TIMED);
            StyleCard(btn60Survival,  C_ACCENT_SURVIVAL);
            StyleCard(btn120Survival, C_ACCENT_SURVIVAL);

            // Task 49 — the modes are COLUMN HEADERS (title + the one-line rule) instead of a
            // per-card label AND a floating footer legend explaining the same thing twice.
            EnsureColumnHeaders(grid);

            // 13C — icon-only Home, top-left, matching the polished chrome.
            StyleHomeButton();
        }

        private static void PlaceCentered(RectTransform rt, float y, float width, float height)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        // Card = surface-2 fill + accent outline. Task 49 — the card carries ONLY its big
        // duration: the mode lives in the column header above (per-card mode labels said the
        // same thing four times). `hero` marks the recommended first run (60s TIMED).
        private static void StyleCard(Button card, Color accent, bool hero = false)
        {
            if (card == null) return;

            var img = card.GetComponent<Image>();
            if (img != null)
            {
                UIThemeManager.ApplyOutlineButton(img, accent); // Task 25 — ghost card, accent border over black
                if (hero) UIThemeManager.ApplyNeonGlow(img, accent, hero: true); // the ONE bright cue
                img.raycastTarget = true;
            }

            // The ring sprite now carries the accent border, so drop the old offset Outline edge.
            var outline = card.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            var top = card.transform.Find("LabelTop")?.GetComponent<TMP_Text>();
            if (top != null)
            {
                TypeScale.Apply(top, TypeRole.Headline); // Task 42 — the card's hero duration number
                top.alignment = TextAlignmentOptions.Center;
                top.enableWordWrapping = false;
                top.raycastTarget = false;
                top.rectTransform.anchoredPosition = Vector2.zero; // centred alone in the bigger cell
                top.rectTransform.sizeDelta = new Vector2(320f, 96f);
            }

            var bottom = card.transform.Find("LabelBottom")?.GetComponent<TMP_Text>();
            if (bottom != null) bottom.gameObject.SetActive(false); // Task 49 — the column header owns the mode
        }

        // Task 49 — the modes are column headers over their grid columns: accent title + a
        // one-line rule (reward seconds pulled from the canonical Survival config so the copy
        // can never drift from BalanceConfig). Replaces the per-card mode label AND the old
        // floating footer legend. Positioned from the grid's LIVE metrics; idempotent.
        private void EnsureColumnHeaders(GridLayoutGroup grid)
        {
            if (explainerText != null) explainerText.gameObject.SetActive(false); // the legend is retired

            if (grid == null) return;
            var grt = grid.GetComponent<RectTransform>();
            if (grt == null) return;

            float colX = (grid.cellSize.x + grid.spacing.x) * 0.5f;
            float gridTop = grt.anchoredPosition.y + (grid.cellSize.y * 2f + grid.spacing.y) * 0.5f;
            int reward = Mathf.RoundToInt(TimeAttackConfig.DefaultSurvival().survivalRewardSeconds);

            BuildColumnHeader("ColHeadTimed", -colX, gridTop,
                "TIMED", "beat the clock", C_ACCENT_TIMED);
            BuildColumnHeader("ColHeadSurvival", colX, gridTop,
                "SURVIVAL", $"+{reward}s per word", C_ACCENT_SURVIVAL);
        }

        private void BuildColumnHeader(string name, float x, float gridTop, string title,
            string caption, Color accent)
        {
            var existing = transform.Find(name);
            GameObject go = existing != null ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));
            if (existing == null) go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, gridTop + COLHEAD_LIFT);
            rt.sizeDelta = new Vector2(CARD_CELL.x, 92f);
            if (existing != null) return; // built once; later enables only re-seat it

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(go.transform, false);
            var trt = (RectTransform)titleGo.transform;
            trt.anchorMin = new Vector2(0f, 0.5f); trt.anchorMax = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 22f); trt.sizeDelta = new Vector2(0f, 46f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            TypeScale.Apply(titleTmp, TypeRole.Label);
            titleTmp.color = accent;
            titleTmp.characterSpacing = 6f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            var capGo = new GameObject("Caption", typeof(RectTransform));
            capGo.transform.SetParent(go.transform, false);
            var crt = (RectTransform)capGo.transform;
            crt.anchorMin = new Vector2(0f, 0.5f); crt.anchorMax = new Vector2(1f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, -20f); crt.sizeDelta = new Vector2(0f, 32f);
            var capTmp = capGo.AddComponent<TextMeshProUGUI>();
            capTmp.text = caption;
            TypeScale.Apply(capTmp, TypeRole.Caption);
            capTmp.color = C_TEXT_MUTED;
            capTmp.alignment = TextAlignmentOptions.Center;
            capTmp.raycastTarget = false;
        }

        // Task 49 — press squish precedes the confirm (the same tactile beat as everywhere else).
        private void ConfirmWithPunch(Button card, float baseSeconds, TimeAttackSubMode sub)
        {
            if (!UIAnimations.ReduceMotion && isActiveAndEnabled && card != null)
                StartCoroutine(UIAnimations.ScaleButtonTap((RectTransform)card.transform));
            Confirm(baseSeconds, sub);
        }

        private static Sprite _homeIconSprite;
        private static Sprite GetHomeIconSprite()
        {
            if (_homeIconSprite != null) return _homeIconSprite;
            var tex = Resources.Load<Texture2D>("Icons/home");
            if (tex == null) return null;
            _homeIconSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            return _homeIconSprite;
        }

        // Icon-only house in the top-left corner — transparent hit target, mirrors GameplayScreen.
        private void StyleHomeButton()
        {
            if (backButton == null) return;

            var icon = GetHomeIconSprite();

            var rt = backButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Canonical HOME placement (matches GameplayScreen/Library BackButton) so it
                // lines up with the shared top-right Settings gear on every screen.
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(60f, -130f);
                rt.sizeDelta = new Vector2(icon != null ? 96f : 150f, 80f);
            }

            var img = backButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0f, 0f, 0f, 0f); // transparent — icon only
                img.raycastTarget = true;              // keep the full rect tappable
            }

            var lbl = backButton.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null)
            {
                lbl.raycastTarget = false;
                if (icon != null)
                {
                    lbl.text = string.Empty;
                }
                else
                {
                    lbl.text = "HOME";
                    TypeScale.Apply(lbl, TypeRole.Label); // Task 42
                    lbl.alignment = TextAlignmentOptions.Center;
                }
            }

            var iconTf = backButton.transform.Find("HomeIcon");
            Image iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                var go = new GameObject("HomeIcon", typeof(RectTransform));
                go.transform.SetParent(backButton.transform, false);
                var irt = go.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(52f, 52f);
                iconImg = go.AddComponent<Image>();
            }
            iconImg.enabled = icon != null;
            iconImg.sprite = icon;
            iconImg.color = C_TEXT_PRIMARY;
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
        }

        /// <summary>
        /// §5.3 — Build a TimeAttackConfig from the canonical factory then override
        /// the (baseTimeSeconds, subMode) tuple chosen by the player. Uses the live
        /// TimeAttackConfig API exposed by Assets/Scripts/Game/Modes/TimeAttackMode.cs.
        /// </summary>
        private void Confirm(float baseSeconds, TimeAttackSubMode sub)
        {
            // Start from the closest matching factory so addTime/survival defaults stay sane.
            TimeAttackConfig cfg;
            if (sub == TimeAttackSubMode.Survival)
            {
                cfg = TimeAttackConfig.DefaultSurvival();
            }
            else
            {
                cfg = Mathf.Approximately(baseSeconds, 120f)
                    ? TimeAttackConfig.Default120()
                    : TimeAttackConfig.Default60();
            }

            cfg.baseTimeSeconds = baseSeconds;
            cfg.subMode = sub;

            OnConfigConfirmed?.Invoke(cfg);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UIAnimations.PlayScreenEntrance(this);
            PlayCardEntrance(); // Task 49 — the four cards settle in with the shared calm stagger
        }

        public void Hide() => gameObject.SetActive(false);

        private Coroutine _cardsEntrance;

        private void PlayCardEntrance()
        {
            if (UIAnimations.ReduceMotion || !isActiveAndEnabled) return;
            if (_cardsEntrance != null) StopCoroutine(_cardsEntrance);
            var rects = new System.Collections.Generic.List<RectTransform>(4);
            foreach (var b in new[] { btn60Timed, btn60Survival, btn120Timed, btn120Survival })
                if (b != null) rects.Add((RectTransform)b.transform);
            if (rects.Count > 0)
                _cardsEntrance = StartCoroutine(UIAnimations.StaggeredPop(rects));
        }
    }
}
