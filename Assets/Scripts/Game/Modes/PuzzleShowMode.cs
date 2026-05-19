using UnityEngine;
using System.Collections.Generic;

public class PuzzleShowMode : MonoBehaviour, IGameMode
{
    private GameController gameController;
    private int currentTier = 1;
    private int currentPuzzleInTier = 0;
    private int coinsEarned = 0;
    private List<List<string>> tierPuzzles;

    public void Initialize()
    {
        gameController = GetComponent<GameController>();
        gameController.PuzzleCompleted += OnPuzzleCompleted;
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
        LoadNextPuzzle();
        Logger.Log("Puzzle Show Mode started");
    }

    private void LoadNextPuzzle()
    {
        if (currentPuzzleInTier < tierPuzzles.Count)
        {
            gameController.GenerateNewPuzzle(currentTier);
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

    public void OnGameOver()
    {
        Logger.Log($"Puzzle Show Mode ended at Tier {currentTier}");
    }

    public int GetCoinsEarned() => coinsEarned;
    public string GetModeName() => "Puzzle Show";
}
