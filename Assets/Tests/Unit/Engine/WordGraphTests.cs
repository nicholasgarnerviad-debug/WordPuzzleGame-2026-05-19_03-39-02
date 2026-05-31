using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using WordPuzzle.Puzzle;

[TestFixture]
public class WordGraphTests
{
    private WordGraph graph;

    [SetUp]
    public void Setup()
    {
        graph = new WordGraph();

        // Build small test graph
        graph.AddWord("cat");
        graph.AddWord("bat");
        graph.AddWord("hat");
        graph.AddWord("mat");
        graph.AddWord("map");

        graph.BuildAdjacencies();
    }

    [Test]
    public void GetShortestPath_ValidPath_ReturnsCorrectSequence()
    {
        var path = graph.GetShortestPath("cat", "map");

        Assert.Greater(path.Count, 0);
        Assert.AreEqual("cat", path[0]);
        Assert.AreEqual("map", path[path.Count - 1]);
    }

    [Test]
    public void GetDistance_ValidWords_ReturnsCorrectDistance()
    {
        int distance = graph.GetDistance("cat", "bat");
        Assert.AreEqual(1, distance);
    }

    [Test]
    public void CanSolve_ValidPath_ReturnsTrue()
    {
        bool canSolve = graph.CanSolve("cat", "map");
        Assert.IsTrue(canSolve);
    }

    // ----------------------------------------------------------------------
    // TASK 1 — Wildcard bucketing oracle test.
    //   Builds two graphs from the same sample, one using the production
    //   wildcard-bucket BuildAdjacencies, the other using a brute-force
    //   nested-loop oracle. Adjacency sets must be identical per word.
    // ----------------------------------------------------------------------
    [Test]
    public void BuildAdjacencies_WildcardBuckets_MatchBruteForceOracle()
    {
        var sample = SampleWords();

        var wildcardGraph = new WordGraph();
        foreach (var w in sample) wildcardGraph.AddWord(w);
        wildcardGraph.BuildAdjacencies();

        var oracle = BuildOracleAdjacency(sample);

        foreach (var w in sample)
        {
            var actual = NeighborsOf(wildcardGraph, w);
            oracle.TryGetValue(w, out var expected);
            expected = expected ?? new HashSet<string>();

            Assert.IsFalse(actual.Contains(w), $"word '{w}' is its own neighbor");
            CollectionAssert.AreEquivalent(expected, actual,
                $"Adjacency mismatch for word '{w}'");
        }
    }

    [Test]
    public void BuildAdjacencies_FullLibrary_CompletesUnder200ms()
    {
        var asset = Resources.Load<TextAsset>("Data/word_library");
        if (asset == null)
        {
            Assert.Ignore("word_library asset not available at test time; skipping timing assertion.");
            return;
        }

        var libraryGraph = new WordGraph();
        var json = asset.text;
        foreach (var w in ExtractWords(json))
        {
            libraryGraph.AddWord(w);
        }

        var sw = Stopwatch.StartNew();
        libraryGraph.BuildAdjacencies();
        sw.Stop();

        UnityEngine.Debug.Log($"[WordGraphTests] BuildAdjacencies on full library: {sw.ElapsedMilliseconds}ms");
        Assert.Less(sw.ElapsedMilliseconds, 200,
            $"BuildAdjacencies took {sw.ElapsedMilliseconds}ms; target < 200ms.");
    }

    // ---------- helpers ----------

    private static string[] SampleWords()
    {
        // Mixed-length sample (3 and 4 letter) — covers same-length-only adjacency
        // and ensures cross-length words never bucket together.
        return new[]
        {
            "cat","bat","hat","mat","map","rat","sat","fat","vat","oat",
            "car","bar","far","jar","mar","tar","ear","ear","tar",
            "cap","bap","gap","lap","map","nap","rap","sap","tap","zap",
            "dog","bog","cog","fog","hog","jog","log","tog",
            "tang","tank","tans","sang","band","bang","bank","bans",
            "cane","came","cake","care","case","cast","cart",
        };
    }

    private static Dictionary<string, HashSet<string>> BuildOracleAdjacency(IList<string> words)
    {
        // Brute-force i/j nested loop — the original BuildAdjacencies algorithm,
        // ported here as the verification oracle.
        var oracle = new Dictionary<string, HashSet<string>>();
        var unique = new HashSet<string>();
        foreach (var w in words)
        {
            var lower = w.ToLower();
            if (unique.Add(lower)) oracle[lower] = new HashSet<string>();
        }
        var list = new List<string>(unique);
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                if (WordOps.HaveOneLetterDifference(list[i], list[j]))
                {
                    oracle[list[i]].Add(list[j]);
                    oracle[list[j]].Add(list[i]);
                }
            }
        }
        return oracle;
    }

    private static HashSet<string> NeighborsOf(WordGraph g, string word)
    {
        // Probe neighbors via GetShortestPath: any word at distance==1 is a neighbor.
        var result = new HashSet<string>();
        // BfsTraversalCount is irrelevant here; this is verification.
        foreach (var other in g.GetWordsOfLength(word.Length))
        {
            if (other == word) continue;
            if (g.GetDistance(word, other) == 1) result.Add(other);
        }
        return result;
    }

    private static IEnumerable<string> ExtractWords(string json)
    {
        // Minimal JSON scrape for the {"words":[...]} shape. Avoids pulling in a
        // dependency: matches quoted lowercase tokens of length 3-7.
        var words = new List<string>();
        int i = 0;
        while (i < json.Length)
        {
            int q = json.IndexOf('"', i);
            if (q < 0) break;
            int q2 = json.IndexOf('"', q + 1);
            if (q2 < 0) break;
            string tok = json.Substring(q + 1, q2 - q - 1);
            if (tok.Length >= 3 && tok.Length <= 7)
            {
                bool ok = true;
                for (int k = 0; k < tok.Length; k++)
                {
                    char c = tok[k];
                    if (c < 'a' || c > 'z') { ok = false; break; }
                }
                if (ok) words.Add(tok);
            }
            i = q2 + 1;
        }
        return words;
    }
}
