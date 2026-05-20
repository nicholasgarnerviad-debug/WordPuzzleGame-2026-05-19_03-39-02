using UnityEngine;

public class ClassicMode : MonoBehaviour, IGameMode
{
    private GameModeContext context;
    private WordPuzzle currentPuzzle;
    private ClassicModeStats stats;
    private bool isActive;
    private System.Random random;

    public void Initialize(GameModeContext context)
    {
        this.context = context;
        this.stats = new ClassicModeStats();
        this.random = new System.Random();
    }

    public void StartGame()
    {
        isActive = true;
        stats.gamesPlayed++;
        LoadNextPuzzle();
        Logger.Log("Classic Mode started");
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
        else if (state.isLost)
        {
            OnGameOver();
        }
    }

    public void Update(float deltaTime)
    {
        // No-op for Classic Mode
    }

    public void OnGameOver()
    {
        isActive = false;
        context.RaiseGameOver();
        Logger.Log("Classic Mode ended");
    }

    public ModeStats GetStats()
    {
        return new ModeStats
        {
            modeName = "Classic",
            coinsEarned = stats.totalCoinsEarned,
            puzzlesCompleted = stats.totalPuzzlesCompleted,
            totalTime = 0
        };
    }

    private void LoadNextPuzzle()
    {
        // Generate random puzzle: Easy or Medium 50/50
        Difficulty difficulty = random.Next(2) == 0 ? Difficulty.Easy : Difficulty.Medium;
        var puzzleDefinition = context.puzzleGenerator.GenerateRandomPuzzle(difficulty);

        currentPuzzle = new WordPuzzle(
            puzzleDefinition.puzzleId,
            puzzleDefinition.startWord,
            puzzleDefinition.endWord,
            puzzleDefinition.optimalSteps,
            puzzleDefinition.solution,
            puzzleDefinition.seedValue,
            difficulty
        );

        context.stateManager.StartNewPuzzle(currentPuzzle);
        Logger.Log($"Classic Mode: Loaded puzzle {currentPuzzle.puzzleId} ({difficulty})");
    }

    private void OnPuzzleComplete()
    {
        stats.gamesWon++;
        stats.totalPuzzlesCompleted++;

        int reward = CalculateReward();
        // Fire and forget the async operation
        _ = context.economy.AddCoinsAsync(reward, "classic_mode");
        stats.totalCoinsEarned += reward;

        Logger.Log($"Puzzle completed! Earned {reward} coins");

        LoadNextPuzzle();
    }

    private int CalculateReward()
    {
        const int baseReward = 10;
        var state = context.stateManager.GetCurrentState();
        int livesBonus = Mathf.Max(0, state.lives * 5);
        int totalReward = baseReward + livesBonus;
        return Mathf.Max(10, totalReward);
    }
}

public class ClassicModeStats
{
    public int gamesPlayed;
    public int gamesWon;
    public int totalPuzzlesCompleted;
    public int totalCoinsEarned;

    public ClassicModeStats()
    {
        gamesPlayed = 0;
        gamesWon = 0;
        totalPuzzlesCompleted = 0;
        totalCoinsEarned = 0;
    }
}
