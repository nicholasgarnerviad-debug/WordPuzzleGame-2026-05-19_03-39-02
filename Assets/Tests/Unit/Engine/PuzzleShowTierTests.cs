using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordPuzzle.Modes;

/// <summary>
/// TASK 15 (expanded to 7×100) — Puzzle Show tier-data integrity + completion-state mapping guards.
/// Loads the real tier_definitions.json and asserts the 7×100 structure, ladder
/// validity, in-dictionary membership, no intra-tier duplicates, a non-decreasing
/// per-tier minimum step count, AND the multi-route guarantee (every tier puzzle has
/// ≥ 2 distinct optimal-length routes through the full dictionary graph, with the
/// stored solution length equal to that true shortest distance). Also pins the
/// card-state resolver (PuzzleShowMode.ResolveState) used by the Puzzle Library colouring.
/// </summary>
[TestFixture]
public class PuzzleShowTierTests
{
    [System.Serializable] private class PuzzleEntry { public int puzzleId; public string startWord; public string endWord; public int optimalSteps; public string[] solution; }
    [System.Serializable] private class TierEntry  { public int tierId; public bool isUnlocked; public PuzzleEntry[] puzzles; }
    [System.Serializable] private class TierDefs    { public TierEntry[] tiers; }
    [System.Serializable] private class WordListWrapper { public string[] words; }

    private static TierDefs LoadTiers()
    {
        var asset = Resources.Load<TextAsset>("Data/tier_definitions");
        Assert.IsNotNull(asset, "tier_definitions.json missing");
        var defs = JsonUtility.FromJson<TierDefs>(asset.text);
        Assert.IsNotNull(defs?.tiers, "tier_definitions parse failed");
        return defs;
    }

    private static HashSet<string> LoadDictionary()
    {
        var asset = Resources.Load<TextAsset>("Data/word_library");
        Assert.IsNotNull(asset, "word_library.json missing");
        var w = JsonUtility.FromJson<WordListWrapper>(asset.text);
        return new HashSet<string>(w.words.Select(x => x.ToLower()));
    }

    private static bool Hamming1(string a, string b)
        => a.Length == b.Length && a.Zip(b, (x, y) => x != y).Count(d => d) == 1;

