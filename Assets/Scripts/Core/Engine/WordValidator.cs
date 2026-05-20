using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class WordValidator : IWordValidator
{
    private WordGraph wordGraph;
    private string previousWord;
    private string targetWord;
    private List<string> currentChain;

    public WordValidator(WordGraph wordGraph)
    {
        this.wordGraph = wordGraph;
    }

    public void Initialize(string startWord, string endWord, string[] currentWordChain)
    {
        previousWord = currentWordChain.Length > 0
            ? currentWordChain[currentWordChain.Length - 1]
            : startWord;
        targetWord = endWord;
        currentChain = new List<string>(currentWordChain);
    }

    public ValidationResult ValidateWord(string word)
    {
        var sw = Stopwatch.StartNew();
        word = word.ToLower();

        // Check if word exists
        if (!wordGraph.IsValidWord(word))
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 1)
            {
                Debug.LogWarning($"[Performance] ValidateWord (invalid dict check) took {sw.ElapsedMilliseconds}ms - exceeds 1ms target");
            }
            return new ValidationResult(
                valid: false,
                msg: "Word not in dictionary",
                nextStep: false,
                progress: false,
                distStart: -1,
                distEnd: -1
            );
        }

        // Check if already in chain
        if (currentChain.Contains(word))
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 1)
            {
                Debug.LogWarning($"[Performance] ValidateWord (chain check) took {sw.ElapsedMilliseconds}ms - exceeds 1ms target");
            }
            return new ValidationResult(
                valid: false,
                msg: "Word already used",
                nextStep: false,
                progress: false,
                distStart: -1,
                distEnd: -1
            );
        }

        // Check one letter difference from previous word
        if (!HaveOneLetterDifference(previousWord, word))
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 1)
            {
                Debug.LogWarning($"[Performance] ValidateWord (letter diff check) took {sw.ElapsedMilliseconds}ms - exceeds 1ms target");
            }
            return new ValidationResult(
                valid: false,
                msg: "Must change exactly one letter",
                nextStep: false,
                progress: false,
                distStart: -1,
                distEnd: -1
            );
        }

        int distStart = wordGraph.GetDistance(word, previousWord);
        int distEnd = wordGraph.GetDistance(word, targetWord);
        bool isProgress = distEnd < wordGraph.GetDistance(previousWord, targetWord);

        sw.Stop();
        if (sw.ElapsedMilliseconds > 1)
        {
            Debug.LogWarning($"[Performance] ValidateWord (full validation) took {sw.ElapsedMilliseconds}ms - exceeds 1ms target");
        }
        else
        {
            Debug.Log($"[Performance] ValidateWord completed in {sw.ElapsedMilliseconds}ms for word '{word}'");
        }

        return new ValidationResult(
            valid: true,
            msg: "Valid word",
            nextStep: true,
            progress: isProgress,
            distStart: distStart,
            distEnd: distEnd
        );
    }

    public bool IsValidNextWord(string word, string previousWord)
    {
        return HaveOneLetterDifference(previousWord, word.ToLower());
    }

    private bool HaveOneLetterDifference(string word1, string word2)
    {
        if (word1.Length != word2.Length)
            return false;

        int differences = 0;
        for (int i = 0; i < word1.Length; i++)
        {
            if (word1[i] != word2[i])
                differences++;
            if (differences > 1)
                return false;
        }

        return differences == 1;
    }
}

public interface IWordValidator
{
    void Initialize(string startWord, string endWord, string[] currentWordChain);
    ValidationResult ValidateWord(string word);
    bool IsValidNextWord(string word, string previousWord);
}
