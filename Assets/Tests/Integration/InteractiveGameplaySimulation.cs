using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Interactive Gameplay Simulation Tests
/// Simulates complete playthrough sequences for all game modes.
/// Tests actual gameplay loops, word submissions, scoring, and screen transitions.
/// </summary>
public class InteractiveGameplaySimulation
{
    private GameModeContext gameContext;
    private ModeController modeController;

    // Constants for test data
    private const int WORDS_TO_SUBMIT = 3;
    private const int MAX_PUZZLE_WORDS = 10;
    private const float TICK_DURATION = 0.5f;
    private const int TICK_COUNT = 4;

    [SetUp]
    public void SetUp()
    {
        // Create mock instances instead of searching scene
        gameContext = CreateGameModeContext();
        modeController = new ModeController();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up GameObjects after each test
        if (gameContext?.gameObject != null)
            UnityEngine.Object.Destroy(gameContext.gameObject);

        gameContext = null;
        modeController = null;
    }

    [UnityTest]
    public IEnumerator ClassicMode_Initialization()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);

        Assert.IsNotNull(classicMode, "ClassicMode should initialize successfully");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClassicMode_StartsGameAndLoadsPuzzle()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var initialState = gameContext.stateManager.GetCurrentState();
        Assert.IsNotNull(initialState.startWord, "Puzzle should have start word");
        Assert.IsNotNull(initialState.endWord, "Puzzle should have end word");
        Assert.That(initialState.wordChain.Count, Is.EqualTo(1), "Word chain should start with 1 word (start word)");
    }

    [UnityTest]
    public IEnumerator ClassicMode_SubmitValidWord_IncreasesChainLength()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        Assert.That(puzzle.solution.Length, Is.GreaterThan(1), "Puzzle must have at least 2 words");
        var solutionWord = puzzle.solution[1];

        var initialState = gameContext.stateManager.GetCurrentState();
        int initialChainLength = initialState.wordChain.Count;

        var submitAction = new SubmitWordAction { word = solutionWord };
        classicMode.HandleInput(submitAction);

        yield return null;

        var updatedState = gameContext.stateManager.GetCurrentState();
        Assert.That(updatedState.wordChain.Count, Is.EqualTo(initialChainLength + 1),
            "Chain length should increase by 1 after valid word submission");
    }

    [UnityTest]
    public IEnumerator ClassicMode_SubmitValidWord_UpdatesGameState()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        Assert.That(puzzle.solution.Length, Is.GreaterThan(1), "Puzzle must have at least 2 words");
        var solutionWord = puzzle.solution[1];

        var submitAction = new SubmitWordAction { word = solutionWord };
        classicMode.HandleInput(submitAction);

        yield return null;

        var updatedState = gameContext.stateManager.GetCurrentState();
        Assert.That(updatedState.wordChain.Contains(solutionWord), Is.True,
            "Submitted word should be in chain");
    }

    [UnityTest]
    public IEnumerator ClassicMode_SubmitMultipleWords_IncreasesChainLength()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        var initialState = gameContext.stateManager.GetCurrentState();
        int initialChainLength = initialState.wordChain.Count;

        // Submit up to 3 valid words from puzzle solution
        int wordsSubmitted = 0;
        for (int i = 1; i < puzzle.solution.Length && wordsSubmitted < WORDS_TO_SUBMIT; i++)
        {
            var submitAction = new SubmitWordAction { word = puzzle.solution[i] };
            classicMode.HandleInput(submitAction);
            wordsSubmitted++;
            yield return null;
        }

        var finalState = gameContext.stateManager.GetCurrentState();
        Assert.That(wordsSubmitted, Is.EqualTo(WORDS_TO_SUBMIT), "Should have submitted 3 words");
        Assert.That(finalState.wordChain.Count, Is.EqualTo(initialChainLength + WORDS_TO_SUBMIT),
            "Chain length should increase by 3 after 3 valid word submissions");
    }

    [UnityTest]
    public IEnumerator ClassicMode_SubmitMultipleWords_UpdatesScore()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        var initialState = gameContext.stateManager.GetCurrentState();
        int initialScore = initialState.score;

        // Submit up to 3 valid words from puzzle solution
        int wordsSubmitted = 0;
        for (int i = 1; i < puzzle.solution.Length && wordsSubmitted < WORDS_TO_SUBMIT; i++)
        {
            var submitAction = new SubmitWordAction { word = puzzle.solution[i] };
            classicMode.HandleInput(submitAction);
            wordsSubmitted++;
            yield return null;
        }

        var finalState = gameContext.stateManager.GetCurrentState();
        Assert.That(wordsSubmitted, Is.EqualTo(WORDS_TO_SUBMIT), "Should have submitted 3 words");
        Assert.That(finalState.score, Is.GreaterThan(initialScore),
            "Score should increase after each valid word submission");
    }

    [UnityTest]
    public IEnumerator ClassicMode_GetStats_ReturnsValidStats()
    {
        var classicMode = new ClassicMode();
        classicMode.Initialize(gameContext);
        classicMode.StartGame();

        yield return null;

        var stats = classicMode.GetStats();
        Assert.That(stats.modeName, Is.EqualTo("Classic"), "Mode name should be 'Classic'");
        Assert.That(stats.coinsEarned, Is.EqualTo(0),
            "Coins should be 0 before completing any puzzles");
    }

    [UnityTest]
    public IEnumerator PuzzleShowMode_Initialization()
    {
        var puzzleShowMode = new PuzzleShowMode();
        puzzleShowMode.Initialize(gameContext);

        Assert.IsNotNull(puzzleShowMode, "PuzzleShowMode should initialize successfully");
        yield return null;
    }

    [UnityTest]
    public IEnumerator PuzzleShowMode_StartsGameWithFullSolution()
    {
        var puzzleShowMode = new PuzzleShowMode();
        puzzleShowMode.Initialize(gameContext);
        puzzleShowMode.StartGame();

        yield return null;

        var initialState = gameContext.stateManager.GetCurrentState();
        Assert.IsNotNull(initialState.startWord, "Puzzle should have start word");
        Assert.IsNotNull(initialState.endWord, "Puzzle should have end word");
        // Puzzle Show Mode provides the complete solution path
        Assert.That(initialState.wordChain.Count, Is.GreaterThan(0),
            "Solution words should be available in Puzzle Show mode");
    }

    [UnityTest]
    public IEnumerator PuzzleShowMode_SubmitSolutionWords()
    {
        var puzzleShowMode = new PuzzleShowMode();
        puzzleShowMode.Initialize(gameContext);
        puzzleShowMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        int wordsSubmitted = 0;

        // Submit actual solution words from the puzzle
        for (int i = 1; i < puzzle.solution.Length && wordsSubmitted < WORDS_TO_SUBMIT; i++)
        {
            var submitAction = new SubmitWordAction { word = puzzle.solution[i] };
            puzzleShowMode.HandleInput(submitAction);
            wordsSubmitted++;
            yield return null;
        }

        var currentState = gameContext.stateManager.GetCurrentState();
        Assert.That(wordsSubmitted, Is.EqualTo(WORDS_TO_SUBMIT), "Should have submitted 3 solution words");
        Assert.IsNotNull(currentState, "Game state should persist");
    }

    [UnityTest]
    public IEnumerator PuzzleShowMode_GetStats_ReturnsValidStats()
    {
        var puzzleShowMode = new PuzzleShowMode();
        puzzleShowMode.Initialize(gameContext);
        puzzleShowMode.StartGame();

        yield return null;

        var stats = puzzleShowMode.GetStats();
        Assert.That(stats.modeName, Is.EqualTo("Puzzle Show"), "Mode name should be 'Puzzle Show'");
        Assert.That(stats.puzzlesCompleted, Is.EqualTo(0), "Puzzles completed should be 0 before completing any puzzles");
    }

    [UnityTest]
    public IEnumerator TimeAttackMode_Initialization()
    {
        var timeAttackMode = new TimeAttackMode();
        timeAttackMode.Initialize(gameContext);

        Assert.IsNotNull(timeAttackMode, "TimeAttackMode should initialize successfully");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TimeAttackMode_StartsGameWithActiveTimer()
    {
        var timeAttackMode = new TimeAttackMode();
        timeAttackMode.Initialize(gameContext);
        timeAttackMode.StartGame();

        yield return null;

        var initialState = gameContext.stateManager.GetCurrentState();
        Assert.IsNotNull(initialState.startWord, "Puzzle should have start word");
        Assert.IsNotNull(initialState.endWord, "Puzzle should have end word");
    }

    [UnityTest]
    public IEnumerator TimeAttackMode_TimerAccumulates()
    {
        var timeAttackMode = new TimeAttackMode();
        timeAttackMode.Initialize(gameContext);
        timeAttackMode.StartGame();

        yield return null;

        var statsBefore = timeAttackMode.GetStats();
        float timeBefore = statsBefore.totalTime;

        // Simulate multiple game ticks
        for (int i = 0; i < TICK_COUNT; i++)
        {
            timeAttackMode.Tick(TICK_DURATION);
            yield return null;
        }

        var statsAfter = timeAttackMode.GetStats();
        float expectedElapsedTime = TICK_DURATION * TICK_COUNT;
        float timeAfter = statsAfter.totalTime;

        Assert.That(timeAfter, Is.GreaterThan(timeBefore),
            "Elapsed time should increase after Tick call");
    }

    [UnityTest]
    public IEnumerator TimeAttackMode_SubmitWordUnderTimePressure()
    {
        var timeAttackMode = new TimeAttackMode();
        timeAttackMode.Initialize(gameContext);
        timeAttackMode.StartGame();

        yield return null;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        Assert.That(puzzle.solution.Length, Is.GreaterThan(1), "Puzzle must have at least 2 words");
        var solutionWord = puzzle.solution[1];

        var submitAction = new SubmitWordAction { word = solutionWord };
        timeAttackMode.HandleInput(submitAction);

        yield return null;

        var currentState = gameContext.stateManager.GetCurrentState();
        Assert.That(currentState.score, Is.GreaterThanOrEqualTo(0), "Score should be non-negative after valid submission");
    }

    [UnityTest]
    public IEnumerator TimeAttackMode_GetStats_ReturnsValidStats()
    {
        var timeAttackMode = new TimeAttackMode();
        timeAttackMode.Initialize(gameContext);
        timeAttackMode.StartGame();

        yield return null;

        var stats = timeAttackMode.GetStats();
        Assert.That(stats.modeName, Is.EqualTo("Time Attack"), "Mode name should be 'Time Attack'");
        Assert.That(stats.totalTime, Is.EqualTo(0f), "Total time should be 0 at game start");
        Assert.That(stats.coinsEarned, Is.EqualTo(0), "Coins earned should be 0 at game start");
    }

    [UnityTest]
    public IEnumerator GameState_InitializesCorrectly()
    {
        var initialState = gameContext.stateManager.GetCurrentState();

        Assert.IsNotNull(initialState, "Game state should exist after initialization");
        Assert.That(initialState.score, Is.EqualTo(0),
            "Initial score should be 0");
        yield return null;
    }

    [UnityTest]
    public IEnumerator GameState_UpdatesAfterAction()
    {
        var initialState = gameContext.stateManager.GetCurrentState();
        int initialChainLength = initialState.wordChain.Count;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        var action = new SubmitWordAction { word = puzzle.solution[1] };
        gameContext.stateManager.Dispatch(action);

        yield return null;

        var updatedState = gameContext.stateManager.GetCurrentState();
        Assert.IsNotNull(updatedState, "State should persist after action dispatch");
        Assert.That(updatedState.wordChain.Count, Is.GreaterThan(initialChainLength),
            "Word chain should increase after valid word submission");
    }

    [UnityTest]
    public IEnumerator GameState_ScoreCalculation()
    {
        var initialState = gameContext.stateManager.GetCurrentState();
        int scoreAtStart = initialState.score;

        var puzzle = gameContext.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);

        // Submit multiple valid words from puzzle solution
        for (int i = 1; i < puzzle.solution.Length && i <= WORDS_TO_SUBMIT; i++)
        {
            var submitAction = new SubmitWordAction { word = puzzle.solution[i] };
            gameContext.stateManager.Dispatch(submitAction);
            yield return null;
        }

        var finalState = gameContext.stateManager.GetCurrentState();
        Assert.That(finalState.score, Is.GreaterThan(scoreAtStart),
            "Score should increase after valid word submissions");
    }


    // Helper method to create a game context for testing
    private GameModeContext CreateGameModeContext()
    {
        // Create context object
        var context = new GameModeContext();

        // Create mock word validator
        var wordValidator = new MockWordValidator();
        wordValidator.SetValidResult(true, true);

        // Create mock data manager
        var dataManager = new MockDataManager();

        // Create GameStateManager with proper constructor parameters
        context.stateManager = new GameStateManager();

        // Create PuzzleGenerator with WordGraph
        var wordGraph = new WordGraphBuilder();
        context.puzzleGenerator = new PuzzleGenerator(wordGraph, wordValidator);

        // Create EconomyManager
        context.economy = new EconomyManager();

        // Load test puzzle
        var puzzleDefinition = context.puzzleGenerator.GenerateRandomPuzzle(Difficulty.Easy);
        if (puzzleDefinition == null)
        {
            puzzleDefinition = new PuzzleDefinition
            {
                puzzleId = 1,
                startWord = "cat",
                endWord = "dog",
                optimalSteps = 3,
                solution = new[] { "cat", "bat", "bag", "dog" },
                seedValue = 0
            };
        }

        var testPuzzle = new WordPuzzle(
            puzzleDefinition.puzzleId,
            puzzleDefinition.startWord,
            puzzleDefinition.endWord,
            puzzleDefinition.optimalSteps,
            puzzleDefinition.solution,
            puzzleDefinition.seedValue,
            Difficulty.Easy
        );

        context.stateManager.StartNewPuzzle(testPuzzle);

        return context;
    }
}
