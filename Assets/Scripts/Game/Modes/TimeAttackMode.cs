using System;
using UnityEngine;

public class TimeAttackMode : MonoBehaviour, IGameMode
{
    private GameModeContext context;
    private float timeRemaining;
    private float currentTimeLimit;
    private int roundCount;
    private TimeAttackModeStats stats;
    private bool isActive;
    private System.Random random;

    public event Action<float> TimeChanged;

    public void Initialize(GameModeContext context)
    {
        this.context = context;
        this.stats = new TimeAttackModeStats();
        this.random = new System.Random();
    }

    public void StartGame()
    {
        isActive = true;
        roundCount = 0;
        stats.sessionStartTime = Time.time;
        StartNewRound();
        Logger.Log("Time Attack Mode started");
    }

    public void HandleInput(GameAction action)
    {
        if (!isActive)
            return;

        context.stateManager.Dispatch(action);
        var state = context.stateManager.GetCurrentState();

        if (state.isWon)
        {
            OnPuzzleComplete();
        }
    }

    public void Update(float deltaTime)
    {
        if (!isActive) return;

        timeRemaining -= deltaTime;
        TimeChanged?.Invoke(timeRemaining);

        if (timeRemaining <= 0)
        {
            OnTimeUp();
        }
    }

    public void OnGameOver()
    {
        isActive = false;
        context.RaiseGameOver();
    }

    public ModeStats GetStats()
    {
        float elapsedTime = Time.time - stats.sessionStartTime;
        return new ModeStats
        {
            modeName = "Time Attack",
            coinsEarned = stats.totalCoinsEarned,
            puzzlesCompleted = roundCount,
            totalTime = (int)elapsedTime
        };
    }

    private void StartNewRound()
    {
        roundCount++;

        if (roundCount == 1)
        {
            currentTimeLimit = Constants.TIME_ATTACK_START;
        }
        else
        {
            currentTimeLimit = Mathf.Max(Constants.TIME_ATTACK_MIN, currentTimeLimit - Constants.TIME_ATTACK_DECREMENT);
        }

        timeRemaining = currentTimeLimit;

        // Generate random medium puzzle
        var puzzleDefinition = context.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Medium);
        var puzzle = new WordPuzzle(
            puzzleDefinition.puzzleId,
            puzzleDefinition.startWord,
            puzzleDefinition.endWord,
            puzzleDefinition.optimalSteps,
            puzzleDefinition.solution,
            puzzleDefinition.seedValue,
            Difficulty.Medium
        );

        context.stateManager.StartNewPuzzle(puzzle);
        Logger.Log($"Time Attack: Round {roundCount}, Time: {currentTimeLimit}s");
    }

    private async void OnPuzzleComplete()
    {
        int reward = Constants.TIME_ATTACK_BASE_REWARD + (int)(Constants.TIME_ATTACK_BONUS_PER_SECOND * timeRemaining);
        await context.economy.AddCoinsAsync(reward, "time_attack");
        stats.totalCoinsEarned += reward;
        stats.bestRoundReached = Mathf.Max(stats.bestRoundReached, roundCount);

        Logger.Log($"Puzzle completed! Earned {reward} coins");

        StartNewRound();
    }

    private void OnTimeUp()
    {
        isActive = false;
        Logger.Log($"Time Attack ended after {roundCount} rounds");
        context.RaiseGameOver();
    }
}

public class TimeAttackModeStats
{
    public int bestRoundReached;
    public int totalCoinsEarned;
    public float sessionStartTime;

    public TimeAttackModeStats()
    {
        bestRoundReached = 0;
        totalCoinsEarned = 0;
        sessionStartTime = 0f;
    }
}
