using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using WordPuzzle.Puzzle;

/// <summary>
/// ACCEPTANCE 5C — Generation quality.
/// Loads the real word_library.json + common_words.json from Resources, builds a
/// WordGraph, and exercises GenerateRandomPuzzleOfLength across lengths 3/4/5 N times.
/// Every generated puzzle MUST:
///   - use start/end words that are BOTH in the common subset (core 5C acceptance);
///   - have a solution of length >= 2 with optimalSteps == solution.Length - 1;
///   - be a valid one-letter-edit ladder (consecutive words differ by exactly one letter,
///     same length);
///   - pass ValidatePuzzle(startWord, endWord).
/// The all-common fallback ladder (cat-cot-cog-dog) preserves the start/end-in-common
/// invariant even when generation bottoms out.
/// </summary>
[TestFixture]
public class GenerationQualityTests
{
    private PuzzleGenerator generator;
    private WordGraph wordGraph;
    private HashSet<string> commonSet;

    [System.Serializable]
    private class WordListWrapper
    {
        public string[] words;
    }

    private static string[] LoadWords(string resourcePath)
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        Assert.IsNotNull(asset, $"Missing Resources asset: {resourcePath}");
        var wrapper = JsonUtility.FromJson<WordListWrapper>(asset.text);
        Assert.IsNotNull(wrapper, $"Failed to parse {resourcePath}");
        Assert.IsNotNull(wrapper.words, $"{resourcePath} has no 'words' array");
        return wrapper.words;
    }

    [SetUp]
    public void Setup()
    {
        wordGraph = new WordGraph();

        // Full library — every playable word.
        foreach (var w in LoadWords("Data/word_library"))
            wordGraph.AddWord(w);

        // Common subset — used both as the generation filter AND added to the graph
        // (some common words might not appear in the library otherwise).
        commonSet = new HashSet<string>();
        foreach (var w in LoadWords("Data/common_words"))
        {
            string lower = w.ToLower();
            commonSet.Add(lower);
            wordGraph.AddWord(lower);
        }

        wordGraph.BuildAdjacencies();

        generator = new PuzzleGenerator(wordGraph);
        generator.SetCommonWords(commonSet);
    }

    [Test]
    public void GenerateRandomPuzzleOfLength_AllInvariantsHold_Over200Puzzles()
    {
        const int N = 200;
        int[] lengths = { 3, 4, 5 };
        var failures = new List<string>();

        for (int i = 0; i < N; i++)
        {
            int len = lengths[i % lengths.Length];
            PuzzleDefinition puzzle = generator.GenerateRandomPuzzleOfLength(len);

            if (puzzle == null)
            {
                failures.Add($"[{i}] len={len}: generator returned null");
                continue;
            }

            string start = puzzle.startWord;
            string end = puzzle.endWord;
            string[] sol = puzzle.solution;

            // Invariant 1: start AND end are both in the common subset.
            if (!commonSet.Contains(start))
                failures.Add($"[{i}] len={len}: startWord '{start}' not in common set");
            if (!commonSet.Contains(end))
                failures.Add($"[{i}] len={len}: endWord '{end}' not in common set");

            // Invariant 2: solution length and optimalSteps consistency.
            if (sol == null || sol.Length < 2)
            {
                failures.Add($"[{i}] len={len}: solution length < 2 (start={start}, end={end})");
                continue; // remaining checks need a usable solution array
            }
            if (puzzle.optimalSteps != sol.Length - 1)
                failures.Add($"[{i}] len={len}: optimalSteps={puzzle.optimalSteps} != solution.Length-1={sol.Length - 1}");

            // Solution endpoints must match the declared start/end.
            if (sol[0] != start)
                failures.Add($"[{i}] len={len}: solution[0]='{sol[0]}' != startWord '{start}'");
            if (sol[sol.Length - 1] != end)
                failures.Add($"[{i}] len={len}: solution[last]='{sol[sol.Length - 1]}' != endWord '{end}'");

            // Invariant 3: every consecutive pair differs by exactly one letter, same length.
            for (int s = 0; s < sol.Length - 1; s++)
            {
                if (!DiffersByExactlyOneLetter(sol[s], sol[s + 1]))
                {
                    failures.Add($"[{i}] len={len}: '{sol[s]}' -> '{sol[s + 1]}' is not a single-letter edit");
                }
            }

            // Invariant 4: the puzzle is solvable per the generator's own validator.
            if (!generator.ValidatePuzzle(start, end))
                failures.Add($"[{i}] len={len}: ValidatePuzzle('{start}','{end}') returned false");
        }

        Assert.IsEmpty(failures,
            $"{failures.Count} of {N} generated puzzles violated a 5C invariant:\n" +
            string.Join("\n", failures.GetRange(0, System.Math.Min(failures.Count, 25))));
    }

    private static bool DiffersByExactlyOneLetter(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i] && ++diff > 1)
                return false;
        }
        return diff == 1;
    }
}
