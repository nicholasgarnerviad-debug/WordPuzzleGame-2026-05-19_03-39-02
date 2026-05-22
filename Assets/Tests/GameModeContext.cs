using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.Puzzle;
using UnityEngine;

/// <summary>
/// Test context object that holds references to game managers and generators.
/// Inherits from MonoBehaviour to enable proper cleanup in test TearDown.
/// </summary>
public class GameModeContext : MonoBehaviour
{
    public GameStateManager stateManager;
    public ModeController modeController;
    public PuzzleGenerator puzzleGenerator;
    public EconomyManager economy;
}
