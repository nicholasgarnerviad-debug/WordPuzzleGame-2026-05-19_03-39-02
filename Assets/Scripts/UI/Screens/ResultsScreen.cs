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

        // Direction B — rich-text colour tags derive their hex from the canonical Palette (no raw theme hex).
        private static string Hx(Color c) => ColorUtility.ToHtmlStringRGB(c);

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
        // Task 36 36K — daily reward doubler (watch a rewarded ad to double today's daily coins).
        public event Action OnDoubleReward;

        private Button nextTierButton; // Task 16 — created on demand for Puzzle Show.
        private Button doublerButton;  // Task 36 36K — created on demand for the daily reward doubler.

        // Daily results state. _isDaily routes the layout to Daily's OWN block (not Puzzle Show's).
        // _dailyStreakLine is a dedicated, created-once streak label that is SET (never appended) each view
        // — the root-cause fix for the status line stacking on repeat Daily opens.
        private bool _isDaily;
        private TextMeshProUGUI _dailyStreakLine;

        // Style tokens.
        // Task 8A: gold is kept for the primary streak number (focal element in streakText richtext)
        // and for the toast confirmation. longestStreakText (Best: N) is secondary — demoted to muted.
        // Task 38 — gold now comes from the shared GameAccents.Gold token (no inline hex re-declared here).
        private static readonly Color C_TEXT_PRIMARY  = Palette.TextPrimary;
        private static readonly Color C_TEXT_MUTED   = Palette.TextMuted;

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

            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background

            // Task 25 — outline ("ghost") buttons: Play Again = green CTA, Home/Share = muted; light labels.
            UIThemeManager.ApplyOutlineButton(playAgainButton, Palette.AccentAqua, Palette.TextPrimary);
            UIThemeManager.ApplyOutlineButton(mainMenuButton,  Palette.AccentPeriwinkle, Palette.TextPrimary);
            UIThemeManager.ApplyOutlineButton(shareButton,     Palette.AccentPeriwinkle, Palette.TextPrimary);

            // Consolidate + modernize the stat block (display-only; see StyleResultsLayout).
            StyleResultsLayout();
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
            label.color = Palette.TextPrimary;
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
                toastText.color = GameAccents.Gold;
                toastText.gameObject.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(HideToastAfter(1.6f));
                return;
            }
            // Fallback: append to mode name briefly (silent if that's also null).
            if (modeNameText != null && !string.IsNullOrEmpty(message))
            {
                string original = modeNameText.text;
                modeNameText.text = $"{original}   <color=#{Hx(Palette.Coins)}>· {message}</color>";
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
                modeNameText.text = $"{stats.modeName} Results"; // drop redundant "Mode" — reads cleaner

            if (wordsFoundText != null)
                wordsFoundText.text = $"Words Found: {stats.wordsFound}";

            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {stats.accuracy:F1}%";

            if (timeText != null)
                timeText.text = $"Time: {stats.totalTime:F1}s";

            if (scoreText != null)
                scoreText.text = $"Score: {stats.score}";
        }

        // ================================================================
        //  Results layout consolidation + polish (display-only)
        // ================================================================
        /// <summary>
        /// One clean, evenly-spaced stat block on the palette. An older design left an unwired
        /// "StatsContainer" list (WORDS FOUND / TIME / ACCURACY / BEST WORD) that overlapped the real,
        /// code-bound values with empty dashes — it's dead scene UI (nothing references it, and
        /// GameModeStats has no best-word field), so it is hidden, not populated. The real values stay in
        /// the existing SerializeField texts; here we only restyle/position them (colour, size, anchor) and
        /// fix the oversized clipping title. Runtime-driven (no scene edit). NOTE: ResultsScreen is one
        /// shared instance, so this consolidation applies to every mode's results equally — no per-mode
        /// stats/scoring/logic is touched.
        /// </summary>
        private void StyleResultsLayout()
        {
            // Common — kill the orphaned duplicate list (resolves the overlap + the empty dashes).
            var orphan = transform.Find("StatsContainer");
            if (orphan != null) orphan.gameObject.SetActive(false);

            var scoreLabel = transform.Find("ScoreLabel");

            // Title — one line under the notch, on the shared title token (was 90pt, wrapped + clipped).
            if (modeNameText != null)
            {
                if (_isDaily) modeNameText.text = "Daily Results"; // never inherit a stale "Puzzle Show Results"
                modeNameText.color = MenuPalette.TitleColor;
                modeNameText.fontStyle = FontStyles.Bold;
                modeNameText.fontSize = 54f;
                modeNameText.enableAutoSizing = false;
                modeNameText.enableWordWrapping = true;
                modeNameText.alignment = TextAlignmentOptions.Center;
                PlaceTopCenter(modeNameText.rectTransform, -285f, 110f);
            }

            if (_isDaily)
            {
                // ── Daily's OWN block: a grade/par hero + the streak line. Hide the score/accuracy/time
                // metrics — Daily's result is the grade + streak (not a numeric score), and the re-show path
                // never repopulates those fields, so hiding them also prevents stale leftovers from a prior
                // Puzzle Show view. This is what makes the Daily screen distinct, not a renamed Puzzle Show.
                SetActiveTmp(scoreText, false);
                SetActiveTmp(accuracyText, false);
                SetActiveTmp(timeText, false);
                if (scoreLabel != null) scoreLabel.gameObject.SetActive(false);

                if (wordsFoundText != null) // holds the grade/par/stars line (set by ShowDailyResult)
                {
                    wordsFoundText.gameObject.SetActive(true);
                    wordsFoundText.color = Palette.TextPrimary;
                    wordsFoundText.fontStyle = FontStyles.Bold;
                    wordsFoundText.fontSize = 44f;
                    wordsFoundText.enableAutoSizing = false;
                    wordsFoundText.enableWordWrapping = true;
                    wordsFoundText.alignment = TextAlignmentOptions.Center;
                    PlaceTopCenter(wordsFoundText.rectTransform, -770f, 120f);
                }

                if (_dailyStreakLine != null)
                    PlaceTopCenter(_dailyStreakLine.rectTransform, -960f, 130f);
                return;
            }

            // ── Non-daily (Puzzle Show / Time Attack) — the standard score/accuracy/time block. ──
            // Re-show anything a prior Daily hid, and hide the Daily-only streak line.
            SetActiveTmp(scoreText, true);
            SetActiveTmp(accuracyText, true);
            SetActiveTmp(timeText, true);
            if (scoreLabel != null) scoreLabel.gameObject.SetActive(true);
            if (_dailyStreakLine != null) _dailyStreakLine.gameObject.SetActive(false);

            // "FINAL SCORE" caption (scene-only label) — quiet, muted.
            StyleResultText(scoreLabel, 28f, Palette.TextMuted, FontStyles.Bold, -560f, 44f);

            // Score — the hero number. Gold is the app's reward/achievement accent (same token as the
            // streak), routed through GameAccents.Gold so it's a token, not a scene straggler.
            if (scoreText != null)
            {
                scoreText.color = GameAccents.Gold;
                scoreText.fontStyle = FontStyles.Bold;
                scoreText.fontSize = 64f;
                scoreText.enableAutoSizing = false;
                scoreText.alignment = TextAlignmentOptions.Center;
                PlaceTopCenter(scoreText.rectTransform, -680f, 88f);
            }

            // The three stat lines — bright primary text, readable, evenly spaced.
            PlaceStatLine(wordsFoundText, -920f);
            PlaceStatLine(accuracyText,   -1040f);
            PlaceStatLine(timeText,       -1160f);
        }

        private static void SetActiveTmp(TMP_Text t, bool on)
        {
            if (t != null) t.gameObject.SetActive(on);
        }

        private static void PlaceStatLine(TMP_Text t, float topOffsetY)
        {
            if (t == null) return;
            t.color = Palette.TextPrimary;
            t.fontStyle = FontStyles.Normal;
            t.fontSize = 34f;
            t.enableAutoSizing = false;
            t.alignment = TextAlignmentOptions.Center;
            PlaceTopCenter(t.rectTransform, topOffsetY, 60f);
        }

        private static void StyleResultText(Transform tf, float size, Color color, FontStyles style,
            float topOffsetY, float height)
        {
            if (tf == null) return;
            var t = tf.GetComponent<TMP_Text>();
            if (t == null) return;
            t.color = color;
            t.fontStyle = style;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.alignment = TextAlignmentOptions.Center;
            PlaceTopCenter(t.rectTransform, topOffsetY, height);
        }

        // Anchor to top-centre and place at a deterministic offset below the top edge, so the whole block
        // reads as one even rhythm regardless of the scene's mixed authored anchors (and stays clear of the
        // notch at the top and the buttons below).
        private static void PlaceTopCenter(RectTransform rt, float y, float height)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(900f, height);
        }

        /// <summary>
        /// Task 1C — render the daily-streak surface after a daily completion.
        /// Accent-gold streak number; muted "Come back tomorrow" line. If the same
        /// day has already been counted, swap the bottom line for an "already
        /// counted today" notice.
        /// </summary>
        public void ShowDailyStreak(int currentStreak, int longestStreak, bool alreadyCountedToday)
        {
            string streakLine = $"Streak: <color=#{Hx(Palette.Coins)}>{currentStreak}</color> days";
            string bestLine   = $"Best: {longestStreak}";
            string footerLine = alreadyCountedToday
                ? "Already counted today"
                : "Come back tomorrow";

            // If the scene ever wires the dedicated streak fields, drive them (SET — never additive).
            bool wired = streakText != null || longestStreakText != null || comeBackTomorrowText != null;
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

            // BUG 2 — the stacking root-cause fix. The streak previously APPENDED to the title
            // (modeNameText.text += …) whenever those scene fields were unwired (which they are), so it grew
            // by one line on EVERY Daily open because nothing reset it. Render into a dedicated label that is
            // created ONCE (cached → no per-open instantiation) and whose text is SET/overwritten each call
            // → exactly one streak line per view, no matter how many times Daily is opened.
            EnsureDailyStreakLine();
            if (_dailyStreakLine != null)
            {
                if (wired)
                {
                    _dailyStreakLine.gameObject.SetActive(false); // the scene fields own it; don't double up
                }
                else
                {
                    _dailyStreakLine.richText = true;
                    _dailyStreakLine.text =
                        $"Streak <color=#{Hx(Palette.Coins)}>{currentStreak}</color> · Best {longestStreak}\n" +
                        $"<size=80%><color=#{Hx(Palette.TextMuted)}>{footerLine}</color></size>";
                    _dailyStreakLine.gameObject.SetActive(true);
                }
            }
        }

        // Create the dedicated Daily streak label once (cached). Positioned by StyleResultsLayout's Daily
        // branch on show; a sensible default here covers the case it is set before the first layout pass.
        private void EnsureDailyStreakLine()
        {
            if (_dailyStreakLine != null) return;
            var go = new GameObject("DailyStreakLine", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dailyStreakLine = go.AddComponent<TextMeshProUGUI>();
            _dailyStreakLine.fontSize = 30f;
            _dailyStreakLine.color = Palette.TextPrimary;
            _dailyStreakLine.alignment = TextAlignmentOptions.Center;
            _dailyStreakLine.raycastTarget = false;
            _dailyStreakLine.enableWordWrapping = true;
            PlaceTopCenter(_dailyStreakLine.rectTransform, -960f, 130f);
        }

        // ================================================================
        //  Task 16 — context-aware post-win surface configuration
        // ================================================================

        /// <summary>Endless run-end (Time Attack timer expired): "Play Again" → new run + Home.</summary>
        /// <param name="laddersCompleted">If >= 0, headline the run's solved-ladder count.</param>
        public void ConfigureForEndless(int laddersCompleted = -1)
        {
            _isDaily = false;
            SetButtonVisible(playAgainButton, true);
            SetButtonLabel(playAgainButton, "PLAY AGAIN");
            SetButtonVisible(nextTierButton, false);
            HideDailyOnlyWidgets(); // Task 38 — a non-daily result shows NONE of the daily-only widgets
            if (laddersCompleted >= 0 && wordsFoundText != null)
                wordsFoundText.text = laddersCompleted == 1
                    ? "1 puzzle solved" : $"{laddersCompleted} puzzles solved";
        }

        /// <summary>Daily: no "Play Again" (don't re-run the daily). Just Home (+ streak + share).</summary>
        public void ConfigureForDaily()
        {
            _isDaily = true;
            SetButtonVisible(playAgainButton, false);
            SetButtonVisible(nextTierButton, false);
            // BUG 1 — Daily shows its OWN screen, not Puzzle Show's. Set the Daily title here so it's correct
            // on BOTH paths: the fresh run (DisplayStats had set "{mode} Results") AND the re-show path
            // (ShowStoredDailyResult never calls DisplayStats, so the title was stale from the last view —
            // that's why a prior "Puzzle Show Results" leaked onto the Daily screen).
            if (modeNameText != null) modeNameText.text = "Daily Results";
        }

        /// <summary>
        /// Daily 2.0 (Task 36) — headline the par-scored result: grade + stars, or "Failed today",
        /// then "Par N · You got X". Reuses the words-found line (falls back to the mode-name line).
        /// </summary>
        public void ShowDailyResult(int stars, int par, int playerSteps, bool failed, int dailyNumber, int streak)
        {
            int s = Mathf.Clamp(stars, 0, 3);
            // Use Dingbats stars (U+272D/U+2729): the plain ★ (U+2605, Misc-Symbols block) isn't in the
            // dynamic fallback font's source → it rendered as a □ tofu box. The font demonstrably covers the
            // Dingbats block (the ✓ state-icon renders), so these five-pointed stars render where ★ didn't.
            string starGlyphs = new string('✭', s) + new string('✩', 3 - s);
            string headline = failed
                ? "Failed today"
                : $"{DailyGradeName(s)}  <color=#{Hx(Palette.Coins)}>{starGlyphs}</color>";
            string line = $"{headline}  ·  Par {par}  ·  You got {playerSteps}";

            if (wordsFoundText != null)
            {
                wordsFoundText.richText = true;
                wordsFoundText.text = line;
            }
            else if (modeNameText != null)
            {
                modeNameText.richText = true;
                modeNameText.text += $"\n<size=80%>{line}</size>";
            }
        }

        private static string DailyGradeName(int stars) =>
            stars >= 3 ? "Perfect" : stars == 2 ? "Good" : stars == 1 ? "Solved" : "Failed";

        /// <summary>
        /// Puzzle Show: "Next Puzzle" (another in the current tier) + optional "Tier N ▸"
        /// (when the next tier is unlocked) + Home.
        /// </summary>
        public void ConfigureForPuzzleShow(bool hasNextTier, int nextTierNumber)
        {
            _isDaily = false;
            SetButtonVisible(playAgainButton, true);
            SetButtonLabel(playAgainButton, "NEXT PUZZLE");
            HideDailyOnlyWidgets(); // Task 38 — a non-daily result shows NONE of the daily-only widgets

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

            // Task 25 — gold outline emphasis on the tier-up action; light label.
            UIThemeManager.ApplyOutlineButton(nextTierButton, GameAccents.Gold, Palette.TextPrimary);
        }

        /// <summary>
        /// Task 36 36K — show/hide the "double your daily reward" action (a rewarded-ad faucet).
        /// Created on demand by cloning the Play Again template (same pattern as the tier button), so
        /// it needs no scene edit. Shown even when no ad is loaded yet; the tap handler degrades to a
        /// toast so the surface is still placeable in builds without an ad SDK.
        /// </summary>
        public void ConfigureDailyDoubler(bool available)
        {
            EnsureDoublerButton();
            SetButtonVisible(doublerButton, available);
            if (available) SetButtonLabel(doublerButton, "DOUBLE (WATCH AD)");
        }

        /// <summary>Hide the doubler and confirm the bonus (call after the ad grants the extra coins).</summary>
        public void MarkDoublerClaimed(string toast)
        {
            SetButtonVisible(doublerButton, false);
            if (!string.IsNullOrEmpty(toast)) ShowToast(toast);
        }

        private void EnsureDoublerButton()
        {
            if (doublerButton != null || playAgainButton == null) return;
            var parent = playAgainButton.transform.parent;
            var src = playAgainButton.GetComponent<RectTransform>();
            if (parent == null || src == null) return;

            var go = UnityEngine.Object.Instantiate(playAgainButton.gameObject, parent);
            go.name = "DailyDoublerButton";
            doublerButton = go.GetComponent<Button>();
            doublerButton.onClick.RemoveAllListeners();
            doublerButton.onClick.AddListener(() => OnDoubleReward?.Invoke());

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.sizeDelta.y + 16f);
            go.transform.SetSiblingIndex(playAgainButton.transform.GetSiblingIndex());

            // Gold outline emphasis (a bonus action); light label.
            UIThemeManager.ApplyOutlineButton(doublerButton, GameAccents.Gold, Palette.TextPrimary);
        }

        // Task 38 — the ResultsScreen is a single shared instance, so daily-only widgets persist their
        // visibility between runs. A non-daily result (PuzzleShow / Time Attack) must therefore explicitly
        // hide the reward doubler AND the daily streak lines, or they leak in from the last daily.
        private void HideDailyOnlyWidgets()
        {
            SetButtonVisible(doublerButton, false);
            if (streakText != null)           streakText.gameObject.SetActive(false);
            if (longestStreakText != null)    longestStreakText.gameObject.SetActive(false);
            if (comeBackTomorrowText != null) comeBackTomorrowText.gameObject.SetActive(false);
            if (_dailyStreakLine != null)     _dailyStreakLine.gameObject.SetActive(false);
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
