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
                workingState.currentInput
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
                elapsedTime = 0f
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

        private void HandleUseHint(UseHintAction action)
        {
            if (workingState.isWon || workingState.isLost)
                return;

            // Hint logic would go here
        }

        private void HandleUseReveal()
        {
            if (workingState.isWon || workingState.isLost)
                return;

            // Reveal logic would go here
        }

        private void HandleUndo()
        {
            if (workingState.wordChain.Count <= 1 || workingState.isWon || workingState.isLost)
                return;

            // Remove last word
            var lastWord = workingState.wordChain[workingState.wordChain.Count - 1];
            workingState.wordChain.RemoveAt(workingState.wordChain.Count - 1);
            workingState.currentInput = "";

            // Decrement score if this was a found word
            if (workingState.foundWords.Contains(lastWord))
            {
                workingState.foundWords.Remove(lastWord);
                workingState.score -= lastWord.Length;
            }
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
