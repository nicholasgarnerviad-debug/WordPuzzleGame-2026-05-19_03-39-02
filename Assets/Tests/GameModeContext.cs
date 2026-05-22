using WordPuzzle.State;
using WordPuzzle.Modes;
using WordPuzzle.Puzzle;
using UnityEngine;

/// <summary>
/// Test context object that holds references to game managers and generators.
/// Used by integration tests to manage game state during test execution.
/// </summary>
public class GameModeContext
{
    public GameStateManager stateManager;
    public ModeController modeController;
    public PuzzleGenerator puzzleGenerator;
    public EconomyManager economy;
}
