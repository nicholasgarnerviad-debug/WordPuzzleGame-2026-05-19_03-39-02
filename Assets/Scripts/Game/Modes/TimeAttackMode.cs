using UnityEngine;

public class TimeAttackMode : MonoBehaviour, IGameMode
{
    private GameController gameController;
    private float currentTimeLimit = 90f; // Start at 90 seconds
    private float timeRemaining = 90f;
    private int currentRound = 0;
    private int coinsEarned = 0;
    private bool isActive = false;

    public void Initialize()
    {
        gameController = GetComponent<GameController>();
        gameController.PuzzleCompleted += OnPuzzleCompleted;
    }

    public void StartGame()
    {
        currentRound = 0;
        coinsEarned = 0;
        currentTimeLimit = 90f;
        StartNewRound();
        isActive = true;
        Logger.Log("Time Attack Mode started");
    }

    private void StartNewRound()
    {
        currentRound++;
        timeRemaining = currentTimeLimit;
        gameController.GenerateNewPuzzle(1); // Fixed difficulty
        Logger.Log($"Time Attack: Round {currentRound}, Time: {currentTimeLimit}s");
    }

    private void Update()
    {
        if (!isActive) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            OnTimeUp();
        }
    }

    private void OnPuzzleCompleted(int score)
    {
        coinsEarned += Constants.TIME_ATTACK_COIN_REWARD;
        coinsEarned += Constants.TIME_ATTACK_BONUS_PER_ROUND; // Bonus for surviving round

        // Decrease time for next round
        currentTimeLimit = Mathf.Max(30f, currentTimeLimit - 5f); // Minimum 30s

        Logger.Log($"Round {currentRound} completed. Next round time: {currentTimeLimit}s");
        StartNewRound();
    }

    private void OnTimeUp()
    {
        isActive = false;
        Logger.Log($"Time Attack ended after {currentRound} rounds");
    }

    public void OnGameOver()
    {
        isActive = false;
        Logger.Log($"Time Attack Mode ended");
    }

    public int GetCoinsEarned() => coinsEarned;
    public float GetTimeRemaining() => timeRemaining;
    public int GetCurrentRound() => currentRound;
    public string GetModeName() => "Time Attack";
}
