#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that validates Assets/Resources/Data/tier_definitions.json against
/// Assets/Resources/Data/word_library.json per architect spec §4.3.
///
/// Rules enforced per puzzle:
///   1. Hamming-1 chain: each adjacent pair in solution differs by exactly ONE letter at same position.
///   2. All real words: every word in the solution must be in word_library.json (case-insensitive).
///   3. startWord != endWord.
///   4. optimalSteps == solution.Length - 1.
///   5. All words same length, matching the tier's word length.
///   6. All lowercase a-z only.
///   7. No mid-chain repeats.
///   8. No two puzzles in the same tier share (start, end) pair (either direction).
/// </summary>
public static class VerifyWordLibrary
{
    private const string TierDefinitionsPath = "Assets/Resources/Data/tier_definitions.json";
    private const string WordLibraryPath = "Assets/Resources/Data/word_library.json";

    [Serializable]
    private class PuzzleData
    {
        public int puzzleId;
        public string startWord;
        public string endWord;
        public int optimalSteps;
        public string[] solution;
        public int seedValue;
    }

    [Serializable]
    private class TierData
    {
        public int tierId;
        public bool isUnlocked;
        public long unlockedTimestamp;
        public PuzzleData[] puzzles;
    }

    [Serializable]
    private class TierDefinitionsRoot
    {
        public TierData[] tiers;
    }

    [Serializable]
    private class WordLibraryRoot
    {
        public string[] words;
    }

    [MenuItem("Tools/Verify Library/Run")]
    public static void Run()
    {
        int passCount = 0;
        int failCount = 0;
        var failures = new List<string>();

        // --- Load word library ---
        if (!File.Exists(WordLibraryPath))
        {
            Debug.LogError($"[VerifyWordLibrary] FAIL — word library missing: {WordLibraryPath}");
            return;
        }
        var libJson = File.ReadAllText(WordLibraryPath);
        var libRoot = JsonUtility.FromJson<WordLibraryRoot>(libJson);
        if (libRoot == null || libRoot.words == null || libRoot.words.Length == 0)
        {
            Debug.LogError("[VerifyWordLibrary] FAIL — word library is empty or unparseable");
            return;
        }

        var wordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in libRoot.words)
        {
            if (!string.IsNullOrEmpty(w)) wordSet.Add(w.ToLowerInvariant());
        }
        Debug.Log($"[VerifyWordLibrary] Loaded {wordSet.Count} words from {WordLibraryPath}");

        // --- Load tier definitions ---
        if (!File.Exists(TierDefinitionsPath))
        {
            Debug.LogError($"[VerifyWordLibrary] FAIL — tier definitions missing: {TierDefinitionsPath}");
            return;
        }
        var tierJson = File.ReadAllText(TierDefinitionsPath);
        var tierRoot = JsonUtility.FromJson<TierDefinitionsRoot>(tierJson);
        if (tierRoot == null || tierRoot.tiers == null || tierRoot.tiers.Length == 0)
        {
            Debug.LogError("[VerifyWordLibrary] FAIL — tier definitions empty or unparseable");
            return;
        }

