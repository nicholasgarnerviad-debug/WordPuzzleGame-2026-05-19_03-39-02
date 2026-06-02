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

        // Task 5C — filter set; null means "use full graph".
        private HashSet<string> commonWords;

        public PuzzleGenerator(WordGraph wordGraph, Dictionary<int, TierData> tierCache = null)
        {
            this.wordGraph = wordGraph ?? throw new System.ArgumentNullException(nameof(wordGraph));
            this.tierCache = tierCache ?? new Dictionary<int, TierData>();
            this.random = new System.Random();
        }

        /// <summary>
        /// Task 5C — supply the common-words filter. When non-null and non-empty,
        /// GetRandomWordOfLength and GetNeighborsFromGraph only return words in this set,
        /// ensuring generated ladders use familiar words. Pass null to clear.
        /// </summary>
        public void SetCommonWords(HashSet<string> set)
        {
            commonWords = (set != null && set.Count > 0) ? set : null;
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

        // ── Tier puzzle access (curated — exempt from common-word filter) ────────

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
        /// Return a random puzzle from the specified tier (1-based). Tier/daily puzzles
        /// are curated and exempt from the common-word filter. Falls back to
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

        /// <summary>
        /// Task 18E — return a random puzzle from the tier the player has NOT yet completed,
        /// or null when every puzzle in the tier is done (caller routes to the library grid).
        /// Keeps "Next Puzzle" advancing through the tier instead of replaying solved ones.
        /// </summary>
        public PuzzleDefinition GetUnplayedTierPuzzle(int tierId, ICollection<int> completedIds)
        {
            if (!tierCache.TryGetValue(tierId, out var tier) || tier.puzzles == null || tier.puzzles.Length == 0)
                return null;

            var unplayed = new List<PuzzleDefinition>();
            foreach (var p in tier.puzzles)
                if (p != null && (completedIds == null || !completedIds.Contains(p.puzzleId)))
                    unplayed.Add(p);

            if (unplayed.Count == 0) return null;
            return unplayed[random.Next(unplayed.Count)];
        }

        // ── Random generation with common-word fallback chain ────────────────────

        public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
        {
            int wordLength    = GetWordLengthForDifficulty(difficulty);
            // Task 17 — never aim below the length floor.
            int targetDistance = System.Math.Max(GetTargetDistance(difficulty),
                                                  BalanceConfig.MinMovesForLength(wordLength));

            // (1) Strict common: both start/end and all intermediates must be common.
            if (commonWords != null)
            {
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLength(startWord, targetDistance, strictCommon: true);
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }

                // (2) Relaxed: start/end must be common; intermediates may be any graph word.
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLengthRelaxed(startWord, targetDistance);
                    if (MeetsFloor(path) && IsCommonWord(path[path.Count - 1]))
                        return BuildPuzzle(path);
                }
            }
            else
            {
                // No filter — normal generation.
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLength(startWord, targetDistance, strictCommon: false);
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }
            }

            // (3) Fallback.
            return CreateFallbackPuzzle();
        }

        /// <summary>
        /// Spec §2 — generate a random puzzle whose start/end words are exactly
        /// <paramref name="wordLength"/> letters long, separated by approximately
        /// <paramref name="targetDistance"/> edits in the word graph. When the caller
        /// passes -1 (the default), targetDistance is derived from the length floor as
        /// max(BalanceConfig.MinMovesForLength(wordLength), wordLength-2), and any caller value
        /// is clamped up to that floor. Acceptance additionally requires the TRUE full-graph
        /// shortest distance to meet the floor (see MeetsFloor) — never a 1-move puzzle.
        /// </summary>
        public PuzzleDefinition GenerateRandomPuzzleOfLength(int wordLength, int targetDistance = -1)
        {
            if (wordLength < 2) wordLength = 3;
            // Task 17 — target derives from the length floor (one place), never the inline max(2,len-2).
            if (targetDistance < 1)
                targetDistance = System.Math.Max(BalanceConfig.MinMovesForLength(wordLength), wordLength - 2);
            else
                targetDistance = System.Math.Max(targetDistance, BalanceConfig.MinMovesForLength(wordLength));

            // (1) Strict common.
            if (commonWords != null)
            {
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLength(startWord, targetDistance, strictCommon: true);
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }

                // (2) Relaxed retry.
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLengthRelaxed(startWord, targetDistance);
                    if (MeetsFloor(path) && IsCommonWord(path[path.Count - 1]))
                        return BuildPuzzle(path);
                }

                // Relaxed: any path of length >= 2.
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindAnyShortPath(startWord, System.Math.Max(2, targetDistance));
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }
            }
            else
            {
                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindPathOfLength(startWord, targetDistance, strictCommon: false);
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }

                for (int attempt = 0; attempt < BalanceConfig.MaxGenerationAttempts; attempt++)
                {
                    string startWord = GetRandomWordOfLength(wordLength);
                    if (string.IsNullOrEmpty(startWord)) continue;

                    var path = FindAnyShortPath(startWord, System.Math.Max(2, targetDistance));
                    if (MeetsFloor(path))
                        return BuildPuzzle(path);
                }
            }

            return CreateFallbackPuzzle();
        }

        // Helper for the relaxed retry: BFS that returns the longest reachable path
        // (>= 2 nodes) starting from start, capped at maxDepth edges.
        private List<string> FindAnyShortPath(string start, int maxDepth)
        {
            var queue = new Queue<(string word, List<string> path)>();
            var visited = new HashSet<string>();
            queue.Enqueue((start, new List<string> { start }));
            visited.Add(start);

            List<string> best = null;
            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();
                if (path.Count - 1 >= 1 && (best == null || path.Count > best.Count))
                    best = path;

                if (path.Count - 1 >= maxDepth) continue;

                foreach (string neighbor in GetNeighborsFromGraph(current, strictCommon: false))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, new List<string>(path) { neighbor }));
                    }
                }
            }
            return best ?? new List<string>();
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
            end   = end.ToLower();

            if (!wordGraph.IsValidWord(start) || !wordGraph.IsValidWord(end))
                return false;

            var queue   = new Queue<string>();
            var visited = new HashSet<string>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                foreach (string neighbor in GetNeighborsFromGraph(current, strictCommon: false))
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

        // ── private helpers ──────────────────────────────────────────────────────

        // Task 17 — a candidate path is acceptable only if the TRUE shortest-path distance
        // (over the FULL graph the player can traverse, not just the common subgraph the path
        // was walked in) meets the length floor. This prevents puzzles that look like N moves
        // but are actually solvable in fewer (e.g. 1) via another route.
        private bool MeetsFloor(List<string> path)
        {
            if (path == null || path.Count < 2) return false;
            int len = path[0].Length;
            int floor = BalanceConfig.MinMovesForLength(len);   // already >= AbsoluteMinMoves (2)
            return TrueShortestDistance(path[0], path[path.Count - 1]) >= floor;
        }

        /// <summary>True shortest-path edit distance over the full word graph (BFS). int.MaxValue if unreachable.</summary>
        private int TrueShortestDistance(string start, string end)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end)) return int.MaxValue;
            if (start == end) return 0;

            var visited = new HashSet<string> { start };
            var queue   = new Queue<(string word, int dist)>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();
                if (dist >= BalanceConfig.MaxBfsDepth) continue;
                foreach (string neighbor in GetNeighborsFromGraph(current, strictCommon: false))
                {
                    if (neighbor == end) return dist + 1;
                    if (visited.Add(neighbor))
                        queue.Enqueue((neighbor, dist + 1));
                }
            }
            return int.MaxValue;
        }

        private PuzzleDefinition BuildPuzzle(List<string> path)
        {
            return new PuzzleDefinition
            {
                puzzleId    = random.Next(10000, 99999),
                startWord   = path[0],
                endWord     = path[path.Count - 1],
                optimalSteps = path.Count - 1,
                solution    = path.ToArray(),
                seedValue   = random.Next()
            };
        }

        private int GetWordLengthForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy   => BalanceConfig.EasyWordLength,
                Difficulty.Medium => BalanceConfig.MediumWordLength,
                Difficulty.Hard   => BalanceConfig.HardWordLength,
                _                 => BalanceConfig.EasyWordLength
            };
        }

        private int GetTargetDistance(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Easy   => BalanceConfig.EasyTargetDistance,
                Difficulty.Medium => BalanceConfig.MediumTargetDistance,
                Difficulty.Hard   => BalanceConfig.HardTargetDistance,
                _                 => BalanceConfig.EasyTargetDistance
            };
        }

        private static Difficulty DifficultyForTier(int tierId)
        {
            if (tierId <= 2) return Difficulty.Easy;
            if (tierId <= 4) return Difficulty.Medium;
            return Difficulty.Hard;
        }

        /// <summary>
        /// Return a random word of the given length. When commonWords is set,
        /// only returns words that appear in both the graph and the common set.
        /// </summary>
        private string GetRandomWordOfLength(int length)
        {
            var graphWords = wordGraph.GetWordsOfLength(length);
            if (graphWords.Count == 0)
                return null;

            if (commonWords != null)
            {
                var filtered = graphWords.Where(w => commonWords.Contains(w)).ToList();
                if (filtered.Count > 0)
                    return filtered[random.Next(filtered.Count)];
                // Common set has no words of this length — fall through to full graph.
            }

            return graphWords[random.Next(graphWords.Count)];
        }

        private bool IsCommonWord(string word)
        {
            return commonWords == null || commonWords.Contains(word);
        }

        /// <summary>BFS toward targetLength edges. strictCommon controls neighbor filtering.</summary>
        private List<string> FindPathOfLength(string start, int targetLength, bool strictCommon)
        {
            var queue   = new Queue<(string word, List<string> path)>();
            var visited = new HashSet<string>();

            queue.Enqueue((start, new List<string> { start }));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (current, path) = queue.Dequeue();

                if (path.Count - 1 == targetLength)
                    return path;

                if (path.Count - 1 >= BalanceConfig.MaxBfsDepth || path.Count - 1 > targetLength)
                    continue;

                foreach (string neighbor in GetNeighborsFromGraph(current, strictCommon))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, new List<string>(path) { neighbor }));
                    }
                }
            }

            return new List<string>();
        }

        /// <summary>
        /// Relaxed BFS: uses the full graph for neighbors so intermediates need not be
        /// common, but the caller checks that the final word is common.
        /// </summary>
        private List<string> FindPathOfLengthRelaxed(string start, int targetLength)
        {
            return FindPathOfLength(start, targetLength, strictCommon: false);
        }

        /// <summary>
        /// Returns one-letter-edit neighbors of word. When strictCommon is true
        /// and commonWords is set, only neighbors present in commonWords are returned.
        /// </summary>
        private List<string> GetNeighborsFromGraph(string word, bool strictCommon)
        {
            var neighbors  = new List<string>();
            var candidates = wordGraph.GetWordsOfLength(word.Length);
            foreach (string candidate in candidates)
            {
                if (candidate == word) continue;
                if (!WordOps.HaveOneLetterDifference(word, candidate)) continue;
                if (strictCommon && commonWords != null && !commonWords.Contains(candidate)) continue;
                neighbors.Add(candidate);
            }
            return neighbors;
        }

        /// <summary>
        /// Verified fallback ladder: cat→cot→cog→dog.
        /// Each step changes exactly one letter (a→o, t→g, c→d). All words are common.
        /// optimalSteps = 3.
        /// </summary>
        private static PuzzleDefinition CreateFallbackPuzzle()
        {
            return new PuzzleDefinition
            {
                puzzleId     = 1,
                startWord    = "cat",
                endWord      = "dog",
                optimalSteps = 3,
                solution     = new[] { "cat", "cot", "cog", "dog" },
                seedValue    = 0
            };
        }
    }

    public interface IPuzzleGenerator
    {
        PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex);
        PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty);
        // Spec §2 — random puzzle of an exact word length (Classic mode).
        PuzzleDefinition GenerateRandomPuzzleOfLength(int wordLength, int targetDistance = -1);
    }

    // Used by Initialize() and TierDataLoader — defined here to avoid duplication
    // if TierDataLoader already declares it in its own namespace, suppress with partial.
    [System.Serializable]
    internal class TierDefinitionsWrapper
    {
        public TierData[] tiers;
    }
}
