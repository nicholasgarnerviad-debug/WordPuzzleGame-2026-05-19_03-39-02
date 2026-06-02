using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using WordPuzzle.Modes;

namespace WordPuzzle.UI
{
    public class ResultsScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI modeNameText;
        [SerializeField] private TextMeshProUGUI wordsFoundText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        // Task 1C — daily-streak surface. Optional; ShowDailyStreak no-ops if all three
        // are null. When unwired, the streak summary is appended to modeNameText so
        // players still see something without a scene edit.
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI longestStreakText;
        [SerializeField] private TextMeshProUGUI comeBackTomorrowText;

        // Task 2A — share button + toast. Optional in scene; ShowToast no-ops when null.
        [SerializeField] private Button shareButton;
        [SerializeField] private TextMeshProUGUI toastText;

        public event Action OnPlayAgain;
        public event Action OnMainMenu;
        public event Action OnShareRequested;
        // Task 16 — Puzzle Show "advance to the next unlocked tier" choice.
        public event Action OnNextTier;

        private Button nextTierButton; // Task 16 — created on demand for Puzzle Show.

        // Style tokens.
        // Task 8A: gold is kept for the primary streak number (focal element in streakText richtext)
        // and for the toast confirmation. longestStreakText (Best: N) is secondary — demoted to muted.
        private static readonly Color C_ACCENT_GOLD  = new Color32(0xC9, 0xB4, 0x58, 0xFF);
        private static readonly Color C_TEXT_PRIMARY  = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
        private static readonly Color C_TEXT_MUTED   = new Color32(0x8A, 0x93, 0xA1, 0xFF);

        private UnityAction playAgainAction;
        private UnityAction mainMenuAction;

        private void OnEnable()
        {
            if (playAgainButton != null)
            {
                playAgainAction = new UnityAction(() => OnPlayAgain?.Invoke());
                playAgainButton.onClick.AddListener(playAgainAction);
            }

            if (mainMenuButton != null)
            {
                mainMenuAction = new UnityAction(() => OnMainMenu?.Invoke());
                mainMenuButton.onClick.AddListener(mainMenuAction);
            }

            // §2.1 Visual swap: main-menu button becomes the spec "⌂ HOME" Home button.
            // SerializedField name unchanged per §2.3; behavior (navigate to MainMenu) unchanged.
            ApplyHomeButtonLabel(mainMenuButton);

            // Task 2A — share button listener.
            if (shareButton != null)
                shareButton.onClick.AddListener(() => OnShareRequested?.Invoke());
            if (toastText != null) toastText.gameObject.SetActive(false);

            // Task 21B — consistent rounded corners on the results buttons.
            foreach (var b in new[] { playAgainButton, mainMenuButton, shareButton })
                if (b != null) UIThemeManager.ApplyRoundedButton(b.GetComponent<Image>());
        }

        // §2.1/§2.3 Home-button visual swap.
        // Label "⌂ HOME"; falls back to "HOME" if U+2302 isn't present in the current TMP font.
        private static void ApplyHomeButtonLabel(Button host)
        {
            if (host == null) return;
            var label = host.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;
            bool glyphSupported = label.font != null && label.font.HasCharacter('⌂');
            label.text = glyphSupported ? "⌂ HOME" : "HOME";
            label.fontSize = glyphSupported ? 26f : 28f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
        }

        private void OnDisable()
        {
            if (playAgainButton != null && playAgainAction != null)
                playAgainButton.onClick.RemoveListener(playAgainAction);

            if (mainMenuButton != null && mainMenuAction != null)
                mainMenuButton.onClick.RemoveListener(mainMenuAction);

            if (shareButton != null) shareButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Task 2A — show a short auto-fading confirmation. Uses the dedicated
        /// toastText if wired, otherwise temporarily writes into modeNameText.
        /// </summary>
        public void ShowToast(string message)
        {
            if (toastText != null)
            {
                toastText.text = message;
                toastText.color = C_ACCENT_GOLD;
                toastText.gameObject.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(HideToastAfter(1.6f));
                return;
            }
            // Fallback: append to mode name briefly (silent if that's also null).
            if (modeNameText != null && !string.IsNullOrEmpty(message))
            {
                string original = modeNameText.text;
                modeNameText.text = $"{original}   <color=#C9B458>· {message}</color>";
                StopAllCoroutines();
                StartCoroutine(RestoreModeNameAfter(1.6f, original));
            }
        }

        private System.Collections.IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (toastText != null) toastText.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator RestoreModeNameAfter(float seconds, string original)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (modeNameText != null) modeNameText.text = original;
        }

        public void DisplayStats(GameModeStats stats)
        {
            if (modeNameText != null)
                modeNameText.text = $"{stats.modeName} Mode Results";

            if (wordsFoundText != null)
                wordsFoundText.text = $"Words Found: {stats.wordsFound}";

            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {stats.accuracy:F1}%";

            if (timeText != null)
                timeText.text = $"Time: {stats.totalTime:F1}s";

            if (scoreText != null)
                scoreText.text = $"Score: {stats.score}";
        }

