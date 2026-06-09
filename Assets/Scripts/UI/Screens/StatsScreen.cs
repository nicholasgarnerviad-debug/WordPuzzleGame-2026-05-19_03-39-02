using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    // -------------------------------------------------------------------------
    //  StatsScreen  (Task 9F; rebuilt Task 38; polished Task 40)
    //  Displays persisted player statistics in clean, grouped sections. A pure
    //  view-model struct (StatsViewModel) holds all data so QA can unit-test the
    //  number derivation without a MonoBehaviour. The VISUAL layout is built at
    //  RUNTIME from layout-group cards (the same code-driven approach as
    //  MainMenuScreen/ShopScreen) — auto-spaced, so there are no hand-placed
    //  positions to collide. The flat scene-authored labels are hidden.
    //
    //  Task 40 polish — on every open the cards REVEAL with a staggered fade +
    //  scale-in, the numeric values COUNT UP from zero, and the hero day-streak
    //  gives a brief gold pop. All motion is gated on UIAnimations.ReduceMotion
    //  (instant final state when reduced) and is re-entrant: re-opening the
    //  screen cleanly restarts the reveal without leftover coroutines.
    // -------------------------------------------------------------------------

    /// <summary>Pure, testable view-model for the stats screen. Build with <see cref="StatsScreen.BuildStatsViewModel"/>.</summary>
    public struct StatsViewModel
    {
        // Daily streak
        public int currentStreak;
        public int longestStreak;
        public int dailyCompleted;

        // Daily 2.0 (Task 36) — trailing-365 Win/Loss record (skill, alongside the habit streak).
        public int dailyGamesPlayed;
        public int dailyWins;
        public int dailyLosses;
        public int dailyWinRatePct;

        // Overall
        public int totalCoins;
        public int totalPuzzlesCompleted;

        // Classic mode
        public int classicGamesPlayed;
        public int classicGamesWon;

        // Time Attack mode
        public int timeAttackGamesPlayed;
        public int timeAttackBestRound;
    }

    public class StatsScreen : MonoBehaviour
    {
        // Scene-authored labels — kept wired so the prefab binds, but HIDDEN at runtime and superseded by
        // the grouped layout built in EnsureBuilt(). Read-only now (presentation moved into code).
        [SerializeField] private TextMeshProUGUI currentStreakValue;
        [SerializeField] private TextMeshProUGUI longestStreakValue;
        [SerializeField] private TextMeshProUGUI dailyCompletedValue;
        [SerializeField] private TextMeshProUGUI totalCoinsValue;
        [SerializeField] private TextMeshProUGUI totalPuzzlesValue;
        [SerializeField] private TextMeshProUGUI classicPlayedValue;
        [SerializeField] private TextMeshProUGUI classicWonValue;
        [SerializeField] private TextMeshProUGUI timeAttackPlayedValue;
        [SerializeField] private TextMeshProUGUI timeAttackBestRoundValue;
        [SerializeField] private Button homeButton;

        public event Action OnBackToMenu;

        // Runtime-built value labels (populated by ApplyViewModel).
        private TMP_Text _coinText, _streakHero, _longestVal, _winRateVal, _wlVal,
                         _classicPlayedVal, _classicWonVal, _taPlayedVal, _taBestVal, _overallText;
        private bool _built;

        // ── Task 40 polish — reveal/count-up state ──────────────────────────────
        // Cards (+ coin pill) animated in, in this order, by the staggered reveal.
        private readonly List<RevealTarget> _revealTargets = new List<RevealTarget>();
        private readonly List<Coroutine> _running = new List<Coroutine>();
        private StatsViewModel _vm;   // last applied data — drives the count-up targets
        private bool _hasData;

        private struct RevealTarget
        {
            public CanvasGroup group;
            public RectTransform rect;
        }

        // Reveal tuning — calm, premium (ease-out, no bounce).
        private const float RevealStagger = 0.07f;   // delay between successive cards
        private const float RevealDuration = 0.30f;   // per-card fade + scale-in
        private const float CountUpDuration = 0.55f;   // numeric roll-up
        private const float RevealStartScale = 0.94f;   // cards scale up from here

        // ── Layout sizing (centralized — no scattered magic numbers) ──────────────
        private const float CONTENT_W       = 960f;  // card-stack width
        private const float CONTENT_TOP_Y   = 300f;  // top edge just below the title
        private const float STACK_SPACING   = 18f;   // gap between cards
        private const float ROW_SPACING     = 16f;   // gap inside horizontal rows
        private const int   CARD_PAD_X      = 28;     // card horizontal padding
        private const int   CARD_PAD_TOP    = 16;
        private const int   CARD_PAD_BOT    = 18;
        private const float CARD_SPACING    = 8f;    // gap between a card's header and body
        private const float SECTION_FONT    = 24f;   // card section header
        private const float HERO_FONT       = 76f;   // DAILY streak — the focal number
        private const float HERO_BAND_H     = 116f;  // hero + support band height (snug → no dead space)
        private const float HERO_BLOCK_W    = 300f;  // hero column width (support stats take the rest)
        private const float CAPTION_FONT    = 24f;   // stat captions (raised 19→24 for legibility)
        private const float SUPPORT_VALUE_FONT = 34f; // stat values (longest / mode numbers)
        private const float MODE_CELLS_H    = 74f;   // CLASSIC / TIME ATTACK value-cell row height
        private const float FOOTER_FONT     = 24f;   // overall footer line

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject, UIThemeManager.ReadabilityScrimAlpha); // shared backdrop + readability scrim
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.onClick.RemoveListener(OnHomeClicked);
            StopReveal();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UIAnimations.PlayScreenEntrance(this);
            PlayReveal(); // Task 40 — staggered card reveal + number count-ups
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Build (once) the grouped layout, then display stats from persisted data. Null-safe.</summary>
        public void Populate(DailyProgress daily, PlayerProgress player)
        {
            var vm = BuildStatsViewModel(daily, player);
            EnsureBuilt();
            _vm = vm;
            _hasData = true;
            ApplyViewModel(vm);
        }

        /// <summary>Pure factory — derive the view-model from raw data. Null-safe; unit-testable without a MonoBehaviour.</summary>
        public static StatsViewModel BuildStatsViewModel(
            DailyProgress daily,
            PlayerProgress player)
        {
            var vm = new StatsViewModel();

            if (daily != null)
            {
                vm.currentStreak   = daily.currentStreak;
                vm.longestStreak   = daily.longestStreak;
                vm.dailyCompleted  = daily.completedDates?.Count ?? 0;

                // Daily 2.0 — trailing-365 W/L record, computed inline from the persisted ledger
                // (UI cannot reference the Game assembly, so we count daily.outcomes directly here).
                int dailyGames = daily.outcomes?.Count ?? 0;
                int dailyWins = 0;
                if (daily.outcomes != null)
                    foreach (var o in daily.outcomes) if (o.won) dailyWins++;
                vm.dailyGamesPlayed = dailyGames;
                vm.dailyWins        = dailyWins;
                vm.dailyLosses      = dailyGames - dailyWins;
                vm.dailyWinRatePct  = dailyGames > 0 ? (int)System.Math.Round(100.0 * dailyWins / dailyGames) : 0;
            }

            if (player != null)
            {
                vm.totalCoins            = player.totalCoins;
                vm.totalPuzzlesCompleted = player.totalPuzzlesCompleted;

                if (player.classicStats != null)
                {
                    vm.classicGamesPlayed = player.classicStats.gamesPlayed;
                    vm.classicGamesWon    = player.classicStats.gamesWon;
                }

                if (player.timeAttackStats != null)
                {
                    vm.timeAttackGamesPlayed = player.timeAttackStats.gamesPlayed;
                    vm.timeAttackBestRound   = player.timeAttackStats.bestRoundReached;
                }
            }

            return vm;
        }

        // ── Display (write the built labels) ─────────────────────────────────────
        private void ApplyViewModel(StatsViewModel vm)
        {
            if (_coinText   != null) _coinText.text   = vm.totalCoins.ToString("N0");
            if (_streakHero != null) _streakHero.text = vm.currentStreak.ToString();
            if (_longestVal != null) _longestVal.text = vm.longestStreak.ToString();

            bool hasDaily = vm.dailyGamesPlayed > 0;
            if (_winRateVal != null) _winRateVal.text = hasDaily ? $"{vm.dailyWinRatePct}%" : "—";
            if (_wlVal      != null) _wlVal.text      = hasDaily ? $"{vm.dailyWins}–{vm.dailyLosses}" : "—";

            if (_classicPlayedVal != null) _classicPlayedVal.text = vm.classicGamesPlayed.ToString();
            if (_classicWonVal    != null) _classicWonVal.text    = vm.classicGamesWon.ToString();
            if (_taPlayedVal      != null) _taPlayedVal.text      = vm.timeAttackGamesPlayed.ToString();
            if (_taBestVal        != null) _taBestVal.text        = vm.timeAttackBestRound.ToString();

            if (_overallText != null)
                _overallText.text = vm.totalPuzzlesCompleted == 1
                    ? "1 puzzle completed"
                    : $"{vm.totalPuzzlesCompleted} puzzles completed";
        }

        // ── Task 40 — staggered reveal + number count-up ─────────────────────────

        /// <summary>
        /// Play the open animation: each card (and the coin pill) fades + scales in on a short stagger,
        /// while its numeric values roll up from zero. Re-entrant (stops any prior reveal first) and
        /// null/inactive-safe. ReduceMotion ⇒ snap to the final values instantly.
        /// </summary>
        private void PlayReveal()
        {
            if (!_built || !_hasData) return;

            StopReveal();

            // Reduced motion (or inactive host) — present the final state immediately.
            if (UIAnimations.ReduceMotion || !isActiveAndEnabled)
            {
                ApplyViewModel(_vm);
                foreach (var t in _revealTargets)
                {
                    if (t.group != null) t.group.alpha = 1f;
                    if (t.rect  != null) t.rect.localScale = Vector3.one;
                }
                return;
            }

            // Stagger the cards in, kicking off each card's count-ups as it appears.
            for (int i = 0; i < _revealTargets.Count; i++)
            {
                var t = _revealTargets[i];
                if (t.group != null) t.group.alpha = 0f;
                if (t.rect  != null) t.rect.localScale = Vector3.one * RevealStartScale;
                _running.Add(StartCoroutine(RevealOne(t, i * RevealStagger)));
            }

            // Numeric roll-ups, phased to land with their owning card's reveal.
            bool hasDaily = _vm.dailyGamesPlayed > 0;
            QueueCount(_coinText, _vm.totalCoins, "N0", 0f);
            QueueCount(_streakHero, _vm.currentStreak, null, RevealStagger);
            QueueCount(_longestVal, _vm.longestStreak, null, RevealStagger);
            if (hasDaily) QueueCount(_winRateVal, _vm.dailyWinRatePct, "0'%'", RevealStagger);
            QueueCount(_classicPlayedVal, _vm.classicGamesPlayed, null, RevealStagger * 2f);
            QueueCount(_classicWonVal,    _vm.classicGamesWon,    null, RevealStagger * 2f);
            QueueCount(_taPlayedVal, _vm.timeAttackGamesPlayed, null, RevealStagger * 3f);
            QueueCount(_taBestVal,   _vm.timeAttackBestRound,   null, RevealStagger * 3f);

            // Hero day-streak gets a brief gold pop once it has finished counting.
            if (_streakHero != null)
                _running.Add(StartCoroutine(HeroPop((RectTransform)_streakHero.transform,
                    RevealStagger + CountUpDuration)));
        }

        private void QueueCount(TMP_Text label, int target, string format, float delay)
        {
            if (label == null) return;
            _running.Add(StartCoroutine(CountUp(label, target, format, delay)));
        }

        private void StopReveal()
        {
            for (int i = 0; i < _running.Count; i++)
                if (_running[i] != null) StopCoroutine(_running[i]);
            _running.Clear();
        }

        // Fade + scale a single card/pill into place (ease-out, non-bouncy).
        private IEnumerator RevealOne(RevealTarget t, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            float e = 0f;
            while (e < RevealDuration)
            {
                e += Time.unscaledDeltaTime;
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(e / RevealDuration));
                if (t.group != null) t.group.alpha = p;
                if (t.rect  != null) t.rect.localScale = Vector3.one * Mathf.Lerp(RevealStartScale, 1f, p);
                yield return null;
            }
            if (t.group != null) t.group.alpha = 1f;
            if (t.rect  != null) t.rect.localScale = Vector3.one;
        }

        // Roll an integer label from 0 → target with an ease-out so it decelerates onto the final value.
        private IEnumerator CountUp(TMP_Text label, int target, string format, float delay)
        {
            // Nothing to animate for zero — just show it (already applied) and bail.
            if (target <= 0)
            {
                label.text = Format(0, format);
                yield break;
            }
            label.text = Format(0, format);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float e = 0f;
            while (e < CountUpDuration)
            {
                e += Time.unscaledDeltaTime;
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(e / CountUpDuration));
                int v = Mathf.Clamp(Mathf.RoundToInt(target * p), 0, target);
                label.text = Format(v, format);
                yield return null;
            }
            label.text = Format(target, format);
        }

        private static string Format(int v, string format)
            => string.IsNullOrEmpty(format) ? v.ToString() : v.ToString(format);

        // Brief gold pop on the hero number (1.0 → 1.12 → 1.0, ease-out) for a satisfying landing.
        private IEnumerator HeroPop(RectTransform rt, float delay)
        {
            if (rt == null) yield break;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            const float dur = 0.20f, peak = 1.12f;
            float e = 0f;
            while (e < dur)
            {
                e += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(e / dur);
                // up then down — symmetric ease-out either side of the midpoint.
                float s = p < 0.5f
                    ? Mathf.Lerp(1f, peak, UIAnimations.EaseOutCubic(p / 0.5f))
                    : Mathf.Lerp(peak, 1f, UIAnimations.EaseOutCubic((p - 0.5f) / 0.5f));
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ── Runtime layout (built once) ──────────────────────────────────────────
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            // Hide the flat, hand-placed scene labels — keep only the title + HOME button; the grouped
            // cards below replace them. (Deactivated GameObjects stay inactive across show/hide.)
            var title = transform.Find("TitleLabel");
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (title != null && child == title) continue;
                if (homeButton != null && child == homeButton.transform) continue;
                child.gameObject.SetActive(false);
            }
            if (title != null)
            {
                var tl = title.GetComponent<TMP_Text>();
                if (tl != null) { tl.text = "STATS"; tl.color = MenuPalette.TitleColor; tl.fontStyle = FontStyles.Bold; }
            }

            // HOME — restyle the scene-authored grey slab into the app's glowing outline button (no scene edit).
            if (homeButton != null)
                UIThemeManager.ApplyOutlineButton(homeButton, MenuPalette.SecondaryBorder, MenuPalette.SecondaryLabel);

            BuildCoinPill();
            BuildContent();
        }

        // Gold coin pill, top-right (display only) — consistent with the menu/shop.
        private void BuildCoinPill()
        {
            var go = new GameObject("CoinPill", typeof(RectTransform), typeof(Image),
                                    typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-36f, -210f); // inset from the top-right; clears the notch
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(img, Palette.AccentPeriwinkle); // theme ring (coin token + number stay gold)
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f; hlg.padding = new RectOffset(22, 24, 10, 10);
            var csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var token = new GameObject("Token", typeof(RectTransform), typeof(Image));
            token.transform.SetParent(go.transform, false);
            var dimg = token.GetComponent<Image>(); dimg.color = GameAccents.Gold; dimg.raycastTarget = false;
            UIThemeManager.ApplyRoundedButton(dimg); // rounded gold token
            var dle = token.AddComponent<LayoutElement>(); dle.preferredWidth = 26f; dle.preferredHeight = 26f;

            _coinText = MakeText(go.transform, "0", 30f, GameAccents.Gold, FontStyles.Bold, TextAlignmentOptions.Center);

            RegisterReveal(go); // coin pill reveals first
        }

        // Centered, top-anchored stack of grouped cards — tight, content-sized, hero-led (no dead space).
        private void BuildContent()
        {
            var go = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, CONTENT_TOP_Y);
            rt.sizeDelta = new Vector2(CONTENT_W, 0f); // height driven by the ContentSizeFitter
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = STACK_SPACING; vlg.childAlignment = TextAnchor.UpperCenter;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var content = (RectTransform)go.transform;

            // ── DAILY headline card — hero streak LEFT, support stats RIGHT, vertically centred together
            //    in one snug band so there is no dead vertical space. ──
            var daily = MakeCard(content, 0f);
            CardHeader(daily, "DAILY");
            var band = MakeHRow(daily, HERO_BAND_H, expandChildHeight: true, forceExpandWidth: false);
            var heroCol = MakeVColumn(band, HERO_BLOCK_W);
            _streakHero = MakeText(heroCol, "0", HERO_FONT, GameAccents.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            var heroCap = MakeText(heroCol, "DAY STREAK", CAPTION_FONT, Palette.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center);
            heroCap.characterSpacing = 4f;
            var tri = MakeHRow(band, 0f, expandChildHeight: false, forceExpandWidth: true);
            tri.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _longestVal = MakeStatCell(tri, "LONGEST");
            _winRateVal = MakeStatCell(tri, "WIN %");
            _wlVal      = MakeStatCell(tri, "W–L");
            RegisterReveal(daily.gameObject);

            // ── CLASSIC + TIME ATTACK — matched pair, content-sized, caption-over-value cells (no sparse gap). ──
            var modeRow = MakeHRow(content, 0f, expandChildHeight: false, forceExpandWidth: true, controlHeight: false);
            var classic = MakeCard(modeRow, 0f);
            CardHeader(classic, "CLASSIC");
            var classicCells = MakeHRow(classic, MODE_CELLS_H, expandChildHeight: true, forceExpandWidth: true);
            _classicPlayedVal = MakeStatCell(classicCells, "PLAYED");
            _classicWonVal    = MakeStatCell(classicCells, "WON");
            RegisterReveal(classic.gameObject);
            var timeAttack = MakeCard(modeRow, 0f);
            CardHeader(timeAttack, "TIME ATTACK");
            var taCells = MakeHRow(timeAttack, MODE_CELLS_H, expandChildHeight: true, forceExpandWidth: true);
            _taPlayedVal = MakeStatCell(taCells, "PLAYED");
            _taBestVal   = MakeStatCell(taCells, "BEST");
            RegisterReveal(timeAttack.gameObject);

            // ── OVERALL footer — slim full-width card (closing rhythm). ──
            var footer = MakeCard(content, 0f);
            _overallText = MakeText(footer, "0 puzzles completed", FOOTER_FONT, Palette.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            _overallText.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
            RegisterReveal(footer.gameObject);
        }

        // ── tiny builders (mirror ShopScreen) ────────────────────────────────────

        // Task 40 — register a built element as a staggered-reveal target. Ensures a CanvasGroup
        // (for the fade) and caches the RectTransform (for the scale-in). Order of calls == reveal order.
        private void RegisterReveal(GameObject go)
        {
            if (go == null) return;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            _revealTargets.Add(new RevealTarget { group = cg, rect = (RectTransform)go.transform });
        }

        private RectTransform MakeCard(Transform parent, float fixedHeight)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(img, GameAccents.CardOutline); // subtle slate ring, transparent centre
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = CARD_SPACING; vlg.padding = new RectOffset(CARD_PAD_X, CARD_PAD_X, CARD_PAD_TOP, CARD_PAD_BOT);
            vlg.childAlignment = TextAnchor.UpperCenter;
            var le = go.GetComponent<LayoutElement>(); le.flexibleWidth = 1f;
            if (fixedHeight > 0f) { le.minHeight = fixedHeight; le.preferredHeight = fixedHeight; }
            else go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return (RectTransform)go.transform;
        }

        private void CardHeader(Transform card, string label)
        {
            var t = MakeText(card, label, SECTION_FONT, MenuPalette.TitleColor, FontStyles.Bold, TextAlignmentOptions.Left);
            t.characterSpacing = 3f;
            t.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private RectTransform MakeHRow(Transform parent, float height, bool expandChildHeight,
                                       bool controlHeight = true, bool forceExpandWidth = true)
        {
            var go = new GameObject("HRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = forceExpandWidth;
            hlg.childControlHeight = controlHeight; hlg.childForceExpandHeight = expandChildHeight;
            hlg.spacing = ROW_SPACING; hlg.childAlignment = TextAnchor.MiddleCenter;
            var le = go.GetComponent<LayoutElement>();
            if (height > 0f) { le.minHeight = height; le.preferredHeight = height; }
            return (RectTransform)go.transform;
        }

        // A content-sized vertical column with an optional fixed width (used for the DAILY hero block).
        private RectTransform MakeVColumn(Transform parent, float fixedWidth)
        {
            var go = new GameObject("VCol", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 0f; vlg.childAlignment = TextAnchor.MiddleCenter;
            var le = go.GetComponent<LayoutElement>();
            if (fixedWidth > 0f) { le.preferredWidth = fixedWidth; le.flexibleWidth = 0f; }
            return (RectTransform)go.transform;
        }

        // A vertical "caption over value" cell for the 3-up daily row; returns the VALUE label.
        private TMP_Text MakeStatCell(Transform row, string caption)
        {
            var go = new GameObject("Cell", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(row, false);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 2f; vlg.childAlignment = TextAnchor.MiddleCenter;
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            MakeText(go.transform, caption, CAPTION_FONT, Palette.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center);
            return MakeText(go.transform, "0", SUPPORT_VALUE_FONT, Palette.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private TMP_Text MakeText(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.enableWordWrapping = false;
            return t;
        }

        private void OnHomeClicked() => OnBackToMenu?.Invoke();
    }
}
