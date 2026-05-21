using UnityEngine;
using System.Threading.Tasks;

public class PuzzleShowMode : MonoBehaviour, IGameMode
{
    private GameModeContext context;
    private int currentTier = 1;
    private int currentPuzzleIndex = 0;
    private int maxTiersUnlocked = 1;
    private PuzzleShowModeStats stats;

    public void Initialize(GameModeContext context)
    {
        this.context = context;
        stats = new PuzzleShowModeStats();
    }

    public void StartGame()
    {
        currentTier = 1;
        currentPuzzleIndex = 0;
        stats.sessionStartTime = Time.time;
        LoadPuzzleAtPosition(1, 0);
    }

    public void HandleInput(GameAction action)
    {
        if (context?.stateManager != null)
        {
            context.stateManager.Dispatch(action);

            // Check if puzzle is won or lost
            GameState state = context.stateManager.GetCurrentState();
            if (state.isWon)
            {
                OnPuzzleComplete();
            }
            else if (state.isLost)
            {
                OnGameOver();
            }
        }
    }

    public void Tick(float deltaTime)
    {
        // No-op
    }

    public void OnGameOver()
    {
        context.RaiseGameOver();
    }

    public ModeStats GetStats()
    {
        return new ModeStats
        {
            modeName = "Puzzle Show",
            coinsEarned = stats.totalCoinsEarned,
            puzzlesCompleted = stats.totalPuzzlesCompleted,
            totalTime = (int)(Time.time - stats.sessionStartTime)
        };
    }

    private async void LoadPuzzleAtPosition(int tier, int index)
    {
        TierData tierData = await context.dataManager.GetTierDataAsync(tier);

        if (index >= tierData.puzzles.Length)
        {
            currentTier++;
            currentPuzzleIndex = 0;

            if (currentTier > maxTiersUnlocked)
            {
                maxTiersUnlocked = currentTier;
            }

            if (currentTier <= Constants.MAX_TIERS)
            {
                LoadPuzzleAtPosition(currentTier, 0);
            }
            else
            {
                Debug.Log("All tiers complete!");
                OnGameOver();
            }
        }
        else
        {
            PuzzleDefinition puzzleDef = tierData.puzzles[index];
            WordPuzzle puzzle = new WordPuzzle(
                puzzleDef.puzzleId,
                puzzleDef.startWord,
                puzzleDef.endWord,
                puzzleDef.optimalSteps,
                puzzleDef.solution,
                puzzleDef.seedValue,
                Difficulty.Medium
            );

            context.stateManager.StartNewPuzzle(puzzle);
        }
    }

    private async void OnPuzzleComplete()
    {
        currentPuzzleIndex++;
        stats.totalPuzzlesCompleted++;

        int reward = Constants.PUZZLE_SHOW_BASE_REWARD;
        await context.economy.AddCoinsAsync(reward, "puzzle_show");
        stats.totalCoinsEarned += reward;

        LoadPuzzleAtPosition(currentTier, currentPuzzleIndex);
    }
}

public class PuzzleShowModeStats
{
    public int totalPuzzlesCompleted;
    public int totalCoinsEarned;
    public float sessionStartTime;
}
