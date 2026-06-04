using System;
using System.Collections.Generic;
using System.Linq;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using WordPuzzle.Persistence;
using WordPuzzleModel = WordPuzzle.Puzzle.WordPuzzle;
using UnityEngine;

namespace WordPuzzle.State
{
    /// <summary>
    /// Manages the game state using a reducer pattern. Maintains mutable state for runtime
    /// while persisting immutable GameState snapshots. Works with the immutable GameState class
    /// by creating new instances through functional transforms.
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private MutableGameState workingState;
        private WordPuzzleModel currentPuzzle;
        private IWordValidator wordValidator;
        private IDataManager dataManager;
        private List<Action<GameState>> subscribers;

        /// <summary>
        /// Fired by HandleUseAddTime when an AddTime power-up successfully grants bonus
        /// seconds. Subscribers (e.g. TimeAttackMode) receive the grant amount in seconds.
        /// </summary>
        public event Action<float> OnTimeAdded;

        /// <summary>
        /// Spec §1 — fires for every SubmitWordAction with a SubmissionResult describing
        /// whether the word was accepted, and if not, the user-facing reason. The
        /// orchestrator (GameBootstrap) relays this to GameplayScreen.ShowFeedback /
        /// ShakeCurrentInput. NEVER decrements lives on failure.
        /// </summary>
        public event Action<SubmissionResult> OnWordSubmissionResult;

        public GameStateManager(IWordValidator wordValidator, IDataManager dataManager)
        {
            this.wordValidator = wordValidator;
            this.dataManager = dataManager;
            this.subscribers = new List<Action<GameState>>();
            this.workingState = null;
        }

        public GameState GetCurrentState()
        {
            if (workingState == null)
                throw new InvalidOperationException("No puzzle started");

            return new GameState(
                currentPuzzle,
                new List<string>(workingState.wordChain),
                workingState.score,
                workingState.foundWords.Count,
                workingState.elapsedTime,
                workingState.currentInput,
                workingState.hintsRemaining,
                workingState.revealsRemaining,
                new HashSet<int>(workingState.revealedLetterIndices),
                workingState.hintLetterIndex,
                workingState.revealedNextWord,
                workingState.addTimesRemaining,
                workingState.addTimeGrantSeconds
            );
        }

        // Task 33 — optional provider for the player's OWNED hint/reveal inventory (set by GameBootstrap).
        // When present, StartNewPuzzle seeds the per-puzzle charges from it instead of the BalanceConfig
        // defaults, so shop purchases + the starting/daily grants actually affect gameplay. Null in unit
        // tests => the BalanceConfig seed stands, so isolated GameStateManager tests are unchanged.
        private System.Func<(int hints, int reveals)> ownedPowerUpProvider;
        public void SetOwnedPowerUpProvider(System.Func<(int hints, int reveals)> provider) => ownedPowerUpProvider = provider;

        public void StartNewPuzzle(WordPuzzleModel puzzle)
        {
            currentPuzzle = puzzle;
            workingState = new MutableGameState
            {
                wordChain = new List<string> { puzzle.startWord },
                currentInput = "",
                // Spec §1 — sentinel. Lives are no longer decremented on bad submissions;
                // we keep the field so legacy persistence/UI code that reads it doesn't break.
                lives = 999,
                isWon = false,
                isLost = false,
                score = 0,
                currentStreak = 0,
                wordsRemaining = 0,
                timeRemaining = 0f,
                foundWords = new List<string>(),
                longestStreak = 0,
                elapsedTime = 0f,
                hintsRemaining = BalanceConfig.DefaultHintsPerPuzzle,
                revealsRemaining = BalanceConfig.DefaultRevealsPerPuzzle,
                revealedLetterIndices = new HashSet<int>(),
                hintLetterIndex = -1,
                revealedNextWord = "",
                addTimesRemaining = 0,
                addTimeGrantSeconds = 0f
            };

            // Task 33 — override the per-puzzle seed with the player's OWNED hint/reveal inventory when wired
            // (full real economy). Provider null in unit tests => the BalanceConfig defaults above stand.
            if (ownedPowerUpProvider != null)
            {
                var owned = ownedPowerUpProvider();
                workingState.hintsRemaining = Mathf.Max(0, owned.hints);
                workingState.revealsRemaining = Mathf.Max(0, owned.reveals);
            }

            wordValidator.Initialize(puzzle.startWord, puzzle.endWord, workingState.wordChain.ToArray());

            NotifySubscribers();
            SaveState();
        }

