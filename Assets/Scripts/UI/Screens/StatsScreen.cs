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

        private void OnHomeClicked() => OnBackToMenu?.Invoke();
    }
}
