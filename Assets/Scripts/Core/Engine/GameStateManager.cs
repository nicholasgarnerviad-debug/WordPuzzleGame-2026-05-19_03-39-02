using System;
using System.Collections.Generic;
using System.Linq;
using WordPuzzle.Puzzle;
using WordPuzzle.State;
using UnityEngine;

namespace WordPuzzle.State
{
    public class GameStateManager : IGameStateManager
    {
    private GameState currentState;
    private WordPuzzle currentPuzzle;
    private IWordValidator wordValidator;
    private IDataManager dataManager;
    private List<Action<GameState>> subscribers;
    private List<string> foundWords;
    private int longestStreak;
    private int totalScore;

    public GameStateManager(IWordValidator wordValidator, IDataManager dataManager)
    {
        this.wordValidator = wordValidator;
        this.dataManager = dataManager;
        this.subscribers = new List<Action<GameState>>();
        this.currentState = new GameState();
        this.foundWords = new List<string>();
        this.longestStreak = 0;
        this.totalScore = 0;
    }

    public GameState GetCurrentState()
    {
        return currentState.Clone();
    }

    public void StartNewPuzzle(WordPuzzle puzzle)
    {
        currentPuzzle = puzzle;
        currentState = new GameState
        {
            wordChain = new[] { puzzle.startWord },
            currentInput = "",
            lives = 3,
            isWon = false,
            isLost = false,
            hintedLetterIndex = null,
            revealedWord = null,
            previousChainLength = 0,
            targetWord = puzzle.endWord,
            score = totalScore,
            currentStreak = 0,
            wordsRemaining = 0,
            timeRemaining = 0f
        };

        wordValidator.Initialize(puzzle.startWord, puzzle.endWord, currentState.wordChain);

        NotifySubscribers();
        SaveState();
    }

    public void Dispatch(GameAction action)
    {
        GameState newState = currentState.Clone();

        switch (action)
        {
            case PressLetterAction a:
                HandlePressLetter(newState, a);
                break;
            case DeleteLetterAction:
                HandleDeleteLetter(newState);
                break;
            case SubmitWordAction a:
                HandleSubmitWord(newState, a);
                break;
            case UseHintAction a:
                HandleUseHint(newState, a);
                break;
            case UseRevealAction:
                HandleUseReveal(newState);
                break;
            case UndoStepAction:
                HandleUndo(newState);
                break;
            case ResetGameAction a:
                StartNewPuzzle(a.puzzle);
                return;
        }

        currentState = newState;
        NotifySubscribers();
        SaveState();
    }

    public IDisposable Subscribe(Action<GameState> observer)
    {
        subscribers.Add(observer);
        return new Unsubscriber(subscribers, observer);
    }

    private void HandlePressLetter(GameState state, PressLetterAction action)
    {
        if (state.currentInput.Length >= 10 || state.isWon || state.isLost)
            return;

        state.currentInput += char.ToLower(action.letter);
    }

    private void HandleDeleteLetter(GameState state)
    {
        if (state.currentInput.Length > 0 && !state.isWon && !state.isLost)
        {
            state.currentInput = state.currentInput.Substring(0, state.currentInput.Length - 1);
        }
    }

    private void HandleSubmitWord(GameState state, SubmitWordAction action)
    {
        if (state.isWon || state.isLost)
            return;

        string word = action.word.ToLower();
        var validation = wordValidator.ValidateWord(word);

        if (validation.isValid && validation.isNextStep)
        {
            // Add to chain
            var newChain = new List<string>(state.wordChain)
            {
                word
            };
            state.wordChain = newChain.ToArray();
            state.previousChainLength = state.wordChain.Length - 1;
            state.currentInput = "";

            // Track valid word
            if (!foundWords.Contains(word))
            {
                foundWords.Add(word);
                // Calculate points based on word length
                int points = word.Length;
                totalScore += points;
                state.score = totalScore;
            }

            // Update streak
            state.currentStreak++;
            longestStreak = Mathf.Max(longestStreak, state.currentStreak);

            // Check win condition
            if (word == currentPuzzle.endWord)
            {
                state.isWon = true;
            }
        }
        else
        {
            // Invalid word - lose a life and reset streak
            state.lives--;
            state.currentInput = "";
            state.currentStreak = 0;

            if (state.lives <= 0)
            {
                state.isLost = true;
            }
        }

        state.hintedLetterIndex = null;
    }

