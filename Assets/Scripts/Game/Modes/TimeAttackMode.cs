using System;
using WordPuzzle.State;
using WordPuzzle.Puzzle;
using UnityEngine;

namespace WordPuzzle.Modes
{
    /// <summary>
    /// Sub-mode selector for Time Attack runs (§4).
    /// <para>
    /// • Timed: classic countdown — game ends when the timer reaches 0.<br/>
    /// • Survival: timer counts down but each completed puzzle grants a configurable
    ///   reward in seconds; the run only ends when the player runs out of time.
    /// </para>
    /// </summary>
    public enum TimeAttackSubMode
    {
        Timed,
        Survival
    }

    /// <summary>
    /// Run-time configuration for Time Attack mode (§4 + §6 routing).
    /// </summary>
    public sealed class TimeAttackConfig
    {
        /// <summary>Base time (60 or 120 seconds per spec).</summary>
        public float baseTimeSeconds = 60f;

        /// <summary>Timed vs Survival sub-mode.</summary>
        public TimeAttackSubMode subMode = TimeAttackSubMode.Timed;

        /// <summary>Number of AddTime power-up charges seeded per run.</summary>
        public int addTimeCharges = 1;

        /// <summary>Seconds granted by each AddTime charge.</summary>
        public float addTimeGrantSeconds = 10f;

        /// <summary>
        /// Survival-only: seconds awarded when the player completes a puzzle. Ignored in Timed.
        /// </summary>
        public float survivalRewardSeconds = 15f;

        /// <summary>Convenience factory for the canonical 60s Timed config.</summary>
        public static TimeAttackConfig Default60() => new TimeAttackConfig
        {
            baseTimeSeconds    = BalanceConfig.TimeAttackBaseSecondsShort,
            subMode            = TimeAttackSubMode.Timed,
            addTimeCharges     = BalanceConfig.AddTimeChargesShort,
            addTimeGrantSeconds = BalanceConfig.AddTimeGrantSeconds,
            survivalRewardSeconds = 0f
        };

        /// <summary>Convenience factory for the 120s Timed config.</summary>
        public static TimeAttackConfig Default120() => new TimeAttackConfig
        {
            baseTimeSeconds    = BalanceConfig.TimeAttackBaseSecondsLong,
            subMode            = TimeAttackSubMode.Timed,
            addTimeCharges     = BalanceConfig.AddTimeChargesLong,
            addTimeGrantSeconds = BalanceConfig.AddTimeGrantSeconds,
            survivalRewardSeconds = 0f
        };

        /// <summary>Convenience factory for Survival (60s base + 15s per completion).</summary>
        public static TimeAttackConfig DefaultSurvival() => new TimeAttackConfig
        {
            baseTimeSeconds    = BalanceConfig.TimeAttackBaseSecondsShort,
            subMode            = TimeAttackSubMode.Survival,
            addTimeCharges     = BalanceConfig.AddTimeChargesLong,
            addTimeGrantSeconds = BalanceConfig.AddTimeGrantSeconds,
            survivalRewardSeconds = BalanceConfig.SurvivalRewardSeconds
        };
    }

    /// <summary>
    /// Time Attack mode: Complete as many words as possible before time runs out.
    /// Supports two sub-modes (Timed / Survival) and the AddTime power-up.
    /// </summary>
    public class TimeAttackMode : IGameMode
    {
        private GameStateManager stateManager;
        private WordPuzzle.Puzzle.WordPuzzle currentPuzzle;
        private float timeRemaining;
        private readonly TimeAttackConfig config;
        private int puzzlesCompleted;
        private bool addTimeWired;
        private bool lastPuzzleWasComplete;
        private bool timerSeeded;   // Task 16 — one-shot timer seed per run (survives ladder advance)

        /// <summary>Fired whenever the countdown ticks. Argument is seconds remaining.</summary>
        public event Action<float> TimeChanged;

        /// <summary>
        /// Fired whenever bonus seconds are credited (AddTime power-up or Survival reward).
        /// Argument is the seconds added.
        /// </summary>
        public event Action<float> OnTimeAdded;

        public TimeAttackConfig Config => config;
        public TimeAttackSubMode SubMode => config.subMode;
        public float BaseTimeSeconds => config.baseTimeSeconds;
        public int PuzzlesCompleted => puzzlesCompleted;

        /// <summary>Default constructor — uses the canonical 60s Timed config.</summary>
        public TimeAttackMode() : this(TimeAttackConfig.Default60()) { }

        /// <summary>Configured constructor — caller supplies a TimeAttackConfig.</summary>
        public TimeAttackMode(TimeAttackConfig config)
        {
            this.config = config ?? TimeAttackConfig.Default60();
        }

