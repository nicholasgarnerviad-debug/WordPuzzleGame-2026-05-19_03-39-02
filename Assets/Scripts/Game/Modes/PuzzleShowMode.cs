using UnityEngine;
using System.Collections.Generic;

public class PuzzleShowMode : MonoBehaviour, IGameMode
{
    private GameModeContext context;
    private int currentTier = 1;
    private int currentPuzzleInTier = 0;
    private int coinsEarned = 0;
    private int puzzlesCompleted = 0;
    private int totalTime = 0;
    private List<List<string>> tierPuzzles;

    public void Initialize(GameModeContext context)
    {
        this.context = context;
        LoadTierData();
    }

    private void LoadTierData()
    {
        tierPuzzles = new List<List<string>>
        {
            new List<string> { "apple", "apply" },
            new List<string> { "brave", "break" },
            new List<string> { "crane", "craft" }
        };
    }

    public void StartGame()
    {
        currentTier = 1;
        currentPuzzleInTier = 0;
        coinsEarned = 0;
        puzzlesCompleted = 0;
        totalTime = 0;
        LoadNextPuzzle();
        Logger.Log("Puzzle Show Mode started");
    }

    private void LoadNextPuzzle()
    {
        if (currentPuzzleInTier < tierPuzzles.Count)
        {
            Logger.Log($"Puzzle Show: Tier {currentTier}, Puzzle {currentPuzzleInTier + 1}");
        }
        else
        {
            UnlockNextTier();
        }
    }

    private void OnPuzzleCompleted(int score)
    {
        coinsEarned += Constants.PUZZLE_SHOW_COIN_REWARD;
        puzzlesCompleted++;
        currentPuzzleInTier++;
        LoadNextPuzzle();
    }

    private void UnlockNextTier()
    {
        currentTier++;
        if (currentTier > 3)
        {
            Logger.Log("All tiers complete!");
            return;
        }
        currentPuzzleInTier = 0;
        LoadNextPuzzle();
    }

    public void HandleInput(GameAction action)
    {
        if (context?.stateManager != null)
        {
            context.stateManager.Dispatch(action);
        }
    }

    public void Update(float deltaTime)
    {
        totalTime += (int)deltaTime;
    }

    public void OnGameOver()
    {
        Logger.Log($"Puzzle Show Mode ended at Tier {currentTier}");
    }

    public ModeStats GetStats()
    {
        return new ModeStats
        {
            modeName = "Puzzle Show",
            coinsEarned = coinsEarned,
            puzzlesCompleted = puzzlesCompleted,
            totalTime = totalTime
        };
    }
}