    private void HandleUseHint(GameState state, UseHintAction action)
    {
        if (state.isWon || state.isLost)
            return;

        state.hintedLetterIndex = action.letterIndex;
    }

    private void HandleUseReveal(GameState state)
    {
        if (state.isWon || state.isLost)
            return;

        state.revealedWord = currentPuzzle.endWord.ToCharArray().Select(c => c.ToString()).ToArray();
    }

    private void HandleUndo(GameState state)
    {
        if (state.wordChain.Length <= 1 || state.isWon || state.isLost)
            return;

        // Remove last word
        var newChain = new List<string>(state.wordChain);
        newChain.RemoveAt(newChain.Count - 1);
        state.wordChain = newChain.ToArray();
        state.currentInput = "";
    }

    private void NotifySubscribers()
    {
        foreach (var subscriber in subscribers)
        {
            try
            {
                subscriber(currentState.Clone());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error notifying subscriber: {ex.Message}");
            }
        }
    }

    private void SaveState()
    {
        // Async save - don't wait for it
        var snapshot = new GameStateSnapshot
        {
            currentMode = "Gameplay",
            currentPuzzleId = currentPuzzle?.puzzleId ?? 0,
            wordChain = currentState.wordChain,
            currentInput = currentState.currentInput,
            lives = currentState.lives,
            hintsUsed = 0,  // TODO: Track from economy
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
        // Return all letters that haven't been used yet in the current chain
        var usedLetters = new HashSet<char>();
        foreach (var word in currentState.wordChain)
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
        return totalScore;
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
        if (!IsValidWord(word))
            return 0;

        word = word.ToLower();
        if (foundWords.Contains(word))
            return 0; // Already found

        foundWords.Add(word);
        int points = word.Length;
        totalScore += points;
        currentState.score = totalScore;
        currentState.currentStreak++;
        longestStreak = Mathf.Max(longestStreak, currentState.currentStreak);

        return points;
    }

    public int GetCurrentStreak()
    {
        return currentState.currentStreak;
    }

    public int GetWordsRemaining()
    {
        return currentState.wordsRemaining;
    }

    public void SetWordsRemaining(int count)
    {
        currentState.wordsRemaining = count;
        NotifySubscribers();
    }

    public float GetTimeRemaining()
    {
        return currentState.timeRemaining;
    }

    public void SetTimeRemaining(float time)
    {
        currentState.timeRemaining = time;
        NotifySubscribers();
    }

    public string GetBestWord()
    {
        if (foundWords.Count == 0)
            return "--";

        return foundWords.OrderByDescending(w => w.Length).FirstOrDefault() ?? "--";
    }

    public int GetLongestStreak()
    {
        return longestStreak;
    }

    public ResultsScreen.GameStats GetFinalStats()
    {
        float gameDuration = Time.time; // Would be better tracked separately
        int validAttempts = foundWords.Count;
        int totalAttempts = foundWords.Count; // Simplified - would need full tracking for accuracy

        return new ResultsScreen.GameStats
        {
            finalScore = totalScore,
            gameDuration = gameDuration,
            wordsFound = foundWords.Count,
            validAttempts = validAttempts,
            totalAttempts = totalAttempts,
            bestWord = GetBestWord(),
            currentStreak = currentState.currentStreak,
            longestStreak = longestStreak
        };
    }

    public void ResetTracking()
    {
        foundWords.Clear();
        longestStreak = 0;
        totalScore = 0;
    }

    private class Unsubscriber : IDisposable
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

    public interface IGameStateManager
    {
        GameState GetCurrentState();
        void StartNewPuzzle(WordPuzzle puzzle);
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
        ResultsScreen.GameStats GetFinalStats();
        void ResetTracking();
    }

}
