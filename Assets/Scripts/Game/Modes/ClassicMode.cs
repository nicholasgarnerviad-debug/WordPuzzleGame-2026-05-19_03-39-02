using UnityEngine;

public class ClassicMode : MonoBehaviour, IGameMode
{
    private GameController gameController;
    private int totalScore = 0;
    private int puzzlesCompleted = 0;
    private int coinsEarned = 0;

    public void Initialize()
    {
        gameController = GetComponent<GameController>();
        gameController.PuzzleCompleted += OnPuzzleCompleted;
    }

    public void StartGame()
    {
        totalScore = 0;
        puzzlesCompleted = 0;
        coinsEarned = 0;
        gameController.GenerateNewPuzzle(1);
        Logger.Log("Classic Mode started");
    }

    private void OnPuzzleCompleted(int score)
    {
        puzzlesCompleted++;
        totalScore += score;
        coinsEarned += Constants.CLASSIC_COIN_REWARD;

        Logger.Log($"Puzzle {puzzlesCompleted} completed. Total score: {totalScore}");

        // Auto-generate next puzzle
        gameController.GenerateNewPuzzle(puzzlesCompleted);
    }

    public void OnPuzzleComplete(int score)
    {
        OnPuzzleCompleted(score);
    }

    public void OnGameOver()
    {
        Logger.Log($"Classic Mode ended. Final score: {totalScore}");
    }

    public int GetCoinsEarned() => coinsEarned;
    public string GetModeName() => "Classic";
}
