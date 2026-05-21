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
    [SerializeField] private TimerDisplay timerDisplay;

    private IGameMode activeMode;
    private GameModeContext modeContext;

    public event Action<ModeStats> ModeCompleted;
    public ModeType LastMode { get; private set; }

    public void Initialize(IDataManager data, IEconomyManager economy,
                           IPuzzleGenerator puzzleGen,
                           IGameStateManager stateManager, IWordValidator wordValidator)
    {
        modeContext = new GameModeContext
        {
            puzzleGenerator = puzzleGen,
            dataManager     = data,
            economy         = economy,
            stateManager    = stateManager,
            wordValidator   = wordValidator
        };
        modeContext.OnModeComplete += OnModeComplete;
    }

    public void SwitchMode(ModeType modeType)
    {
        if (activeMode != null)
        {
            activeMode.OnGameOver();
            if (activeMode is MonoBehaviour old) Destroy(old);
        }

        LastMode = modeType;
        activeMode = CreateMode(modeType);

        if (activeMode != null)
        {
            activeMode.Initialize(modeContext);
            activeMode.StartGame();
            Logger.Log($"Switched to mode: {modeType}");
        }

        timerDisplay?.BindToMode(activeMode as TimeAttackMode);
    }

    public void HandleInput(GameAction action) => activeMode?.HandleInput(action);

    private void Update()
    {
        activeMode?.Update(Time.deltaTime);
    }

    private IGameMode CreateMode(ModeType modeType)
    {
        return modeType switch
        {
            ModeType.Classic    => gameObject.AddComponent<ClassicMode>(),
            ModeType.PuzzleShow => gameObject.AddComponent<PuzzleShowMode>(),
            ModeType.TimeAttack => gameObject.AddComponent<TimeAttackMode>(),
            _ => null
        };
    }

    private void OnModeComplete(ModeStats stats)
    {
        ModeCompleted?.Invoke(stats);
    }
}