        public void Dispatch(GameAction action)
        {
            if (workingState == null)
                throw new InvalidOperationException("No puzzle started");

            switch (action)
            {
                case PressLetterAction a:
                    HandlePressLetter(a);
                    break;
                case DeleteLetterAction:
                    HandleDeleteLetter();
                    break;
                case SubmitWordAction a:
                    HandleSubmitWord(a);
                    break;
                case UseHintAction a:
                    HandleUseHint(a);
                    break;
                case UseRevealAction:
                    HandleUseReveal();
                    break;
                case UseAddTimeAction:
                    HandleUseAddTime();
                    break;
                case UndoStepAction:
                    HandleUndo();
                    break;
                case ResetGameAction a:
                    StartNewPuzzle(a.puzzle);
                    return;
            }

            NotifySubscribers();
            SaveState();
        }

        public IDisposable Subscribe(Action<GameState> observer)
        {
            subscribers.Add(observer);
            return new Unsubscriber(subscribers, observer);
        }

        private void HandlePressLetter(PressLetterAction action)
        {
            if (workingState.currentInput.Length >= 10 || workingState.isWon || workingState.isLost)
                return;

            workingState.currentInput += char.ToLower(action.letter);
        }

        private void HandleDeleteLetter()
        {
            if (workingState.currentInput.Length > 0 && !workingState.isWon && !workingState.isLost)
            {
                workingState.currentInput = workingState.currentInput.Substring(0, workingState.currentInput.Length - 1);
            }
        }

        private void HandleSubmitWord(SubmitWordAction action)
        {
            // Spec §1 — early-out if puzzle already terminal.
            if (workingState.isWon || workingState.isLost)
                return;

            // Spec §1 — normalize input.
            string word = (action.word ?? string.Empty).Trim().ToLowerInvariant();

            // §1 — Empty input: clear input, fire result, do not touch lives.
            if (word.Length == 0)
            {
                workingState.currentInput = "";
                FireSubmissionResult(false, word, "Type a word", SubmissionRejectReason.Empty);
                return;
            }

            // §1 — Length check (must match previous word). Done before validator so we
            // can give a clean "must be N letters" message rather than the validator's
            // generic "must change exactly one letter".
            string previousWord = workingState.wordChain[workingState.wordChain.Count - 1];
            if (word.Length != previousWord.Length)
            {
                workingState.invalidAttempts++;
                workingState.currentInput = "";
                workingState.currentStreak = 0;
                FireSubmissionResult(false, word,
                    $"Word must be {previousWord.Length} letters",
                    SubmissionRejectReason.WrongLength);
                return;
            }

            var validation = wordValidator.ValidateWord(word);

            if (validation.isValid && validation.isNextStep)
            {
                // Add to chain
                workingState.wordChain.Add(word);
                workingState.currentInput = "";

                // §1.5 — chain advanced, so the current hint/reveal preview is stale.
                workingState.hintLetterIndex = -1;
                workingState.revealedNextWord = string.Empty;

                // Track valid word
                if (!workingState.foundWords.Contains(word))
                {
                    workingState.foundWords.Add(word);
                    int points = word.Length;
                    workingState.score += points;
                }

                // Update streak
                workingState.currentStreak++;
                workingState.longestStreak = Mathf.Max(workingState.longestStreak, workingState.currentStreak);

                // Daily 2.0 — an accepted move that did NOT get strictly closer to the target is
                // a DETOUR (costs score, not the run). The final winning move is always progress
                // (distance-to-target hits 0), so it never counts as a detour.
                if (workingState.isDaily)
                {
                    if (!validation.isProgress) workingState.detourCount++;
                    // Phase 4 — record the row class for the share card (0 = optimal, 1 = detour).
                    workingState.dailyStepClasses.Add(validation.isProgress ? 0 : 1);
                }

                // Check win condition
                if (word == currentPuzzle.endWord)
                {
                    workingState.isWon = true;

                    // Daily 2.0 — solved: score the path against par (the one-and-done result).
                    if (workingState.isDaily)
                        workingState.dailyResult = ComputeDailyResult(ranOutOfMistakes: false);
                }

                // Reset validator with current state
                wordValidator.Initialize(word, currentPuzzle.endWord, workingState.wordChain.ToArray());

                // §1 — fire success result (carries the daily result on the winning move; null otherwise).
                FireSubmissionResult(true, word, "Nice!", SubmissionRejectReason.None, workingState.dailyResult);
            }
            else
            {
                // Track invalid attempt — NO lives decrement, NO isLost flip per §1 (Classic).
                workingState.invalidAttempts++;
                workingState.currentInput = "";
                workingState.currentStreak = 0;

                // Daily 2.0 — an invalid guess (correct length, but not a valid next step) is a
                // MISTAKE: it costs the run, not the score. Exhausting the budget FAILS the daily
                // (one-and-done). The wrong-LENGTH path above is malformed input, not a mistake.
                if (workingState.isDaily)
                {
                    // Phase 4 — record the mistake row for the share card (2 = mistake).
                    workingState.dailyStepClasses.Add(2);
                    workingState.mistakesRemaining = Mathf.Max(0, workingState.mistakesRemaining - 1);
                    if (workingState.mistakesRemaining <= 0)
                    {
                        workingState.isLost = true;
                        workingState.dailyResult = ComputeDailyResult(ranOutOfMistakes: true);
                    }
                }

                // Map the validator's typed RejectReason to SubmissionRejectReason + user text.
                var (reason, userReason) = MapWordRejectReason(validation?.RejectReason ?? WordRejectReason.None);
                FireSubmissionResult(false, word, userReason, reason, workingState.dailyResult);
            }
        }