        int totalPuzzles = 0;
        foreach (var tier in tierRoot.tiers)
        {
            var pairSeen = new HashSet<string>();
            if (tier.puzzles == null) continue;

            foreach (var puzzle in tier.puzzles)
            {
                totalPuzzles++;
                bool ok = true;
                string prefix = $"T{tier.tierId} P{puzzle.puzzleId}";

                if (puzzle.solution == null || puzzle.solution.Length < 2)
                {
                    failures.Add($"{prefix}: solution null or too short");
                    failCount++;
                    continue;
                }

                int expectedLen = puzzle.solution[0].Length;

                // Rule 5+6: all words same length, lowercase a-z
                foreach (var w in puzzle.solution)
                {
                    if (string.IsNullOrEmpty(w))
                    {
                        failures.Add($"{prefix}: empty word in solution"); ok = false; continue;
                    }
                    if (w.Length != expectedLen)
                    {
                        failures.Add($"{prefix}: word '{w}' wrong length (expected {expectedLen})"); ok = false;
                    }
                    if (!IsLowerAlpha(w))
                    {
                        failures.Add($"{prefix}: word '{w}' is not lowercase a-z"); ok = false;
                    }
                    // Rule 2: in word library
                    if (!wordSet.Contains(w))
                    {
                        failures.Add($"{prefix}: word '{w}' missing from word_library.json"); ok = false;
                    }
                }

                // Rule 3: startWord != endWord
                if (string.Equals(puzzle.startWord, puzzle.endWord, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{prefix}: startWord == endWord"); ok = false;
                }

                // start/end consistency with solution endpoints
                if (!string.Equals(puzzle.startWord, puzzle.solution[0], StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{prefix}: startWord '{puzzle.startWord}' != solution[0] '{puzzle.solution[0]}'"); ok = false;
                }
                if (!string.Equals(puzzle.endWord, puzzle.solution[puzzle.solution.Length - 1], StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{prefix}: endWord '{puzzle.endWord}' != solution[last]"); ok = false;
                }

                // Rule 4: optimalSteps == solution.Length - 1
                if (puzzle.optimalSteps != puzzle.solution.Length - 1)
                {
                    failures.Add($"{prefix}: optimalSteps {puzzle.optimalSteps} != solution.Length-1 ({puzzle.solution.Length - 1})"); ok = false;
                }

                // Rule 1: Hamming-1 chain
                for (int i = 0; i < puzzle.solution.Length - 1; i++)
                {
                    int diffs = HammingDistance(puzzle.solution[i], puzzle.solution[i + 1]);
                    if (diffs != 1)
                    {
                        failures.Add($"{prefix}: '{puzzle.solution[i]}' -> '{puzzle.solution[i + 1]}' diff={diffs} (must be 1)"); ok = false;
                    }
                }

                // Rule 7: no mid-chain repeats
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var w in puzzle.solution)
                {
                    if (!seen.Add(w))
                    {
                        failures.Add($"{prefix}: repeat word '{w}' in chain"); ok = false;
                        break;
                    }
                }

                // Rule 8: unique (start,end) pair per tier
                string pairKey = string.CompareOrdinal(puzzle.startWord, puzzle.endWord) < 0
                    ? $"{puzzle.startWord}|{puzzle.endWord}"
                    : $"{puzzle.endWord}|{puzzle.startWord}";
                if (!pairSeen.Add(pairKey))
                {
                    failures.Add($"{prefix}: duplicate (start,end) pair in tier {tier.tierId}"); ok = false;
                }

                if (ok) passCount++;
                else failCount++;
            }
        }

        // --- Summary ---
        var sb = new StringBuilder();
        sb.AppendLine($"[VerifyWordLibrary] === Verification Summary ===");
        sb.AppendLine($"  Word library size : {wordSet.Count}");
        sb.AppendLine($"  Tiers             : {tierRoot.tiers.Length}");
        sb.AppendLine($"  Total puzzles     : {totalPuzzles}");
        sb.AppendLine($"  PASS              : {passCount}");
        sb.AppendLine($"  FAIL              : {failCount}");

        if (failCount == 0)
        {
            Debug.Log(sb.ToString().TrimEnd());
            Debug.Log("[VerifyWordLibrary] ALL PUZZLES VALID");
        }
        else
        {
            sb.AppendLine($"  --- First {Mathf.Min(failures.Count, 30)} failures ---");
            int maxShow = Mathf.Min(failures.Count, 30);
            for (int i = 0; i < maxShow; i++) sb.AppendLine($"    {failures[i]}");
            Debug.LogError(sb.ToString().TrimEnd());
        }
    }

    private static int HammingDistance(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length) return int.MaxValue;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i])) diff++;
        }
        return diff;
    }

    private static bool IsLowerAlpha(string w)
    {
        if (string.IsNullOrEmpty(w)) return false;
        foreach (var c in w)
        {
            if (c < 'a' || c > 'z') return false;
        }
        return true;
    }
}
#endif
