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
                workingState.revealedNextWord
            );
        }

        public void StartNewPuzzle(WordPuzzleModel puzzle)
        {
            currentPuzzle = puzzle;
            workingState = new MutableGameState
            {
                wordChain = new List<string> { puzzle.startWord },
                currentInput = "",
                lives = 3,
                isWon = false,
                isLost = false,
                score = 0,
                currentStreak = 0,
                wordsRemaining = 0,
                timeRemaining = 0f,
                foundWords = new List<string>(),
                longestStreak = 0,
                elapsedTime = 0f,
                hintsRemaining = 2,
                revealsRemaining = 1,
                undoHistory = new Stack<GameSnapshot>(),
                revealedLetterIndices = new HashSet<int>(),
                hintLetterIndex = -1,
                revealedNextWord = ""
            };

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
            if (workingState.isWon || workingState.isLost)
                return;

            string word = action.word.ToLower();
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

                // Check win condition
                if (word == currentPuzzle.endWord)
                {
                    workingState.isWon = true;
                }

                // Reset validator with current state
                wordValidator.Initialize(word, currentPuzzle.endWord, workingState.wordChain.ToArray());
            }
            else
            {
                // Track invalid attempt
                workingState.invalidAttempts++;

                // Invalid word - lose a life and reset streak
                workingState.lives--;
                workingState.currentInput = "";
                workingState.currentStreak = 0;

                if (workingState.lives <= 0)
                {
                    workingState.isLost = true;
                }
            }
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

            var (nextTarget, changedIndex) = ResolveHintTarget();
            if (string.IsNullOrEmpty(nextTarget))
                return; // no actionable reveal, do not consume counter

            workingState.revealedNextWord = nextTarget;
            workingState.hintLetterIndex = changedIndex;
            workingState.revealsRemaining--;
        }

        private void HandleUndo()
        {
            if (workingState.wordChain.Count <= 1 || workingState.isWon || workingState.isLost)
                return;

            // Check if undo history is available
            if (workingState.undoHistory.Count == 0)
            {
                // Fallback: simple word removal without full state restoration
                var lastWord = workingState.wordChain[workingState.wordChain.Count - 1];
                workingState.wordChain.RemoveAt(workingState.wordChain.Count - 1);
                workingState.currentInput = "";

                if (workingState.foundWords.Contains(lastWord))
                {
                    workingState.foundWords.Remove(lastWord);
                    workingState.score -= lastWord.Length;
                }

                // Reset streak since we undid a valid step
                workingState.currentStreak = Mathf.Max(0, workingState.currentStreak - 1);

                // §1.3 — chain rewound, so any active hint/reveal preview is stale.
                workingState.hintLetterIndex = -1;
                workingState.revealedNextWord = string.Empty;
                return;
            }

            // Restore from snapshot
            var snapshot = workingState.undoHistory.Pop();
            workingState.wordChain = new List<string>(snapshot.wordChain);
            workingState.lives = snapshot.lives;
            workingState.score = snapshot.score;
            workingState.currentStreak = snapshot.currentStreak;
            workingState.foundWords = new List<string>(snapshot.foundWords);
            workingState.invalidAttempts = snapshot.invalidAttempts;
            workingState.hintsRemaining = snapshot.hintsRemaining;
            workingState.revealsRemaining = snapshot.revealsRemaining;
            workingState.currentInput = "";

            // §1.3 — chain rewound; clear hint/reveal preview rather than restoring stale state.
            workingState.hintLetterIndex = -1;
            workingState.revealedNextWord = string.Empty;
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
    }

    /// <summary>
    /// Snapshot of game state for undo history. Stores complete state at a point in time.
    /// </summary>
    internal struct GameSnapshot
    {
        public List<string> wordChain;
        public int lives;
        public int score;
        public int currentStreak;
        public List<string> foundWords;
        public int invalidAttempts;
        public int hintsRemaining;
        public int revealsRemaining;
        public int hintLetterIndex;
        public string revealedNextWord;
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

        // Economy tracking for Phase 2
        public int hintsRemaining = 2;
        public int revealsRemaining = 1;
        public Stack<GameSnapshot> undoHistory = new Stack<GameSnapshot>();
        // DEPRECATED — replaced by hintLetterIndex + revealedNextWord. Retained for back-compat
        // with consumers that still read the index set; new hint/reveal paths do not write to it.
        public HashSet<int> revealedLetterIndices = new HashSet<int>();

        // Hint/reveal surface: index that the most recent hint exposed (-1 if none),
        // and the next solution word once the player spends a reveal ("" if none).
        public int hintLetterIndex = -1;
        public string revealedNextWord = "";
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