        // §1 — translate WordValidator's typed WordRejectReason into SubmissionRejectReason + UI text.
        // No string parsing: switch on the enum set by WordValidator.ValidateWord.
        private static (SubmissionRejectReason reason, string userMessage) MapWordRejectReason(WordRejectReason r)
        {
            switch (r)
            {
                case WordRejectReason.NotInDictionary:
                    return (SubmissionRejectReason.NotInDictionary, "Not a real word");
                case WordRejectReason.AlreadyUsed:
                    return (SubmissionRejectReason.AlreadyUsed, "Already used");
                case WordRejectReason.NotOneLetterDifferent:
                    return (SubmissionRejectReason.NotOneLetterDifferent, "Change exactly one letter");
                default:
                    return (SubmissionRejectReason.NotInDictionary, "Not a real word");
            }
        }

        private void FireSubmissionResult(bool accepted, string word, string reason, SubmissionRejectReason rejectReason, PathScoreResult? pathScore = null)
        {
            try
            {
                OnWordSubmissionResult?.Invoke(new SubmissionResult
                {
                    accepted = accepted,
                    word = word,
                    reason = reason,
                    rejectReason = rejectReason,
                    pathScore = pathScore
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking OnWordSubmissionResult: {ex.Message}");
            }
        }

        /// <summary>
        /// Daily 2.0 (Task 36) — build the par-scored result for the current run through the
        /// single PathScoring entry point. Called once, on the terminal submission (solve OR
        /// fail). On a fail, mistakesRemaining is 0 so mistakesUsed == the full budget.
        /// </summary>
        private PathScoreResult ComputeDailyResult(bool ranOutOfMistakes)
        {
            int playerSteps = Mathf.Max(0, workingState.wordChain.Count - 1);
            int mistakesUsed = workingState.dailyMistakeBudget - workingState.mistakesRemaining;
            return PathScoring.Score(
                workingState.dailyPar,
                playerSteps,
                workingState.detourCount,
                mistakesUsed,
                ranOutOfMistakes,
                workingState.usedPowerUpThisRun);
        }

        /// <summary>
        /// Picks a "best-effort" next solution word when the chain has wandered off the
        /// canonical solution path. Chooses the solution entry with the smallest non-zero
        /// Hamming distance to lastWord (equal-length comparisons only); ties broken by
        /// preferring later indices. Falls back to the final solution word.
        /// </summary>
        private static string FindBestNextTarget(string lastWord, string[] solution)
        {
            if (solution == null || solution.Length == 0)
                return null;
            if (string.IsNullOrEmpty(lastWord))
                return solution[solution.Length - 1];

            // Special case: chain is still on the start word.
            if (solution.Length > 1 && string.Equals(solution[0], lastWord, StringComparison.OrdinalIgnoreCase))
                return solution[1];

            int bestDistance = int.MaxValue;
            int bestIndex = -1;
            for (int i = 1; i < solution.Length; i++)
            {
                string candidate = solution[i];
                if (string.IsNullOrEmpty(candidate) || candidate.Length != lastWord.Length)
                    continue;

                int distance = 0;
                for (int k = 0; k < candidate.Length; k++)
                {
                    if (char.ToLowerInvariant(candidate[k]) != char.ToLowerInvariant(lastWord[k]))
                        distance++;
                }

                if (distance == 0)
                    continue;

                // Prefer smaller distance; tie-break by preferring later index (>=).
                if (distance < bestDistance || (distance == bestDistance && i > bestIndex))
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
                return solution[bestIndex];

            return solution[solution.Length - 1];
        }

        /// <summary>
        /// Locates the next solution word after the current chain tail and the index in
        /// that word which differs from the tail. Returns (null, -1) when no actionable
        /// hint is possible (e.g. chain already at last solution word).
        /// </summary>
        private (string nextTarget, int changedIndex) ResolveHintTarget()
        {
            if (currentPuzzle == null)
                return (null, -1);
            var solution = currentPuzzle.solution;
            if (solution == null || solution.Length < 2)
                return (null, -1);
            if (workingState == null || workingState.wordChain == null || workingState.wordChain.Count == 0)
                return (null, -1);

            string lastChainWord = workingState.wordChain[workingState.wordChain.Count - 1];
            string nextTarget = null;

            // 1) Exact match in solution → take the following entry.
            for (int i = 0; i < solution.Length - 1; i++)
            {
                if (string.Equals(solution[i], lastChainWord, StringComparison.OrdinalIgnoreCase))
                {
                    nextTarget = solution[i + 1];
                    break;
                }
            }

            // 2) Off-path fallback.
            if (string.IsNullOrEmpty(nextTarget))
                nextTarget = FindBestNextTarget(lastChainWord, solution);

            if (string.IsNullOrEmpty(nextTarget))
                return (null, -1);

            // 3) Compute first differing index between lastChainWord and nextTarget.
            int diffIndex = -1;
            int sharedLen = Math.Min(lastChainWord.Length, nextTarget.Length);
            for (int k = 0; k < sharedLen; k++)
            {
                if (char.ToLowerInvariant(lastChainWord[k]) != char.ToLowerInvariant(nextTarget[k]))
                {
                    diffIndex = k;
                    break;
                }
            }
            if (diffIndex < 0)
            {
                if (lastChainWord.Length == nextTarget.Length)
                    return (null, -1); // identical — nothing to hint
                diffIndex = sharedLen; // one is a prefix of the other
            }

            return (nextTarget, diffIndex);
        }

        private void HandleUseHint(UseHintAction action)
        {
            if (workingState.isWon || workingState.isLost)
                return;

            if (workingState.hintsRemaining <= 0)
                return;

            // §4 solution guard — do not consume the counter when no solution exists.
            if (currentPuzzle == null || currentPuzzle.solution == null || currentPuzzle.solution.Length < 2)
            {
                Debug.LogWarning("[GameStateManager] No solution path available — hint/reveal disabled for this puzzle.");
                return;
            }

            var (nextTarget, changedIndex) = ResolveHintTarget();
            if (string.IsNullOrEmpty(nextTarget) || changedIndex < 0)
                return; // no actionable hint, do not consume counter

            workingState.hintLetterIndex = changedIndex;
            workingState.hintsRemaining--;
            workingState.usedPowerUpThisRun = true;   // Daily 2.0 — recorded only (does not change the grade).
        }

        private void HandleUseReveal()
        {
            if (workingState.isWon || workingState.isLost)
                return;

            if (workingState.revealsRemaining <= 0)
                return;

            // §4 solution guard — do not consume the counter when no solution exists.
            if (currentPuzzle == null || currentPuzzle.solution == null || currentPuzzle.solution.Length < 2)
            {
                Debug.LogWarning("[GameStateManager] No solution path available — hint/reveal disabled for this puzzle.");
                return;
            }

            var (nextTarget, _) = ResolveHintTarget();
            if (string.IsNullOrEmpty(nextTarget))
                return; // no actionable reveal, do not consume counter

            // Task 31 — Reveal shows ONLY its preview row. It must NOT also set hintLetterIndex:
            // that gold-fills the active input tile exactly like Hint does (the unwanted overlap the
            // player saw). The preview row's own gold changed-letter is computed separately
            // (ComputeChangedIndex from chain tail vs revealedNextWord), so this doesn't affect it.
            workingState.revealedNextWord = nextTarget;
            workingState.revealsRemaining--;
            workingState.usedPowerUpThisRun = true;   // Daily 2.0 — recorded only (does not change the grade).
        }

        /// <summary>
        /// TimeAttack §4 — consumes one AddTime charge and notifies subscribers (the
        /// active TimeAttackMode) of the seconds to grant. Refuses without spending the
        /// counter when no charges remain or the grant amount is non-positive.
        /// </summary>
        private void HandleUseAddTime()
        {
            if (workingState == null) return;
            if (workingState.isWon || workingState.isLost) return;
            if (workingState.addTimesRemaining <= 0) return;
            if (workingState.addTimeGrantSeconds <= 0f) return;

            workingState.addTimesRemaining--;
            workingState.usedPowerUpThisRun = true;   // Daily 2.0 — recorded only (does not change the grade).
            var grant = workingState.addTimeGrantSeconds;

            try
            {
                OnTimeAdded?.Invoke(grant);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking OnTimeAdded: {ex.Message}");
            }
        }

        /// <summary>
        /// TimeAttack §4 — seeds the AddTime power-up economy on the working state.
        /// Called by TimeAttackMode after StartNewPuzzle to inject configured charges and
        /// grant size derived from TimeAttackConfig. Charges &lt; 0 are clamped to 0.
        /// </summary>
        public void ConfigureAddTimePowerUp(int charges, float grantSeconds)
        {
            if (workingState == null) return;
            workingState.addTimesRemaining = Mathf.Max(0, charges);
            workingState.addTimeGrantSeconds = Mathf.Max(0f, grantSeconds);
            NotifySubscribers();
        }

        /// <summary>
        /// Daily 2.0 (Task 36) — promote the current run to a par-scored daily with stakes.
        /// Called by the daily launcher AFTER StartNewPuzzle (mirrors ConfigureAddTimePowerUp).
        /// <paramref name="par"/> is the WordGraph shortest distance start->end (the daily caller
        /// computes it once); <paramref name="mistakeBudget"/> is BalanceConfig.DailyMistakeBudget.
        /// Idempotent: re-arming resets the two resources for a fresh run.
        /// </summary>
        public void ConfigureDailyRun(int mistakeBudget, int par)
        {
            if (workingState == null) return;
            workingState.isDaily = true;
            workingState.dailyMistakeBudget = Mathf.Max(0, mistakeBudget);
            workingState.mistakesRemaining = workingState.dailyMistakeBudget;
            workingState.dailyPar = Mathf.Max(0, par);
            workingState.detourCount = 0;
            workingState.usedPowerUpThisRun = false;
            workingState.dailyResult = null;
            workingState.dailyStepClasses = new List<int>();
            NotifySubscribers();
        }

        private void HandleUndo()
        {
            if (workingState.wordChain.Count <= 1 || workingState.isWon || workingState.isLost)
                return;

            // Single authoritative undo path: chain rewind only. Power-ups stay spent.
            // undoHistory was never pushed to, so the snapshot-restore branch is removed.
            var lastWord = workingState.wordChain[workingState.wordChain.Count - 1];
            workingState.wordChain.RemoveAt(workingState.wordChain.Count - 1);
            workingState.currentInput = "";

            if (workingState.foundWords.Contains(lastWord))
            {
                workingState.foundWords.Remove(lastWord);
                // C2 score floor — never go negative.
                workingState.score = Mathf.Max(0, workingState.score - lastWord.Length);
            }

            // Streak decrements by one but never below zero.
            workingState.currentStreak = Mathf.Max(0, workingState.currentStreak - 1);

            // §1.3 — chain rewound, so any active hint/reveal preview is stale.
            workingState.hintLetterIndex = -1;
            workingState.revealedNextWord = string.Empty;

            // Daily 2.0 — undo steps back one detour (floor 0). A spent MISTAKE is NOT refunded
            // (a bad guess stays spent). Deliberate simplification: this decrements even if the
            // undone step was progress; see the Phase 1 report note on per-step detour accounting.
            if (workingState.isDaily)
                workingState.detourCount = Mathf.Max(0, workingState.detourCount - 1);
        }

        private void NotifySubscribers()
        {
            var stateSnapshot = GetCurrentState();
            foreach (var subscriber in subscribers)
            {
                try
                {
                    subscriber(stateSnapshot);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error notifying subscriber: {ex.Message}");
                }
            }
        }

        private void SaveState()
        {
            if (workingState == null || currentPuzzle == null)
                return;

            var snapshot = new GameStateSnapshot
            {
                currentMode = "Gameplay",
                currentPuzzleId = currentPuzzle.puzzleId,
                wordChain = workingState.wordChain.ToArray(),
                currentInput = workingState.currentInput,
                lives = workingState.lives,
                hintsUsed = 0,
                revealsUsed = 0,
                undosUsed = 0,
                timestamp = System.DateTime.UtcNow.Ticks,
                sessionId = ""
            };

            _ = dataManager.SaveGameStateAsync(snapshot);
        }

        // UI Integration Methods
        public char[] GetAvailableLetters()
        {
            if (workingState == null)
                return new char[0];

            var usedLetters = new HashSet<char>();
            foreach (var word in workingState.wordChain)
            {
                foreach (var c in word)
                {
                    usedLetters.Add(c);
                }
            }

            var available = new List<char>();
            for (char c = 'a'; c <= 'z'; c++)
            {
                if (!usedLetters.Contains(c))
                {
                    available.Add(c);
                }
            }

            return available.ToArray();
        }

        public int GetCurrentScore()
        {
            return workingState?.score ?? 0;
        }

        public bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word) || currentPuzzle == null)
                return false;

            var validation = wordValidator.ValidateWord(word.ToLower());
            return validation.isValid && validation.isNextStep;
        }

