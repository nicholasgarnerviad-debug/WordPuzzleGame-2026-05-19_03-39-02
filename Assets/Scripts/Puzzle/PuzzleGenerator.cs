using System.Collections.Generic;
using System.Linq;

namespace WordPuzzle.Puzzle
{
    public class PuzzleGenerator : IPuzzleGenerator
{
    private WordGraph wordGraph;
    private Dictionary<int, TierData> tierCache;
    private System.Random random;

    public PuzzleGenerator(WordGraph wordGraph, Dictionary<int, TierData> tierCache = null)
    {
        this.wordGraph = wordGraph;
        this.tierCache = tierCache ?? new Dictionary<int, TierData>();
        this.random = new System.Random();
    }

    public PuzzleDefinition GetTierPuzzle(int tierId, int puzzleIndex)
    {
        if (!tierCache.ContainsKey(tierId))
        {
            return null;
        }

        TierData tier = tierCache[tierId];

        if (puzzleIndex >= tier.puzzles.Length)
        {
            return null;
        }

        return tier.puzzles[puzzleIndex];
    }

    public PuzzleDefinition GenerateRandomPuzzle(Difficulty difficulty)
    {
        int maxAttempts = 20;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Get words of appropriate length based on difficulty
            int wordLength = GetWordLengthForDifficulty(difficulty);
            string startWord = GetRandomWordOfLength(wordLength);

            if (string.IsNullOrEmpty(startWord))
                continue;

            int targetDistance = GetTargetDistance(difficulty);

            // Find end word at approximately targetDistance away
            var path = FindPathOfLength(startWord, targetDistance);

            if (path.Count > 1)
            {
                string endWord = path[path.Count - 1];
                var puzzle = new PuzzleDefinition
                {
                    puzzleId = random.Next(10000, 99999),
                    startWord = startWord,
                    endWord = endWord,
                    optimalSteps = path.Count - 1,
                    solution = path.ToArray(),
                    seedValue = random.Next()
                };

                return puzzle;
            }
        }

        // Fallback: return a simple puzzle if generation fails
        return CreateFallbackPuzzle();
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

    private string GetRandomWordOfLength(int length)
    {
        if (wordGraph == null)
            return null;

        // Get all words of the requested length from the word graph
        var wordsOfLength = wordGraph.GetWordsOfLength(length);
        if (wordsOfLength.Count == 0)
            return null;

        // Return a random word from the filtered list
        int randomIndex = random.Next(wordsOfLength.Count);
        return wordsOfLength[randomIndex];
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

    private List<string> FindPathOfLength(string start, int targetLength)
    {
        // BFS to find path of approximately targetLength
        var queue = new Queue<(string word, List<string> path)>();
        var visited = new HashSet<string>();

        queue.Enqueue((start, new List<string> { start }));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();

            // If we reach target distance, return path
            if (path.Count - 1 == targetLength)
                return path;

            // Don't go beyond target distance
            if (path.Count - 1 > targetLength)
                continue;

            // Get neighbors from word graph
            if (wordGraph != null)
            {
                // Query actual neighbors from the word graph
                var neighbors = GetNeighborsFromGraph(current);

                foreach (string neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<string>(path);
                        newPath.Add(neighbor);
                        queue.Enqueue((neighbor, newPath));
                    }
                }
            }
        }

        // Return any valid path if we didn't reach exact target
        return new List<string>();
    }

    private List<string> GetNeighborsFromGraph(string word)
    {
        // Use reflection to get neighbors from the word graph's adjacency list
        // Since the adjacencyList is private, we use a fallback approach
        var neighbors = new List<string>();

        // Try to find neighbors by checking words with one letter difference
        var allWords = wordGraph.GetWordsOfLength(word.Length);
        foreach (string candidate in allWords)
        {
            if (candidate != word && HaveOneLetterDifference(word, candidate))
                neighbors.Add(candidate);
        }

        return neighbors;
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

    private PuzzleDefinition CreateFallbackPuzzle()
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
}
