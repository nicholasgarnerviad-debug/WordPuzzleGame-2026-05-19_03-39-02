using System;
using UnityEngine;

public enum ModeType
{
    Classic,
    PuzzleShow,
    TimeAttack
}

public class ModeController : MonoBehaviour
{
    private IGameMode activeMode;
    private GameModeContext modeContext;

    public event Action<ModeStats> ModeCompleted;

    public void Initialize(IDataManager data, IEconomyManager economy, IPuzzleGenerator puzzleGen)
    {
        modeContext = new GameModeContext
        {
            puzzleGenerator = puzzleGen,
            dataManager = data,
            economy = economy
        };

        // Wire up mode complete events
        if (modeContext.OnModeComplete != null)
        {
            modeContext.OnModeComplete -= OnModeComplete;
        }
        modeContext.OnModeComplete += OnModeComplete;
    }

    public void SwitchMode(ModeType modeType)
    {
        // Clean up existing mode
        if (activeMode != null)
        {
            activeMode.OnGameOver();
        }

        // Create new mode
        activeMode = CreateMode(modeType);

        // Initialize and start
        if (activeMode != null)
        {
            activeMode.Initialize(modeContext);
            activeMode.StartGame();
            Logger.Log($"Switched to mode: {modeType}");
        }
    }

    private void Update()
    {
        if (activeMode != null)
        {
            activeMode.Update(Time.deltaTime);
        }
    }

    private IGameMode CreateMode(ModeType modeType)
    {
        return modeType switch
        {
            ModeType.Classic => new ClassicMode(),
            ModeType.PuzzleShow => new PuzzleShowMode(),
            ModeType.TimeAttack => new TimeAttackMode(),
            _ => null
        };
    }

    private void OnModeComplete(ModeStats stats)
    {
        ModeCompleted?.Invoke(stats);
    }
}