        /// <summary>
        /// Task 1C — render the daily-streak surface after a daily completion.
        /// Accent-gold streak number; muted "Come back tomorrow" line. If the same
        /// day has already been counted, swap the bottom line for an "already
        /// counted today" notice.
        /// </summary>
        public void ShowDailyStreak(int currentStreak, int longestStreak, bool alreadyCountedToday)
        {
            string streakLine = $"Streak: <color=#C9B458>{currentStreak}</color> days";
            string bestLine   = $"Best: {longestStreak}";
            string footerLine = alreadyCountedToday
                ? "Already counted today"
                : "Come back tomorrow";

            if (streakText != null)
            {
                streakText.richText = true;
                streakText.text = streakLine;
                streakText.color = C_TEXT_PRIMARY;
                streakText.gameObject.SetActive(true);
            }
            if (longestStreakText != null)
            {
                longestStreakText.text = bestLine;
                // Task 8A: "Best: N" is secondary info — demoted to text-muted. Gold is reserved
                // for the primary streak number in the streakText richtext above.
                longestStreakText.color = C_TEXT_MUTED;
                longestStreakText.gameObject.SetActive(true);
            }
            if (comeBackTomorrowText != null)
            {
                comeBackTomorrowText.text = footerLine;
                comeBackTomorrowText.color = C_TEXT_MUTED;
                comeBackTomorrowText.gameObject.SetActive(true);
            }

            // Fallback when scene wiring is incomplete: append a short summary to
            // the mode-name line so the streak is still visible in v1 builds.
            if (streakText == null && longestStreakText == null && comeBackTomorrowText == null
                && modeNameText != null)
            {
                modeNameText.richText = true;
                modeNameText.text += $"\n<size=70%>Streak <color=#C9B458>{currentStreak}</color> · Best {longestStreak} · {footerLine}</size>";
            }
        }

        // ================================================================
        //  Task 16 — context-aware post-win surface configuration
        // ================================================================

        /// <summary>Endless run-end (Time Attack timer expired): "Play Again" → new run + Home.</summary>
        /// <param name="laddersCompleted">If >= 0, headline the run's solved-ladder count.</param>
        public void ConfigureForEndless(int laddersCompleted = -1)
        {
            SetButtonVisible(playAgainButton, true);
            SetButtonLabel(playAgainButton, "PLAY AGAIN");
            SetButtonVisible(nextTierButton, false);
            if (laddersCompleted >= 0 && wordsFoundText != null)
                wordsFoundText.text = laddersCompleted == 1
                    ? "1 puzzle solved" : $"{laddersCompleted} puzzles solved";
        }

        /// <summary>Daily: no "Play Again" (don't re-run the daily). Just Home (+ streak + share).</summary>
        public void ConfigureForDaily()
        {
            SetButtonVisible(playAgainButton, false);
            SetButtonVisible(nextTierButton, false);
        }

        /// <summary>
        /// Puzzle Show: "Next Puzzle" (another in the current tier) + optional "Tier N ▸"
        /// (when the next tier is unlocked) + Home.
        /// </summary>
        public void ConfigureForPuzzleShow(bool hasNextTier, int nextTierNumber)
        {
            SetButtonVisible(playAgainButton, true);
            SetButtonLabel(playAgainButton, "NEXT PUZZLE");

            EnsureNextTierButton();
            if (nextTierButton != null)
            {
                SetButtonVisible(nextTierButton, hasNextTier);
                if (hasNextTier) SetButtonLabel(nextTierButton, $"TIER {nextTierNumber} ▸");
            }
        }

        private void EnsureNextTierButton()
        {
            if (nextTierButton != null || playAgainButton == null) return;
            var parent = playAgainButton.transform.parent;
            var src = playAgainButton.GetComponent<RectTransform>();
            if (parent == null || src == null) return;

            var go = UnityEngine.Object.Instantiate(playAgainButton.gameObject, parent);
            go.name = "NextTierButton";
            nextTierButton = go.GetComponent<Button>();
            nextTierButton.onClick.RemoveAllListeners();
            nextTierButton.onClick.AddListener(() => OnNextTier?.Invoke());

            // Sit just above "Next Puzzle"; if a layout group manages the parent it reorders anyway.
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.sizeDelta.y + 16f);
            go.transform.SetSiblingIndex(playAgainButton.transform.GetSiblingIndex());

            // Gold emphasis on the tier-up action.
            var img = nextTierButton.GetComponent<Image>();
            if (img != null) img.color = C_ACCENT_GOLD;
        }

        private static void SetButtonVisible(Button b, bool visible)
        {
            if (b != null) b.gameObject.SetActive(visible);
        }

        private static void SetButtonLabel(Button b, string label)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null) t.text = label;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
