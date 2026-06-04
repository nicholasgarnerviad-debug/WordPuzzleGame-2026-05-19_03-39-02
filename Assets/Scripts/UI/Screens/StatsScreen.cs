using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordPuzzle.Persistence;

namespace WordPuzzle.UI
{
    // -------------------------------------------------------------------------
    //  StatsScreen  (Task 9F)
    //  Displays persisted player statistics.  A pure view-model struct
    //  (StatsViewModel) holds all data so QA can unit-test number derivation
    //  without a MonoBehaviour.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pure, testable view-model for the stats screen.
    /// Build with <see cref="StatsScreen.BuildStatsViewModel"/>.
    /// </summary>
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

    /// <summary>
    /// Statistics screen (Task 9F).
    /// Accepts data via <see cref="Populate"/> (MonoBehaviour path) or call
    /// <see cref="BuildStatsViewModel"/> directly for unit-testing.
    /// Gold (#C9B458) is reserved for the headline streak number only.
    /// All other values use text-muted (#8A93A1) or text-primary (#E7E1C4).
    /// </summary>
    public class StatsScreen : MonoBehaviour
    {
        // --- Headline: current streak (gold, large) ---
        [SerializeField] private TextMeshProUGUI currentStreakValue;

        // --- Secondary stats (text-primary) ---
        [SerializeField] private TextMeshProUGUI longestStreakValue;
        [SerializeField] private TextMeshProUGUI dailyCompletedValue;
        [SerializeField] private TextMeshProUGUI totalCoinsValue;
        [SerializeField] private TextMeshProUGUI totalPuzzlesValue;

        // --- Classic mode ---
        [SerializeField] private TextMeshProUGUI classicPlayedValue;
        [SerializeField] private TextMeshProUGUI classicWonValue;

        // --- Time Attack mode ---
        [SerializeField] private TextMeshProUGUI timeAttackPlayedValue;
        [SerializeField] private TextMeshProUGUI timeAttackBestRoundValue;

        // Task 36 (36G/36K) — daily trailing-365 W/L record + win%. The values are computed in the
        // view-model but the prefab has no slot for them, so this label is synthesized at RUNTIME next to
        // the Daily-Completed stat (no scene edit needed; matches the project's runtime-driven UI idiom).
        private TextMeshProUGUI dailyRecordValue;

        // --- Navigation ---
        [SerializeField] private Button homeButton;

        public event Action OnBackToMenu;

        // Design tokens
        private static readonly Color Gold      = new Color32(0xC9, 0xB4, 0x58, 0xFF);
        private static readonly Color TextMuted = new Color32(0x8A, 0x93, 0xA1, 0xFF);
        private static readonly Color TextPrimary = new Color32(0xE7, 0xE1, 0xC4, 0xFF);

        private void OnEnable()
        {
            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
        }

        private void OnDisable()
        {
            if (homeButton != null)
                homeButton.onClick.RemoveAllListeners();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Build and display stats from persisted data objects.
        /// Null-safe: missing data objects are treated as defaults.
        /// </summary>
        public void Populate(DailyProgress daily, PlayerProgress player)
        {
            var vm = BuildStatsViewModel(daily, player);
            ApplyViewModel(vm);
        }

        /// <summary>
        /// Pure factory — derive the view-model from raw data.
        /// Null-safe; unit-testable without MonoBehaviour.
        /// </summary>
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

        private void ApplyViewModel(StatsViewModel vm)
        {
            // Headline streak: gold only
            SetLabel(currentStreakValue,     vm.currentStreak.ToString(), Gold);

            // Secondary: text-primary
            SetLabel(longestStreakValue,     vm.longestStreak.ToString(),   TextPrimary);
            SetLabel(dailyCompletedValue,    vm.dailyCompleted.ToString(),  TextPrimary);

            // Daily 2.0 (Task 36) — trailing-365 W/L record + win%, on a runtime-synthesized label.
            EnsureDailyRecordLabel();
            if (dailyRecordValue != null)
            {
                dailyRecordValue.text = vm.dailyGamesPlayed > 0
                    ? $"Daily W-L  <color=#E7E1C4>{vm.dailyWins}-{vm.dailyLosses}</color>  <color=#8A93A1>({vm.dailyWinRatePct}% wins)</color>"
                    : "Daily W-L  <color=#8A93A1>—</color>";
            }
            SetLabel(totalCoinsValue,        vm.totalCoins.ToString(),      TextPrimary);
            SetLabel(totalPuzzlesValue,      vm.totalPuzzlesCompleted.ToString(), TextPrimary);

            // Mode stats: text-muted
            SetLabel(classicPlayedValue,     vm.classicGamesPlayed.ToString(), TextMuted);
            SetLabel(classicWonValue,        vm.classicGamesWon.ToString(),    TextMuted);
            SetLabel(timeAttackPlayedValue,  vm.timeAttackGamesPlayed.ToString(), TextMuted);
            SetLabel(timeAttackBestRoundValue, vm.timeAttackBestRound.ToString(), TextMuted);
        }

        private static void SetLabel(TextMeshProUGUI label, string text, Color color)
        {
            if (label == null) return;
            label.text  = text;
            label.color = color;
        }

        /// <summary>
        /// Synthesize the daily W/L record label at runtime, styled after the Daily-Completed value and
        /// placed just beneath it. Idempotent (created once). No-op if that anchor label isn't wired.
        /// Placed after the anchor in sibling order so a VerticalLayoutGroup flows it directly below; for
        /// an absolutely-positioned stats panel it also offsets one row down from the anchor.
        /// </summary>
        private void EnsureDailyRecordLabel()
        {
            if (dailyRecordValue != null) return;
            var anchor = dailyCompletedValue;
            if (anchor == null || anchor.transform.parent == null) return;

            var go = new GameObject("DailyRecordValue", typeof(RectTransform));
            go.transform.SetParent(anchor.transform.parent, false);

            var t = go.AddComponent<TextMeshProUGUI>();
            t.font        = anchor.font;
            t.fontSize    = anchor.fontSize * 0.8f;
            t.fontStyle   = anchor.fontStyle;
            t.alignment   = anchor.alignment;
            t.richText    = true;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            t.color = TextMuted;

            var src = anchor.rectTransform;
            var rt  = t.rectTransform;
            rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
            rt.sizeDelta = src.sizeDelta; rt.localScale = Vector3.one;
            go.transform.SetSiblingIndex(anchor.transform.GetSiblingIndex() + 1);
            rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, -(src.rect.height + 10f));

            dailyRecordValue = t;
        }

        private void OnHomeClicked() => OnBackToMenu?.Invoke();
    }
}
