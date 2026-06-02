using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordPuzzle.Puzzle;

/// <summary>
/// TASK 17 — minimum-move floor. Locks in: no puzzle (generated OR curated) is solvable in
/// fewer than 2 moves, and generated puzzles meet the length curve — measured by TRUE
/// full-dictionary BFS shortest distance, not the generator's self-reported optimalSteps.
/// </summary>
[TestFixture]
public class MinMovesFloorTests
{
    private PuzzleGenerator generator;
    private WordGraph wordGraph;
    private Dictionary<int, Dictionary<string, List<string>>> adjByLen;

    [System.Serializable] private class WordListWrapper { public string[] words; }
    [System.Serializable] private class PuzzleEntry { public string startWord; public string endWord; public string[] solution; }
    [System.Serializable] private class TierEntry { public PuzzleEntry[] puzzles; }
    [System.Serializable] private class TierDefs { public TierEntry[] tiers; }
    [System.Serializable] private class DailyDefs { public PuzzleEntry[] puzzles; }

    private static string[] LoadWords(string path)
    {
        var asset = Resources.Load<TextAsset>(path);
        Assert.IsNotNull(asset, $"Missing {path}");
        return JsonUtility.FromJson<WordListWrapper>(asset.text).words;
    }

    [SetUp]
    public void Setup()
    {
        wordGraph = new WordGraph();
        var library = LoadWords("Data/word_library");
        foreach (var w in library) wordGraph.AddWord(w);
        var commonSet = new HashSet<string>();
        foreach (var w in LoadWords("Data/common_words")) { commonSet.Add(w.ToLower()); wordGraph.AddWord(w.ToLower()); }
        wordGraph.BuildAdjacencies();
        generator = new PuzzleGenerator(wordGraph);
        generator.SetCommonWords(commonSet);

        // Full-dictionary wildcard-bucket adjacency per length for independent true-shortest BFS.
        adjByLen = new Dictionary<int, Dictionary<string, List<string>>>();
        var byLen = new Dictionary<int, List<string>>();
        foreach (var w in library.Select(x => x.ToLower()).Distinct())
            (byLen.TryGetValue(w.Length, out var l) ? l : (byLen[w.Length] = new List<string>())).Add(w);
        foreach (var kv in byLen) adjByLen[kv.Key] = BuildAdjacency(kv.Value);
    }

    private static Dictionary<string, List<string>> BuildAdjacency(List<string> words)
    {
        var buckets = new Dictionary<string, List<string>>();
        foreach (var w in words)
            for (int i = 0; i < w.Length; i++)
            {
                string key = w.Substring(0, i) + "*" + w.Substring(i + 1);
                if (!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<string>();
                l.Add(w);
            }
        var adj = new Dictionary<string, List<string>>();
        foreach (var grp in buckets.Values)
            foreach (var a in grp)
            {
                if (!adj.TryGetValue(a, out var na)) adj[a] = na = new List<string>();
                foreach (var b in grp) if (a != b) na.Add(b);
            }
        return adj;
    }

    // True shortest edit distance over the full dictionary (the player can use ANY valid word).
    private int TrueShortest(string s, string e)
    {
        s = s.ToLower(); e = e.ToLower();
        if (s == e) return 0;
        if (!adjByLen.TryGetValue(s.Length, out var adj)) return int.MaxValue;
        var seen = new HashSet<string> { s };
        var q = new Queue<(string, int)>();
        q.Enqueue((s, 0));
        while (q.Count > 0)
        {
            var (cur, d) = q.Dequeue();
            if (d >= 12) continue;
            if (!adj.TryGetValue(cur, out var nbrs)) continue;
            foreach (var n in nbrs)
            {
                if (n == e) return d + 1;
                if (seen.Add(n)) q.Enqueue((n, d + 1));
            }
        }
        return int.MaxValue;
    }

    [Test]
    public void MinMovesForLength_FollowsCurve_AndNeverBelowTwo()
    {
        Assert.AreEqual(2, BalanceConfig.MinMovesForLength(3));
        Assert.AreEqual(2, BalanceConfig.MinMovesForLength(4));
        Assert.AreEqual(3, BalanceConfig.MinMovesForLength(5));
        Assert.AreEqual(3, BalanceConfig.MinMovesForLength(6));
        Assert.AreEqual(4, BalanceConfig.MinMovesForLength(7));
        for (int len = 2; len <= 9; len++)
            Assert.GreaterOrEqual(BalanceConfig.MinMovesForLength(len), 2,
                $"MinMovesForLength({len}) must never be below the absolute floor of 2.");
    }

    [Test]
    public void GeneratedPuzzles_MeetTrueShortestFloor_Over200()
    {
        int[] lengths = { 3, 4, 5 };
        var failures = new List<string>();
        for (int i = 0; i < 200; i++)
        {
            int len = lengths[i % lengths.Length];
            var p = generator.GenerateRandomPuzzleOfLength(len);
            Assert.IsNotNull(p, "generator returned null");
            int trueMoves = TrueShortest(p.startWord, p.endWord);
            int floor = BalanceConfig.MinMovesForLength(len);
            if (trueMoves < 2)
                failures.Add($"[{i}] len{len} {p.startWord}->{p.endWord}: TRUE {trueMoves} move(s) — ONE-MOVE PUZZLE");
            else if (trueMoves < floor)
                failures.Add($"[{i}] len{len} {p.startWord}->{p.endWord}: TRUE {trueMoves} < curve {floor}");
        }
        Assert.IsEmpty(failures,
            $"{failures.Count}/200 generated puzzles below the floor:\n" + string.Join("\n", failures.Take(20)));
    }

    [Test]
    public void CuratedPuzzles_NoSubTwoMove_TierAndDaily()
    {
        var failures = new List<string>();
        var tiers = JsonUtility.FromJson<TierDefs>(Resources.Load<TextAsset>("Data/tier_definitions").text);
        foreach (var t in tiers.tiers)
            foreach (var p in t.puzzles)
                if (TrueShortest(p.startWord, p.endWord) < 2)
                    failures.Add($"tier {p.startWord}->{p.endWord} solvable in <2 moves");

        var daily = JsonUtility.FromJson<DailyDefs>(Resources.Load<TextAsset>("Data/daily_puzzles").text);
        foreach (var p in daily.puzzles)
            if (TrueShortest(p.startWord, p.endWord) < 2)
                failures.Add($"daily {p.startWord}->{p.endWord} solvable in <2 moves");

        Assert.IsEmpty(failures,
            $"{failures.Count} curated puzzles below the 2-move floor:\n" + string.Join("\n", failures.Take(20)));
    }
}
