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

        // Style tokens (accent-gold for streak number, text-muted for the come-back line).
        private static readonly Color C_ACCENT_GOLD = new Color32(0xC9, 0xB4, 0x58, 0xFF);
        private static readonly Color C_TEXT_PRIMARY = new Color32(0xE7, 0xE1, 0xC4, 0xFF);
        private static readonly Color C_TEXT_MUTED  = new Color32(0x8A, 0x93, 0xA1, 0xFF);

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
                longestStreakText.color = C_ACCENT_GOLD;
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

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
