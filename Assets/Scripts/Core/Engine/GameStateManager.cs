using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameStateManager : IGameStateManager
{
    private GameState currentState;
    private WordPuzzle currentPuzzle;
    private WordValidator wordValidator;
    private IDataManager dataManager;
    private List<Action<GameState>> subscribers;

    public GameStateManager(WordValidator wordValidator, IDataManager dataManager)
    {
        this.wordValidator = wordValidator;
        this.dataManager = dataManager;
        this.subscribers = new List<Action<GameState>>();
        this.currentState = new GameState();
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
            previousChainLength = 0
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

            // Check win condition
            if (word == currentPuzzle.endWord)
            {
                state.isWon = true;
            }
        }
        else
        {
            // Invalid word - lose a life
            state.lives--;
            state.currentInput = "";

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
}

public interface IGameStateManager
{
    GameState GetCurrentState();
    void StartNewPuzzle(WordPuzzle puzzle);
    void Dispatch(GameAction action);
    IDisposable Subscribe(Action<GameState> observer);
}
