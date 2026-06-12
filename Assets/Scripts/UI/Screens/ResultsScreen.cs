using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using WordPuzzle.Modes;
using WordPuzzle.UI.Components;

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

        // Task 45 — payout choreography. The GameBootstrap daily chain arrives in PIECES
        // (result → coins → doubler → streak), so the pieces are BUFFERED and the single master
        // sequence starts at ShowDailyStreak — the caboose of BOTH the fresh and re-show paths.
        // Exactly ONE payout coroutine per view, stopped before any restart (never stacked —
        // the Reveal-flicker lesson). The re-show path is a recall, not a payout: at rest,
        // no haptics, no doubler re-offer (Task 38's lock stays authoritative).
        private Coroutine _payoutRoutine;
        private bool _payoutAnimated;          // fresh show (animate) vs re-show / ReduceMotion (rest)
        private bool _pendingDoublerReveal;    // the doubler waits for the coin count-up
        private int  _pendingCoinReward = -1;  // <0 ⇒ no coin line this view
        private int  _pendingLadders = -1;     // Time Attack "N puzzles solved" count-up
        private int  _earnedStars;
        private TextMeshProUGUI _dailyCoinLine;
        private IHaptics _haptics;             // one light tap per EARNED star (toggle-respecting impl)
        private SfxManager _sfx;               // Task 45 no-op hooks — clips drop in later (§13)

        /// <summary>True while a payout sequence is animating (test probe; false at rest).</summary>
        public bool PayoutAnimating => _payoutRoutine != null;

        /// <summary>Task 45 — inject haptics (GameBootstrap, beside the gameplay wiring).</summary>
        public void SetHaptics(IHaptics haptics) => _haptics = haptics;

        /// <summary>Task 45 — inject the SFX manager (no-op slots until clips exist).</summary>
        public void SetSfxManager(SfxManager sfx) => _sfx = sfx;

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
            if (toastText != null)
            {
                TypeScale.Apply(toastText, TypeRole.Body); // Task 42 (colour set per toast)
                toastText.gameObject.SetActive(false);
            }

            UIThemeManager.ApplyScreenBackground(gameObject); // Task 25 — true-black background

            // Task 43 tiers — Play Again = aqua outline CTA, Share = muted outline; HOME recedes to
            // ghost (tinted "⌂ HOME" text on an invisible hit target — navigation, not an action).
            UIThemeManager.ApplyOutlineButton(playAgainButton, Palette.AccentAqua, Palette.TextPrimary);
            UIThemeManager.ApplyGhostButton(mainMenuButton,    Palette.AccentPeriwinkle);
            UIThemeManager.ApplyOutlineButton(shareButton,     Palette.AccentPeriwinkle, Palette.TextPrimary);

            // Task 42 — outline labels carry the Label role (the outline calls only tint them).
            // NOT the ghost HOME: ApplyGhostButton already applies the role and then tints, and a
            // later TypeScale.Apply would reset the label back to TextPrimary.
            ApplyButtonLabelType(playAgainButton);
            ApplyButtonLabelType(shareButton);

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
            TypeScale.Apply(label, TypeRole.Label); // Task 42
            label.alignment = TextAlignmentOptions.Center;
        }

        // Task 42 — apply the Label role to a button's TMP child (idempotent).
        private static void ApplyButtonLabelType(Button host)
        {
            if (host == null) return;
            var label = host.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) TypeScale.Apply(label, TypeRole.Label);
        }

        private void OnDisable()
        {
            StopPayout(); // Task 45 — never carry a half-run payout into the next view

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
            // Task 45 — stop only the prior TOAST timer. The old StopAllCoroutines() here would
            // have killed an in-flight payout sequence whenever a toast appeared mid-celebration
            // (e.g. tapping Share during the star pops).
            if (toastText != null)
            {
                toastText.text = message;
                toastText.color = GameAccents.Gold;
                toastText.gameObject.SetActive(true);
                if (_toastRoutine != null) StopCoroutine(_toastRoutine);
                _toastRoutine = StartCoroutine(HideToastAfter(1.6f));
                return;
            }
            // Fallback: append to mode name briefly (silent if that's also null).
            if (modeNameText != null && !string.IsNullOrEmpty(message))
            {
                string original = modeNameText.text;
                modeNameText.text = $"{original}   <color=#{Hx(Palette.Coins)}>· {message}</color>";
                if (_toastRoutine != null) StopCoroutine(_toastRoutine);
                _toastRoutine = StartCoroutine(RestoreModeNameAfter(1.6f, original));
            }
        }

        private Coroutine _toastRoutine;

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
            StopPayout(); // Task 45 — a re-shown view renders fresh; never stack payout coroutines
            _pendingLadders = -1;

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

            // Task 45 — the run-end payout beat: the hero Final Score counts up, then
            // Words/Accuracy/Time fade in as ONE calm group (no per-stat stagger). ReduceMotion
            // renders everything at rest (the guard here + the helpers' own snap paths). The
            // Daily route re-hides these texts and stops this sequence via its own StopPayout.
            if (isActiveAndEnabled && !UIAnimations.ReduceMotion)
                _payoutRoutine = StartCoroutine(StatsPayoutSequence(stats.score));
        }

        // Task 45 — Puzzle Show / Time Attack payout: hero score count-up → grouped stat fade →
        // (Time Attack) the solved-ladders line counts up, buffered by ConfigureForEndless.
        private System.Collections.IEnumerator StatsPayoutSequence(int score)
        {
            var a = EnsureGroupAlpha(wordsFoundText, 0f);
            var b = EnsureGroupAlpha(accuracyText, 0f);
            var c = EnsureGroupAlpha(timeText, 0f);

            if (scoreText != null)
                yield return UIAnimations.CountUpInt(scoreText, 0, score, UIAnimations.STANDARD, "Score: {0}");

            float t = 0f;
            while (t < UIAnimations.STANDARD)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, UIAnimations.MICRO); // clamped-dt
                float p = UIAnimations.EaseOutCubic(Mathf.Clamp01(t / UIAnimations.STANDARD));
                if (a != null) a.alpha = p;
                if (b != null) b.alpha = p;
                if (c != null) c.alpha = p;
                yield return null;
            }
            if (a != null) a.alpha = 1f;
            if (b != null) b.alpha = 1f;
            if (c != null) c.alpha = 1f;

            // Time Attack's "N puzzles solved" headline counts up (singular stays static).
            if (_pendingLadders > 1 && wordsFoundText != null)
                yield return UIAnimations.CountUpInt(wordsFoundText, 0, _pendingLadders,
                    UIAnimations.STANDARD, "{0} puzzles solved");

            _payoutRoutine = null;
        }

        private static CanvasGroup EnsureGroupAlpha(TMP_Text label, float alpha)
        {
            if (label == null) return null;
            var cg = label.GetComponent<CanvasGroup>();
            if (cg == null) cg = label.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
            return cg;
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
                TypeScale.Apply(modeNameText, TypeRole.Headline); // Task 42 — screen title
                modeNameText.color = MenuPalette.TitleColor;
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

                // Frame the result (stars + grade + streak) in a subtle ghost card.
                EnsureDailyCard();
                if (_dailyCard != null)
                {
                    _dailyCard.sizeDelta = new Vector2(780f, 480f);
                    _dailyCard.anchoredPosition = new Vector2(0f, -880f);
                    _dailyCard.gameObject.SetActive(true);
                }

                if (wordsFoundText != null) // holds the grade/par/stars line (set by ShowDailyResult)
                {
                    wordsFoundText.gameObject.SetActive(true);
                    TypeScale.Apply(wordsFoundText, TypeRole.Title); // Task 42 — the Daily grade word IS the Title role
                    wordsFoundText.enableAutoSizing = false;
                    wordsFoundText.enableWordWrapping = true;
                    wordsFoundText.alignment = TextAlignmentOptions.Center;
                    PlaceTopCenter(wordsFoundText.rectTransform, -840f, 120f);
                }

                if (_dailyStarRow != null)
                {
                    _dailyStarRow.anchoredPosition = new Vector2(0f, -720f); // the gold rating, just above the grade word
                    _dailyStarRow.gameObject.SetActive(true);
                }
                if (_dailyStreakLine != null)
                    PlaceTopCenter(_dailyStreakLine.rectTransform, -1000f, 130f);
                return;
            }

            // ── Non-daily (Puzzle Show / Time Attack) — the standard score/accuracy/time block. ──
            // Re-show anything a prior Daily hid, and hide the Daily-only streak line.
            SetActiveTmp(scoreText, true);
            SetActiveTmp(accuracyText, true);
            SetActiveTmp(timeText, true);
            if (scoreLabel != null) scoreLabel.gameObject.SetActive(true);
            if (_dailyStreakLine != null) _dailyStreakLine.gameObject.SetActive(false);
            if (_dailyStarRow != null) _dailyStarRow.gameObject.SetActive(false);
            if (_dailyCard != null) _dailyCard.gameObject.SetActive(false);

            // "FINAL SCORE" caption (scene-only label) — quiet, muted.
            StyleResultText(scoreLabel, TypeRole.Caption, Palette.TextMuted, -560f, 44f);

            // Score — the hero number. Gold is the app's reward/achievement accent (same token as the
            // streak), routed through GameAccents.Gold so it's a token, not a scene straggler.
            if (scoreText != null)
            {
                TypeScale.Apply(scoreText, TypeRole.Headline); // Task 42 — hero number (64)
                scoreText.color = GameAccents.Gold;
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
            TypeScale.Apply(t, TypeRole.Body); // Task 42
            t.enableAutoSizing = false;
            t.alignment = TextAlignmentOptions.Center;
            PlaceTopCenter(t.rectTransform, topOffsetY, 60f);
        }

        private static void StyleResultText(Transform tf, TypeRole role, Color color,
            float topOffsetY, float height)
        {
            if (tf == null) return;
            var t = tf.GetComponent<TMP_Text>();
            if (t == null) return;
            TypeScale.Apply(t, role); // Task 42
            t.color = color;
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

            // Task 45 — the GameBootstrap daily chain ends here on BOTH paths (fresh and re-show),
            // so this is where the buffered payout choreography starts (or renders at rest).
            StartDailyPayout();
        }

        // Create the dedicated Daily streak label once (cached). Positioned by StyleResultsLayout's Daily
        // branch on show; a sensible default here covers the case it is set before the first layout pass.
        private void EnsureDailyStreakLine()
        {
            if (_dailyStreakLine != null) return;
            var go = new GameObject("DailyStreakLine", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dailyStreakLine = go.AddComponent<TextMeshProUGUI>();
            TypeScale.Apply(_dailyStreakLine, TypeRole.Body); // Task 42
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
            {
                wordsFoundText.text = laddersCompleted == 1
                    ? "1 puzzle solved" : $"{laddersCompleted} puzzles solved";
                // Task 45 — buffer for the running StatsPayoutSequence (started by DisplayStats this
                // same frame), which counts the headline up after the group fade. At rest (ReduceMotion)
                // no sequence runs and the final text above already stands.
                _pendingLadders = laddersCompleted;
            }
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
        /// Task 40B — an assisted run discloses with a muted "assisted" segment (text, not ⚡:
        /// the bundled font has no U+26A1 glyph — same tofu class as the ★ that became meshes).
        /// </summary>
        public void ShowDailyResult(int stars, int par, int playerSteps, bool failed, int dailyNumber, int streak,
                                    bool usedPowerUp = false, bool animate = true)
        {
            StopPayout(); // Task 45 — fresh view; never stack on a running payout
            _payoutAnimated = animate && !UIAnimations.ReduceMotion && isActiveAndEnabled;
            _pendingCoinReward = -1;
            _pendingDoublerReveal = false;
            _earnedStars = Mathf.Clamp(stars, 0, 3);

            int s = Mathf.Clamp(stars, 0, 3);
            string headline = failed ? "Failed today" : DailyGradeName(s);
            // Tint just the grade name — gold to tie it to the gold stars; muted for a failed day.
            string assisted = usedPowerUp ? $"  ·  <color=#{Hx(Palette.TextMuted)}>assisted</color>" : "";
            string line = $"<color=#{Hx(failed ? Palette.TextMuted : Palette.Coins)}>{headline}</color>{assisted}  ·  Par {par}  ·  You got {playerSteps}";

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

            // The grade stars render as real geometry (the bundled font has no ★ glyph — it tofu'd to □).
            SetDailyStars(s);

            // Task 45 — an animated payout starts hidden: grade line at alpha 0, stars at the pop
            // trough. The master sequence (started by ShowDailyStreak, the chain's last call) then
            // pops stars → settles the grade → counts the coins → punches the streak → doubler.
            var gradeLabel = wordsFoundText != null ? wordsFoundText : modeNameText;
            EnsureGroupAlpha(gradeLabel, _payoutAnimated ? 0f : 1f);
            if (_payoutAnimated && _dailyStars != null)
                foreach (var star in _dailyStars)
                    if (star != null) star.rectTransform.localScale = Vector3.one * UIAnimations.PopScale(0f);
        }

        /// <summary>
        /// Task 45 — surface today's coin payout ("+N coins", warm gold). Animated views count it
        /// up from 0 inside the payout sequence; at-rest views (re-show / ReduceMotion) render the
        /// final value immediately. Display only — the grant already happened in the economy.
        /// </summary>
        public void ShowDailyCoinReward(int coins)
        {
            if (coins < 0) coins = 0;
            EnsureDailyCoinLine();
            if (_dailyCoinLine == null) return;
            _pendingCoinReward = coins;
            _dailyCoinLine.gameObject.SetActive(true);
            _dailyCoinLine.text = _payoutAnimated ? "+0 coins" : $"+{coins} coins";
        }

        // Created once (cached), SET each view — the same anti-stacking discipline as the streak line.
        private void EnsureDailyCoinLine()
        {
            if (_dailyCoinLine != null) return;
            var go = new GameObject("DailyCoinLine", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dailyCoinLine = go.AddComponent<TextMeshProUGUI>();
            TypeScale.Apply(_dailyCoinLine, TypeRole.Title); // the payout is a moment — Title-weight
            _dailyCoinLine.color = GameAccents.Gold;
            _dailyCoinLine.alignment = TextAlignmentOptions.Center;
            _dailyCoinLine.raycastTarget = false;
            PlaceTopCenter(_dailyCoinLine.rectTransform, -850f, 64f); // between the daily card and the streak line
        }

        // Task 45 — start (or settle) the buffered payout. ShowDailyStreak calls this on BOTH
        // paths; the re-show path buffered animate:false, so it renders at rest with no motion.
        private void StartDailyPayout()
        {
            if (_payoutRoutine != null) { StopCoroutine(_payoutRoutine); _payoutRoutine = null; }
            if (!_payoutAnimated || !isActiveAndEnabled)
            {
                RenderPayoutAtRest();
                return;
            }
            _payoutRoutine = StartCoroutine(DailyPayoutSequence());
        }

        private void RenderPayoutAtRest()
        {
            if (_dailyStars != null)
                foreach (var star in _dailyStars)
                    if (star != null) star.rectTransform.localScale = Vector3.one;
            RestoreLabelAlpha(wordsFoundText);
            RestoreLabelAlpha(modeNameText);
            if (_dailyCoinLine != null && _pendingCoinReward >= 0)
                _dailyCoinLine.text = $"+{_pendingCoinReward} coins";
            if (_pendingDoublerReveal)
            {
                SetButtonVisible(doublerButton, true);
                _pendingDoublerReveal = false;
            }
        }

        // The Daily payout beat (~1s total): stars → grade → coins → streak → doubler. Weighted,
        // not slot-machine; every step is the existing motion vocabulary and ReduceMotion never
        // reaches this coroutine (StartDailyPayout routes to RenderPayoutAtRest instead).
        private System.Collections.IEnumerator DailyPayoutSequence()
        {
            // 1 — stars pop in a stagger; one light haptic per EARNED star on its pop beat.
            if (_dailyStars != null && _dailyStars.Length > 0)
            {
                var rects = new RectTransform[_dailyStars.Length];
                for (int i = 0; i < _dailyStars.Length; i++)
                    rects[i] = _dailyStars[i] != null ? _dailyStars[i].rectTransform : null;
                _starPopRoutine = StartCoroutine(UIAnimations.StaggeredPop(rects));
                var pop = _starPopRoutine;
                for (int i = 0; i < _earnedStars; i++)
                {
                    if (_haptics != null) _haptics.LightTap();
                    if (_sfx != null) _sfx.PlayStarPop();
                    yield return new WaitForSecondsRealtime(UIAnimations.MICRO * 0.5f); // the stagger beat (per × overlap)
                }
                yield return pop;
            }

            // 2 — the gold grade word fades/settles in after the last star.
            var gradeLabel = wordsFoundText != null ? wordsFoundText : modeNameText;
            if (gradeLabel != null)
            {
                var cg = EnsureGroupAlpha(gradeLabel, 0f);
                yield return UIAnimations.FadeTransition(cg, true);
            }

            // 3 — the coin payout counts up from 0.
            if (_dailyCoinLine != null && _pendingCoinReward >= 0)
                yield return UIAnimations.CountUpInt(_dailyCoinLine, 0, _pendingCoinReward,
                    UIAnimations.STANDARD, "+{0} coins");

            // 4 — the streak number gets its single punch (the existing tile-tap scale).
            if (_dailyStreakLine != null && _dailyStreakLine.gameObject.activeSelf)
                yield return UIAnimations.ScaleTileTap(_dailyStreakLine.rectTransform);
            else if (streakText != null && streakText.gameObject.activeSelf)
                yield return UIAnimations.ScaleTileTap(streakText.rectTransform);

            // 5 — only now does the doubler offer appear (no double-grant race perception).
            if (_pendingDoublerReveal)
            {
                SetButtonVisible(doublerButton, true);
                _pendingDoublerReveal = false;
            }
            _payoutRoutine = null;
        }

        // Stop the running payout and settle every VISUAL half-state (scales/alphas) to rest.
        // Logical reveals (doubler/coins) stay with their owners — a stale pending reveal must
        // never leak onto the next view (ShowDailyResult resets the buffers).
        private Coroutine _starPopRoutine; // nested under the master — must be stopped explicitly

        private void StopPayout()
        {
            if (_payoutRoutine != null)
            {
                StopCoroutine(_payoutRoutine);
                _payoutRoutine = null;
            }
            if (_starPopRoutine != null)
            {
                // Stopping the master does NOT cascade to coroutines it spawned.
                StopCoroutine(_starPopRoutine);
                _starPopRoutine = null;
            }
            if (_dailyStars != null)
                foreach (var star in _dailyStars)
                    if (star != null) star.rectTransform.localScale = Vector3.one;
            RestoreLabelAlpha(wordsFoundText);
            RestoreLabelAlpha(accuracyText);
            RestoreLabelAlpha(timeText);
            RestoreLabelAlpha(modeNameText);
        }

        private static void RestoreLabelAlpha(TMP_Text label)
        {
            var cg = label != null ? label.GetComponent<CanvasGroup>() : null;
            if (cg != null) cg.alpha = 1f;
        }

        private static string DailyGradeName(int stars) =>
            stars >= 3 ? "Perfect" : stars == 2 ? "Good" : stars == 1 ? "Solved" : "Failed";

        // ── Daily grade stars as real geometry (StarGraphic) — font-independent. The bundled TMP font has no
        // ★ glyph (it tofu'd to □) and the built-in font can't be loaded for a runtime fallback, so we draw the
        // rating as actual star meshes instead of text. Created once (cached row), recoloured per result.
        private RectTransform _dailyStarRow;
        private StarGraphic[] _dailyStars;
        private RectTransform _dailyCard;
        private const float STAR_SIZE = 60f, STAR_GAP = 22f;

        private void EnsureDailyStarRow()
        {
            if (_dailyStarRow != null) return;
            var go = new GameObject("DailyStarRow", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _dailyStarRow = (RectTransform)go.transform;
            _dailyStarRow.anchorMin = _dailyStarRow.anchorMax = new Vector2(0.5f, 1f);
            _dailyStarRow.pivot = new Vector2(0.5f, 0.5f);

            float totalW = 3f * STAR_SIZE + 2f * STAR_GAP;
            _dailyStarRow.sizeDelta = new Vector2(totalW, STAR_SIZE);
            _dailyStars = new StarGraphic[3];
            for (int i = 0; i < 3; i++)
            {
                var sgo = new GameObject($"Star{i}", typeof(RectTransform));
                sgo.transform.SetParent(_dailyStarRow, false);
                var srt = (RectTransform)sgo.transform;
                srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(STAR_SIZE, STAR_SIZE);
                srt.anchoredPosition = new Vector2(-totalW * 0.5f + STAR_SIZE * 0.5f + i * (STAR_SIZE + STAR_GAP), 0f);
                var star = sgo.AddComponent<StarGraphic>();
                star.raycastTarget = false;
                _dailyStars[i] = star;
            }
        }

        private void SetDailyStars(int filled)
        {
            EnsureDailyStarRow();
            if (_dailyStars == null) return;
            Color earned = GameAccents.Gold;                                                  // warm gold — earned
            Color empty  = new Color(Palette.TextMuted.r, Palette.TextMuted.g, Palette.TextMuted.b, 0.35f); // dim — unearned
            for (int i = 0; i < _dailyStars.Length; i++)
                if (_dailyStars[i] != null) _dailyStars[i].color = i < filled ? earned : empty;
            if (_dailyStarRow != null) _dailyStarRow.gameObject.SetActive(true);
        }

        // A subtle rounded ghost ring behind the Daily result cluster — matches the app's outline-button
        // language (transparent centre + a soft glow), created once and reused. Gives the page structure
        // instead of a sparse void.
        private void EnsureDailyCard()
        {
            if (_dailyCard != null) return;
            var go = new GameObject("DailyResultCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            _dailyCard = (RectTransform)go.transform;
            _dailyCard.anchorMin = _dailyCard.anchorMax = new Vector2(0.5f, 1f);
            _dailyCard.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            UIThemeManager.ApplyOutlineButton(img, Palette.CardOutline); // subtle amethyst ring + soft glow
            go.transform.SetAsFirstSibling(); // render behind the stars / grade / streak
        }

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
            // Task 45 — on an animated payout the doubler appears only AFTER the coin count-up
            // completes (kills the double-grant race perception; the grant logic is untouched).
            if (available && _payoutAnimated)
            {
                _pendingDoublerReveal = true;
                SetButtonVisible(doublerButton, false);
            }
            else
            {
                _pendingDoublerReveal = false;
                SetButtonVisible(doublerButton, available);
            }
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
            if (_dailyStarRow != null)        _dailyStarRow.gameObject.SetActive(false);
            if (_dailyCard != null)           _dailyCard.gameObject.SetActive(false);
            if (_dailyCoinLine != null)       _dailyCoinLine.gameObject.SetActive(false); // Task 45
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
