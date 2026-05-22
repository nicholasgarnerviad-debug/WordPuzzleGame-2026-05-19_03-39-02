using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using WordPuzzleGame.UI;

public class GameplayScreen : MonoBehaviour
{
    [Header("Header Components")]
    [SerializeField] private TextMeshProUGUI gameModeLabel;
    [SerializeField] private TextMeshProUGUI scoreDisplay;
    [SerializeField] private TextMeshProUGUI timerDisplay;
    [SerializeField] private Button menuButton;

    [Header("Input Section")]
    [SerializeField] private TMP_InputField currentWordInput;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button submitButton;

    [Header("Tile Grid")]
    [SerializeField] private Transform tileGridContainer;
    [SerializeField] private LetterTile letterTilePrefab;

    [Header("Word Chain")]
    [SerializeField] private Transform wordListContent;
    [SerializeField] private TextMeshProUGUI wordItemPrefab;

    [Header("Game Status")]
    [SerializeField] private TextMeshProUGUI streakDisplay;
    [SerializeField] private TextMeshProUGUI wordsRemainingDisplay;
    [SerializeField] private Button endGameButton;
    [SerializeField] private CanvasGroup canvasGroup;

    private IGameStateManager stateManager;
    private ModeController modeController;
    private UIManager uiManager;
    private IDisposable stateSubscription;
    private readonly List<LetterTile> letterTiles = new();
    private GameState currentState;
    private LayoutGroup wordChainLayout;

    public void InjectDependencies(IGameStateManager sm, ModeController mc, UIManager ui)
    {
        stateManager   = sm;
        modeController = mc;
        uiManager      = ui;
    }

    private void CacheComponents()
    {
        if (wordListContent != null)
        {
            wordChainLayout = wordListContent.GetComponent<LayoutGroup>();
        }
    }

    private void OnEnable()
    {
        if (stateManager == null) return;

        CacheComponents();
        stateSubscription = stateManager.Subscribe(OnStateChanged);

        if (letterTiles.Count == 0) BuildKeyboard();

        if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearInput);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuPressed);
        if (endGameButton != null) endGameButton.onClick.AddListener(OnEndGamePressed);

        UpdateModeLabel();
        Refresh(stateManager.GetCurrentState());
    }

    private void OnDisable()
    {
        stateSubscription?.Dispose();
        stateSubscription = null;

        if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
        if (clearButton != null) clearButton.onClick.RemoveListener(OnClearInput);
        if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuPressed);
        if (endGameButton != null) endGameButton.onClick.RemoveListener(OnEndGamePressed);
    }

    private void BuildKeyboard()
    {
        if (letterTilePrefab == null || tileGridContainer == null) return;

        for (char c = 'a'; c <= 'z'; c++)
        {
            var tile = Instantiate(letterTilePrefab, tileGridContainer);
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

        UpdateScoreDisplay(state);
        UpdateStreakDisplay(state);
        UpdateWordsRemainingDisplay(state);
        UpdateTimerDisplay(state);
        UpdateWordChain(state);
        UpdateInputDisplay(state);

        bool canInput = !state.isWon && !state.isLost;
        if (submitButton != null) submitButton.interactable = canInput && state.currentInput.Length > 0;
        if (clearButton != null) clearButton.interactable = canInput && state.currentInput.Length > 0;

        foreach (var tile in letterTiles)
            tile.SetEnabled(canInput);

        if (state.hintedLetterIndex.HasValue)
            HighlightHintedLetter(state);
    }

    private void UpdateScoreDisplay(GameState state)
    {
        if (scoreDisplay != null)
            scoreDisplay.text = state.score.ToString();
    }

    private void UpdateStreakDisplay(GameState state)
    {
        if (streakDisplay != null)
            streakDisplay.text = $"Streak: {state.currentStreak}";
    }

    private void UpdateWordsRemainingDisplay(GameState state)
    {
        if (wordsRemainingDisplay != null)
            wordsRemainingDisplay.text = $"Words: {state.wordsRemaining}";
    }

    private void UpdateTimerDisplay(GameState state)
    {
        if (timerDisplay != null)
        {
            // Only show timer for TimeAttack mode - check if mode supports it
            bool hasTimeRemaining = state.timeRemaining > 0;
            timerDisplay.gameObject.SetActive(hasTimeRemaining);

            if (hasTimeRemaining)
                timerDisplay.text = $"{Mathf.Max(0, state.timeRemaining):F1}s";
        }
    }

    private void UpdateModeLabel()
    {
        if (gameModeLabel != null && modeController != null)
        {
            // Mode label should be set based on current game mode
            gameModeLabel.text = "Game Mode";  // Will be updated by mode controller if needed
        }
    }

    private void UpdateWordChain(GameState state)
    {
        if (wordListContent != null && state.wordChain.Length > 0)
        {
            // Batch layout updates to avoid redundant recalculation
            if (wordChainLayout != null)
                wordChainLayout.enabled = false;

            // Word chain display - could instantiate word items here if needed
            // For now, just validate the container exists

            if (wordChainLayout != null)
            {
                wordChainLayout.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutHierarchy((RectTransform)wordListContent);
            }
        }
    }

    private void UpdateInputDisplay(GameState state)
    {
        if (currentWordInput != null)
        {
            currentWordInput.text = state.currentInput;
        }
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

    private void OnClearInput()
    {
        if (currentWordInput != null)
            currentWordInput.text = string.Empty;
    }

    private void OnMenuPressed()
    {
        if (modeController != null)
            modeController.EndMode();
    }

    private void OnEndGamePressed()
    {
        if (modeController != null)
            modeController.EndMode();
    }
}
