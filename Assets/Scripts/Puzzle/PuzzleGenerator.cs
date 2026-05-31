using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordPuzzle.Puzzle
{
    public class PuzzleGenerator : IPuzzleGenerator
    {
        private WordGraph wordGraph;
        private Dictionary<int, TierData> tierCache;
        private System.Random random;

        private const int MaxBfsDepth = 10;
        private const int MaxGenerationAttempts = 20;

        public PuzzleGenerator(WordGraph wordGraph, Dictionary<int, TierData> tierCache = null)
        {
            this.wordGraph = wordGraph ?? throw new System.ArgumentNullException(nameof(wordGraph));
            this.tierCache = tierCache ?? new Dictionary<int, TierData>();
            this.random = new System.Random();
        }

        /// <summary>
        /// Load tier data from a JSON text asset and populate the tier cache.
        /// Expects TierDefinitionsWrapper format (same as Resources/Data/tier_definitions).
        /// </summary>
        public void Initialize(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
                return;

            try
            {
                var wrapper = JsonUtility.FromJson<TierDefinitionsWrapper>(jsonText);
                if (wrapper?.tiers == null)
                    return;

                tierCache.Clear();
                foreach (var tier in wrapper.tiers)
                    tierCache[tier.tierId] = tier;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PuzzleGenerator.Initialize failed: {ex.Message}");
            }
        }

        public PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
        {
            if (!tierCache.ContainsKey(tierId))
                return null;

            TierData tier = tierCache[tierId];
            if (tier.puzzles == null || puzzleIndex >= tier.puzzles.Length)
                return null;

            return tier.puzzles[puzzleIndex];
        }

        /// <summary>
        /// Return a random puzzle from the specified tier (1-based). Falls back to
        /// GenerateRandomPuzzle when the tier is not cached.
        /// </summary>
        public PuzzleDefinition GetRandomTierPuzzle(int tierId)
        {
            if (!tierCache.ContainsKey(tierId))
                return GenerateRandomPuzzle(DifficultyForTier(tierId));

            TierData tier = tierCache[tierId];
            if (tier.puzzles == null || tier.puzzles.Length == 0)
                return GenerateRandomPuzzle(DifficultyForTier(tierId));

            int index = random.Next(tier.puzzles.Length);
            return tier.puzzles[index];
        }

        public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
        {
            int wordLength = GetWordLengthForDifficulty(difficulty);
            int targetDistance = GetTargetDistance(difficulty);

            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                string startWord = GetRandomWordOfLength(wordLength);
                if (string.IsNullOrEmpty(startWord))
                    continue;

                var path = FindPathOfLength(startWord, targetDistance);
                if (path.Count > 1)
                    return BuildPuzzle(path);
            }

            return CreateFallbackPuzzle();
        }

        /// <summary>
        /// BFS validation: returns true when a word-ladder path exists between
        /// start and end (both must already be in the word graph).
        /// </summary>
        public bool ValidatePuzzle(string start, string end)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                return false;
            if (start == end)
                return true;

            start = start.ToLower();
            end = end.ToLower();

            if (!wordGraph.IsValidWord(start) || !wordGraph.IsValidWord(end))
                return false;

            var queue = new Queue<string>();
            var visited = new HashSet<string>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (string neighbor in GetNeighborsFromGraph(current))
                {
                    if (neighbor == end)
                        return true;
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        // ── private helpers ────────────────────────────────────────────────

        private PuzzleDefinition BuildPuzzle(List<string> path)
        {
            return new PuzzleDefinition
            {
                puzzleId = random.Next(10000, 99999),
                startWord = path[0],
                endWord = path[path.Count - 1],
                optimalSteps = path.Count - 1,
                solution = path.ToArray(),
                seedValue = random.Next()
            };
        }

        private int GetWordLengthForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => 3,
                Difficulty.Medium => 4,
                Difficulty.Hard => 5,
                _ => 3
            };
        }

        private int GetTargetDistance(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy => 2,
                Difficulty.Medium => 4,
                Difficulty.Hard => 6,
                _ => 3
            };
        }

        private static Difficulty DifficultyForTier(int tierId)
        {
            if (tierId <= 2) return Difficulty.Easy;
            if (tierId <= 4) return Difficulty.Medium;
            return Difficulty.Hard;
        }

        private string GetRandomWordOfLength(int length)
        {
            var words = wordGraph.GetWordsOfLength(length);
            if (words.Count == 0)
                return null;
            return words[random.Next(words.Count)];
        }

        private List<string> FindPathOfLength(string start, int targetLength)
        {
            var queue = new Queue<(string word, List<string> path)>();
            var visited = new HashSet<string>();

            queue.Enqueue((start, new List<string> { start }));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();

                if (path.Count - 1 == targetLength)
                    return path;

                if (path.Count - 1 >= MaxBfsDepth || path.Count - 1 > targetLength)
                    continue;

                foreach (string neighbor in GetNeighborsFromGraph(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<string>(path) { neighbor };
                        queue.Enqueue((neighbor, newPath));
                    }
                }
            }

            return new List<string>();
        }

        private List<string> GetNeighborsFromGraph(string word)
        {
            var neighbors = new List<string>();
            var candidates = wordGraph.GetWordsOfLength(word.Length);
            foreach (string candidate in candidates)
            {
                if (candidate != word && HaveOneLetterDifference(word, candidate))
                    neighbors.Add(candidate);
            }
            return neighbors;
        }

        private static bool HaveOneLetterDifference(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i] && ++diff > 1)
                    return false;
            }
            return diff == 1;
        }

        private static PuzzleDefinition CreateFallbackPuzzle()
        {
            return new PuzzleDefinition
            {
                puzzleId = 1,
                startWord = "cat",
                endWord = "dog",
                optimalSteps = 3,
                solution = new[] { "cat", "bat", "bag", "dog" },
                seedValue = 0
            };
        }
    }

    public interface IPuzzleGenerator
    {
        PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex);
        PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty);
    }

    // Used by Initialize() and TierDataLoader — defined here to avoid duplication
    // if TierDataLoader already declares it in its own namespace, suppress with partial.
    [System.Serializable]
    internal class TierDefinitionsWrapper
    {
        public TierData[] tiers;
    }
}
