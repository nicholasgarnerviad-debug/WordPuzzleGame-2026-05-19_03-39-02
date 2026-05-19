using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    private PuzzleGenerator puzzleGenerator;
    private PuzzleData currentPuzzle;
    private List<string> currentUserWords = new List<string>();
    private int currentScore = 0;
    private float gameStartTime = 0f;

    public delegate void OnPuzzleCompleted(int score);
    public delegate void OnGameOver();
    public event OnPuzzleCompleted PuzzleCompleted;
    public event OnGameOver GameOver;

    private void Start()
    {
        // Initialize with default word list (will be loaded from JSON later)
        var defaultWords = new List<string>
        {
            "apple", "apply", "about", "brave", "break", "broad"
        };
        puzzleGenerator = new PuzzleGenerator(defaultWords);
    }

    public void GenerateNewPuzzle(int difficulty = 1)
    {
        currentPuzzle = puzzleGenerator.GeneratePuzzle(difficulty);
        currentUserWords.Clear();
        currentScore = 0;
        gameStartTime = Time.time;

        Logger.Log($"Generated puzzle with {currentPuzzle.words.Count} words");
    }

    public bool SubmitWord(string word)
    {
        if (!puzzleGenerator.ValidateWord(word))
        {
            Logger.LogWarning($"Invalid word: {word}");
            return false;
        }

        if (currentUserWords.Contains(word))
        {
            Logger.LogWarning($"Word already found: {word}");
            return false;
        }

        currentUserWords.Add(word);
        currentScore += word.Length; // Score = letter count

        Logger.Log($"Word accepted: {word} (+{word.Length} points)");

        // Check if puzzle complete
        if (IsCurrentPuzzleComplete())
        {
            CompletePuzzle();
        }

        return true;
    }

    public bool IsCurrentPuzzleComplete()
    {
        if (currentPuzzle == null) return false;

        foreach (var word in currentPuzzle.words)
        {
            if (!currentUserWords.Contains(word))
                return false;
        }
        return true;
    }

    private void CompletePuzzle()
    {
        Logger.Log($"Puzzle completed! Score: {currentScore}");
        PuzzleCompleted?.Invoke(currentScore);
    }

    public PuzzleData GetCurrentPuzzle() => currentPuzzle;
    public List<string> GetCurrentUserWords() => currentUserWords;
    public int GetCurrentScore() => currentScore;
    public float GetGameElapsedTime() => Time.time - gameStartTime;
}
