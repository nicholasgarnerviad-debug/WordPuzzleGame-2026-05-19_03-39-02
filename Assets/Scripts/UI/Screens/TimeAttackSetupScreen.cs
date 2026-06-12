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
        private const float TITLE_Y     =  680f;
        private const float SUBTITLE_Y  =  560f;
        private const float GRID_Y      =  -20f;
        private const float EXPLAINER_Y = -560f;

        private void OnEnable()
        {
            if (titleText != null) titleText.text = "TIME ATTACK";

            if (btn60Timed != null)
                btn60Timed.onClick.AddListener(() => Confirm(60f, TimeAttackSubMode.Timed));
            if (btn60Survival != null)
                btn60Survival.onClick.AddListener(() => Confirm(60f, TimeAttackSubMode.Survival));
            if (btn120Timed != null)
                btn120Timed.onClick.AddListener(() => Confirm(120f, TimeAttackSubMode.Timed));
            if (btn120Survival != null)
                btn120Survival.onClick.AddListener(() => Confirm(120f, TimeAttackSubMode.Survival));

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
                titleText.text = "TIME ATTACK";
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

            // 13A — centre the grid in its slot and re-seat it below the subtitle.
            var grid = GetComponentInChildren<GridLayoutGroup>(true);
            if (grid != null)
            {
                grid.childAlignment = TextAnchor.MiddleCenter;
                var grt = grid.GetComponent<RectTransform>();
                if (grt != null)
                {
                    grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 0.5f);
                    grt.anchoredPosition = new Vector2(0f, GRID_Y);
                }
            }

            // 13B — card styling + the TIMED/SURVIVAL meaning scheme (two distinct on-palette accents).
            StyleCard(btn60Timed,     C_ACCENT_TIMED);
            StyleCard(btn120Timed,    C_ACCENT_TIMED);
            StyleCard(btn60Survival,  C_ACCENT_SURVIVAL);
            StyleCard(btn120Survival, C_ACCENT_SURVIVAL);

            // 13C — explainer with the real Timed/Survival rules (reward sourced from config).
            EnsureExplainer();

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

        // Card = surface-2 fill + accent outline. LabelTop = duration (cream),
        // LabelBottom = sub-mode (accent — the colour that carries the Timed/Survival meaning).
        private static void StyleCard(Button card, Color accent)
        {
            if (card == null) return;

            var img = card.GetComponent<Image>();
            if (img != null)
            {
                UIThemeManager.ApplyOutlineButton(img, accent); // Task 25 — ghost card, accent border over black
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
                top.rectTransform.sizeDelta = new Vector2(260f, 90f);
            }

            var bottom = card.transform.Find("LabelBottom")?.GetComponent<TMP_Text>();
            if (bottom != null)
            {
                TypeScale.Apply(bottom, TypeRole.Body); // Task 42
                bottom.color = accent;
                bottom.characterSpacing = 4f;
                bottom.alignment = TextAlignmentOptions.Center;
                bottom.enableWordWrapping = false;
                bottom.raycastTarget = false;
                bottom.rectTransform.sizeDelta = new Vector2(260f, 50f);
            }
        }

        // Two-line explainer; reward seconds pulled from the canonical Survival config
        // so the copy can never drift from BalanceConfig.
        private void EnsureExplainer()
        {
            if (explainerText == null)
            {
                var go = new GameObject("ExplainerText", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                explainerText = go.AddComponent<TextMeshProUGUI>();
            }

            int reward = Mathf.RoundToInt(TimeAttackConfig.DefaultSurvival().survivalRewardSeconds);
            string timed    = "#" + ColorUtility.ToHtmlStringRGB(C_ACCENT_TIMED);
            string survival = "#" + ColorUtility.ToHtmlStringRGB(C_ACCENT_SURVIVAL);
            explainerText.richText = true;
            explainerText.text =
                $"<color={timed}><b>TIMED</b></color>  ·  fixed countdown — beat the clock\n" +
                $"<color={survival}><b>SURVIVAL</b></color>  ·  each solved word adds +{reward}s";
            TypeScale.Apply(explainerText, TypeRole.Body); // Task 42
            explainerText.color = C_TEXT_MUTED;
            explainerText.lineSpacing = 18f;
            explainerText.alignment = TextAlignmentOptions.Center;
            explainerText.raycastTarget = false;
            PlaceCentered(explainerText.rectTransform, EXPLAINER_Y, 920f, 180f);
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

        public void Show() { gameObject.SetActive(true); UIAnimations.PlayScreenEntrance(this); }
        public void Hide() => gameObject.SetActive(false);
    }
}