        public int SubmitWord(string word)
        {
            if (workingState == null)
                return 0;

            int scoreBefore = workingState.score;
            Dispatch(new SubmitWordAction(word));
            int scoreAfter = workingState.score;

            return scoreAfter - scoreBefore;
        }

        public int GetCurrentStreak()
        {
            return workingState?.currentStreak ?? 0;
        }

        public int GetWordsRemaining()
        {
            return workingState?.wordsRemaining ?? 0;
        }

        public void SetWordsRemaining(int count)
        {
            if (workingState != null)
            {
                workingState.wordsRemaining = count;
                NotifySubscribers();
            }
        }

        public float GetTimeRemaining()
        {
            return workingState?.timeRemaining ?? 0f;
        }

        public void SetTimeRemaining(float time)
        {
            if (workingState != null)
            {
                workingState.timeRemaining = time;
                NotifySubscribers();
            }
        }

        public string GetBestWord()
        {
            if (workingState?.foundWords.Count == 0)
                return "--";

            return workingState.foundWords.OrderByDescending(w => w.Length).FirstOrDefault() ?? "--";
        }

        public int GetLongestStreak()
        {
            return workingState?.longestStreak ?? 0;
        }

        public GameStats GetFinalStats()
        {
            if (workingState == null)
                return new GameStats();

            int totalAttempts = workingState.foundWords.Count + workingState.invalidAttempts;
            float accuracy = totalAttempts > 0 ? (workingState.foundWords.Count / (float)totalAttempts * 100f) : 0f;

            return new GameStats
            {
                wordsFound = workingState.foundWords.Count,
                totalTime = workingState.elapsedTime,
                score = workingState.score,
                accuracy = accuracy,
                currentStreak = workingState.currentStreak,
                longestStreak = workingState.longestStreak
            };
        }

