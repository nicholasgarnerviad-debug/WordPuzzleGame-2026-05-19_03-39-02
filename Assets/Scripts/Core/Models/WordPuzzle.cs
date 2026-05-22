using System.Collections.Generic;
using WordPuzzle.Puzzle;

public class WordPuzzle
{
    public int puzzleId;
    public string startWord;
    public string endWord;
    public int optimalSteps;           // Shortest solution length
    public string[] solution;          // Pre-computed solution path
    public int seedValue;              // For reproducibility
    public Difficulty difficulty;

    public WordPuzzle() { }

    public WordPuzzle(int id, string start, string end, int optimal,
                      string[] solutionPath, int seed, Difficulty diff)
    {
        puzzleId = id;
        startWord = start;
        endWord = end;
        optimalSteps = optimal;
        solution = solutionPath;
        seedValue = seed;
        difficulty = diff;
    }
}