    // Hamming-1 adjacency over a word set (wildcard bucketing), mirrors WordGraph / the Python tool.
    private static Dictionary<string, List<string>> BuildGraph(IEnumerable<string> words)
    {
        var set = new HashSet<string>(words);
        var buckets = new Dictionary<string, List<string>>();
        foreach (var w in set)
            for (int i = 0; i < w.Length; i++)
            {
                string key = w.Substring(0, i) + "*" + w.Substring(i + 1);
                if (!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<string>();
                l.Add(w);
            }
        var adj = new Dictionary<string, HashSet<string>>();
        foreach (var ws in buckets.Values)
            foreach (var a in ws)
            {
                if (!adj.TryGetValue(a, out var s)) adj[a] = s = new HashSet<string>();
                foreach (var c in ws) if (a != c) s.Add(c);
            }
        return adj.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
    }

    // Distinct shortest-path count + true shortest distance via BFS DAG-DP (clamped).
    private static (int routes, int dist) CountOptimalRoutes(
        Dictionary<string, List<string>> adj, string s, string e, int cap = 256)
    {
        if (s == e) return (1, 0);
        var dist = new Dictionary<string, int> { [s] = 0 };
        var np = new Dictionary<string, int> { [s] = 1 };
        var q = new Queue<string>(); q.Enqueue(s);
        int target = -1;
        while (q.Count > 0)
        {
            string u = q.Dequeue();
            int du = dist[u];
            if (target >= 0 && du >= target) continue;
            if (!adj.TryGetValue(u, out var nbrs)) continue;
            foreach (var v in nbrs)
            {
                if (!dist.ContainsKey(v))
                {
                    dist[v] = du + 1; np[v] = np[u];
                    if (v == e) target = dist[v];
                    q.Enqueue(v);
                }
                else if (dist[v] == du + 1)
                    np[v] = System.Math.Min(cap, np[v] + np[u]);
            }
        }
        return (np.TryGetValue(e, out var n) ? n : 0, dist.TryGetValue(e, out var d) ? d : -1);
    }

    [Test]
    public void Tiers_Are7x100_AllValidSolvableLadders()
    {
        var defs = LoadTiers();
        var dict = LoadDictionary();
        var failures = new List<string>();

        Assert.AreEqual(7, defs.tiers.Length, "Expected 7 tiers.");

        foreach (var tier in defs.tiers)
        {
            Assert.AreEqual(100, tier.puzzles.Length, $"Tier {tier.tierId} must have 100 puzzles.");
            var pairs = new HashSet<string>();
            foreach (var p in tier.puzzles)
            {
                string key = p.startWord + "->" + p.endWord;
                if (!pairs.Add(key))
                    failures.Add($"Tier {tier.tierId}: duplicate puzzle {key}");

                if (p.solution == null || p.solution.Length < 2)
                { failures.Add($"Tier {tier.tierId} #{p.puzzleId}: solution < 2"); continue; }

                if (p.solution[0].ToLower() != p.startWord.ToLower())
                    failures.Add($"Tier {tier.tierId} #{p.puzzleId}: solution[0] != startWord");
                if (p.solution[^1].ToLower() != p.endWord.ToLower())
                    failures.Add($"Tier {tier.tierId} #{p.puzzleId}: solution[last] != endWord");
                if (p.optimalSteps != p.solution.Length - 1)
                    failures.Add($"Tier {tier.tierId} #{p.puzzleId}: optimalSteps != solution.Length-1");

                for (int i = 0; i < p.solution.Length; i++)
                {
                    string w = p.solution[i].ToLower();
                    if (!dict.Contains(w))
                        failures.Add($"Tier {tier.tierId} #{p.puzzleId}: '{w}' not in dictionary");
                    if (i > 0 && !Hamming1(p.solution[i - 1].ToLower(), w))
                        failures.Add($"Tier {tier.tierId} #{p.puzzleId}: '{p.solution[i-1]}'->'{w}' not single-letter");
                }
            }
        }
        Assert.IsEmpty(failures, string.Join("\n", failures.Take(25)));
    }

    [Test]
    public void Tiers_EveryPuzzle_HasAtLeastTwoOptimalRoutes()
    {
        var defs = LoadTiers();
        var dict = LoadDictionary();
        // Full-dictionary Hamming-1 graphs, one per word length present in the tiers.
        var byLen = new Dictionary<int, Dictionary<string, List<string>>>();
        Dictionary<string, List<string>> GraphFor(int len)
        {
            if (!byLen.TryGetValue(len, out var g))
                byLen[len] = g = BuildGraph(dict.Where(w => w.Length == len));
            return g;
        }

        var failures = new List<string>();
        foreach (var tier in defs.tiers)
            foreach (var p in tier.puzzles)
            {
                string s = p.startWord.ToLower(), e = p.endWord.ToLower();
                var (routes, dist) = CountOptimalRoutes(GraphFor(s.Length), s, e);
                if (dist != p.optimalSteps)
                    failures.Add($"Tier {tier.tierId} #{p.puzzleId} ({s}->{e}): optimalSteps {p.optimalSteps} != true shortest {dist}");
                if (routes < 2)
                    failures.Add($"Tier {tier.tierId} #{p.puzzleId} ({s}->{e}): only {routes} optimal route(s), need >=2");
            }
        Assert.IsEmpty(failures,
            $"{failures.Count} tier puzzle(s) violate the >=2-optimal-route guarantee:\n" +
            string.Join("\n", failures.Take(25)));
    }

    [Test]
    public void Tiers_MinStepCount_IsNonDecreasing()
    {
        var defs = LoadTiers();
        var ordered = defs.tiers.OrderBy(t => t.tierId).ToList();
        int prevMin = 0;
        foreach (var t in ordered)
        {
            int min = t.puzzles.Min(p => p.optimalSteps);
            Assert.GreaterOrEqual(min, prevMin,
                $"Tier {t.tierId} min steps {min} < previous tier {prevMin} (difficulty must not regress).");
            prevMin = min;
        }
    }

    [Test]
    public void ResolveState_MapsProgressToCardStates()
    {
        var completed = new HashSet<int> { 1, 2 };
        var inProgress = new HashSet<int> { 3 };

        // Locked tier → everything Locked regardless of progress.
        Assert.AreEqual(PuzzleState.Locked, PuzzleShowMode.ResolveState(1, false, completed, inProgress));
        // Unlocked tier resolves by membership.
        Assert.AreEqual(PuzzleState.Completed,        PuzzleShowMode.ResolveState(1, true, completed, inProgress));
        Assert.AreEqual(PuzzleState.Completed,        PuzzleShowMode.ResolveState(2, true, completed, inProgress));
        Assert.AreEqual(PuzzleState.InProgress,       PuzzleShowMode.ResolveState(3, true, completed, inProgress));
        Assert.AreEqual(PuzzleState.UnlockedUnplayed, PuzzleShowMode.ResolveState(9, true, completed, inProgress));
    }

    [Test]
    public void ProgressiveUnlock_RisesPerTier_AndTier1Is10()
    {
        Assert.AreEqual(10, PuzzleShowMode.PuzzlesRequiredToAdvance(1), "Tier 1 gate must stay 10.");
        int prev = 0;
        for (int t = 1; t <= 7; t++)
        {
            int need = PuzzleShowMode.PuzzlesRequiredToAdvance(t);
            Assert.GreaterOrEqual(need, prev, "Unlock requirement must rise (or hold) per tier.");
            Assert.LessOrEqual(need, 100, "Unlock requirement cannot exceed puzzles-per-tier.");
            prev = need;
        }
    }
}
