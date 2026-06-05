using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    // -------------------------------------------------------------------------
    //  StatsScreen  (Task 9F; rebuilt Task 38)
    //  Displays persisted player statistics in clean, grouped sections. A pure
    //  view-model struct (StatsViewModel) holds all data so QA can unit-test the
    //  number derivation without a MonoBehaviour. The VISUAL layout is built at
    //  RUNTIME from layout-group cards (the same code-driven approach as
    //  MainMenuScreen/ShopScreen) — auto-spaced, so there are no hand-placed
    //  positions to collide. The flat scene-authored labels are hidden.
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

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject); // shared true-black background
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.onClick.RemoveListener(OnHomeClicked);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Build (once) the grouped layout, then display stats from persisted data. Null-safe.</summary>
        public void Populate(DailyProgress daily, PlayerProgress player)
        {
            var vm = BuildStatsViewModel(daily, player);
            EnsureBuilt();
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
            UIThemeManager.ApplyOutlineButton(img, GameAccents.Gold); // gold ring
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
        }

        // Centered, top-anchored vertical stack of grouped cards (auto-spaced — no manual positions).
        private void BuildContent()
        {
            var go = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 300f); // top edge just below the title; HOME stays at the bottom
            rt.sizeDelta = new Vector2(960f, 0f);        // height driven by the ContentSizeFitter
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 22f; vlg.childAlignment = TextAnchor.UpperCenter;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var content = (RectTransform)go.transform;

            // DAILY card — hero current-streak + a 3-up row (Longest / Win % / W–L).
            var daily = MakeCard(content, 0f);
            CardHeader(daily, "DAILY");
            _streakHero = MakeText(daily, "0", 90f, GameAccents.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            _streakHero.gameObject.AddComponent<LayoutElement>().minHeight = 98f;
            var heroCap = MakeText(daily, "DAY STREAK", 22f, MenuPalette.SecondaryBorder, FontStyles.Bold, TextAlignmentOptions.Center);
            heroCap.characterSpacing = 4f;
            var tri = MakeHRow(daily, 92f, true);
            _longestVal = MakeStatCell(tri, "LONGEST");
            _winRateVal = MakeStatCell(tri, "WIN %");
            _wlVal      = MakeStatCell(tri, "W–L");

            // CLASSIC + TIME ATTACK side-by-side.
            var modeRow = MakeHRow(content, 196f, true);
            var classic = MakeCard(modeRow, 196f);
            CardHeader(classic, "CLASSIC");
            _classicPlayedVal = MakeKeyVal(classic, "Played");
            _classicWonVal    = MakeKeyVal(classic, "Won");
            var timeAttack = MakeCard(modeRow, 196f);
            CardHeader(timeAttack, "TIME ATTACK");
            _taPlayedVal = MakeKeyVal(timeAttack, "Played");
            _taBestVal   = MakeKeyVal(timeAttack, "Best round");

            // OVERALL footer.
            _overallText = MakeText(content, "0 puzzles completed", 24f, MenuPalette.SecondaryBorder, FontStyles.Normal, TextAlignmentOptions.Center);
            _overallText.gameObject.AddComponent<LayoutElement>().minHeight = 40f;
        }

        // ── tiny builders (mirror ShopScreen) ────────────────────────────────────
        private RectTransform MakeCard(Transform parent, float fixedHeight)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(img, GameAccents.CardOutline); // subtle slate ring, transparent centre
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 8f; vlg.padding = new RectOffset(28, 28, 20, 22);
            vlg.childAlignment = TextAnchor.UpperCenter;
            var le = go.GetComponent<LayoutElement>(); le.flexibleWidth = 1f;
            if (fixedHeight > 0f) { le.minHeight = fixedHeight; le.preferredHeight = fixedHeight; }
            else go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return (RectTransform)go.transform;
        }

        private void CardHeader(Transform card, string label)
        {
            var t = MakeText(card, label, 24f, MenuPalette.TitleColor, FontStyles.Bold, TextAlignmentOptions.Left);
            t.characterSpacing = 3f;
            t.gameObject.AddComponent<LayoutElement>().minHeight = 34f;
        }

        private RectTransform MakeHRow(Transform parent, float height, bool expandHeight)
        {
            var go = new GameObject("HRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = expandHeight;
            hlg.spacing = 14f; hlg.childAlignment = TextAnchor.MiddleCenter;
            var le = go.GetComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height;
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
            MakeText(go.transform, caption, 19f, MenuPalette.SecondaryBorder, FontStyles.Bold, TextAlignmentOptions.Center);
            return MakeText(go.transform, "0", 34f, MenuPalette.SecondaryLabel, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        // A "Caption ........ Value" row (muted caption left, prominent value right); returns the VALUE label.
        private TMP_Text MakeKeyVal(Transform card, string caption)
        {
            var row = new GameObject("KV", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(card, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft; hlg.spacing = 6f;
            row.GetComponent<LayoutElement>().minHeight = 44f;
            var cap = MakeText(row.transform, caption, 24f, MenuPalette.SecondaryBorder, FontStyles.Normal, TextAlignmentOptions.Left);
            cap.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var val = MakeText(row.transform, "0", 30f, MenuPalette.SecondaryLabel, FontStyles.Bold, TextAlignmentOptions.Right);
            var vle = val.gameObject.AddComponent<LayoutElement>(); vle.preferredWidth = 100f; vle.flexibleWidth = 0f;
            return val;
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