        public void Initialize(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));

            // Wire OnTimeAdded once: the state manager raises it when HandleUseAddTime
            // consumes an AddTime charge; we add the grant to the local countdown and
            // re-broadcast for the UI layer.
            if (!addTimeWired)
            {
                this.stateManager.OnTimeAdded += HandleStateAddTime;
                addTimeWired = true;
            }
        }

        public void StartGame(WordPuzzle.Puzzle.WordPuzzle puzzle)
        {
            if (stateManager == null)
                throw new InvalidOperationException("Must call Initialize first");

            currentPuzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            stateManager.StartNewPuzzle(puzzle);

            // First puzzle of the run — seed the timer and the AddTime power-up economy ONCE.
            // Task 16: use a one-shot flag (not puzzlesCompleted, which stays 0 in Timed sub-mode)
            // so auto-advancing to the next ladder keeps the run's clock running.
            if (!timerSeeded)
            {
                timerSeeded = true;
                timeRemaining = config.baseTimeSeconds;
                stateManager.ConfigureAddTimePowerUp(config.addTimeCharges, config.addTimeGrantSeconds);
            }

            lastPuzzleWasComplete = false;
        }

        public void HandleWordSubmission(string word)
        {
            if (stateManager == null || currentPuzzle == null || timeRemaining <= 0f) return;
            stateManager.SubmitWord(word);
        }

        public void Tick(float deltaTime)
        {
            if (stateManager == null) return;

            timeRemaining -= deltaTime;
            if (timeRemaining < 0f) timeRemaining = 0f;

            stateManager.IncrementElapsedTime(deltaTime);

            // Survival reward: when the active puzzle just completed, grant the configured
            // bonus seconds and notify subscribers. Latched via lastPuzzleWasComplete so a
            // single completion only awards once per puzzle.
            if (config.subMode == TimeAttackSubMode.Survival && !lastPuzzleWasComplete)
            {
                var state = SafeGetState();
                if (state != null && state.IsPuzzleComplete)
                {
                    lastPuzzleWasComplete = true;
                    puzzlesCompleted++;
                    if (config.survivalRewardSeconds > 0f)
                    {
                        timeRemaining += config.survivalRewardSeconds;
                        try { OnTimeAdded?.Invoke(config.survivalRewardSeconds); }
                        catch (Exception ex) { Debug.LogError($"OnTimeAdded subscriber threw: {ex.Message}"); }
                    }
                }
            }

            TimeChanged?.Invoke(timeRemaining);
        }

        public GameModeStats GetStats()
        {
            var state = SafeGetState();
            var timeUsed = Mathf.Max(0f, config.baseTimeSeconds - timeRemaining);

            return new GameModeStats
            {
                // The mode is "Timed" everywhere now; the results title disambiguates the sub-mode
                // ("Timed Survival Results" vs "Timed Results") so it's obvious which you played.
                modeName = config.subMode == TimeAttackSubMode.Survival
                    ? "Timed Survival"
                    : "Timed",
                wordsFound = state?.wordsFound ?? 0,
                totalTime = timeUsed,
                score = state?.score ?? 0,
                accuracy = 100f // All accepted submissions are valid by definition.
            };
        }

        public void Reset()
        {
            timeRemaining = config.baseTimeSeconds;
            currentPuzzle = null;
            puzzlesCompleted = 0;
            lastPuzzleWasComplete = false;
            timerSeeded = false;
        }

        public bool IsGameOver() => timeRemaining <= 0f;

        public bool IsTimeUp() => timeRemaining <= 0f;

        public float GetTimeRemaining() => timeRemaining;

        /// <summary>
        /// Task 6B — Rewarded "Continue": inject seconds after time expires so the run
        /// can resume. Only meaningful when IsGameOver(); safe to call at any time.
        /// </summary>
        public void GrantContinueSeconds(float seconds)
        {
            if (seconds <= 0f) return;
            timeRemaining += seconds;
            try { OnTimeAdded?.Invoke(seconds); }
            catch (Exception ex) { Debug.LogError($"GrantContinueSeconds subscriber threw: {ex.Message}"); }
        }

        /// <summary>Forwarder used by GameStateManager.OnTimeAdded — credits seconds locally.</summary>
        private void HandleStateAddTime(float seconds)
        {
            if (seconds <= 0f) return;
            timeRemaining += seconds;
            try { OnTimeAdded?.Invoke(seconds); }
            catch (Exception ex) { Debug.LogError($"OnTimeAdded subscriber threw: {ex.Message}"); }
        }

        private GameState SafeGetState()
        {
            try { return stateManager?.GetCurrentState(); }
            catch { return null; }
        }
    }
}
