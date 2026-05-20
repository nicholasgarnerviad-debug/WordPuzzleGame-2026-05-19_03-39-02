using UnityEngine;

public class ClassicMode : MonoBehaviour, IGameMode
{
    private GameModeContext context;
    private int totalScore = 0;
    private int puzzlesCompleted = 0;
    private int coinsEarned = 0;
    private int totalTime = 0;

    public void Initialize(GameModeContext context)
    {
        this.context = context;
    }

    public void StartGame()
    {
        totalScore = 0;
        puzzlesCompleted = 0;
        coinsEarned = 0;
        totalTime = 0;
        Logger.Log("Classic Mode started");
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
        Logger.Log($"Classic Mode ended. Final score: {totalScore}");
    }

    public ModeStats GetStats()
    {
        return new ModeStats
        {
            modeName = "Classic",
            coinsEarned = coinsEarned,
            puzzlesCompleted = puzzlesCompleted,
            totalTime = totalTime
        };
    }
}
