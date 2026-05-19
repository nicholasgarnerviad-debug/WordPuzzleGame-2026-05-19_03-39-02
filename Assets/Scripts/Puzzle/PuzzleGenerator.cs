using UnityEngine;
using System.Collections.Generic;

public class PuzzleGenerator
{
    private WordGraph wordGraph;
    private List<string> allWords;
    private System.Random random;

    public PuzzleGenerator(List<string> words)
    {
        allWords = words;
        wordGraph = new WordGraph(words);
        random = new System.Random();
    }

    public PuzzleData GeneratePuzzle(int difficulty = 1)
    {
        PuzzleData puzzle = new PuzzleData();
        puzzle.difficulty = Mathf.Clamp(difficulty, 1, 5);

        // Select random words based on difficulty
        int wordCount = difficulty + 2; // Difficulty 1 = 3 words, etc.
        var selectedWords = SelectRandomWords(wordCount);
        puzzle.words = selectedWords;

        // Set center letter (most common first letter)
        if (selectedWords.Count > 0)
        {
            puzzle.centerLetter = selectedWords[0][0].ToString().ToUpper();
        }

        return puzzle;
    }

    private List<string> SelectRandomWords(int count)
    {
        var selected = new List<string>();
        var available = new List<string>(allWords);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = random.Next(available.Count);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }

        return selected;
    }

    public bool ValidateWord(string word)
    {
        return wordGraph.IsValidWord(word);
    }

    public string GetHint(List<string> words)
    {
        if (words.Count == 0) return "";

        int index = random.Next(words.Count);
        string hint = words[index];

        // Return first letter + dashes
        return hint[0] + new string('-', hint.Length - 1);
    }
}