        public void ResetTracking()
        {
            if (workingState != null)
            {
                workingState.foundWords.Clear();
                workingState.longestStreak = 0;
                workingState.score = 0;
                workingState.currentStreak = 0;
            }
        }

        public void IncrementElapsedTime(float deltaTime)
        {
            if (workingState == null || deltaTime <= 0f) return;
            workingState.elapsedTime += deltaTime;
        }

        // ── Daily 2.0 (Task 36) — read-only daily run state for the HUD, results panel, and tests ──
        public bool IsDailyRun() => workingState?.isDaily ?? false;
        public int GetDetourCount() => workingState?.detourCount ?? 0;
        public int GetMistakesRemaining() => workingState?.mistakesRemaining ?? 0;
        public int GetDailyPar() => workingState?.dailyPar ?? 0;
        public PathScoreResult? GetDailyResult() => workingState?.dailyResult;
        public IReadOnlyList<int> GetDailyStepClasses() => workingState?.dailyStepClasses;
    }

    /// <summary>
    /// Mutable working state for the game. Used internally by GameStateManager
    /// to maintain runtime state while preserving the immutability contract of GameState.
    /// </summary>
    internal class MutableGameState
    {
        public List<string> wordChain;
        public string currentInput;
        public int lives;
        public bool isWon;
        public bool isLost;
        public int score;
        public int currentStreak;
        public int wordsRemaining;
        public float timeRemaining;
        public List<string> foundWords;
        public int longestStreak;
        public float elapsedTime;
        public int invalidAttempts = 0;

        // Economy tracking for Phase 2 — defaults mirror BalanceConfig (StartNewPuzzle always overwrites).
        public int hintsRemaining = BalanceConfig.DefaultHintsPerPuzzle;
        public int revealsRemaining = BalanceConfig.DefaultRevealsPerPuzzle;
        // DEPRECATED — replaced by hintLetterIndex + revealedNextWord. Retained for back-compat
        // with consumers that still read the index set; new hint/reveal paths do not write to it.
        public HashSet<int> revealedLetterIndices = new HashSet<int>();

        // Hint/reveal surface: index that the most recent hint exposed (-1 if none),
        // and the next solution word once the player spends a reveal ("" if none).
        public int hintLetterIndex = -1;
        public string revealedNextWord = "";

        // TimeAttack §4 AddTime economy: per-run charges and grant size.
        public int addTimesRemaining = 0;
        public float addTimeGrantSeconds = 0f;

        // Daily 2.0 (Task 36) — two-resource run state. Inert unless isDaily (set by
        // ConfigureDailyRun after StartNewPuzzle). detourCount = accepted non-progress moves
        // (cost score); mistakesRemaining = invalid guesses left (cost the run). dailyResult is
        // computed once, on the terminal submission (solve OR fail), via the PathScoring entry point.
        public bool isDaily = false;
        public int detourCount = 0;
        public int mistakesRemaining = 0;
        public int dailyMistakeBudget = 0;
        public int dailyPar = 0;
        public bool usedPowerUpThisRun = false;
        public PathScoreResult? dailyResult = null;
        // Per-step shape for the path-shape share card (Task 36 Phase 4): one entry per accepted step
        // AND per invalid attempt, in order. 0 = optimal (🟩), 1 = detour (🟨), 2 = mistake (⬛).
        public List<int> dailyStepClasses = new List<int>();
    }

    internal class Unsubscriber : IDisposable
    {
        private List<Action<GameState>> subscribers;
        private Action<GameState> observer;

        public Unsubscriber(List<Action<GameState>> subs, Action<GameState> obs)
        {
            subscribers = subs;
            observer = obs;
        }

        public void Dispose()
        {
            subscribers.Remove(observer);
        }
    }

    /// <summary>
    /// Game statistics returned by the state manager.
    /// </summary>
    public struct GameStats
    {
        public int wordsFound;
        public float totalTime;
        public int score;
        public float accuracy;
        public int currentStreak;
        public int longestStreak;
    }

    /// <summary>
    /// Spec §1 — result of a SubmitWordAction. Always raised on
    /// <see cref="GameStateManager.OnWordSubmissionResult"/>; never decrements lives.
    /// </summary>
    public struct SubmissionResult
    {
        public bool accepted;
        public string word;
        public string reason;
        public SubmissionRejectReason rejectReason;
        // Daily 2.0 (Task 36) — populated only on the terminal submission (solve OR fail); null otherwise.
        public PathScoreResult? pathScore;
    }

    /// <summary>Spec §1 — typed rejection categories for failed submissions.</summary>
    public enum SubmissionRejectReason
    {
        None,
        NotInDictionary,
        NotOneLetterDifferent,
        AlreadyUsed,
        WrongLength,
        Empty
    }

    public interface IGameStateManager
    {
        GameState GetCurrentState();
        void StartNewPuzzle(WordPuzzleModel puzzle);
        void Dispatch(GameAction action);
        IDisposable Subscribe(Action<GameState> observer);

        // UI Integration Methods
        char[] GetAvailableLetters();
        int GetCurrentScore();
        bool IsValidWord(string word);
        int SubmitWord(string word);
        int GetCurrentStreak();
        int GetWordsRemaining();
        void SetWordsRemaining(int count);
        float GetTimeRemaining();
        void SetTimeRemaining(float time);
        string GetBestWord();
        int GetLongestStreak();
        GameStats GetFinalStats();
        void ResetTracking();
    }

}
