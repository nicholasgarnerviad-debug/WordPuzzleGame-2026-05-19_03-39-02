using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class GameplayScreen : MonoBehaviour
{
    [Header("Word Display")]
    [SerializeField] private WordChainDisplay wordChainDisplay;
    [SerializeField] private CurrentWordInput currentWordInput;

    [Header("Keyboard")]
    [SerializeField] private Transform keyboardContainer;
    [SerializeField] private LetterTile letterTilePrefab;

    [Header("HUD")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Buttons")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button revealButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button deleteButton;

    [Header("Overlays")]
    [SerializeField] private GameObject winOverlay;
    [SerializeField] private GameObject lossOverlay;

    private IGameStateManager stateManager;
    private ModeController modeController;
    private UIManager uiManager;
    private IDisposable stateSubscription;
    private readonly List<LetterTile> letterTiles = new();
    private GameState currentState;

    public void InjectDependencies(IGameStateManager sm, ModeController mc, UIManager ui)
    {
        stateManager   = sm;
        modeController = mc;
        uiManager      = ui;
    }

    private void OnEnable()
    {
        if (stateManager == null) return;

        stateSubscription = stateManager.Subscribe(OnStateChanged);

        if (letterTiles.Count == 0) BuildKeyboard();

        submitButton.onClick.AddListener(OnSubmit);
        hintButton.onClick.AddListener(OnHint);
        revealButton.onClick.AddListener(OnReveal);
        undoButton.onClick.AddListener(OnUndo);
        deleteButton.onClick.AddListener(OnDelete);

        Refresh(stateManager.GetCurrentState());
    }

    private void OnDisable()
    {
        stateSubscription?.Dispose();
        stateSubscription = null;

        submitButton.onClick.RemoveListener(OnSubmit);
        hintButton.onClick.RemoveListener(OnHint);
        revealButton.onClick.RemoveListener(OnReveal);
        undoButton.onClick.RemoveListener(OnUndo);
        deleteButton.onClick.RemoveListener(OnDelete);
    }

    private void BuildKeyboard()
    {
        if (letterTilePrefab == null || keyboardContainer == null) return;

        for (char c = 'a'; c <= 'z'; c++)
        {
            var tile = Instantiate(letterTilePrefab, keyboardContainer);
            tile.Initialize(c);
            var captured = c;
            tile.OnLetterPressed += _ => modeController.HandleInput(new PressLetterAction(captured));
            letterTiles.Add(tile);
        }
    }

    private void OnStateChanged(GameState state) => Refresh(state);

    private void Refresh(GameState state)
    {
        if (state == null) return;
        currentState = state;

        wordChainDisplay?.UpdateChain(state.wordChain);
        currentWordInput?.UpdateInput(state.currentInput, state.targetWord ?? "?");

        if (livesText != null)  livesText.text  = $"Lives: {state.lives}";
        if (scoreText != null)  scoreText.text  = $"Steps: {Mathf.Max(0, state.wordChain.Length - 1)}";

        bool canInput = !state.isWon && !state.isLost;
        if (submitButton != null) submitButton.interactable = canInput && state.currentInput.Length > 0;
        if (hintButton   != null) hintButton.interactable   = canInput;
        if (revealButton != null) revealButton.interactable  = canInput;
        if (undoButton   != null) undoButton.interactable    = canInput && state.wordChain.Length > 1;
        if (deleteButton != null) deleteButton.interactable  = canInput && state.currentInput.Length > 0;

        winOverlay?.SetActive(state.isWon);
        lossOverlay?.SetActive(state.isLost);

        foreach (var tile in letterTiles)
            tile.SetEnabled(canInput);

        if (state.hintedLetterIndex.HasValue)
            HighlightHintedLetter(state);
    }

    private void HighlightHintedLetter(GameState state)
    {
        if (state.targetWord == null || !state.hintedLetterIndex.HasValue) return;
        int idx = state.hintedLetterIndex.Value;
        if (idx < 0 || idx >= state.targetWord.Length) return;
        char hintChar = state.targetWord[idx];
        foreach (var tile in letterTiles)
        {
            if (tile.Letter == hintChar)
            {
                tile.HighlightHint();
                break;
            }
        }
    }

    private void OnSubmit()
    {
        string word = currentState?.currentInput;
        if (!string.IsNullOrEmpty(word))
            modeController.HandleInput(new SubmitWordAction(word));
    }

    private void OnHint()   => modeController.HandleInput(new UseHintAction(0));
    private void OnReveal() => modeController.HandleInput(new UseRevealAction());
    private void OnUndo()   => modeController.HandleInput(new UndoStepAction());
    private void OnDelete() => modeController.HandleInput(new DeleteLetterAction());
}
