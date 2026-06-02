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

    // ======================================================================
    // TASK 14 — Dictionary integrity guards (data-quality regression tests).
    // These read the real shipped JSON so a future cleanup/expansion pass
    // cannot silently re-introduce junk or delete a curated/long word.
    // ======================================================================

    // Known abbreviations/acronyms/initialisms that must never validate as words.
    private static readonly string[] JunkBlocklist =
        { "abc", "fbi", "gps", "ibm", "irs", "mph", "mpg", "rpm", "std", "qty", "cpu", "asst", "dept" };

    [System.Serializable] private class PuzzleEntry { public string startWord; public string endWord; public string[] solution; }
    [System.Serializable] private class TierEntry   { public PuzzleEntry[] puzzles; }
    [System.Serializable] private class TierDefs     { public TierEntry[] tiers; }
    [System.Serializable] private class DailyDefs    { public PuzzleEntry[] puzzles; }

    private static HashSet<string> LoadLibrarySet()
    {
        var set = new HashSet<string>();
        foreach (var w in LoadWords("Data/word_library")) set.Add(w.ToLower());
        return set;
    }

    private static T LoadJson<T>(string resourcePath) where T : class
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        Assert.IsNotNull(asset, $"Missing Resources asset: {resourcePath}");
        var parsed = JsonUtility.FromJson<T>(asset.text);
        Assert.IsNotNull(parsed, $"Failed to parse {resourcePath}");
        return parsed;
    }

    [Test]
    public void Dictionary_ContainsNoJunkAbbreviations()
    {
        var lib = LoadLibrarySet();
        var present = new List<string>();
        foreach (var j in JunkBlocklist)
            if (lib.Contains(j)) present.Add(j);
        Assert.IsEmpty(present,
            "Junk abbreviations/acronyms must not be in the dictionary: " + string.Join(", ", present));
    }

    [Test]
    public void Dictionary_ContainsEveryCuratedSolutionWord_HammingOneValid()
    {
        var lib = LoadLibrarySet();
        var failures = new List<string>();

        void Check(string kind, PuzzleEntry[] puzzles)
        {
            if (puzzles == null) return;
            foreach (var p in puzzles)
            {
                if (p.solution == null || p.solution.Length < 2) continue;
                for (int i = 0; i < p.solution.Length; i++)
                {
                    string w = p.solution[i].ToLower();
                    if (!lib.Contains(w))
                        failures.Add($"{kind}: curated word '{w}' missing from dictionary");
                    if (i > 0 && !DiffersByExactlyOneLetter(p.solution[i - 1].ToLower(), w))
                        failures.Add($"{kind}: '{p.solution[i - 1]}'->'{w}' not a single-letter edit");
                }
            }
        }

        var tiers = LoadJson<TierDefs>("Data/tier_definitions");
        int tierPuzzles = 0;
        if (tiers.tiers != null)
            foreach (var t in tiers.tiers) { Check("tier", t.puzzles); tierPuzzles += t.puzzles?.Length ?? 0; }
        var daily = LoadJson<DailyDefs>("Data/daily_puzzles");
        Check("daily", daily.puzzles);

        Assert.AreEqual(350, tierPuzzles, "Expected 350 curated tier puzzles (7 tiers x 50).");
        Assert.AreEqual(450, daily.puzzles.Length, "Expected 450 curated daily puzzles.");
        Assert.IsEmpty(failures,
            $"{failures.Count} curated solution word(s) broken by a dictionary edit:\n" +
            string.Join("\n", failures.GetRange(0, System.Math.Min(failures.Count, 25))));
    }

    [Test]
    public void Dictionary_HasMinimumLongWordCoverage()
    {
        var lib = LoadLibrarySet();
        int six = 0, seven = 0;
        foreach (var w in lib)
        {
            if (w.Length == 6) six++;
            else if (w.Length == 7) seven++;
        }
        Assert.GreaterOrEqual(six, 2000, $"6-letter coverage gutted: {six} (expected >= 2000).");
        Assert.GreaterOrEqual(seven, 2000, $"7-letter coverage gutted: {seven} (expected >= 2000).");
    }
}
