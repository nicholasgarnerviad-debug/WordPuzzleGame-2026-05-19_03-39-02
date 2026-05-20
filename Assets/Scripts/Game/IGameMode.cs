using System;

public interface IGameMode
{
    void Initialize(GameModeContext context);
    void StartGame();
    void HandleInput(GameAction action);
    void Update(float deltaTime);
    void OnGameOver();
    ModeStats GetStats();
}

public class GameModeContext
{
    public IPuzzleGenerator puzzleGenerator;
    public IWordValidator wordValidator;
    public IGameStateManager stateManager;
    public IEconomyManager economy;
    public IDataManager dataManager;

    public event Action<ModeStats> OnModeComplete;
    public event Action OnGameOver;

    public void RaiseModeComplete(ModeStats stats)
    {
        OnModeComplete?.Invoke(stats);
    }

    public void RaiseGameOver()
    {
        OnGameOver?.Invoke();
    }
}

public class ModeStats
{
    public string modeName;
    public int coinsEarned;
    public int puzzlesCompleted;
    public int totalTime;
}
