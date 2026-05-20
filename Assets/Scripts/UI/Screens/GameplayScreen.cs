using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class GameplayScreen : MonoBehaviour
{
    [SerializeField] private WordChainDisplay wordChainDisplay;
    [SerializeField] private CurrentWordInput currentWordInput;
    [SerializeField] private Transform letterTileContainer;
    [SerializeField] private LetterTile letterTilePrefab;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button revealButton;
    [SerializeField] private Button undoButton;

    private IGameStateManager gameStateManager;
    private IEconomyManager economyManager;
    private GameState currentState;
    private IDisposable stateSubscription;
    private List<LetterTile> letterTiles;

    public void Initialize(IGameStateManager gameStateManager, IEconomyManager economyManager)
    {
        this.gameStateManager = gameStateManager;
        this.economyManager = economyManager;

        // Subscribe to state changes
        stateSubscription = gameStateManager.Subscribe(OnGameStateChanged);

        // Instantiate letter tiles A-Z
        letterTiles = new List<LetterTile>();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            LetterTile tile = Instantiate(letterTilePrefab, letterTileContainer);
            tile.Initialize(c);
            tile.OnLetterPressed += OnLetterPressed;
            letterTiles.Add(tile);
        }

        // Wire up button handlers
        submitButton.onClick.AddListener(OnSubmitPressed);
        hintButton.onClick.AddListener(OnHintPressed);
        revealButton.onClick.AddListener(OnRevealPressed);
        undoButton.onClick.AddListener(OnUndoPressed);

        // Initial UI refresh
        RefreshUI();
    }

    private void OnGameStateChanged(GameState newState)
    {
        currentState = newState;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentState == null)
            return;

        // Update word chain display
        wordChainDisplay.UpdateChain(currentState.wordChain);

        // Update current input display
        currentWordInput.UpdateInput(currentState.currentInput, currentState.targetWord ?? "?");

        // Update lives display
        livesText.text = $"Lives: {currentState.lives}";
    }

    private void OnLetterPressed(char letter)
    {
        gameStateManager.Dispatch(new PressLetterAction(letter));
    }

    private void OnSubmitPressed()
    {
        if (currentState != null && !string.IsNullOrEmpty(currentState.currentInput))
        {
            gameStateManager.Dispatch(new SubmitWordAction(currentState.currentInput));
        }
    }

    private void OnHintPressed()
    {
        if (currentState != null && currentState.currentInput.Length > 0)
        {
            int hintIndex = Mathf.Min(currentState.currentInput.Length, currentState.currentInput.Length - 1);
            gameStateManager.Dispatch(new UseHintAction(hintIndex));
        }
    }

    private void OnRevealPressed()
    {
        gameStateManager.Dispatch(new UseRevealAction());
    }

    private void OnUndoPressed()
    {
        gameStateManager.Dispatch(new UndoStepAction());
    }

    private void OnDestroy()
    {
        stateSubscription?.Dispose();

        // Clean up button handlers to prevent duplicate action risk
        if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitPressed);
        if (hintButton != null) hintButton.onClick.RemoveListener(OnHintPressed);
        if (revealButton != null) revealButton.onClick.RemoveListener(OnRevealPressed);
        if (undoButton != null) undoButton.onClick.RemoveListener(OnUndoPressed);

        // Clean up tile event handlers to prevent memory leaks
        if (letterTiles != null)
        {
            foreach (var tile in letterTiles)
            {
                if (tile != null) tile.OnLetterPressed -= OnLetterPressed;
            }
        }
    }
}
